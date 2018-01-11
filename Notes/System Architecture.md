# MAP System Architecture

The purpose of this document is to provide a detailed overview of the primary components of the MAP production infrastructure.

This will also serve as the build documentation used while building the production environment. As such, it will evolve as the infrastructure design process continues.

## Design Objectives

This architecture is intended to conform to the following objectives, roughly prioritized in this order.

* The system must be resilient and able to withstand the failure of one or more servers.
* The system must be available in a secondary data center, in case of the failure of the primary data center.
* Failover within the primary data center should never result in data loss.
* Failover within the primary data center should be automatic.
* System maintenance & updates must be possible without taking the application offline.
* Backups must be maintained at all times, and they must conform to an established [Recovery Point Objective](https://en.wikipedia.org/wiki/Recovery_point_objective) and [Recovery Time Objective](https://en.wikipedia.org/wiki/Recovery_time_objective).
* Data backups must be regularly tested to verify that they are valid and usable for running the application.
* Failover & recovery processes must be documented in sufficient level of detail for any member of the MAP team to recover the system in the case of an outage.
* Failover from the primary data center to the secondary data center should involve as little manual work as possible.
* Backups should have as little impact on production performance as possible.

## Data Centers

MAP will be hosted in MedInsight's data centers. The primary data center is located in Washington state, and the secondary (emergency) data center is located in Oklahoma.

## Hosting Platform

MAP will run entirely on virtual machines, which will be hosted on physical hosts dedicated to MAP. No physical hardware will be shared with other systems, with the possible exception of storage.

Two physical hosts will be located in Washington and one will be located in Oklahoma. One complete set of virtual machines will exist on each host, unless otherwise documented. (See the table below for more information.)

F5 load balancers already present in the MedInsight data centers will distribute user requests among active servers.

MedInsight's infrastructure team will be responsible for the physical maintenance of hardware and for the management of the VM environment and load balancers. PRM's infrastructure team will be responsible for administering the operating systems and applications running within the VMs.

## Types of Virtual Machines

VMs in the MAP environment are segmented by function and user access. Throughout this document, VMs will be referred to by category, not by name.

|VM Type|Primary Functions|Availability to users|
|----|----|----|
|Application server|IIS hosting|Available to end users over the web|
|QlikView Server|Surface QlikView reports|Available to end users over the web|
|QlikView Publisher|Reduce QlikView reports and host the Milliman Reduction Service|Not available directly to end users. Application Servers will interface with Publishers via the Publisher API|
|Database server|Host PostgreSQL databases|Not available directly to end users. The application will interface with Databases via NPGSQL|
|File share(s)|Store QlikView reports and other content|Not available directly to end users. The application will serve content from file shares to authorized users.|
|Domain Controller|Active Directory|Provide common credentials and security policy for all MAP servers. May take advantage of existing infrastructure in the data center, pending Availability & discussion with MedInsight staff. If we supply our own domain controllers, only one will exist in each data center, rather than one per host.|

## Load balancing within primary data center

Load balancers will handle incoming user requests both to Application and QlikView Servers. When all nodes are available within the primary data center, all will be utilized simultaneously, to spread out the computational load. Since one of each type of VM will run on each host, this will also distribute load among the physical hosts.

User requests will be distributed on a per-session basis, meaning that an individual user will be routed to the same Application or QlikView Server for the duration of their session.

### Application availability

Using load balancers enables us to use multiple application servers at the same time. If one becomes unavailable, the load balancer will stop serving requests to that server and route all requests through the remaining server.

Individual users may experience a brief disruption at the time of a server failure (particularly those who were previously being routed to the now-offline server), but the application will remain available as long as any one application server is online.

### QlikView clustering

QlikView Server and QlikView Publisher both have support for clustering, with multiple available nodes simultaneously using one license.

We will leverage this functionality and the load balancers to create a redundant, resilient QlikView environment. The above section regarding failover methods for the application also applies to QlikView's services.

QlikView Server will serve reports out of a single file share, avoiding duplication of data and any possibility of mis-matched report directories.

### Database mirroring

We will utilize PostgreSQL's built-in support for log-shipping mirroring to maintain up-to-date copies of the database on all database servers.

In this model, there is only one "active" database server at a time. The others maintain copies of the databases on the primary node and remain in a "standby" state, ready to take over if the primary node goes offline.

The database servers in the primary data center will be in synchronous mode, which enables zero-data-loss failover between them. The secondary data center's database server will be in asynchronous mode, to avoid performance penalties caused by network latency between the two data centers.

The load balancers should be configured to route requests to a virtual IP that will then be directed to the currently active PostgreSQL server. Details for how this should be implemented will be determined at a later date, but typically the high-level approach is to have the load balancer issue small status queries against all the nodes and route requests to the one that responds with a result.

## File share mirroring

The file share hosting content out of the primary data center should be replicated to the secondary data center, for use in case of emergency.

## Operating out of secondary data center

In the case that the primary data center becomes unavailable, we will need to operate out of the secondary data center. Switching to this data center may require some manual steps:

* Update public DNS records to point at IP addresses served by secondary data centers
* Bring PostgreSQL server online (make it the primary node)

### Data center failover drills

While failover within the primary data center will be tested regularly as part of system update procedures (see below), failover to the secondary data center should also be tested on a regular schedule. Such failover drills are useful for many reasons:

* Testing the failover process provides additional assurance that replication processes are working as intended.
* Planned failovers provide an opportunity for staff to gain experience with the failover process without the stress of an unplanned failover.
* Planned failovers provide an opportunity to validate failover process documentation, to ensure that it is up to date and useful in the case of an unplanned failover.

Data center failover tests must be scheduled in advance in coordination with any staff that will be required to get the secondary data center back online or to switch back to the primary data center.

Tests should be conducted during off-peak usage hours to avoid disrupting a significant number of users.

Steps to a successful failover test:

1. Trigger the application to stop running in the primary data center
2. Bring the application online in the secondary data centers
3. Perform any manual steps required for the application to operate and become available to the public
4. Verify that the application is functioning correctly. This includes authentication, loading reports, and making changes in the database.
5. Manually fail back to the primary data center
6. Undo any manual steps that were performed to run out of the secondary data center
7. Verify that the application is running correctly out of the primary data center.
8. Verify that the changes made while running in the secondary data center are reflected while running in the primary data center.

## Security Policies

### Configuration Encryption

Sensitive configuration options will be stored in Azure Key Vault.

### Firewall Configuration

Inbound requests from the public internet must pass through a hardware firewall. Additionally, the operating system firewall must be enabled and properly configured on each VM.

In the table below, all public rules also apply to internal requests. Internal traffic may be open for additional protocols/services.

In addition to the services outlined in the table, Microsoft Remote Desktop should be allowed to all servers from internal (Milliman) IP addresses. Zabbix monitoring will be allowed internally for all servers as well (TCP & UDP ports 10050-10051).

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

### Software update workflow

To maintain a robust environment, precautions must be taken during system updates.

* If at all possible, avoid installing updates during peak usage hours, as defined by the MAP support team.
* Take a virtual machine snapshot before installing updates
    * Delete the snapshot once you're sure the machine is working as intended
* Update the servers in the secondary data center first. If an update is going to cause a short-term issue, we'd rather have it happen there.
* Update one server (of each type) at a time, and make sure it is fully online before moving on to the next one.
    * Zabbix monitors should be useful for determining if a server is fully online. A fully operational server should not have any active alerts.
* If an update does cause a problem, work to solve it as quickly as possible. When any node is offline, we are operating in a less resilient configuration.


#### MAP Deployment and Update management

MAP will be deployed with semi-automated build tools, similar to how CI deployments currently happen. This ensures consistent deployment & configuration across all application servers.

The MAP development team will coordinate update releases with the infrastructure team. Updates should only be deployed after completing the formal release process and QRM documentation is complete.

#### Operating System Patch Management

Operating system updates from Microsoft will be installed on a monthly basis. The infrastructure team will install updates 2 weeks after they are released, to make sure only stable updates are applied.

In the case of a patch for a high security risk vulnerability, the PRM security manager will develop and implement a specific response plan as appropriate.

#### PostgreSQL Patch Management

We will strive to keep the database servers up to date with the latest compatible versions of PostgreSQL. This ensures we maintain the most secure database environment possible, with all available performance and stability improvements.

Before deploying an update to PostgreSQL, it must be tested in the CI environment with a copy of the currently deployed version of MAP.

## Monitoring

### Internal monitors - Zabbix

Zabbix will monitor for system performance, availability, and stability issues. Application-specific monitors will be used as appropriate, similar to the system that is already in place for monitoring QlikView and PostgreSQL.

Additionally, we will configure an automated monitor in Zabbix which will actually authenticate to the application and load a demo report. This ensures the application stack is functioning together as intended.

### External monitors

We will utilize an external monitoring & notification service to check that the application is available to the public and alert the PRM infrastructure team if it becomes unavailable.
