## Role Descriptions

|Role|Relation To|Description|Assigned By|
|----|-----------|-----------|-----------|
|Admin|Global|Users with this UserRole are SystemAdmin|SystemAdmin|
|Admin|Client|Administers client|SystemAdmin, ClientAdmin|
|Admin|ProfitCenter|Manage ProfitCenter. In combination with Admin role on a related entity, authorizes elevated administrative actions on that entity|SystemAdmin, ClientAdmin with this ProfitCenter|
|----|-----------|-----------|-----------|
|UserCreator|Global|Creates/Deletes user accounts not targeted to a client|ClientAdmin w/ ProfitCenter
|UserCreator|Client|Creates user accounts targeted to a client|ClientAdmin|
|UserAdmin|Client|Assign user accounts to client and manage content access selections for assigned user|ClientAdmin|
|----|----------|------------|-----------|
|ContentAdmin|RootContentItem|Authorizes distribution of content to client-related RootContentItems|ClientAdmin|
|ContentAdmin|Client|Authorizes create/delete of RootContentItems|ClientAdmin|
|----|----------|------------|-----------|
|ContentUser|RootContentItem ?|Authorizes eligibility to be assigned to a ContentItemGroup||

## Possible User Actions

| View | Action | SystemAdmin | Client Admin | UserCreator | UserAdmin | ContentPublisher | ContentUser | RootContentCreator |
| :--- | :----- | :----------: | :-----------------: | :----------: | :----------: | :-------------: | :----------: | :----------: |
|ACCOUNT INFORMATION|View Account Information|X|X|X|X|X|X||
||Modify personal information|X|X|X|X|X|X||
||Reset personal password|X|X|X|X|X|X||
||Modify personal security question/answer|X|X|X|X|X|X||
||||||||||
|CONTENT VIEW|View content||||||X||
||View authorized content index||||||X||
||Delete existing content item|||X||X|||
||Edit report information (navigates to a separate view)|||||X|||
||View users with access to content item (navigates to a separate view)||||X||||
||||||||||
||||||||||
|CLIENT ADMIN|View Client Admin||X|X|||||
||Create Root Client||X||||||
||Create Child Client|||X|||||
||View Client information|||X|||||
||Edit Client information|||X|||||
||Edit the domain whitelist|||X|||||
||Add exception to the domain whitelist|||X|||||
||Add existing user to client|||X|||||
||Add new user to client (Creates a new user in system)|||X|||||
||Assign user Client Admin role (must be a Client Admin for that client)|||X|||||
||Assign user User Manager role|||X|||||
||Assign user Content Publisher role|||X|||||
||Remove user from client|||X|||||
||Remove Client Administrator|||X|||||
||Remove user manager|||X|||||
||Remove content publisher|||X|||||
||||||||||
||||||||||
|CONTENT MANAGEMENT|View Content Management|||||X|||
||Publish new content|||||X|||
||Update existing content|||||X|||
||Edit existing content item information|||||X|||
||Delete content|||||X|||
||||||||||
||||||||||
|USER MANAGEMENT|View User Management||||X||||
||Add existing user to client||||X||||
||Add new user to client (will create a new user)||||X||||
||View assigned user information||||X||||
||Assign users to content||||X||||
||Create user selection groups for reduceable content||||X||||
||Manage users in selection groups for reduceable content||||X||||
||Manage user selections for reduceable content||||X||||
||||||||||
||||||||||
|SYSTEM ADMINISTRATION|View System Administration Panel|X|||||||
||Temporarily assign a role to user|X|||||||
||Remove User from System|X|||||||
||Remove Client from System|X|||||||
||Remove Content from System|X|||||||
||Create profit center entry|X|||||||
||Add profit center claim to user (Root Client Creator and Client Admin only)|X|||||||
||Send a password reset email|X|||||||
