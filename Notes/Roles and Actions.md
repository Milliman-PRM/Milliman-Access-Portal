## Role Descriptions

|Role|User Type|Description|
| ---| ------- | --------- |
|Super User|Milliman (PRM Support)|Global access|
|Client Administrator|Internal Milliman Consultants|Manage one or more relationships with clients|
|User Manager|Client Employees|Manage user access for client|
|Content Publisher|?|Publishes content items|
|Content User|Client Employees|Reads authorized content|


## Possible Actions by Role

|      | Action | Super User | Client Admin | User Manager | Client Publisher | Content User |
| :--- | :----- | :--------: | :----------: | :----------: | :--------------: | :----------: |
|USER MANAGEMENT|Set Super User|x| | | | |
| |Remove Super User|x| | | | |
| |Remove Client Administrator|x| | | | |
| |Remove user manager|x| | | | |
| |Remove content publisher|x| | | | |
| |Remove User from System|x| | | | |
| |Create client|x| | | | |
| |Set Client Administrator|x|x| | | |
| |Modify client-level information|x|x| | | |
| |Create sub client|x|x| | | |
| |Set number of users to client| |x| | | |
| |Modify (other users) user profile| |x|within group/client| | |
| |Set user manager|x|x|x| | |
| |Set content publisher|x|x|x| | |
| |Create user|x|x|x| | |
| |Associate user to client|x|x|within group/client| | |
| |Remove user from client|x|x|x| | |
| |Modify personal profile|x|x|x|x|x|
| |Reset own Password|x|x|x|x|x|
|CONTENT MANAGEMENT|Manage selections for reduced content| |x|x| | |
| |Update existing reports/content| | | |x| |
| |Publish new reports/content| |x| |x| |
|ContentItemUserGroup MANAGEMENT|Create ContentItemUserGroup| |x|x| | |
| |Assign users to ContentItemUserGroup (Add users to group with a role)| |x|x| | |
| |Remove users from ContentItemUserGroup| |x|x| | |
|DATA ACCESS|Access assigned reports| | | | |x|
