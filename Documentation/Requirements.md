# **1. INTRODUCTION**

##### *Overview of the project*

### 1.1 Purpose

This web application will allow content to be delivered to clients in a secure and controlled way. Due to the nature of the data that Milliman generally works with, security is paramount, and as such, this application will implement a robust system of user access management.

#### 1.2 Background

This project is being undertaken because the current reporting infrastructure is becoming incrementally more difficult to maintain as features are added.

### 1.3 References

*List of documents and supporting reference material used in creating this document*

### 1.4 Assumptions

* Clients currently using the MillFrame web application will be transferred to this application upon release.
* This application will serve QlikView documents.

### 1.5 Constraints

*Constraints (if any) considered in the creation of this document*

### 1.6 Acronyms & Terms

*A list of Terms and Acronyms that will be used in this document*

### 1.7 Roles & Responsibilities

Name|Role|Responsibilities|
----|----|----------------
Shea Parkes|PRM Manager|Project Approval
Michael Reisz|Project Manager|Project Management, UX Design, Front End Development, Back End Development
Tom Pucket|Back End Developer|System Architecture Design, Back End Development
Ben Wyatt|Back End Developer/DBA|System Architecture Design, Back End Development, Database management
Kelsie Stevenson|UX Developer|UX Design, Front End Development
Steve Gredell|Dev-Ops|System Setup, CI Integration, Testing Integration
Naomi Bornemann|Penetration Tester|Penetration Testing

# **2. REQUIREMENTS**

#### Description of project requirements

### 2.1 User Requirements

*List of user requirements (User must be able to...)*

### 2.2 Functional Requirements

*List of functional requirements (System must do...)*
### 2.2.1 User Administration
  - Create new user
    - Option to control whether welcome email is sent
    - Option to immediately associate with a client
  - Bulk create new user
    - Same options?
  - Assign user(s) to clients?
  - Assign user(s) to (root) content
  - Initiate password reset

  - Administer security selections (where applicanble)

### 2.3 Architecture/Design Requirements

### 2.3.1 Controllers

### 2.3.1.1 UserAdminController
The following methods will be implemented to handle incoming requests.

### 2.3.1.1.1 `Index()`
This responds with the landing page for User Admin actions.  The User Admin link in the application nav bar leads here.  
### 2.3.1.1.2 `AddUser()`
This is the landing page for User Admin actions.  The User Admin link in the application nav bar leads here.  
### 2.3.1.1.3 `()`
This is ...  

### 2.3.1.1.4 `()`
This is ...  

### 2.3.1.1.5 `()`
This is ...  

### 2.3.1.1.6 `()`
This is ...  

### 2.4 Performance Requirements

*List of performance requirements (The system must perform...)*

### 2.5 Security Requirements

*List of security requirements (The security framework must...)*

### 2.6 Other Requirements

*List of any other uncategorized requirements*

### 2.7 Project Lifecycle/Update Requirements

*Project lifecycle and update frequency requirements*
