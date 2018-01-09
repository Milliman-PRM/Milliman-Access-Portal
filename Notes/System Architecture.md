# MAP System Architecture

The purpose of this document is to provide a detailed overview of the primary components of the MAP production infrastructure. This will also serve as the build documentation used while building the production environment.

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
|File server/File share|Store QlikView reports and other content|Not available directly to end users. The application will serve content from file shares to authorized users.|
|Domain Controller|Active Directory|Provide common credentials and security policy for all MAP servers. May take advantage of existing infrastructure in the data center, pending Availability & discussion with MedInsight staff. If we supply our own domain controllers, only one will exist in each data center, rather than one per host.|
