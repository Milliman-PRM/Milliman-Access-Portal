# **1. INTRODUCTION**

This project will produce a software application referred to as Milliman Access Portal.  It is primarily an administration and enforcement engine for user authentication and authorization of user access to Milliman product content.  The system is envisioned to serve the immediate need of Milliman's PRM product team.  It is also intended to anticipate and serve the needs of other internal practices that can benefit from a product hosting infrastructure that they don't have to implement themselves. 

### 1.1 Purpose

This web application will allow content to be delivered to clients in a secure and controlled way. Due to the nature of the data that Milliman generally works with, security is paramount, and as such, this application will implement a robust system of user access management. 

#### 1.2 Background

This project is being undertaken because the current reporting infrastructure is becoming incrementally more difficult to maintain as features are added.

### 1.3 References

*List of documents and supporting reference material used in creating this document*

### 1.4 Assumptions

* Clients currently using the MillFrame web application will be transferred to this application as the feature set supports clients' needs, with the eventual goal of phasing out the existing content hosting application (Millframe).

* The MAP application will not itself serve content.  It will provide controlled access to links to content served by other systems, and will provide a user interface within which that content will appear. 

* The system will be available as an authorization engine for a variety of types of content, making it a potentially useful service for controlling access to content for any number of other Milliman practices. 

### 1.5 Constraints

*Constraints (if any) considered in the creation of this document*

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
Michael Reisz|Project Manager|Project Management, UX Design, Front End Development, Back End Development
Tom Pucket|Back End Developer|System Architecture Design, Information Model Design, Back End Development
Ben Wyatt|Back End Developer/DBA|System Architecture Design, Back End Development, Database management
Kelsie Stevenson|UX Developer|UX Design, Front End Development
Steve Gredell|Dev-Ops|System Setup, CI Integration, Testing Integration
Naomi Bornemann|Penetration Tester|Penetration Testing

# **2. REQUIREMENTS**

#### Description of project requirements

### 2.1 General Description
  - A user who wants to access a Milliman product typically requires authorization to access the content.  This system is to provide a way to configure and manage such authorizations, evaluate authorizations in response to user requests to view content, and grant or deny the requested access based on the evaluated authorizations. 
  
### 2.2 User Requirements

*List of user requirements (User must be able to...)*

### 2.3 Functional Requirements

### 2.3.1 General
  - MAP must implement a feature set sufficient that it will serve as a functional replacement for the existing Millframe application.  
  - MAP must provide a means for authorized users to administer privileges of certain other users, including content end users as well as administrative users. 
  - MAP must provide a means for users to view a filtered view of content to which the user is authorized to access. 
  - MAP must limit the user's access to content that is authorized in the system. 
  - MAP must control visibility of user access parameters to the user depending on administered privileges. 
  - MAP must support the existing capability to define and manage filtered access to a subset of a hosted product, known as data reduction or loop and reduce.   
  - MAP must provide a way to add new content to the system. 
  - MAP must provide a way to update content to a new version. 
  - MAP must provide a way to remove content from the system. 
  - MAP must provide a way to administer specific Selections within a content item that a set of end users will be authorized to access. 

### 2.3.2 User Administration
  - Create new user
    - Option to control whether welcome email is sent
    - Option to immediately associate with a client
  - Bulk create new user
    - Same options?
  - Assign user(s) to clients?
  - Assign user(s) to (root) content
  - Initiate password reset

  - Administer security selections (where applicanble)

### 2.3.3 Client Administration
  - Initiate password reset

### 2.4 Architecture/Design Requirements

### 2.4.1 Controllers

### 2.4.1.1 UserAdminController
The following methods will be implemented to handle incoming requests.

### 2.4.1.1.1 `Index()`
This responds with the landing page for User Admin actions.  The User Admin link in the application nav bar leads here.  
### 2.4.1.1.2 `AddUser()`
This is the landing page for User Admin actions.  The User Admin link in the application nav bar leads here.  
### 2.4.1.1.3 `()`
This is ...  

### 2.5 Performance Requirements

*List of performance requirements (The system must perform...)*

### 2.6 Security Requirements

*List of security requirements (The security framework must...)*

### 2.7 Other Requirements

*List of any other uncategorized requirements*

### 2.8 Project Lifecycle/Update Requirements

*Project lifecycle and update frequency requirements*
