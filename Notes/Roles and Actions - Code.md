## Role Descriptions

|Role|Relation To|Description|Assigned By|
|----|-----------|-----------|-----------|
|Admin|Global|Users with this UserRole are SystemAdmin|SystemAdmin|
|Admin|Client|Administers client|SystemAdmin, ClientAdmin|
|Admin|ProfitCenter|Manage ProfitCenter. In combination with Admin role on a related entity, authorizes elevated administrative actions on that entity|SystemAdmin, ClientAdmin with this ProfitCenter|
|----|-----------|-----------|-----------|
|UserCreator|Global|Creates/Deletes user accounts not targeted to a client|ClientAdmin w/ ProfitCenter
|UserCreator|Client|Creates user accounts targeted to a client|ClientAdmin|
|----|-----------|-----------|-----------|
|UserAdmin|Client|Assign user accounts to client and manage content access selections for assigned user|ClientAdmin|
|UserAdmin|RootContentItem|Manages users' hierarchy selections for a RootContentItem|ClientAdmin|
|----|----------|------------|-----------|
|ContentAdmin|RootContentItem|Authorizes distribution of content to client-related RootContentItems|ClientAdmin|
|ContentAdmin|Client|Authorizes create/delete of RootContentItems|ClientAdmin|
|----|----------|------------|-----------|
|ContentUser|RootContentItem ?|Authorizes eligibility to be assigned to a ContentItemGroup|UserAdmin|

## Possible User Actions

|Controller|Action|Notes|Status|
|----------|------|-----|------|
|HostedContent|Index|Site landing page. Content is filtered by the user's authorized content|No page authorization|
|HostedContent|WebHostedContent|Requires ContentUser for the requested content|Redo required|
|----------|------|-----|------|
|ClientAdmin|Index|Should require Admin for any Client|TBD|
|ClientAdmin|ClientFamilyList|User must have Admin for any Client|Done|
|ClientAdmin|ClientUserLists|User must have Admin for the requested Client|Done|
|ClientAdmin|DeleteClient|Password check, Admin role for client & ProfitCenter|Done|
|ClientAdmin|AssignUserToClient|Admin for [requested Client & ProfitCenter]|Done|
|ClientAdmin|RemoveUserFromClient|Admin for [requested Client & ProfitCenter]|Done|
|ClientAdmin|SaveNewClient|Admin to ProfitCenter & parent Client|Done|
|ClientAdmin|EditClient|Admin for client|Done|
|ClientAdmin||||
|----------|------|-----|------|
|UserAdmin|Index|Requires UserAdmin for any Client or any RootContentItem|Client Admin|
