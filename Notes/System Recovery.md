# System Recovery Plan

## Plan Objectives

This document establishes a recovery plan which will be followed in the case of failure or disruption of a MAP component or the overall system. It is highly specific to MAP and is not applicable to other systems.

This policy applies only to system problems, not recovery from user error. Users should have no expectation that PRM infrastructure staff will restore accidentally deleted content, groups, or users. User error is up to the user to correct. PRM support can provide guidance to the customer, and the infrastructure team can assist with that process within reason. The integrity of the system depends on the consistency of audit log records matching the contents of the application database and filesystem. Manually modifying either may degrade the accuracy of the logs.

## System Components

The various components of the system and their function are outlined in detail in the [System Architecture document](System%20Architecture.md). 

## Response Management

Internal operations of PRM are not impacted by a failure within MAP, as our data processing work can continue with MAP offline. However, our ability to deliver work results to clients may be disrupted in the case of a system failure.

System failures or degredation should trigger notification of the security manager. The security manager should be informed of the type of failure, as well as given an assessment of whether any information may have been exposed as a result. If exposure is suspected or confirmed to have occurred, the security manager should begin the incident management process, concurrent with restoring the system. Limiting the scope of any exposure takes priority over restoring system functionality.

In the case of an emergency that is suspected to compromise security, the Azure administrators may disable access to the application by shutting down the Application Gateway or by removing the web server from its back-end pool. Access can also be interrupted by blocking traffic from the application gateway to the web server via the server's Network Security Group in Azure.

### Staff Roles

**Recovery Manager** - responsible for coordination of recovery efforts. This role will be filled by the Infrastructure & Security team leader. The Recovery Manager is responsible for communicating with internal business stakeholders as appropriate, as well as coordinating efforts with the security manager (if that is not the same person).

**Recovery Engineer** - Completes recovery tasks as outlined in this plan. This role will be filled by members of the Infrastructure & Security team, particularly those responsible for managing our Azure resources. Recovery Engineers must ensure data security is maintained during recovery operations.

**Customer Liasons** - Notifies clients of system availability changes as appropriate and responds to inquiries about recovery efforts from clients. This role will be filled by PRM Consultants.

## Recovery Priorities

Security remains the top priority during recovery operations. Recovery from failure must not include deterioration of defined security controls. This includes, but is not limited to:

* Network Security Group rules
* Windows firewall rules
* File system permissions
* Database permissions
* Malware scanning
* Virtual machine disk encryption
* Definition of user roles in the application

Secondary to security is recovering the system as accurately as possible. Consistency of data between the database and file systems should take priority over recovery time.

## Recovery Objectives

Each system component has a defined Recovery Point (RPO) and Recovery Time Objective (RTO). The RPO reflects the point in time (how far in the past) a system ideally would be restorable to. The RTO reflects how long it should take (at most) to perform recovery operations.

Each system component is configured to enable recovery to these objectives. In the case that a failure goes unnoticed for a period of time, the RPO may not be achievable.

Actual recovery time will be measured from the time we begin recovery from a failure, not from the time the failure begins. The actual recovery point will be measured from the time the failure begins.

In the past, we have offered clients a 98% uptime SLA, excluding scheduled maintenance windows. This reflects roughly a 3.25 hour per week outage. We should strive to have any system failure resolved in less than that period of time. Therefore our RTO will be 3 hours or less for all components.

### Virtual Machines

**Data center outage: RPO XX hours, RTO 2 hours**

Our virtual machines are configued to have geo-redundant backups with Azure Site Recovery Services. This means that in the case of a datacenter outage, we will be able to recover our virtual machines in a new Azure region. [Microsoft guarantees a 2 hour RTO for site recovery](https://azure.microsoft.com/en-us/support/legal/sla/site-recovery/v1_2/).

**Single-VM failure: RPO 1 day, RTO 2 hours**

In the case that a single VM has failed, we will restore the most recent available backup. This process should not be time consuming for any VM except for the file servers. Since those are redundant, we should be able to function with just one of them online.

Our VMs are currently configured to back up daily, giving us a longer RPO for VMs than other resources.

### Database

RPO [<1 hour](https://docs.microsoft.com/en-us/azure/postgresql/concepts-business-continuity#features-that-you-can-use-to-provide-business-continuity), RTO <2 hours

Our database instance is configured with geo-redundant backup storage, which is required for recovery to another region. We're indicating a shorter RTO than Microsoft because our databases are expected to remain small.

### Networking

RPO back to most recent stable state, RTO 1 hour

We will maintain a backup of our network configuration which we can use to restore or replace any mis-configured or failing component. This backup should be updated any time the network configuration is changed.

### Azure Key Vault

Azure Key Vault is [geo-redundant by default](https://docs.microsoft.com/en-us/azure/key-vault/key-vault-overview#simplified-administration-of-application-secrets). No additional configuration is required to maintain availability.

## Additional Contingencies

### Storage of confidential information

Confidential information needed to restore functionality is maintained in the PRM LastPass for Teams subscription, which is managed by the Security Manager.

### Breaking the glass

In the case that the Infrastructure & Security team are all unavailable, it may be necessary to "break the glass" - in other words, grant privileges to other individuals in order to complete recovery operations. This should only be done if the Infrastructure & Security team truly cannot be reached.

LastPass access may be granted by Shea Parkes. He maintains administrative privileges in Last Pass in order to facilitate emergency access. This vault contains credentials that can be used to access Azure resources as well as privileged credentials to MAP's Active Directory infrastructure and servers.

It is expected that access granted in this scenario will be revoked as soon as the Infrastructure & Security team returns or a new team is assigned permanent management capabilities.