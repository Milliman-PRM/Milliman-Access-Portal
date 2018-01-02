# **1. INTRODUCTION**

This project will produce a software application referred to as Milliman Access Portal.  It is primarily an administration and enforcement engine for user authentication and authorization of user access to Milliman product content.  The system is envisioned to first meet the need of Milliman's PRM product team and existing Millframe users.  It is also intended to anticipate and serve the needs of other Milliman internal practices that can benefit from a product hosting infrastructure that they don't have to implement themselves.

### 1.1 Purpose

This web application will allow content to be delivered to clients in a secure and controlled way. Due to the nature of the data that Milliman generally works with, security is paramount, and as such, this application will implement a robust system of user access management.

#### 1.2 Background

This project is being undertaken because the current report delivery infrastructure is becoming incrementally more difficult to maintain as features are added.  This system will replace Millframe.

### 1.3 References

*List of documents and supporting reference material used in creating this document*

### 1.4 Assumptions

* Clients currently using the MillFrame web application will be transferred to this application as the feature set supports clients' needs, with the eventual goal of phasing out the existing content hosting application (Millframe).

* The MAP application will not itself serve content.  It will provide controlled access to links to content served by other systems, and will provide a user interface within which that content will appear.  In support of the PRM business a Qlikview content hosting system will be developed concurrently but this is beyond the scope of the MAP system.

* The system will be available as an authorization engine for a variety of types of content, making it a potentially useful service for controlling access to content for any number of other Milliman practices.

### 1.5 Constraints

#### 1.5.1 Schedule
#### 1.5.2 Budget
#### 1.5.3 Staff
#### 1.5.4 Infrastructure

### 1.6 Terms and Definitions
- Client - An entity in the system information model that represents a set of providers and/or users.  The main purpose of a Client is as a focal point for administration of certain user authorizations.  A hierarchy of clients, organized in a tree structure, can be employed to represent organizational relationships.  This is intended as a way to represent organizational subsets, but may be adapted if a useful alternative is discovered. A Client is associated with one Milliman Profit Center.

- Profit Center - An entity in the system information model that represents a Milliman profit center. Users with authorization to administer a Profit Center have elevated rights in the system to administer Clients associated with the Profit Center in ways that have financial implications for Milliman and its customers.

- Hierarchy - Applicable to content items that are intended to support reduced versions of the content.  Refers to a specific set of fields in the data model of a content item that represent a scheme for categorizing and filtering data records.  The hierarchy also includes the set of available values for each field in the hierarchy, and is presented visually as a tree structure by convention.

- Selection - Applicable to a content item that contains a defined Hierarchy.  A Selection represents an authorization to view data record(s) from the content item that match a specific combination of values, one for each field defined in the Hierarchy.  A user or set of users may be authorized to zero or more (any number up to all available) selections defined in the hierarchy of a content item.  

### 1.7 Acronyms
- MAP - Milliman Access Portal

### 1.8 Roles & Responsibilities
Name|Role|Responsibilities|
----|----|----------------
Shea Parkes|PRM Manager|Project Approval
Michael Reisz|Project Manager|Project Management, UX Design, Front End Development
Tom Pucket|Back End Developer|System Architecture Design, Information Model Design, Back End Development
Ben Wyatt|Back End Developer/DBA|System Architecture Design, Back End Development, Database management
Joseph Sweeny|Software Developer|Front End Development,
Kelsie Stevenson|UX Developer|UX Design, Front End Development
Steve Gredell|Dev-Ops|System Setup, CI Integration, Testing Integration

# **2. REQUIREMENTS**

- A user who wants to access a Milliman product typically requires authorization to access the content.  This system is to provide a way to configure and manage such authorizations, evaluate authorizations in response to user requests to view content, and grant or deny the requested access based on the evaluated authorizations.

### 2.1 Functional Requirements
A role indicated in parentheses is the authorization requirement for the page view or action.

