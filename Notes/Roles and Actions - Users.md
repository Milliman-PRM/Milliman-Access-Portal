This file is for describing end user roles and actions.

## User Type Descriptions

|Role|User Type|Description|
| ---| ------- | --------- |
|System Administrator|Milliman (PRM Support)|System level access|
|Client Administrator|Internal Milliman Consultants|Create, edit, and/or view client information for one or more clients including the client tree, client information, and client users|
|Content Publisher|Consultant|Publishes content items|
|Content Access Administrator|Consultant|Create users, give users access to reports, and chooses selections for each user/group|
|Client User|Client Employees|Reads authorized content|


## Possible Actions by Role

View | Action | System Administrator | Client Administrator | Content Access Administrator | Content Publisher | Client User |Notes
:--- | :----- | :--------------: | :--------------: | :------------: | :-----------------: | :--------------: | :-----
AUTHORIZED CONTENT|View Authorized Content|||||X|
|View authorized content item details|||||X|
|||||||
|||||||
USER PROFILE|View Account Information|X|X|X|X|X|
|Modify personal information|X|X|X|X|X|
|Reset personal password|X|X|X|X|X|
|Modify personal security question/answer|X|X|X|X|X|
|||||||
|||||||
CLIENT ADMIN|View Client Admin||X||||
|Create Root Client||X|||Needs Profit Center to be created by System Admin|
|Create Child Client||X||||
|Delete Client||X|||Only if no further children in client|
|View Client information||X||||
|Edit Client information||X||||
|Edit the domain whitelist||X||||
|Add exception to the domain whitelist||X||||
|Assign user **Client Admin** role (must be a **Client Admin** for that client)||X||||
|Assign user **Content Access Admin** role||X||||
|Assign user **Content Publisher** role||X||||
|Remove **Client User** from client||X||||
|Remove **Client Administrator** role||X||||
|Remove **Content Access Admin** role||X||||
|Remove **Content Publisher** role||X||||
|||||||
|||||||
CONTENT ACCESS ADMINISTRATOR|View Content Access Admin|||X|||
|Add existing **Client User** to client|||X|||**Content Access Admin** does not have ability to modify acceptable domain or email exceptions list.
|Add new **Client User** to client (will create a new user in the system)|||X|||**Content Access Admin** does not have ability to modify acceptable domain or email exceptions list.
|View assigned **Client User** information|||X|||
|Assign **Client Users** to content|||X|||**Content Access Admin** can only assign **Client User** role
|Create user selection groups for reducible content|||X|||
|Manage **Client Users** in selection groups for reducible content|||X|||
|Manage **Client Users** selections for reducible content|||X|||
|||||||
|||||||
CONTENT PUBLISHER|View Content Publisher||||X||
|Publish new content||||X||
|Update existing content||||X||
|Edit existing content item information||||X||
|Delete content||||X||
|||||||
|||||||
SYSTEM ADMINISTRATION|View System Administration|X|||||
|Temporarily assign a role to user|X|||||
|Remove user from System|X|||||
|Remove Client from System|X|||||
|Remove Content from System|X|||||
|Create profit center entry|X|||||
|Send a password reset email|X|||||
