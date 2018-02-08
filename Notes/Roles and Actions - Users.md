This file is for describing end user roles and actions.

## User Type Descriptions

|Role|User Type|Description|
| ---| ------- | --------- |
|System Administrator|Milliman (PRM Support)|System level access|
|Client Administrator|Internal Milliman Consultants|Create, edit, and/or view client information for one or more clients including the client tree, client information, and client users|
|Content Publisher|Consultant|Publishes content items|
|Content Access Administrator|Consultant|Create users, give users access to reports, and chooses selections for each user/group|
|Client User|Client Employees|Reads authorized content|


## Possible Actions by Role/View

### Authorized Content Page

#### Content User Role

- View Authorized Content page
- View authorized content item details

### User Profile Page

#### All Roles

- View Account Information
- Modify personal information
- Reset personal password
- Modify personal security question/answer

### Client Administration Page

#### Client Admin

- View Client Admin page
- View Client information
- Edit Client information
- Edit the domain whitelist
- Add exception to the domain whitelist
- Create user
- Assign eligibility for **Content User** (not actually choose if they see content)
- Assign user **Client Admin** role _(must be a **Client Admin** for that client)_
- Assign user **Content Access Admin** role
- Assign user **Content Publisher** role
- Remove **Content User** from client
- Remove **Client Administrator** role
- Remove **Content Access Admin** role
- Remove **Content Publisher** role

#### Client Admin with Business Authority _(in addition to Client Admin)_

- Create Root Client _(Needs Profit Center to be created by System Admin)_
- Create Child Client
- Delete Client _(Only if no further children in client and no content)_

### Content Access Administrator Page

#### Content Access Admin

- View Content Access Admin
- View Client level reports, groups, and selections
- Assign client **Content Users** to content _(Can only assign a user to a single Selection Group per root content item)_
- Create user selection groups for reducible content
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

### System Administrator Page

#### System Admin

##### Users
- Create user
- Remove user from System (delete?)
- Enable/Disable a user from accessing the system
- Send a password reset email (last resort)
- User last access date
- User creation date
- Client Summary
  - Role Summary
  - Content Summary

###### Roles
- Temporarily assign a role to user (_post v1.0.0_)
- Assign role of Client Admin w BA (in Profit Center view)
- Ability to assign roles to any user (for any client)

##### Client
- Remove Client from System
- Create profit center entry
