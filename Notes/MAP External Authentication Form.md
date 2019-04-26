# Background

The following instructions should be provided to external clients who wish to configure their authentication servers to work MAP. 

These instructions should only be shared with clients by, or with the approval of, the Infrastructure and Security team.

# Instructions for client (ADFS/WS-Federation)

Milliman Access Portal (MAP) supports authentication with ADFS/WS-Federation as a Relying Party. Configure your relying parties using the following configuration. Please note that this should be configured for WS-Federation Passive, not SAML or SAML 2.

- Relying party identifier: 
   - https://map.milliman.com - Production
   - https://map.milliman.com:44300 - Staging
 
- Outgoing claims

	|LDAP Attribute|Outgoing Claim Type|Notes|
	|:---------------------|:-----------------------|:-------------------------------------------------------------------|
	|User-Principle-Name|Name|Mandatory - value to be encoded as an email address|
	|Surname|Surname|Optional|
	|Given-Name|Given Name|Optional|
	|Company|Employer|Optional|
	|Email-Addresses|Email Address|Optional|