### 2.1.a System Administration (Admin role - global)
- Create user without Client association (UserCreator - global)
- View all users
- Edit user attributes
- Edit all user roles
  - Client Roles
  - ProfitCenter roles
  - RootContent roles
- Delete user accounts from the system
- Create, Edit, and Delete any ProfitCenter, possibly using SQL


- UI to Create, Edit, and Delete any ProfitCenter
- Temporary role assignment
- Tabular summary
  - For selected clients
    - User list
    - Content list
  - For selected user  
    - Client List
    - Authorized content List

### 2.1.b Client Administration (Admin role - any Client or ProfitCenter)
- CRUD clients
- Create new clients
- Edit client attributes
- Create user with client association
- Select a client as the context of role administration actions
- Assign / remove user membership in client
- Assign / remove user roles for client (requires membership)

### 2.1.c Content Publication (ContentAdmin role - any Client or RootContentItem)
- Create new content record (requires ContentAdmin role on client)
- Publish content for existing content record (requires ContentAdmin role on RootContentItem)
- Edit content attributes
- Delete content (requires ContentAdmin role on client)

### 2.1.d Content Authorization (ContentAdmin role - any RootContentItem)
- Create user with client association with RootContent (and client) association (UserCreator role on client or global)
- Assign eligible content users to a content item (ContentAdmin role - RootContentItem)
- Adjust hierarchy selections for individual users (ContentAdmin role - RootContentItem)
- Adjust hierarchy selections for multiple users at one time (ContentAdmin role - RootContentItem)
- Remove a user from access to a content item (ContentAdmin role - RootContentItem)
  - **Question:** Does this mean removing the user's ContentUser role for the content item or only eliminating the user's selections?
  - **Question:** If this content item is the user's only authorized content in the related client, does this mean removing the user's ContentUser role for the client?

**TODO** Resolve the below content into the above outline

### 2.1.1 General
  - MAP must eventually implement a feature set sufficient that it will serve as a functional replacement for the existing Millframe application over time.
  - MAP must control visibility of user access parameters to the user depending on administered privileges.
  - MAP must support the existing capability to define and manage filtered access to a subset of a hosted product, known as data reduction or loop and reduce.   
  - MAP must provide a way to add new content to the system.
  - MAP must provide a way to update existing content to a new version.
  - MAP must provide a way to remove content from the system.
  - MAP must provide a way to administer specific Selections within a content item that a set of end users will be authorized to access.

### 2.1.2 Content Access
  - MAP must provide a means for a user to view a filtered view of content that he/she is authorized to access.
  - MAP must limit the user's access only to content that is authorized for the user.

### 2.1.3 User Authorization
  - MAP must provide a means for authorized users to administer privileges of certain other users, including administrative and non-administrative users.
  - Create new user
    - Option to replace a marker in the welcome email template with white-label text
    - User is associated with 1 client
  - Assign user(s) to clients?
  - Assign user(s) to (root) content
  - Initiate password reset

  - Administer security selections (where applicable)

  - Bulk create new user
    - Same options?

### 2.1.3 Client Administration
  - Initiate password reset

### 2.2 Architecture/Design Requirements

### 2.2.1 Controllers

### 2.2.1.1 UserAdminController
The following methods will be implemented to handle incoming requests.

### 2.2.1.1.1 `Index()`
This responds with the landing page for User Admin actions.  The User Admin link in the application nav bar leads here.  
### 2.2.1.1.2 `AddUser()`
This is the landing page for User Admin actions.  The User Admin link in the application nav bar leads here.  
### 2.2.1.1.3 `()`
This is ...  

### 2.3 Performance Requirements

*List of performance requirements (The system must perform...)*

### 2.4 Security Requirements

*List of security requirements (The security framework must...)*

### 2.5 Other Requirements

*List of any other uncategorized requirements*

### 2.6 Project Lifecycle/Update Requirements

*Project lifecycle and update frequency requirements*
