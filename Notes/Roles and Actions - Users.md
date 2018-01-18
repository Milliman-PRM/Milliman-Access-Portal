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
- Create Child Client
- Delete Client _(Only if no further children in client)_
- View Client information
- Edit Client information
- Edit the domain whitelist
- Add exception to the domain whitelist
- Assign user **Client Admin** role _(must be a **Client Admin** for that client)_
- Assign user **Content Access Admin** role
- Assign user **Content Publisher** role
- Remove **Client User** from client
- Remove **Client Administrator** role
- Remove **Content Access Admin** role
- Remove **Content Publisher** role

#### Client Admin Enhanced _(in addition to Client Admin)_

- Create Root Client _(Needs Profit Center to be created by System Admin)_

### Content Access Administrator Page

#### Content Access Admin

- View Content Access Admin
- Add existing **Client User** to client _(**Content Access Admin** does not have ability to modify acceptable domain or email exceptions list.)_
- Add new **Client User** to client (will create a new user in the system) _(**Content Access Admin** does not have ability to modify acceptable domain or email exceptions list.)_
- View assigned **Client User** information
- Assign **Client Users** to content _(**Content Access Admin** can only assign **Client User** role)_
- Create user selection groups for reducible content
- Manage **Client Users** in selection groups for reducible content
- Manage **Client Users** selections for reducible content

### Content Publisher Page

#### Content Publisher

- View Content Publisher page
- Publish new content
- Update existing content (republish report)
- Edit existing content item information (edit report name, description, image, etc)
- Delete content

### System Administrator Page

#### System Admin

- View System Administration page
- Temporarily assign a role to user
- Create user
- Assign role of Elevated Client Admin
- Remove user from System
- Remove Client from System
- Remove Content from System
- Create profit center entry
- Send a password reset email
