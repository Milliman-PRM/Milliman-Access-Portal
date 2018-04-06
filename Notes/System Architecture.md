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

MAP will be hosted on Microsoft's Azure platform, in the North Central US region. Backups will be performed to the South Central US region when possible.

## Azure Products Used

We will utilize multiple Azure products to build the production environment. Most will not be accessible to end-users.

* **Azure App Service Web App** - The front-end hosting environment that serves the MAP application to end-users

* **Azure Database for PostgreSQL** - Managed database service to be leveraged by the application

* **Azure Key Vault** - Secure storage of configuration secrets, including connection strings and QlikView Server credentials

* **Availability Sets** - Management layer for VMs to keep them isolated within the data center. Makes the VMs more resilient to power, hardware, and network failures within the data center.

* **Virtual Machines** - 2 for QlikView Server, 2 for QlikView Publisher, 2 for file server clustering, 2 for domain controllers

* **Network Security Groups** - Network-level security configuration for VMs. Applies Firewall rules to VMs which use the Security Group.

* **Application Gateway** - Distribute HTTPS requests to web app or QlikView Servers, as appropriate.
    * **Web Application Firewall** - A feature of the Application Gateway. Applies additional security filtering to ensure malicious traffic doesn't reach either endpoint.

* **Azure Security Center** - Monitor our Azure infrastructure and alert security staff about possible issues.

## Virtual Machines

VMs in the MAP environment are segmented by function and user access. Throughout this document, VMs will be referred to by category, not by name.

|VM Type|Primary Functions|Availability to users|
|----|----|----|
|QlikView Server|Surface QlikView reports|Available to end users over the web|
|QlikView Publisher|Reduce QlikView reports and host the Milliman Reduction Service|Not available to end users. These will operate largely independently, retrieving tasks from the database directly.|
|File Server|Store QVWs and other content to be delivered to end users via the web app|Not available directly to end users. Content will be streamed to users via the web app|

## Load balancing

Application Gateway will perform load balancing of incoming user requests to QlikView Servers. When all nodes are available, all will be utilized simultaneously, to spread out the computational load.

Using load balancing to manage requests to the VMs also enables "online" maintenance of the QlikView servers, as long as one VM of each type stays online.

User requests will be distributed on a per-session basis, meaning that an individual user will be routed to the same QlikView Server for the duration of their session.

### Application availability

Microsoft guarantees a 99.95% availability SLA. This is sufficient for our purposes, so we will plan to maintain a single instance of the application. Note that this SLA is only for Microsoft services, not for our application itself. We have not determined an SLA for our application at this time.

### Virtual Machine Availability

Every virtual machine must be redundant with at least one more providing the same functionality.

