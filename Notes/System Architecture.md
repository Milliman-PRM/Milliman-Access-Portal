# MAP System Architecture

The purpose of this document is to provide a detailed overview of the primary components of the MAP production infrastructure.

This will also serve as the build documentation used while building the production environment. As such, it will evolve as the infrastructure design process continues.

## Design Objectives

This architecture is intended to conform to the following objectives, roughly prioritized in this order.

* Determination of hardware choices should be based on known resource demands, as illustrated in Zabbix's data for Indy-PRM-1.
* The system architecture must be resilient and able to withstand the failure of one or more components.
* System maintenance & updates must be possible without taking the application offline.
* Backups must be maintained at all times, and they must conform to an established [Recovery Point Objective](https://en.wikipedia.org/wiki/Recovery_point_objective) and [Recovery Time Objective](https://en.wikipedia.org/wiki/Recovery_time_objective).
* Backups should be stored in a separate Azure zone when possible (or utilize Geo-Redundant Storage), to enable quick recovery in the case of a full data center outage
* Data backups must be regularly tested to verify that they are valid and usable for running the application.
* Recovery process to a secondary Azure zone must be documented in sufficient detail that any staff with the necessary permissions in Azure can perform the process.
* Backups should have as little impact on production performance as possible.

## Data Centers

MAP will be hosted on Microsoft's Azure platform, in the Central US region (Iowa). Backups will be performed to the East US 2 region (Virginia) when possible.

## Azure Products Used

We will utilize multiple Azure products to build the production environment. Most will not be accessible to end-users.

* **Azure Database for PostgreSQL** - Managed database service to be leveraged by the application

* **Azure Key Vault** - Secure storage of configuration secrets, including connection strings and QlikView Server credentials

* **Availability Sets** - Management layer for VMs to keep them isolated within the data center. Makes the VMs more resilient to power, hardware, and network failures within the data center.

* **Virtual Machines** - 1 for QlikView Server, 1 for QlikView Publisher, 2 for file server clustering, 2 for domain controllers
    * An additional virtual machine will be deployed into a VPN-controlled DMZ. This machine will be used as a [jump box](https://en.wikipedia.org/wiki/Jump_server) to access other servers in the infrastructure.

* **Virtual Networks** - Isolate groups of resources and control which portions of the infrastructure they can access.

* **Virtual Network Gateway** - Create a point-to-site VPN between Milliman and our Azure environment.

* **Network Security Groups** - Network-level security configuration for VMs. Applies Firewall rules to VMs which use the Security Group.

* **Application Gateway** - Distribute HTTPS requests to web app or QlikView Servers, as appropriate.
    * **Web Application Firewall** - A feature of the Application Gateway. Applies additional security filtering to ensure malicious traffic doesn't reach either endpoint.

* **Azure Security Center** - Monitor our Azure infrastructure and alert security staff about possible issues.

* **Azure Site Recovery** - Maintain copies of VMs in the case of data center loss or other large-scale disaster. Additional details are outlined in the [System Recovery document](System%20Recovery.md).

* **Azure File Sync** - Synchronize file shares from the file server VM to Azure Files, to provide redundancy and secure storage.

* **Azure Monitor** - Define and monitor metrics for production systems. Identify issues proactively and notify the infrastructure team.

## Virtual Machines

VMs in the MAP environment are segmented by function and user access. Throughout this document, VMs will be referred to by category, not by name.

|VM Type|Primary Functions|Availability to users|
|----|----|----|
|QlikView Server|Surface QlikView reports|Available to end users over the web|
|QlikView Publisher|Reduce QlikView reports and host the Milliman Content Publishing Service|Not available to end users. These will operate largely independently, retrieving tasks from the database directly.|
|Web Server|Host the application via IIS|Available to users over port 443 (HTTPS) only through the application gateway|
|File Server|Store QVWs and other content to be delivered to end users via the web app|Not available directly to end users. Content will be streamed to users via the web app|
|Domain Controllers|Authentication to internal (MAP server) resources|Not available to users.|
|Remote Administration|Secure entry point for system administrators to access private MAP resources.|VPN access is required to connect. No general users will have access.|

### Application availability

Microsoft guarantees a 99.95% availability SLA. This is sufficient for our purposes, so we will plan to maintain a single instance of the application. Note that this SLA is only for Microsoft services, not for our application itself. We have not determined an SLA for our application at this time.

### Domain Controller Availability

Because of their critical role in our infrastructure, we maintain two domain controllers, which are assigned to an Availability Set. Within the set, each VM is assigned to a different [Fault Domain and Update Domain](https://docs.microsoft.com/en-us/azure/virtual-machines/windows/manage-availability), to reduce risk of downtime from datacenter failures or updates to the underlying infrastructure.

### QlikView Server/Publisher availability

Due to high licensing costs, we will utilize only one VM for QlikView Server and one for QlikView Publisher. 

In the case that one of the servers fails, we can restore the most recent backup fairly quickly.

### Database availability

Microsoft currently offers a 99.99% connectivity SLA for Azure Database for PostgreSQL.

Azure performs backups of every database every 5 minutes, giving us a 5-minute RPO for each of our databases.

We will perform regular restore tests of the Azure backups, to ensure we are able to stand up a new server using the backups in case of an emergency.

In the case that we have to stand up a new PostgreSQL instance, the connection strings stored in Azure Key Vault will also need to be updated.

## File server redundancy

We operate a single file server VM, which uses Azure File Sync to replicate files to Azure Files storage. This enables us to quickly rebuild if needed in an emergency.

## Data backups

All virtual machines and databases will be backed up to geo-redundant storage. This enables recovery in the case of a data center outage. It also provides the ability to recover a single VM in the case of a machine failure.

## Recovery to secondary data center

In the case that the data center becomes unavailable permanently or for a significant period, we will need to transfer our application and services to a new Azure data center.

* If available, transfer resources to the East US 2 region
* If public IP addresses have changed, update public DNS records to point at IP addresses served by the new data center
* Stand up services in the new location, utilizing the configuration scripts used to stand up the original data center
  * Azure Database for PostgreSQL
  * Azure Key Vault
    * Update secret values to reflect changes in the environment, if needed
  * Application Gateway
* Restore Virtual Machine backups
* Restore most recent available PostgreSQL database backups
* Verify that all applications and services are functioning normally

## Security Policies

### Web Application firewall

The Web Application Firewall feature of the Application Gateway guards our infrastructure against common types of attacks and vulnerabilities, as defined by the [OWASP 3.0 Core Rule Set](https://coreruleset.org/). All end-user traffic to MAP and QlikView Server will flow through the WAF/AG.

Due to the unique nature of our application (particularly in uploading QlikView content) some rules may need to be disabled. At this time, the following rules have been disabled because they interfere with user-facing functionality in some way. These rules have been disabled only for the web server's gateway,  unless otherwise noted.

* #942450 SQL Hex Encoding Identified
* #942440 SQL Comment Sequence Detected

### Azure Security Center

We utilize Azure Security Center to monitor for potential issues within our Azure infrastructure. Over time, we will evaluate for possible automated actions to take in response to log entries or other security events.

### File system Encryption

VM disks are stored in [encrypted storage accounts](https://docs.microsoft.com/en-us/azure/storage/common/storage-service-encryption) managed by Microsoft. Each VM is stored in a separate account, and Microsoft does not have access to the data.

### Configuration Encryption

Sensitive configuration options will be stored in Azure Key Vault and protected by Hardware Security Modules.

### Point-to-Site VPN

We utilize a [Virtual Network Gateway](https://docs.microsoft.com/en-us/azure/vpn-gateway/vpn-gateway-about-vpngateways) to establish a VPN between individual Milliman computers and our Azure infrastructure. This gateway will ensure traffic between Milliman's network and our infrastructure is encrypted at all times, providing another layer of security for administrative tasks.

Access to the VPN is controlled by the Azure administrators and is only granted on an as-needed basis.

### Virtual Network Isolation

We utilize Azure Virtual Networks to isolate our Azure resources from each other and allow traffic to flow between networks only as needed.

The below table maps out Peering arrangements between the virtual networks.

Specific ports and protocols will be opened to groups of VMs via Network Security Groups (see below).

|Virtual Network|IP Range|Peered with|
|----|--------|-----------|
|Domain Controllers|10.254.4.0/24|File Servers, Web servers, QlikView Publishers, QlikView Servers, Clients|
|File Servers|10.254.5.0/24|Domain Controllers, Web servers,  QlikView Servers, QlikView Publishers, Remote Administration|
|QlikView Servers|10.254.10.0/24|File Servers, Domain Controllers, Web servers, Application Gateways|
|QlikView Publishers|10.254.12.0/24|File Servers, Domain Controllers|
|Web servers|10.254.11.0/24|File Servers, Qlikview Servers, Application Gateways, Shared Infrastructure|
|Remote Administration|10.254.6.0/24|All vnets|
|VPN Gateway|10.254.0.0/22|Remote Administration|
|Shared infrastructure|10.0.0.0/24|Web servers, Remote Administration|

> The Shared Infrastructure VNET listed above contains VMs and other resources shared with non-MAP infrastructure, such as the SMTP server.

### Network Security Groups & Windows Firewall Configuration

Inbound requests from the public internet will pass through the Application Gateway. Additionally, the operating system firewall will be enabled and properly configured on each VM.

All traffic is allowed to flow between peered virtual networks, as described above.

The table defines rules to be applied both within Network Security Groups as well as the Windows Firewall.

|Server Type|Connections allowed from the Internet|Connections allowed from VPN|
|-----|-----|-----|-----|-----|
|Domain Controllers|---|---|
|QlikView Server|HTTPS, through Application Gateway|HTTPS, through Application Gateway|
|QlikView Publisher|---|---|
|Web Server|HTTPS, through Application Gateway|HTTPS, through Application Gateway|
|File Server|---|---|
|Remote Administration VMs|---|RDP|

Additionally, servers that host Octopus Deploy endpoints allow traffic from Milliman on port 10933. This traffic is HTTPS-only and it will only allow deployments from our in-house Octopus Deploy server.

#### Additional Firewall rule for Azure VMs

Microsoft publishes a [guide to Azure networking](https://docs.microsoft.com/en-us/azure/virtual-network/security-overview#azure-platform-considerations) that specifies two Microsoft-owned IP addresses used for monitoring and managing Azure resources. Our Virtual Machines must allow traffic on both addresses for Azure services to function properly.

A Group Policy Object should be defined to ensure traffic is always accepted from `168.63.129.16` and `169.254.169.254`.

### Remote Administration access

We will utilize the Remote Administration virtual network to host one or more virtual machines to be utilized by developers and administrators via Remote Desktop on an as-needed basis for troubleshooting purposes. Users who need to perform troubleshooting within the network or application who are not Azure administrators will be allowed only to connect to this network.

Access to VMs in this network should not be granted on a permanent basis to users who are not Azure administrators.

### Remote Desktop access

Remote Desktop access from Milliman will only be allowed to VMs within the Remote Administration virtual network, and only via the Point-to-Site VPN.

Non-administrator users will not be allowed to remotely connect to any machines beyond that virtual network unless specifically granted access by the infrastructure team.

Administrators can use these VMs as an entry point and connect from them to VMs in other virtual networks.

These restrictions will be enforced via Group Policy and Network Security Groups.

### File Share Isolation

We will utilize multiple file shares throughout the content publication pipeline, to ensure that components can only access the files they need to complete their tasks.

|Share|Description|Accessed By|
|-------|------|--------|
|Quarantine|Landing place for user content uploads. Virus scanning will be performed here before any other actions are taken on the file.|MAP application|
|Waiting|Holding area for files waiting to be reduced by the Publishers|MAP, QlikView Publishers|
|Reducing|Publishers will copy files here for reduction. Reduced files will undergo a verification process before being promoted for publishing.|QlikView Publishers|
|Live|Holds content currently being served by MAP and content ready for user verification.|MAP, QlikView Servers|

### Malware Protection

All virtual machines run Windows Defender antimalware software, utilizing real-time scanning.

Additionally, files uploaded by users should be scanned before the system takes any action on them or serves them up to end-users. Windows Defender has a [command line interface](https://docs.microsoft.com/en-us/windows/security/threat-protection/windows-defender-antivirus/command-line-arguments-windows-defender-antivirus) and [PowerShell cmdlets](https://docs.microsoft.com/en-us/powershell/module/defender/index?view=win10-ps) that may be useful to developers.

### File Integrity

All uploaded files must be verified to detect possible data corruption or tampering. The primary mechanism for this will be content checksum values, which will be generated and verified at every use.

This applies both to uploaded content files and uploaded user guides.

* When a user uploads content to be published, a checksum is generated client-side and verified server-side. If the checksums don't match, the content is not published and the user is notified that an error has occurred.
   * This checksum is stored in the database and used to validate the master content file in future steps.
*  When the Content Publishing Service processes master content files, the checksum is validated before performing reduction tasks.
* When the reduction server generates reduced content files, a checksum is generated and stored for each output file.
* When users promote/approve content for publication, the checksum is validated again.
* The checksum is validated again before content is presented to the end user.

If at any point a checksum does not match the expected value, the task being performed should be canceled.

This verification increases our confidence in the quality of the content being served and reduces the risk of exploitation for multiple possible ePHI leakage vectors.

## Database Security

Ensuring the integrity of the databases is essential to the security of the application. Multiple policies will be enforced to ensure data is not inappropriately accessed or modified.

### Limited connections allowed

Connections to the PostgreSQL server should only be allowed from within Azure, and only from specific resources.

Enabling VM access to PostgreSQL server requires the creation of a role permitting outbound traffic over port 5432 from the Network Security Group to the destination `Sql.CentralUS`.

Our PostgreSQL server is configured only to allow connections from specific VNets.

At this time, only the MAP application and QlikView Publishers need access to the databases full-time.

Connections will additionally be allowed from specific VMs within the Client access Virtual Network, to facilitate administrative actions and troubleshooting.

### Limited logins

All PostgreSQL users will be limited in the data they can access.

Each application will have its own login to each database (one for the Application DB, and another for the Audit Log DB). Additionally, the application Staging environment will have separate logins from production, and those logins will only have access to the Staging (non-production) databases.

Permissions to read and write data will be granted to group roles, rather than directly to user roles. This allows a DBA to assign access on an as-needed basis, under defined criteria.

No shared accounts/credentials will be created or distributed.

### Limited write access

Only the applications (MAP and the Content Publishing Service) should have write access to the database. At no time will any non-DBA user be granted write access to any database in this environment.

DBAs may temporarily grant themselves write access to the application only when necessary. They should never have write access to the Audit Log database.

## Active Directory Management

### Account Sharing

No shared accounts will be issued. 

### Permissions to groups, not Users

Permissions to resources such as file shares should be granted to security groups, rather than to individual users. This facilitates delegation of permission management in the future if needed and reduces "rot" from deleted accounts being left in permission lists on resources.

Groups will be named with this convention: `[resource type]_[resource name]_[access level]`, e.g. `share_LiveContent_ReadOnly` or `share_Quarantine_ReadWrite`.

### User naming conventions

Regular accounts: `firstname.lastname`

### Service accounts

QlikView services will be installed to run under [Group Managed Service Accounts](https://docs.microsoft.com/en-us/windows-server/security/group-managed-service-accounts/group-managed-service-accounts-overview). Active Directory manages the credentials for these accounts and keep them updated. The servers which need the accounts will be authorized to retrieve the credentials, but they will not be available to any users.

Service accounts will be named with the prefix `svc` to indicate that they are not user accounts.

## Change Management

In any high-availability environment, it is critical to have a change management plan in place. This plan outlines proper procedures for deploying, updating, or replacing components or configuration, whether in the infrastructure or application configuration.

### Configuration Testing

From time to time, system configuration changes may become necessary. When at all feasible, these changes should be tested on non-production systems before being made in production.

### Change tracking and approval

Changes to Azure infrastructure should be submitted first as an issue in Github, where it should be approved by both Azure administrators and application developers before the change is implemented.

In the case of an emergency (defined as a change that's necessary to fix an immediate critical production issue), a change request can be opened after the work is completed. If Azure administrators and application developers do not approve of the change, they must put together a plan to revert the change or identify additional needed changes.

### Automated VM updates

[Automated Windows updates](https://docs.microsoft.com/en-us/azure/virtual-machines/windows/tutorial-monitoring#manage-windows-updates) should be scheduled for all VMs. VMs within an availability set should be set to update on different days of the week, with at least 2 business days between to detect any potential issues caused by the updates.

Updates must be scheduled for installation overnight, to reduce disruption to critical services.

For example, the first VM in each availability set could be scheduled to install Windows updates at 12 AM on Tuesday, and the second set could be scheduled to install them at 12 AM on Friday.

**One exception to this schedule:** Definition Updates should be installed daily on File Servers, to maximize protection from potentially malicious uploads from users.

Additional update installations may be scheduled at the discretion of a Security Manager.

### Manual VM software updates

Manual updates will typically only be applied for QlikView services.

* If at all possible, avoid installing updates during peak usage hours, as defined by the MAP support team.
* Take a virtual machine snapshot before installing updates
    * Delete the snapshot once you're sure the machine is working as intended
* Update one server (of each type) at a time, and make sure it is fully online before moving on to the next one.
    * Zabbix monitors should be useful for determining if a server is fully online. A fully operational server should not have any active alerts.
* If an update does cause a problem, work to solve it as quickly as possible. When any node is offline, we are operating in a less resilient configuration.

### MAP updates

We use Octopus Deploy to perform deployments of MAP and the Content Publishing Service.

There is a completely separate dev/test environment in a standalone Azure subscription which cannot access production. Pull Request code changes are pushed automatically to this environment.

The staging environment is configured as a separate IIS application on the production Web server(s). To leverage this properly, we will maintain separate databases for the staging environment. This will allow us to test changes such as database migrations on the production infrastructure before making the changes live.

Deployments should only ever be made to the Staging deployment slot. Once changes are verified in Staging, swap the slot over to production to complete the update. The previous deployed version will now be in the Staging slot, which makes reverting the update very easy if something goes wrong after switching to Production.

The MAP development team will coordinate update scheduling with the infrastructure team. Updates should only be deployed after completing the formal release process and QRM documentation is complete. At this time, only the infrastructure team is able to push releases to production from Octopus Deploy.

Additional special scenarios should be added to this section as they are identified.

## Monitoring

### Internal monitors - Azure Monitor

We leverage Azure Monitor, including custom metrics defined with Application Insights, to monitor VM performance and service availability. The infrastructure team is alerted by email when any alarm is raised.

### Monitoring Azure Infrastructure

We will configure availability alerts to notify the infrastructure team of any Azure service-level issues. We will also use metric-based alerts as appropriate to alert the infrastructure team when other problems arise, such as reaching the capacity limits of the services we're using.

### External monitors

We will utilize an external monitoring & notification service to check that the application is available to the public and alert the PRM infrastructure team if it becomes unavailable.
