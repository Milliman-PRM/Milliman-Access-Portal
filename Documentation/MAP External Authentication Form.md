Milliman Access Portal (MAP) supports user authentication by external systems using Ws-Federation Passive Protocol.  This enables systems such as Microsoft ADFS to be used for account management.  

The following is a guide for a successful integration between MAP and the external system.  

- Relying party idententifier: 
   - https://map.milliman.com - Production
   - https://map.milliman.com:44300 - Staging
 
- Outgoing claims
	|LDAP Attribute|Outgoing Claim Type|
	|:---------------------|:--------------------------|
	|User-Principle-Name|Name ID|
	