Virtual Machines will be assigned to Availability Sets, with one Set defined for each distinct group of VMs. Within the set, each VM must be assigned to a different [Fault Domain and Update Domain](https://docs.microsoft.com/en-us/azure/virtual-machines/windows/manage-availability), to reduce risk of downtime from datacenter failures or updates to the underlying infrastructure.

### QlikView clustering

QlikView Server and QlikView Publisher both have support for clustering, with multiple available nodes simultaneously using one license.

We will leverage this functionality and the load balancers to create a redundant, resilient QlikView environment. If one server becomes unavailable, the load balancer will stop serving requests to that server and route all requests through the remaining server.

Individual users may experience a brief disruption at the time of a server failure (particularly those who were previously being routed to the now-offline server), but the application will remain available as long as any one application server is online.

QlikView Server will serve reports out of a single file share, avoiding duplication of data and any possibility of mis-matched report directories.

### Database availability

Microsoft currently offers a 99.99% connectivity SLA for Azure Database for PostgreSQL.

Azure performs backups of every database every 5 minutes, giving us a 5-minute RPO for each of our databases.

We will perform regular restore tests of the Azure backups, to ensure we are able to stand up a new server using the backups in case of an emergency.

In the case that we have to stand up a new PostgreSQL instance, the connection strings stored in Azure Key Store will also need to be updated.

## File server redundancy

The File servers in production will be a cluster of two servers, utilizing Windows Server 2016's [Storage Replica feature](https://docs.microsoft.com/en-us/windows-server/storage/storage-replica/storage-replica-overview). This is a block-level replication solution that ensures maximum performance of the replication.

## Data backups

All virtual machines and databases will be backed up to geo-redundant storage. This enables fast recovery in the case of a data center outage. It also provides the ability to recover a single VM in the case of a machine failure.

## Recovery to secondary data center

In the case that the data center becomes unavailable permanently or for a significant period, we will need to transfer our application and services to a new Azure data center.

* If available, transfer resources to the South Central US region
* If public IP addresses have changed, update public DNS records to point at IP addresses served by the new data center
* Stand up services in the new location, utilizing the configuration scripts used to stand up the original data center
  * Azure Database for PostgreSQL
  * Azure Key Vault
  * Load balancer
* Restore the QlikView Server and Publisher backups
* Restore most recent available PostgreSQL database backups
* Deploy MAP to the new location
* Verify that all applications and services are functioning normally

## Security Policies

### Web Application firewall

The Web Application Firewall will guard our infrastructure against common types of attacks and vulnerabilities, as defined by the [OWASP 3.0 Core Rule Set](https://coreruleset.org/). All end-user traffic will flow through the WAF.

### Azure Security Center

We will utilize Azure Security Center to monitor for potential issues within our Azure infrastructure. Over time, we will evaluate for possible automated actions to take in response to log entries or other security events.

### Filesystem Encryption

Virtual machines' file systems must be encrypted at all times.

### Configuration Encryption

Sensitive configuration options will be stored in Azure Key Vault.

### Network Security Groups & Windows Firewall Configuration

Inbound requests from the public internet will pass through the Application Gateway. Additionally, the operating system firewall will be enabled and properly configured on each VM.


The table defines rules to be applied both within Network Security Groups as well as the Windows Firewall.

In addition to the services outlined in the table, Microsoft Remote Desktop should be allowed to all VMs from internal (Milliman) IP addresses. Zabbix monitoring will be allowed internally for all servers as well (TCP & UDP ports 10050-10051).

|Server Type|Public (external) allowed protocols|Internal (From Milliman) connections allowed|Outbound (within Azure) connections allowed|
|-----|-----|-----|------|
|QlikView Server|HTTPS|HTTPS, RDP, Zabbix|File Servers|
|QlikView Publisher|---|RDP, Zabbix|PostgreSQL, File Servers|
|File Server|---|RDP, Zabbix|---|

### Antivirus Software

All virtual machines will run Windows Defender antimalware software, utilizing real-time scanning.

Additionally, files uploaded by users should be scanned before the system takes any action on them or serves them up to end-users. Windows Defender has a [command line interface](https://docs.microsoft.com/en-us/windows/security/threat-protection/windows-defender-antivirus/command-line-arguments-windows-defender-antivirus) and [PowerShell cmdlets](https://docs.microsoft.com/en-us/powershell/module/defender/index?view=win10-ps) that may be useful to developers.

### Virtual Network Isolation

We will utilize Azure Virtual Networks to isolate our Azure resources from each other and allow traffic to flow between networks only as needed.

The below table maps out allowable traffic flows between networks and their IP ranges. The connection relationships used below are implemented as Peering arrangements between the virtual networks.

Specific ports and protocols will be opened to groups of VMs via Network Security Groups (see above).

|VLAN|IP range|Connects to|Allow connections from|
|----|--------|-----------|----------------------|
|Domain Controllers|10.42.1.0/24|---|File Servers, QlikView Publishers, QlikView Servers, Clients|
|File Servers|10.42.2.0/24|Domain Controllers|MAP application, QlikView Servers, QlikView Publishers|
|QlikView Servers|10.42.3.0/24|File Servers, Domain Controllers|MAP application|
|QlikView Publishers|10.42.4.0/24|File Servers, Domain Controllers|---|
|MAP application|10.42.5.0/24|File Servers, Qlikview Servers| --- |
|Clients|10.42.6.0/24|Domain Controllers, Other connections authorized temporarily as needed|---|

### Client access

We will utilize the Clients virtual network to host a virtual machine to be utilized by developers and administrators via Remote Desktop on an as-needed basis for troubleshooting purposes. Users who need to perform troubleshooting within the network or application who are not Azure administrators will be allowed only to connect to this network.

By default, this network can only connect to the Active Directory Domain Controllers, for authentication. Additional peering arrangements may be configured by Azure administrators to facilitate access to other systems as needed.

Access to VMs in this network should not be granted on a permanent basis.

### File Share Isolation

We will utilize multiple file shares throughout the content publication pipeline, to ensure that components can only access the files they need to complete their tasks.

|Share|Description|Accessed By|
|-------|------|--------|
|Quarantine|Landing place for user content uploads. Virus scanning will be performed here before any other actions are taken on the file.|MAP application|
|Waiting for Reduction|Holding area for files waiting to be reduced by the Publishers|MAP, QlikView Publishers|
|Reducing (non-shared)|Local storage on QlikView Publishers. Publishers will copy files locally for reduction. Reduced files will undergo a verification process before being promoted for publishing.|Local only|
|User verification & validation|Holding area for pending content publications. End users with appropriate rights must verify the content before it is published into production.|QlikView Publishers, MAP|
|Live content|Holds content currently being served by MAP.|MAP, QlikView Servers|

### File Integrity

All uploaded files must be verified to detect possible data corruption or tampering. The primary mechanism for this will be content checksum values, which will be generated and verified at every use.

This applies both to uploaded content files and uploaded user guides.

* When a user uploads content to be published, a checksum is generated client-side and verified server-side. If the checksums don't match, the content is not published and the user is notified that an error has occurred.
   * This checksum is stored in the database and used to validate the master content file in future steps.
*  When the reduction service processes master content files, the checksum is validated before performing reduction tasks.
* When the reduction server generates reduced content files, a checksum is generated and stored for each output file.
* When users promote/approve content for publication, the checksum is validated again.
* The checksum is validated again before content is presented to the end user.

If at any point a checksum does not match the expected value, the task being performed should be canceled.

This verification increases our confidence in the quality of the content being served and reduces the risk of exploitation for multiple possible ePHI leakage vectors.

## Database Security

Ensuring the integrity of the databases is essential to the security of the application. Multiple policies will be enforced to ensure data is not inappropriately accessed or modified.

### Limited connections allowed

Connections to the PostgreSQL server should only be allowed from within Azure, and only from specific resources.

At this time, only the MAP application and QlikView Publishers need access to the databases full-time.

Connections will additionally be allowed from specific VMs within the Client access Virtual Network, to facilitate administrative actions and troubleshooting.

### Limited logins

All PostgreSQL users will be limited in the data they can access.

Each application will have its own login to each database (one for the Application DB, and another for the Audit Log DB). Additionally, the application Staging environment will have separate logins from production, and those logins will only have access to the Staging (non-production) databases.

Permissions to read and write data will be granted to group roles, rather than directly to user roles. This allows a DBA to assign access on an as-needed basis, under defined criteria.

No shared accounts/credentials will be created or distributed.

### Limited write access

Only the applications (MAP and the Reduction Service) should have write access to the database. At no time will any non-DBA user be granted write access to any database in this environment.

DBAs may temporarily grant themselves write access to the application only when necessary. They should never have write access to the Audit Log database.

## Active Directory Management

### Separation of duties

Active Directory administrators will have two logins - one for general tasks, and a second for performing administrative functions. The generic account will have read only access to a limited set of resources. Accessing other resources requires elevating to the admin account.

Each administrator will have their own set of accounts

### Permissions to groups, not Users

Permissions to resources such as file shares should be granted to security groups, rather than to individual users. This facilitates delegation of permission management in the future if needed and reduces "rot" from deleted accounts being left in permission lists on resources.

Groups will be named with this convention: `[resource type]_[resource name]_[access level]`, e.g. `share_LiveContent_ReadOnly` or `share_Quarantine_ReadWrite`.

### User naming conventions

Regular accounts: `firstname.lastname`

Admin accounts: `firstname.lastname.admin`

### Service accounts

QlikView services will be installed to run under [Group Managed Service Accounts](https://docs.microsoft.com/en-us/windows-server/security/group-managed-service-accounts/group-managed-service-accounts-overview). Active Directory manages the credentials for these accounts and keep them updated. The servers which need the accounts will be authorized to retrieve the credentials, but they will not be available to any users.

Service accounts will be named with this convention: `svc_[serverGroup]_[ServiceName]`, e.g. `svc_QlikViewServers_QlikView` or `svc_QlikViewPublishers_ReductionService`.

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

We will utilize Azure's [App Service Deployment Slots](https://docs.microsoft.com/en-us/azure/app-service/web-sites-staged-publishing) feature to perform updates in a limited staging environment before updating the production application.

To leverage this properly, we will maintain separate databases for the staging environment. This will allow us to test changes such as database migrations on the production infrastructure before making the changes live.

MAP will be [deployed via Git](https://docs.microsoft.com/en-us/azure/app-service/app-service-deploy-local-git). We will utilize a deployment script, executed by Azure on every git push, to perform any post-update configuration.

Deployments should only ever be made to the Staging deployment slot. Once changes are verified in Staging, swap the slot over to production to complete the update. The previous deployed version will now be in the Staging slot, which makes reverting the update very easy if something goes wrong after switching to Production.

The MAP development team will coordinate update scheduling with the infrastructure team. Updates should only be deployed after completing the formal release process and QRM documentation is complete.

From time to time, MAP updates may require additional planning, and in rare cases can actually require limited application down time.

Additional special scenarios should be added to this section as they are identified.

## Monitoring

### Internal monitors - Zabbix

Zabbix will monitor VMs for system performance, availability, and stability issues. Application-specific monitors will be used as appropriate, similar to the system that is already in place for monitoring Indy-PRM-1 and Indy-PRM-2.

Additionally, we will configure an automated monitor in Zabbix which will actually authenticate to the application and load a demo report. This ensures the application stack is functioning together as intended.

### Azure monitors

We will configure availability alerts to notify the infrastructure team of any Azure service-level issues. We will also use metric-based alerts as appropriate to alert the infrastructure team when other problems arise, such as reaching the capacity limits of the services we're using.

### External monitors

We will utilize an external monitoring & notification service to check that the application is available to the public and alert the PRM infrastructure team if it becomes unavailable.
