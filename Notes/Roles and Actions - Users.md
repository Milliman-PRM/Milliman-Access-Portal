This file is for describing end user roles and actions.

## User Type Descriptions

|Role|User Type|Description|
| ---| ------- | --------- |
|System Administrator|Milliman (PRM Support)|System level access|
|Client Administrator|Internal Milliman Consultants|Create, edit, and/or view client information for one or more clients including the client tree, client information, and client users|
|Content Publisher|Consultant|Publishes content items|
|Content Access Administrator|Consultant|Create users, give users access to reports, and chooses selections for each user/group|
|Content User|Client Employees|Eligible for authorization (by Content Access Administrator) to access content|
|File Drop User|Client Employees and Internal Milliman Consultants|Eligible for authorization to read, write, and delete file drop contents (SFTP)|
|File Drop Administrator|Internal Milliman Consultants|Create, edit, delete file drops / details, manage user permissions, view access history|


## Possible Actions by Role/View

### Authorized Content Page

#### Content User Role

- View authorized content item details
- Access Authorized Content and related files (e.g. user guide, release notes)

### User Profile Page

#### All Roles

- View Account Information
- Modify personal information
- Reset personal password
- Modify personal security question/answer

### Client Administration Page

#### Client Admin

- View Client Admin page
- View details of authorized Clients
- Edit authorized Client information, including domain whitelist and email address whitelist
- Manage user membership to a client (create new or add existing account, and remove membership)
- Create Child Client to an authorized top level Client
- Manage client related roles of member users
  - Content User (does not authorize access to any specific content)
  - Client Admin
  - Content Access Admin
  - Content Publisher
  - File Drop User
  - File Drop Admin

#### Client Admin with Profit Center Authority _(in addition to Client Admin)_

- Create Root Client _(Needs Profit Center to be created by System Admin)_
- Delete Client _(Only if no further children in client and no content)_

### Content Access Administrator Page

#### Content Access Admin

- View Content Access Admin page
- View Client level reports, groups, and selections
- Authorize client **Content Users** to content _(Can only assign a user to a single Selection Group per root content item)_
- Create and delete user selection groups for reducible content
- Manage **Content Users** in selection groups for reducible content (who goes in selection group)
- Manage **Content Users** selections for reducible content (what selections does group receive)

#### Content Access Admin with User Creation - _post v1.0.0_

- Add existing **Content User** to client _(**Content Access Admin** does not have ability to modify acceptable domain or email exceptions list.)_
- Add new **Content User** to client (will create a new user in the system) _(**Content Access Admin** does not have ability to modify acceptable domain or email exceptions list.)_

### Content Publisher Page

#### Content Publisher with Content Creation

- View Content Publisher page
- Publish new content
- Update existing content (republish report)
- Edit existing content item information (edit report name, description, image, etc)
- Delete content

#### Content Publisher _post v1.0.0_?

- Cannot publish new content

### File Drop Page

#### File Drop User
- View File Drop Page
- View SFTP connection settings for an authorized File Drop
- Generate new password for an authorized File Drop
- Select email notifications for an authorized File Drop

#### File Drop Admin (in addition to activities of the File Drop User)
- View and manage user permissions to File Drops for authorized Clients
- View or download activity of all users in authorized File Drops

### System Administrator Page

#### System Admin

##### Users
- Create new user accounts (unassigned to any Client)
- For each existing user account
  - View user profile information (e.g. name employer, phone, email)
  - Enable/Disable access to the system
  - Send a password reset email (last resort)
  - Resend an account activiation email (for unactivated accounts)
  - Assign / revoke system admin role
  - Summary of authorized Content Items
    - View name and type of each Content Item the user is authorized to
  - Summary of authorized Clients
    - View / manage all role assignments for each client

###### Clients
- View all Clients in the system and selected Client details (e.g. Name, code contact)
- for each Client:
  - Modify the number of allowed email domains for a specific Client
  - View details of the associated Profit Center for the Client
  - View the Users assigned to the Client
  - For each assigned user:
    - View selected user details (e.g. name, employer, email, phone)
    - Manage roles of this user for this Client
  - View the Content Items published for the Client
  - For each Content Item:
    - View selected Content Item details (e.g. name, type, description)
    - Cancel an incomplete publication request for the Content Item
    - Suspend all user access to the Content Item
    - View details of User Selection Groups for the Content Item, and a list of users authorized to each group

##### Profit Centers
- View all Profit Centers in the System
- Create a new Profit Center
- Remove a Profit Center
- Edit details of a Profit Center (e.g. Name, code office, contact info)
- View the users having Profit Center Admin role for the Profit Center
- Assign a user with Profit Center Admin role for this Profit Center
- For each Profit Center Admin:
  - View the user's name, email, and phone number
  - View the user's assigned Clients
  - Unassign the Profit Center Admin role for the user
- View the Clients associated with the Profit Center
- For each Client listed:
  - View details of the Client (e.g. name, code, contact info)
  - View a summary of all assigned users and their current roles for the Client
