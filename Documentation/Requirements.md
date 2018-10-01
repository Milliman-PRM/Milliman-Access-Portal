# 1. INTRODUCTION

This project will produce a software application referred to as Milliman Access Portal.  It is primarily an administration and enforcement engine for user authentication and authorization of user access to Milliman product content.  The system is envisioned to first meet the need of Milliman's PRM product team and existing Millframe users.  It is also intended to anticipate and serve the needs of other Milliman internal practices that can benefit from a product hosting infrastructure that they don't have to implement themselves.

### 1.1 Purpose

This web application will allow content to be delivered to clients in a secure and controlled way. Due to the nature of the data that Milliman generally works with, security is paramount, and as such, this application will implement a robust system of user access management.

### 1.2 Background

This project is being undertaken because the current report delivery infrastructure is becoming incrementally more difficult to maintain as features are added.  This system will replace Millframe.

### 1.3 References

*List of documents and supporting reference material used in creating this document*

### 1.4 Assumptions

* Clients currently using the MillFrame web application will be transferred to MAP as the evolving feature set supports clients' needs, with the eventual goal of phasing out Millframe completely.

* The MAP application will not itself serve content.  It will provide controlled access to links to content served by other systems, and will provide a user interface within which that content will appear.  In support of the PRM business a Qlikview content hosting system will be developed concurrently but this is beyond the scope of the MAP system.

* The system will be available as an authorization engine for a variety of types of content, making it a potentially useful service for controlling access to content for any number of other Milliman practices.

### 1.5 Constraints

#### 1.5.1 Schedule

Two staff incentives milestone have been defined as follows:

- Initial hosting of content by June 30, 2018.  
- Sufficient functional replacement of Millframe such that Millframe no longer serves content to end users by October 31, 2018.

#### 1.5.2 Budget  

#### 1.5.3 Staff  

- Michael Reisz
- Tom Puckett
- Ben Wyatt
- Joseph Sweeney
- Kelsie Stevenson

#### 1.5.4 Infrastructure  

### 1.6 Terms and Definitions

- Client - An entity in the system information model that represents a set of providers and/or users.  The main purpose of a Client is as a focal point for administration of certain user authorizations.  A hierarchy of clients, organized in a tree structure, can be employed to represent organizational relationships.  This is intended as a way to represent organizational subsets, but may be adapted if a useful alternative is discovered. A Client is associated with one Milliman Profit Center.

- Profit Center - An entity in the system information model that represents a Milliman profit center. Users with authorization to administer a Profit Center have elevated rights in the system to administer Clients associated with the Profit Center in ways that have financial implications for Milliman and its customers.

- Content Definition - The act of creating a content item record in the database, for which content file(s) can be subsequently published.  The application implementation may provide the user perception that these acts occur together.

- Content Publication - The act of populating file(s) associated with a content item record into the system.  This is distinguished from content definition.

- Hierarchy - Applicable to content items that are intended to support reduced versions of the content.  Refers to a specific set of fields in the data model of a content item that represent a scheme for categorizing and filtering data records.  The hierarchy also includes the set of available values for each field in the hierarchy, and is presented visually as a tree structure by convention.

- Selection - Applicable to a content item that contains a defined Hierarchy.  A Selection represents an authorization to view data record(s) from the content item that match a specific combination of values, one for each field defined in the Hierarchy.  A user or set of users may be authorized to zero or more (any number up to all available) selections defined in the hierarchy of a content item.  

- MAP must send an introductory email upon creation of each new user account.
  - The introductory email must support the ability to include client specific text to support white labeling or other customized messaging.
  - User is associated with at least one client

### 1.7 Acronyms

- MAP - Milliman Access Portal

### 1.8 Roles & Responsibilities

|Name|Role|Responsibilities|  
|:----|:----|:----------------|  
|Shea Parkes|PRM Manager | Project Approval|  
|Michael Reisz|Project Manager|Project Management, UX Design, Front End Development|  
|Tom Puckett|Back End Developer|System Architecture Design, Information Model Design, Back End Development|
|Ben Wyatt|Back End Developer/DBA|System Architecture Design, Back End Development, Database management|
|Joseph Sweeny|Software Developer|Front End Development|
|Kelsie Stevenson|UX Developer|UX Design, Front End Development|
|Steve Gredell|Dev-Ops|System Setup, CI Integration, Testing Integration|

# 2. REQUIREMENTS

A user who accesses a Milliman product typically requires authorization to access the content.  This system provides a way to configure and manage such authorizations, evaluate authorizations in response to user requests to view content, and grant or deny the requested access based on the evaluated authorizations.

### 2.1 Functional Requirements

A role is the authorization requirement for the page view or action.

#### 2.1.1 General

