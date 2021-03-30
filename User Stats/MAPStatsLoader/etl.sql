/*
   CODE OWNERS: Ben Wyatt
   OBJECTIVE: Extract data from the production database and load into the user stats database
   DEVELOPER NOTES: 
*/

BEGIN TRANSACTION;

/*
	SECTION 1: Direct extractions
*/

INSERT INTO public."Users"
	("Id", "FirstName", "LastName", "UserName", "Employer")
	(select "Id", "FirstName", "LastName", "UserName", "Employer" from map."AspNetUsers")
	ON CONFLICT ON CONSTRAINT "PK_Users" DO NOTHING;

INSERT INTO public."ContentType"
	("Id", "Name")
	(select "Id", "TypeEnum"::TEXT from map."ContentType")
	ON CONFLICT ON CONSTRAINT "PK_ContentType" DO NOTHING;

INSERT INTO public."Client"
	("Id", "ClientCode", "Name", "ParentClientId")
	(select "Id", "ClientCode", "Name", "ParentClientId" from  map."Client")
	ON CONFLICT ON CONSTRAINT "PK_Client" DO NOTHING;

INSERT INTO public."RootContentItem"
	("Id", "ClientId", "ContentName", "ContentTypeId")
	(SELECT "Id", "ClientId", "ContentName", "ContentTypeId" FROM map."RootContentItem")
	ON CONFLICT ON CONSTRAINT "PK_RootContentItem" DO NOTHING;

INSERT INTO public."SelectionGroup"
	("Id", "ContentInstanceUrl", "GroupName", "RootContentItemId")
	(SELECT "Id", "ContentInstanceUrl", "GroupName", "RootContentItemId" FROM map."SelectionGroup")
	ON CONFLICT ON CONSTRAINT "PK_SelectionGroup" DO NOTHING;

INSERT INTO public."ProfitCenter"
	("Id", "Name", "ProfitCenterCode")
	(SELECT "Id", "Name", "ProfitCenterCode" FROM map."ProfitCenter")
	ON CONFLICT ON CONSTRAINT "PK_ProfitCenter" DO NOTHING;

/*
	SECTION 2: Client-ProfitCenter relationships
*/

-- Update existing records that are no longer applicable
UPDATE public."ClientInProfitCenter"
SET "EndDate" = (current_timestamp AT TIME ZONE 'UTC')
WHERE ctid IN
	(SELECT cpc.ctid
		FROM public."ClientInProfitCenter" cpc
			LEFT JOIN map."Client" cl ON cpc."ClientId" = cl."Id" AND cpc."ProfitCenterId" = cl."ProfitCenterId"
		WHERE cl."Id" IS NULL)
AND "EndDate" = '12/31/9999';
			
-- Insert currently applicable records
INSERT INTO public."ClientInProfitCenter"
	("ClientId", "ProfitCenterId", "StartDate")
	(SELECT "Id", "ProfitCenterId", (current_timestamp AT TIME ZONE 'UTC') FROM map."Client")
	ON CONFLICT ON CONSTRAINT "UNIQUE_Client_ProfitCenter_Current" DO NOTHING;

/*
	Section 3: User-SelectionGroup relationships
*/

-- Update existing records that are no longer applicable
UPDATE public."UserInSelectionGroup"
SET "EndDate" = (current_timestamp AT TIME ZONE 'UTC')
WHERE ctid IN
    (
        SELECT usg.ctid
        FROM public."UserInSelectionGroup" usg 
            LEFT JOIN map."UserInSelectionGroup" musg ON usg."UserId" = musg."UserId" AND usg."SelectionGroupId" = musg."SelectionGroupId"
        WHERE musg."Id" IS NULL
    )
AND "EndDate" = '12/31/9999';

-- Insert currently applicable records
INSERT INTO public."UserInSelectionGroup"
("UserId", "SelectionGroupId", "StartDate")
(
    SELECT "UserId", "SelectionGroupId", (current_timestamp AT TIME ZONE 'UTC')
    FROM map."UserInSelectionGroup"
)
ON CONFLICT ON CONSTRAINT "UNIQUE_User_SelectionGroup_Current" DO NOTHING;

/*
	SECTION 4: Audit Events

	Similar to Section 1, but sourced from a different database
*/

INSERT INTO public."AuditEvent"
	("Id", "EventCode", "EventData", "EventType", "SessionId", "TimeStampUtc", "User")
	(SELECT "Id", "EventCode", "EventData", "EventType", "SessionId", "TimeStampUtc", "User" 
	 FROM maplog."AuditEvent" 
	 WHERE "EventCode" in ( -- List event codes to be extracted
		1001,
		1008,
		1009,
		2001,
		2002,
		2003,
		2005,
		2006,
		2007,
		3001,
		3004,
		4001,
		4002,
		4003,
		4004,
		4005,
		4006,
		4007,
		4008,
		4009,
		6001,
		6002,
		6003,
		6101,
		6102,
		6103,
		6105,
		6106,
		6107,
		7001,
		7002,
		7003,
		7004,
		7101,
		7103,
		7104,
		7105
		))
	 ON CONFLICT ON CONSTRAINT "PK_AuditEvent" DO NOTHING;

COMMIT;
