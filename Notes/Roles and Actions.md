## Role Descriptions

|Role|User Type|Description|
| ---| ------- | --------- |
|Super User|Milliman (PRM Support)|Global access|
|Client Administrator|Internal Milliman Consultants|Manage one or more relationships with clients|
|User Manager|Consultant or Client Employees|Manage user access for client|
|Content Publisher|Consultant or Client Employees|Publishes content items|
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
| |Set Client Administrator| |x| | | |
| |Modify client-level information| |x| | | |
| |Create sub client| |x| | | |
| |Associate user to client| |x| | | |
| |Set number of users to client| |x| | | |
| |Modify (other users) user profile| |x| | | |
| |Set user manager| |x| | | |
| |Modify (other users) user profile - within group/client| | |x| | |
| |Set content publisher| | |x| | |
| |Create user| | |x| | |
| |Associate user to client - within group/client| | |x| | |
| |Remove user from client| | |x| | |
| |View User information (such as access dates, etc)| | |x| | |
| |Modify personal profile|x|x|x|x|x|
| |Reset own Password|x|x|x|x|x|
|CONTENT MANAGEMENT|Manage selections for reduced content| | |x| | |
| |Update existing reports/content| | | |x| |
|ContentItemUserGroup MANAGEMENT|Set up Content| |x| | | |
| |Remove Content slot| |x| | | |
| |Create ContentItemUserGroup| | |x| | |
| |Assign users to ContentItemUserGroup (Add users to group with a role)| | |x| | |
| |Remove users from ContentItemUserGroup| | |x| | |
| |Remove Content| | | |x| |
|DATA ACCESS|Access assigned reports| | | | |x|
