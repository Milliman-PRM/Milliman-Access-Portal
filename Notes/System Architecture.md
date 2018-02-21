# MAP System Architecture

The purpose of this document is to provide a detailed overview of the primary components of the MAP production infrastructure.

This will also serve as the build documentation used while building the production environment. As such, it will evolve as the infrastructure design process continues.

## Design Objectives

This architecture is intended to conform to the following objectives, roughly prioritized in this order.

* Determination of hardware choices should be based on known resource demands, as illustrated in Zabbix's data for Indy-PRM-1.
* The system architecture must be resilient and able to withstand the failure of one or more components.
* System maintenance & updates must be possible without taking the application offline.
* Backups must be maintained at all times, and they must conform to an established [Recovery Point Objective](https://en.wikipedia.org/wiki/Recovery_point_objective) and [Recovery Time Objective](https://en.wikipedia.org/wiki/Recovery_time_objective).
* Backups should be stored in a separate Azure zone when possible, to enable quick recovery in the case of a full data center outage
* Data backups must be regularly tested to verify that they are valid and usable for running the application.
* Recovery process to a secondary Azure zone must be documented in sufficient detail that any staff with the necessary permissions can perform the process.
* Backups should have as little impact on production performance as possible.

## Data Centers

MAP will be hosted on Microsoft's Azure platform, in the North Central US region. Backups will be performed to the South Central US region when possible.

## Hosting Platform

MAP will run entirely on virtual machines, which will be hosted on physical hosts dedicated to MAP. No physical hardware will be shared with other systems, with the possible exception of storage.

## Azure Products Used

We will utilize multiple Azure products to build the production environment. Most will not be accessible to end-users.

* **Azure App Service Web App** - The front-end hosting environment that serves the MAP application to end-users

* **Azure Database for PostgreSQL** - Managed database service to be leveraged by the application

* **Azure Service Bus** - Out-of-process queue for storing auditing events waiting to be processed

* **Azure Functions** - Scheduled task execution to process log events and load into a database

* **Azure Key Vault** - Secure storage of configuration secrets, including connection strings and QlikView Server credentials

* **Availability Sets** - Management layer for VMs to keep them isolated within the data center. Makes the VMs more resilient to power, hardware, and network failures within the data center.

* **Virtual Machines** - 2 for QlikView Server, 2 for QlikView Publisher, 1 as a standalone file server

* **Load Balancer** - Distribute HTTPS requests to QlikView-related VMs. Ensures traffic is balanced when multiple VMs are available and maintains connectivity when one or more VMs is offline.

## Virtual Machines

VMs in the MAP environment are segmented by function and user access. Throughout this document, VMs will be referred to by category, not by name.

|VM Type|Primary Functions|Availability to users|
|----|----|----|
|QlikView Server|Surface QlikView reports|Available to end users over the web|
|QlikView Publisher|Reduce QlikView reports and host the Milliman Reduction Service|Not available directly to end users. Application Servers will interface with Publishers via the Publisher API|

## Load balancing within primary data center

Load balancers will handle incoming user requests to QlikView Servers and QlikView Publishers. When all nodes are available, all will be utilized simultaneously, to spread out the computational load.

Using load balancers to manage requests to the VMs also enables "online" maintenance of the QlikView servers, as long as one VM of each type stays online.

User requests will be distributed on a per-session basis, meaning that an individual user will be routed to the same QlikView Server for the duration of their session.

### Application availability

Microsoft guarantees a 99.95% availability SLA. This is sufficient for our purposes, so we will plan to maintain a single instance of the application.

### QlikView clustering

QlikView Server and QlikView Publisher both have support for clustering, with multiple available nodes simultaneously using one license.

We will leverage this functionality and the load balancers to create a redundant, resilient QlikView environment. If one server becomes unavailable, the load balancer will stop serving requests to that server and route all requests through the remaining server.

Individual users may experience a brief disruption at the time of a server failure (particularly those who were previously being routed to the now-offline server), but the application will remain available as long as any one application server is online.

QlikView Server will serve reports out of a single file share, avoiding duplication of data and any possibility of mis-matched report directories.

### Database availability

Microsoft does not yet offer an SLA for Azure Database for PostgreSQL, because it is still in "Preview" mode. It should be generally available, which will include an SLA, by the time we launch MAP for clients in the fall of 2018.

For SQL Database (the most similar product with an SLA), they currently offer a 99.99% availability guarantee.

Despite not having an SLA, the PostgreSQL product does perform backups of every database every 5 minutes, giving us a 5-minute RPO for our databases.

We will perform regular restore tests of the Azure backups, to ensure we are able to stand up a new server using the backups in case of an emergency.

In the case that we have to stand up a new PostgreSQL instance, the connection strings stored in Azure Key Store will also need to be updated.

## File storage redundancy

The file storage instance hosting content will be [geo-redundant](https://docs.microsoft.com/en-us/azure/storage/common/storage-redundancy?toc=%2fazure%2fstorage%2fblobs%2ftoc.json#geo-redundant-storage), which means Azure will maintain a second copy in a separate data center (South Central US), which we can stand up in an emergency.

## Virtual machine backups

All virtual machines will be backed up to geo-redundant storage. This enables fast recovery in the case of a data center outage. It also provides the ability to recover a single VM in the case of data corruption.

## Recovery to secondary data center

In the case that the data center becomes unavailable permanently or for a significant period, we will need to transfer our application and services to a new Azure data center.

* If available, transfer resources to the South Central US region
* If public IP addresses have changed, update public DNS records to point at IP addresses served by the new data center
* Stand up services in the new location, utilizing the configuration scripts used to stand up the original data center
  * Azure Database for PostgreSQL
  * Azure Key Vault
  * Service Bus
  * Load balancer
* Restore the QlikView Server and Publisher backups
* Restore most recent available PostgreSQL database backups
* Deploy MAP to the new location
* Verify that all applications and services are functioning normally

## Security Policies

### Filesystem Encryption

Virtual machines' file systems must be encrypted at all times.

### Configuration Encryption

Sensitive configuration options will be stored in Azure Key Vault.

### Firewall Configuration

Inbound requests from the public internet will pass through a hardware firewall. Additionally, the operating system firewall must be enabled and properly configured on each VM.

In the table below, all public rules also apply to internal requests (from Milliman's network). Internal traffic may be open for additional protocols/services.

In addition to the services outlined in the table, Microsoft Remote Desktop should be allowed to all VMs from internal (Milliman) IP addresses. Zabbix monitoring will be allowed internally for all servers as well (TCP & UDP ports 10050-10051).

|Server Type|Public (external) allowed protocols|Additional internal services|
|-----|-----|-----|
|Application servers|HTTPS & HTTP (Only for redirect to HTTPS)|---|
|QlikView Server|HTTPS|---|
|QlikView Publisher|---|HTTPS|
|Database servers|---|PostgreSQL (port 5433)|

### Antivirus Software

All servers will run antivirus software, utilizing real-time scanning.

Additionally, files uploaded by users should be scanned before the system takes any action on them or serves them up to end-users. Specific implementation details should be coordinated between the security manager and back-end developers.

## Change Management

In any high-availability environment, it is critical to have a change management plan in place. This plan outlines proper procedures for deploying, updating, or replacing components, whether hardware or software.

### Configuration Testing

From time to time, system configuration changes may become necessary. When at all feasible, these changes should be tested on non-production systems before being made in production.

### General software update workflow

To maintain a robust environment, precautions must be taken during system updates.

### Automated VM updates

Azure provides the capability to automate Windows updates on VMs. By utilizing Availability Sets, we guarantee that only one server of each type will be updated at a time.

### Manual VM updates

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
