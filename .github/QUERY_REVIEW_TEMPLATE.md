# Manual Query Review

## Issue to be solved

* Describe the issue that needs to be solved
* Identify how the application currently lacks a way to solve the issue

## Data to be Modified

_The manual query must modify as little data as possible. To that end, no query without a WHERE clause may be run._

Paste a SELECT statement which identifies the exact rows that will be updated/deleted. This must include the same `WHERE` clause as the data modification query.

```sql
    SELECT 1;
```

Paste the output of the query:

```
1
```

## Data Modification Statement

Paste the UPDATE or DELETE statement which will be executed:

```sql
    DELETE FROM "TableName" WHERE 1 = 0;
```

## Peer Review Process

Sign-off can be performed only by individuals with Approved Professional status or higher. Other staff may assist in verifying that the data changes are necessary and appropriate.

_Before the modification statement is executed, the peer reviewer must reply with a comment that states the following:_

* I certify that this change cannot be made in the application at this time.
* The `WHERE` clause in use limits the scope of data changes appropriately.
* This change will resolve the issue outlined above.
* An issue [has/has not] been opened to add the ability to make this change within the application in the future.

## Tasks for DBA

- [ ] Execute the query from the administrative query interface on the production web server
- [ ] Verify that the query was logged in the audit log database