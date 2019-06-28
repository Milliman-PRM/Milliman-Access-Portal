# Background

The following instructions should be provided to external clients who wish to configure their authentication servers to work MAP. 

These instructions should only be shared with clients by, or with the approval of, the Infrastructure and Security team.

# MAP Requirements

- In order to configure the integration, MAP must be configured with the Federation Metadata URL of the authentication system. For ADFS systems this is visible in the service endpoints list, and is typically: `/FederationMetadata/2007-06/FederationMetadata.xml`

# Instructions for our integration partner (ADFS/WS-Federation)

MAP supports external authentication acting as a Relying Party using WS-Federation protocol. Configure your system using the following configuration. This should be configured for a WS-Federation Passive endpoint, not SAML or SAML 2.

- Relying party identifier and WS-Federation Passive Endpoint: 
   - https://map.milliman.com - Production
    or
   - https://map.milliman.com:44300 - Staging
 
- Outgoing claims

	|LDAP Attribute|Required/Options|Outgoing Claim Type|
	|:---------------------|:-------------------------------------------------------------------|:------------------------|
	|User-Principle-Name|Mandatory - to be encoded as an email address|Name (http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name) or Name ID (http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier) but not both|
	|Surname|Optional|Surname (http://schemas.xmlsoap.org/ws/2005/05/identity/claims/surname)|
	|Given-Name|Optional|Given Name (http://schemas.xmlsoap.org/ws/2005/05/identity/claims/givenname)|
	|Company|Optional|Employer ()|
	|Email-Addresses|Optional|Email Address (http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress)|
