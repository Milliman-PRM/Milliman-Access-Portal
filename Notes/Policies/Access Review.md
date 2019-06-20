# Security Review Policy

No less than annually, PRM's security manager will prepare for review by a principal a listing of users with access to sensitive resources that are related to MAP, as well as listings of certain Azure resources.

The reviewing principal will respond with necessary changes or approval of the existing accesses.

Review documentation will be stored under `S:\PRM\PRM_IT\Access Reviews`

## User access to be reviewed

### MAP application

* List of users with the System Admin role in MAP production

### MAP network VPN access

* List of users who have the MAP VPN & certificate installed on their Milliman-issued computers

### MAP network resources

* List of (human) users in the Active Directory, with the following details:
    * VMs to which the user has remote desktop access
    * VMs to which the user has local administrator access
    * File shares to which the user has write access (including those inherited from groups)
    * Active Directory group memberships

### Database users

* List of users in the production database server, with the following details:
    * Databases on which the user has select access (and specific tables, if access is more narrow)
    * Databases on which the user has update, insert, or delete access (and specific tables, if access is more narrow)

## Azure resources to be reviewed

* Azure firewall rules for the following resources:
    * Production database server
    * Production key vault
    * Staging key vault
* Network security groups w/ ports open to the internet
* User roles on resources w/in the MAP Production Azure subscription
    * Subscription-level roles
    * Database servers
    * Virtual machines
    * Virtual networks
    * Virtual network gateways
    * Key vaults
    * Power BI Embedded capacities
    * Storage accounts
    * Security Center
    * Recovery Services vaults
    * Network security groups
    * Traffic manager profiles
    * Application Insights
    * Storage Sync services

## Additional resources to be reviewed

* List of users with merge/push rights in the MAP GitHub repository
* List of users with access to shared Azure or MAP credentials in LastPass
* List of user accounts in Splunk
