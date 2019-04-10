Milliman Access Portal (MAP) supports user authentication by external systems using Ws-Federation.  This enables systems such as Microsoft ADFS to be used for account management.  

The following is a guide for a successful configuration of ADFS for integration with MAP.  

- Relying party idententifier: 
   - https://map.milliman.com - Production
   - https://map.milliman.com:44300 - Staging
 
- Outgoing claims

	|LDAP Attribute|Outgoing Claim Type|Notes|
	|:---------------------|:-----------------------|:-------------------------------------------------------------------|
	|User-Principle-Name|Name ID|Mandatory - value to be encoded as an email address|
	|Surname|Surname|Optional|
	|Given-Name|Given Name|Optional|
	|Company|Employer|Optional|
	|Email-Addresses|Email Address|Optional|
