# Background

The following instructions should be provided to external clients who wish to configure their authentication servers to work MAP. 

These instructions should only be shared with clients by, or with the approval of, the Infrastructure and Security team.

# MAP Requirements

- In order to configure the integration, MAP must be configured with the Federation Metadata URL of the authentication system. For ADFS systems this is visible in the service endpoints list, and is typically configured as: 

`/FederationMetadata/2007-06/FederationMetadata.xml`

An absolute, publicly accessible URL is needed, including the scheme and host.  

# Instructions for our integration partner (ADFS/WS-Federation)

MAP supports external authentication acting as a Relying Party using WS-Federation protocol. Configure your system using the following configuration. This should be configured for a WS-Federation Passive endpoint, not SAML or SAML 2.

## Relying party identifier and WS-Federation Passive Endpoint: 
   - https://map.milliman.com - Production
    or
   - https://map.milliman.com:44300 - Staging
 
## Outgoing claims

|LDAP Attribute|Required/Optional|Outgoing Claim Type|
|:---------------------|:-------------------------------------------------------------------|:------------------------|
|User-Principle-Name|Mandatory - to be encoded as an email address|Name (http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name) or Name ID (http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier) but not both|
|Surname|Optional|Surname (http://schemas.xmlsoap.org/ws/2005/05/identity/claims/surname)|
|Given-Name|Optional|Given Name (http://schemas.xmlsoap.org/ws/2005/05/identity/claims/givenname)|
|Company|Optional|Employer |
|Email-Addresses|Optional|Email Address (http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress)|
 
## Launching MAP

A user session of MAP can be initiated with a non-interactive login for a user participating in federated authentication.  The URL to initiate the authentication process is:
```
https://map.milliman.com/Account/RemoteAuthenticate?username=<...>
```
where `<...>` is the username to be signed in.  Note that this username will be used in the following ways:
  - MAP will determine whether remote authentication is supported for the requested user, and which Ws-Federation integration to use. 
  - Added to the initial request to the authenticating server as a query parameter named `username`.  ADFS supports this to prepopulate a login form if that form is configured and no existing authenticated session is identified. Since authentication is a transaction between the server and the user agent, the user identity for authentication is determined by a previously existing session on the client computer, or by an interactive login if no session is identified. 