- MAP must be implemented as a web application.
- MAP must control visibility to the user of user authorizations managed by the system depending on administered privileges.
- MAP must implement a set of web pages, each of which provides a set of related features.  These related feature sets are further described in later sections of this document and include:
  - User authentication
  - Content access
  - User profile management
  - System administration
  - Client administration
  - User authorization to content
  - Content publication
- MAP must provide a way to enable the addition of new content to the system.
- MAP must provide a way to update existing content to a new version.
- MAP must provide a way to remove content from the system.
- MAP must provide a way to administer specific Selections within a content item that a set of end users will be authorized to access.
- MAP must support the capability to define and manage filtered access to a subset of a hosted product, known as data reduction or loop and reduce.   
- MAP must allow for addition or removal of users.

#### 2.1.2 User authentication

- MAP must require authentication of a user before establishing a session in which any authorization controlled features are made available.
- MAP must provide a means for a non-authenticated user to initiate a password reset by providing the account's email address in order to regain system access without intervention by a support person in the event of a forgotten or otherwise unusable password.

#### 2.1.3 Content Access

- MAP must limit the user's visibility of and access to only content items and selections that are authorized for access by the user.
- The application landing page must display a view of the content items that the authenticated user is authorized to access.
  - Each content item represented in the user's view of authorized content must include a clearly identifiable clickable link that leads the user directly to view the content item.
  - The link must function for content served from any host that may or may not be correlated with the MAP infrastructure, provided that the content server is properly accessible to the user.
- Upon the user clicking an authorized content item link, MAP must display a view that contains the selected content.
- For initial release, MAP must support content including Qlikview documents.
-The MAP user interface must provide a navigation bar containing links to the areas of the application that the user is authorized to access.

#### 2.1.4 System Administration (Admin role - global)

- Create user without any Client association
- View all users' profile information
- Edit a user's profile attributes
- Assign and unassign all user roles
  - Client roles
  - ProfitCenter roles
  - RootContentItem roles
- Delete user accounts from the system
- Create, Edit, and Delete any ProfitCenter (possibly using SQL)

###### Beyond V1

- UI to Create, Edit, and Delete any ProfitCenter
- Temporary role delegation
- Tabular summary
  - For selected clients
    - User list
    - Content list
  - For selected user  
    - Client List
    - Authorized content List

#### 2.1.5 Client Administration (Admin role - any Client or ProfitCenter)

- CRUD operations for client entities
  - Create new clients (Admin role for the specified profit center)
  - Edit client attributes (Admin role for the client)
  - View client attributes (Admin role for any related client)
  - Delete a Client (Admin role for the client)
- It is acceptable initially that deleting a client entity can only be done if there is no cascading effect (e.g. a user, content item, or child client still associated with the client)
- Create a new user with client association (UserCreator role for the client)
- Select a client for role administration actions (Admin role for the client)
- Assign / remove user membership in a client (Admin role for the client)
- Assign / remove user roles for a client (Admin role for the client)
  - Admin
  - ContentAuthorizationAdmin
  - ContentUser
  - ContentPublisher
- Assign / remove user roles for a RootContentItem (Admin role for the client)
  - ContentPublisher

#### 2.1.6 Content Publication (ContentPublisher role - Client or RootContentItem)

- Create new content record (ContentPublisher role - selected Client)
- Edit content attributes (ContentPublisher role - selected Client)
- Publish content for existing content record (ContentPublisher role on Client or RootContentItem)
- Publish content for non-existing content record (ContentPublisher role on Client)
- Delete content (ContentPublisher or Admin role on Client)

###### Beyond V1

- A possible expansion is to separate the role based authorizations of creating a content item record and publishing the associated content item file(s) - intended to authorize a higher privileged user to enable content publication by a lower privileged user who cannot create content records.

#### 2.1.7 Content Access Authorization (ContentAccessAdmin role - any RootContentItem)

- Create user with association with client and one content item (UserCreator role on client or global)
- Remove a user from access to a content item (ContentAdmin role - RootContentItem)
  - **Question:** Does this mean removing the user's ContentUser role for the content item or only eliminating the user's selections?
  - **Question:** If this content item is the user's only authorized content in the related client, does this mean removing the user's ContentUser role for the client?

###### Beyond V1

- Assign / remove hierarchy selections (ContentAccessAdmin role - client)
  - Individual users
  - Multiple users at one time (e.g. through ContentItemUserGroup configuration)

### 2.2 Performance Requirements

MAP must perform without noticeable performance degradation under normal workflow load of 30 concurrent users.  

**To Do**: What is the right number?

### 2.3 Security Requirements

*List of security requirements (The security framework must...)*

### 2.4 Other Requirements

*List of any other uncategorized requirements*

### 2.5 Project Lifecycle/Update Requirements

*Project lifecycle and update frequency requirements*
