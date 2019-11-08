/*
   CODE OWNERS: Ben Wyatt
   OBJECTIVE: Define the database objects required for user stats
   DEVELOPER NOTES: 
*/

/*
    Define foreign data wrappers, which allow access to the MAP application and audit log databases from ther user stats database

    Requires the extension postgres_fdw https://www.postgresql.org/docs/9.6/postgres-fdw.html
*/

CREATE EXTENSION postgres_fdw;

GRANT prod_app_readers to map_etl_read;

CREATE SERVER prod_map_app
        FOREIGN DATA WRAPPER postgres_fdw
        OPTIONS (host 'prm-pgsql-02.postgres.database.azure.com', port '5432', dbname 'production_map_app');

CREATE USER MAPPING FOR map_etl_read
        SERVER prod_map_app
        OPTIONS (user 'map_etl_read@prm-pgsql-02', password ''); -- Password must be filled in

CREATE USER MAPPING FOR prmpgadmin
	SERVER prod_map_app
	OPTIONS (user 'map_etl_read@prm-pgsql-02', password ''); -- Password must be filled in

CREATE SCHEMA map;

IMPORT FOREIGN SCHEMA public
    FROM SERVER prod_map_app
    INTO map;

GRANT prod_log_readers to map_etl_read;

 CREATE SERVER prod_map_log
        FOREIGN DATA WRAPPER postgres_fdw
        OPTIONS (host 'prm-pgsql-02.postgres.database.azure.com', port '5432', dbname 'production_map_log');

CREATE USER MAPPING FOR map_etl_read
        SERVER prod_map_log
        OPTIONS (user 'map_etl_read@prm-pgsql-02', password ''); -- Password must be filled in

CREATE USER MAPPING FOR prmpgadmin
	SERVER prod_map_log
	OPTIONS (user 'map_etl_read@prm-pgsql-02', password ''); -- Password must be filled in

CREATE SCHEMA maplog;

IMPORT FOREIGN SCHEMA public
    FROM SERVER prod_map_log
    INTO maplog;

/*
    Create tables
*/

CREATE TABLE public."Users"
(
  "Id" uuid NOT NULL DEFAULT uuid_generate_v4(),
  "FirstName" text,
  "LastName" text,
  "UserName" character varying(256),
  "Employer" text,
  CONSTRAINT "PK_Users" PRIMARY KEY ("Id")
);

CREATE TABLE public."Client"
(
  "Id" uuid NOT NULL DEFAULT uuid_generate_v4(),
  "ClientCode" text,
  "Name" text NOT NULL,
  "ParentClientId" uuid,
  CONSTRAINT "PK_Client" PRIMARY KEY ("Id"),
  CONSTRAINT "FK_Client_Client_ParentClientId" FOREIGN KEY ("ParentClientId")
      REFERENCES public."Client" ("Id") MATCH SIMPLE
      ON UPDATE NO ACTION ON DELETE RESTRICT
);

CREATE TABLE public."ContentType"
(
  "Id" uuid NOT NULL DEFAULT uuid_generate_v4(),
  "Name" text NOT NULL,
  CONSTRAINT "PK_ContentType" PRIMARY KEY ("Id")
);

CREATE TABLE public."RootContentItem"
(
  "Id" uuid NOT NULL DEFAULT uuid_generate_v4(),
  "ClientId" uuid NOT NULL,
  "ContentName" text NOT NULL,
  "ContentTypeId" uuid NOT NULL,
  CONSTRAINT "PK_RootContentItem" PRIMARY KEY ("Id"),
  CONSTRAINT "FK_RootContentItem_Client_ClientId" FOREIGN KEY ("ClientId")
      REFERENCES public."Client" ("Id") MATCH SIMPLE
      ON UPDATE NO ACTION ON DELETE CASCADE,
  CONSTRAINT "FK_RootContentItem_ContentType_ContentTypeId" FOREIGN KEY ("ContentTypeId")
      REFERENCES public."ContentType" ("Id") MATCH SIMPLE
      ON UPDATE NO ACTION ON DELETE CASCADE
);

CREATE TABLE public."SelectionGroup"
(
  "Id" uuid NOT NULL DEFAULT uuid_generate_v4(),
  "ContentInstanceUrl" text,
  "GroupName" text NOT NULL,
  "RootContentItemId" uuid NOT NULL,
  CONSTRAINT "PK_SelectionGroup" PRIMARY KEY ("Id"),
  CONSTRAINT "FK_SelectionGroup_RootContentItem_RootContentItemId" FOREIGN KEY ("RootContentItemId")
      REFERENCES public."RootContentItem" ("Id") MATCH SIMPLE
      ON UPDATE NO ACTION ON DELETE CASCADE
);

CREATE TABLE public."ProfitCenter"
(
  "Id" uuid NOT NULL DEFAULT uuid_generate_v4(),
  "Name" text NOT NULL,
  "ProfitCenterCode" text,
  CONSTRAINT "PK_ProfitCenter" PRIMARY KEY ("Id")
);

CREATE TABLE public."ClientInProfitCenter"
(
   "Id" serial,
   "ClientId" uuid NOT NULL,
   "ProfitCenterId" uuid NOT NULL,
   "StartDate" date NOT NULL,
   "EndDate" date NOT NULL DEFAULT '12-31-9999', -- Default value makes BETWEEN clausess easy to use and makes the current record easy to find
   CONSTRAINT "PK_ClientInProfitCenter" PRIMARY KEY ("Id"),
   CONSTRAINT "FK_ClientInProfitCenter_Client_ClientID" FOREIGN KEY ("ClientId")
	REFERENCES public."Client" ("Id") MATCH SIMPLE
	ON UPDATE NO ACTION ON DELETE CASCADE,
   CONSTRAINT "FK_ClientInProfitCenter_ProfitCenter_ProfitCenterId" FOREIGN KEY ("ProfitCenterId")
	REFERENCES public."ProfitCenter" ("Id") MATCH SIMPLE
	ON UPDATE NO ACTION ON DELETE CASCADE,
   CONSTRAINT "UNIQUE_Client_ProfitCenter_Current" UNIQUE ("ClientId", "ProfitCenterId", "EndDate")
);

CREATE TABLE public."UserInSelectionGroup"
(
   "Id" serial,
   "UserId" uuid NOT NULL,
   "SelectionGroupId" uuid NOT NULL,
   "StartDate" date NOT NULL,
   "EndDate" date NOT NULL DEFAULT '12-31-9999', -- Default value makes BETWEEN clausess easy to use and makes the current record easy to find
   CONSTRAINT "PK_UserInSelectionGroup" PRIMARY KEY ("Id"),
   CONSTRAINT "FK_UserInSelectionGroup_User_UserId" FOREIGN KEY ("UserId")
   REFERENCES public."Users" ("Id") MATCH SIMPLE
   ON UPDATE NO ACTION ON DELETE CASCADE,
   CONSTRAINT "FK_UserInSelectionGroup_SelectionGroup_SelectionGroupId" FOREIGN KEY ("SelectionGroupId")
   REFERENCES map."SelectionGroup" ("Id") MATCH SIMPLE
   ON UPDATE NO ACTION ON DELETE CASCADE,
   CONSTRAINT "UNIQUE_User_SelectionGroup_Current" UNIQUE ("UserId", "SelectionGroupId", "EndDate")
);

CREATE TABLE public."PublishingHistory"
(
   "Id" serial,
   "RootContentItemId" uuid NOT NULL,
   "RequestingUserId" uuid NOT NULL,
   "RequestTimestamp" timestamp NOT NULL,
   "ApprovingUserId" uuid,
   "ApprovalTimestamp" timestamp,
   "RequestStatus" integer,
   CONSTRAINT "PK_PublishingHistory" PRIMARY KEY ("Id"),
   CONSTRAINT "FK_PublishingHistory_RequestingUser_UserId" FOREIGN KEY ("RequestingUserId")
	REFERENCES public."Users" ("Id") MATCH SIMPLE
	ON UPDATE NO ACTION ON DELETE CASCADE,
   CONSTRAINT "FK_PublishingHistory_ApprovingUser_UserId" FOREIGN KEY ("ApprovingUserId")
	REFERENCES public."Users" ("Id") MATCH SIMPLE
	ON UPDATE NO ACTION ON DELETE CASCADE
);

CREATE TABLE public."AuditEvent"
(
  "Id" bigint NOT NULL,
  "EventCode" integer NOT NULL,
  "EventData" jsonb,
  "EventType" text,
  "SessionId" text,
  "TimeStampUtc" timestamp without time zone NOT NULL,
  "User" text,
  CONSTRAINT "PK_AuditEvent" PRIMARY KEY ("Id")
);

CREATE TABLE public."QlikViewSession"
(
   "Id" serial,
   "Timestamp" timestamp,
   "Document" text,
   "ExitReason" text,
   "SessionStartTime" timestamp,
   "SessionDuration" interval,
   "SessionEndTime" timestamp, -- Not present in the log file; must be calculated at insert
   "Username" text,
   "CalType" text,
   "Browser" text,
   "Session" integer,
   "LogFileName" text,
   "LogFileLineNumber" integer,
   CONSTRAINT "PK_QlikViewSession" PRIMARY KEY ("Id"),
   CONSTRAINT "UNIQUE_QVSession_LogFileName_LogFileLine" UNIQUE ("LogFileName", "LogFileLineNumber")
);

CREATE TABLE public."QlikViewAudit"
(
   "Id" serial,
   "Session" integer,
   "Timestamp" timestamp,
   "Document" text,
   "EventType" text,
   "Message" text,
   "LogFileName" text,
   "LogFileLineNumber" integer,
   CONSTRAINT "PK_QlikViewAudit" PRIMARY KEY ("Id"),
   CONSTRAINT "UNIQUE_QVAudit_LogFileName_LogFileLine" UNIQUE ("LogFileName", "LogFileLineNumber")/*,
   CONSTRAINT "FK_QlikViewAudit_QlikViewSession_Session" FOREIGN KEY ("Session")
	REFERENCES public."QlikViewSession" ("Session") MATCH SIMPLE
	ON UPDATE NO ACTION ON DELETE CASCADE*/ -- This key can't be implemented as-is. We need to determine whether Session will always be unique, which seems unlikely at the moment
);

/*

   Define views used by User Stats reports

*/

CREATE OR REPLACE VIEW public."UserInClient" AS
 SELECT pc."Name" AS "ProfitCenterName",
    pc."ProfitCenterCode",
    cl."Id" AS "ClientId",
    cl."Name" AS "ClientName",
    usr."Id" AS "UserId",
    usr."UserName",
    pc."Id" AS "ProfitCenterId"
   FROM "ProfitCenter" pc
     JOIN "ClientInProfitCenter" cpc ON pc."Id" = cpc."ProfitCenterId"
     JOIN "Client" cl ON cl."Id" = cpc."ClientId"
     JOIN map."UserRoleInClient" urc ON cl."Id" = urc."ClientId"
     JOIN "Users" usr ON usr."Id" = urc."UserId";

ALTER TABLE public."UserInClient"
    OWNER TO prmpgadmin;

CREATE OR REPLACE VIEW public."UserInContentItem" AS
 SELECT pc."Name" AS "ProfitCenter",
    cl."Name" AS "ClientName",
    rci."ContentName",
    sg."GroupName",
    usr."UserName"
   FROM "Users" usr
     JOIN map."UserInSelectionGroup" usg ON usg."UserId" = usr."Id"
     JOIN "SelectionGroup" sg ON usg."SelectionGroupId" = sg."Id"
     JOIN "RootContentItem" rci ON sg."RootContentItemId" = rci."Id"
     JOIN map."Client" cl ON rci."ClientId" = cl."Id"
     JOIN map."ProfitCenter" pc ON cl."ProfitCenterId" = pc."Id";

ALTER TABLE public."UserInContentItem"
    OWNER TO prmpgadmin;

CREATE OR REPLACE VIEW public."UserInProfitCenter" AS
 SELECT DISTINCT cpc."ProfitCenterId",
    usr."UserName"
   FROM "ClientInProfitCenter" cpc
     JOIN "Client" cl ON cl."Id" = cpc."ClientId"
     JOIN map."AspNetUserClaims" claims ON cl."Id"::text = claims."ClaimValue"
     JOIN "Users" usr ON usr."Id" = claims."UserId"
  WHERE cpc."EndDate" = '9999-12-31'::date;

ALTER TABLE public."UserInProfitCenter"
    OWNER TO prmpgadmin;

CREATE OR REPLACE VIEW public."UserInContentItemHistory" AS
 SELECT usg."StartDate",
    usg."EndDate",
    usr."UserName",
    sg."GroupName" AS "Group",
    cl."Name" AS "Client",
    rci."ContentName" AS "Content Name",
    pc."Name" AS "Profit Center",
    ct."Name" AS "Content Type"
   FROM "UserInSelectionGroup" usg
     JOIN "Users" usr ON usr."Id" = usg."UserId"
     JOIN "SelectionGroup" sg ON usg."SelectionGroupId" = sg."Id"
     JOIN "RootContentItem" rci ON sg."RootContentItemId" = rci."Id"
     JOIN "ContentType" ct ON ct."Id" = rci."ContentTypeId"
     JOIN "Client" cl ON rci."ClientId" = cl."Id"
     JOIN "ClientInProfitCenter" cpc ON cl."Id" = cpc."ClientId" AND cpc."EndDate" = '9999-12-31'::date
     JOIN "ProfitCenter" pc ON pc."Id" = cpc."ProfitCenterId";

ALTER TABLE public."UserInContentItemHistory"
    OWNER TO prmpgadmin;

CREATE OR REPLACE VIEW public."ClientUserStatus" AS
 SELECT DISTINCT ucl."ProfitCenterName",
    ucl."ProfitCenterCode",
    ucl."ClientName",
    ucl."UserId",
    ucl."UserName",
    ust."AccountActivated",
    ust."IsSuspended",
    ucl."ProfitCenterId"
   FROM "UserInClient" ucl
     JOIN "UserStatus" ust ON ucl."UserId" = ust."Id";

ALTER TABLE public."ClientUserStatus"
    OWNER TO prmpgadmin;

CREATE OR REPLACE VIEW public."QlikViewAuditSession" AS
 SELECT qvs."Id" AS "SessionId",
    qva."Id" AS "AuditId"
   FROM "QlikViewSession" qvs
     JOIN "QlikViewAudit" qva ON qvs."Document" = qva."Document" AND qvs."Session" = qva."Session" AND qva."Timestamp" >= qvs."SessionStartTime" AND qva."Timestamp" <= qvs."SessionEndTime";

ALTER TABLE public."QlikViewAuditSession"
    OWNER TO prmpgadmin;

CREATE OR REPLACE VIEW public."QlikViewSessionFile" AS
 WITH "SessionFiles" AS (
         SELECT "QlikViewSession"."Id" AS "SessionId",
            "QlikViewSession"."SessionStartTime",
            regexp_replace("QlikViewSession"."Document", '\\\\[A-Za-z0-9\-\\]+CONTENT\\'::text, ''::text, 'ig'::text) AS "Filename",
            "QlikViewSession"."Session",
            "QlikViewSession"."LogFileName",
            "QlikViewSession"."LogFileLineNumber"
           FROM "QlikViewSession"
        )
 SELECT sf."SessionId",
    sf."SessionStartTime",
    sf."Filename",
    sf."Session",
    sf."LogFileName",
    sf."LogFileLineNumber",
    substr(sf."Filename", 1, strpos(sf."Filename", '\'::text) - 1) AS "RootContentItemId"
   FROM "SessionFiles" sf;

ALTER TABLE public."QlikViewSessionFile"
    OWNER TO prmpgadmin;

CREATE OR REPLACE VIEW public."UserContentAccess" AS
 SELECT "AuditEvent"."Id",
        CASE
            WHEN ("AuditEvent"."EventData" #>> '{ContentItem,Id}'::text[]) IS NULL THEN "AuditEvent"."EventData" ->> 'ContentItem'::text
            ELSE "AuditEvent"."EventData" #>> '{ContentItem,Id}'::text[]
        END AS "ContentItemId",
        CASE
            WHEN ("AuditEvent"."EventData" #>> '{ContentItem,Id}'::text[]) IS NULL THEN "AuditEvent"."EventData" ->> 'SelectionGroup'::text
            ELSE "AuditEvent"."EventData" #>> '{SelectionGroup,Id}'::text[]
        END AS "SelectionGroupId",
    "AuditEvent"."SessionId",
    "AuditEvent"."TimeStampUtc" AS "TimeStampEasternTime",
    "AuditEvent"."User"
   FROM "AuditEvent"
  WHERE "AuditEvent"."EventCode" = 1008;

ALTER TABLE public."UserContentAccess"
    OWNER TO prmpgadmin;

CREATE OR REPLACE VIEW public."QlikViewSessionStats" AS
 SELECT pc."Name" AS "ProfitCenterName",
    pc."Id" AS "ProfitCenterId",
    cl."Name" AS "ClientName",
    cl."Id" AS "ClientId",
    rci."ContentName",
    qvs."Username",
    rci."Id",
    COALESCE(usrgrp."GroupName", '(Removed from groups)'::text) AS "CurrentGroup",
    sum(
        CASE
            WHEN qvs."SessionStartTime" > (timezone('utc'::text, now()) - '30 days'::interval) THEN 1
            ELSE 0
        END) AS "SessionCount30Days",
    sum(
        CASE
            WHEN qvs."SessionStartTime" > (timezone('utc'::text, now()) - '60 days'::interval) THEN 1
            ELSE 0
        END) AS "SessionCount60Days",
    sum(
        CASE
            WHEN qvs."SessionStartTime" > (timezone('utc'::text, now()) - '90 days'::interval) THEN 1
            ELSE 0
        END) AS "SessionCount90Days",
    sum(
        CASE
            WHEN qvs."SessionStartTime" > (timezone('utc'::text, now()) - '30 days'::interval) THEN qvs."SessionDuration"
            ELSE NULL::interval
        END) AS "TotalDuration30Days",
    sum(
        CASE
            WHEN qvs."SessionStartTime" > (timezone('utc'::text, now()) - '60 days'::interval) THEN qvs."SessionDuration"
            ELSE NULL::interval
        END) AS "TotalDuration60Days",
    sum(
        CASE
            WHEN qvs."SessionStartTime" > (timezone('utc'::text, now()) - '90 days'::interval) THEN qvs."SessionDuration"
            ELSE NULL::interval
        END) AS "TotalDuration90Days",
    avg(
        CASE
            WHEN qvs."SessionStartTime" > (timezone('utc'::text, now()) - '30 days'::interval) THEN qvs."SessionDuration"
            ELSE NULL::interval
        END) AS "AverageDuration30Days",
    avg(
        CASE
            WHEN qvs."SessionStartTime" > (timezone('utc'::text, now()) - '60 days'::interval) THEN qvs."SessionDuration"
            ELSE NULL::interval
        END) AS "AverageDuration60Days",
    avg(
        CASE
            WHEN qvs."SessionStartTime" > (timezone('utc'::text, now()) - '90 days'::interval) THEN qvs."SessionDuration"
            ELSE NULL::interval
        END) AS "AverageDuration90Days"
   FROM "QlikViewSession" qvs
     JOIN "QlikViewSessionFile" sf ON qvs."Id" = sf."SessionId"
     JOIN "RootContentItem" rci ON rci."Id"::text = sf."RootContentItemId"
     JOIN "Client" cl ON rci."ClientId" = cl."Id"
     JOIN "ClientInProfitCenter" cpc ON cl."Id" = cpc."ClientId"
     JOIN "ProfitCenter" pc ON cpc."ProfitCenterId" = pc."Id"
     JOIN "Users" usr ON lower(qvs."Username") = lower(usr."UserName"::text)
     LEFT JOIN ( SELECT usr_1."UserName",
            sg."RootContentItemId",
            sg."GroupName"
           FROM map."UserInSelectionGroup" usg
             JOIN map."SelectionGroup" sg ON usg."SelectionGroupId" = sg."Id"
             JOIN "Users" usr_1 ON usg."UserId" = usr_1."Id") usrgrp ON lower(qvs."Username") = lower(usrgrp."UserName"::text) AND sf."RootContentItemId" = usrgrp."RootContentItemId"::text
  WHERE cpc."EndDate" = '9999-12-31'::date AND qvs."SessionStartTime" > (timezone('utc'::text, now()) - '90 days'::interval)
  GROUP BY pc."Name", cl."Name", rci."ContentName", qvs."Username", rci."Id", usrgrp."GroupName", pc."Id", cl."Id";

ALTER TABLE public."QlikViewSessionStats"
    OWNER TO prmpgadmin;


CREATE OR REPLACE VIEW public."UserStatus" AS
 SELECT "AspNetUsers"."Id",
    "AspNetUsers"."UserName",
    "AspNetUsers"."EmailConfirmed" AS "AccountActivated",
    "AspNetUsers"."IsSuspended"
   FROM map."AspNetUsers";

ALTER TABLE public."UserStatus"
    OWNER TO prmpgadmin;

CREATE OR REPLACE VIEW public."ClientUserStatus" AS
 SELECT DISTINCT ucl."ProfitCenterName",
    ucl."ProfitCenterCode",
    ucl."ClientName",
    ucl."UserId",
    ucl."UserName",
    ust."AccountActivated",
    ust."IsSuspended",
    ucl."ProfitCenterId"
   FROM "UserInClient" ucl
     JOIN "UserStatus" ust ON ucl."UserId" = ust."Id";

ALTER TABLE public."ClientUserStatus"
    OWNER TO prmpgadmin;

CREATE OR REPLACE VIEW public."UserInProfitCenter" AS
 SELECT DISTINCT cpc."ProfitCenterId",
    usr."UserName"
   FROM "ClientInProfitCenter" cpc
     JOIN "Client" cl ON cl."Id" = cpc."ClientId"
     JOIN map."AspNetUserClaims" claims ON cl."Id"::text = claims."ClaimValue"
     JOIN "Users" usr ON usr."Id" = claims."UserId"
  WHERE cpc."EndDate" = '9999-12-31'::date;

ALTER TABLE public."UserInProfitCenter"
    OWNER TO prmpgadmin;

CREATE OR REPLACE VIEW public."UserRecentLogins" AS
 SELECT pc."Name" AS "ProfitCenterName",
    pc."ProfitCenterCode",
    upc."UserName",
    sum(
        CASE
            WHEN ae."TimeStampUtc" IS NULL THEN 0
            WHEN ae."TimeStampUtc" > (timezone('utc'::text, now()) - '30 days'::interval) THEN 1
            ELSE 0
        END) AS "Logins last 30 days",
    sum(
        CASE
            WHEN ae."TimeStampUtc" IS NULL THEN 0
            WHEN ae."TimeStampUtc" > (timezone('utc'::text, now()) - '60 days'::interval) THEN 1
            ELSE 0
        END) AS "Logins last 60 days",
    sum(
        CASE
            WHEN ae."TimeStampUtc" IS NULL THEN 0
            WHEN ae."TimeStampUtc" > (timezone('utc'::text, now()) - '90 days'::interval) THEN 1
            ELSE 0
        END) AS "Logins last 90 days",
    upc."ProfitCenterId"
   FROM "ProfitCenter" pc
     JOIN "UserInProfitCenter" upc ON pc."Id" = upc."ProfitCenterId"
     LEFT JOIN maplog."AuditEvent" ae ON upper(ae."User") = upper(upc."UserName"::text)
  WHERE ae."EventCode" = 1001 OR ae."EventCode" IS NULL
  GROUP BY pc."Name", pc."ProfitCenterCode", upc."UserName", upc."ProfitCenterId";

ALTER TABLE public."UserRecentLogins"
    OWNER TO prmpgadmin;

CREATE OR REPLACE VIEW public."NYExportCounts" AS
 SELECT date_part('year'::text, qva."Timestamp") AS "Year",
    date_part('month'::text, qva."Timestamp") AS "Month",
    pc."Name" AS "ProfitCenterName",
    cl."Name" AS "ClientName",
    rci."ContentName",
    qvs."Username",
    count(DISTINCT qva."Id") AS "ExportCount"
   FROM "QlikViewSession" qvs
     JOIN "QlikViewSessionFile" sf ON qvs."Session" = sf."Session" AND qvs."LogFileName" = sf."LogFileName" AND qvs."LogFileLineNumber" = sf."LogFileLineNumber"
     JOIN "QlikViewAuditSession" qvas ON qvs."Id" = qvas."SessionId"
     JOIN "QlikViewAudit" qva ON qva."Id" = qvas."AuditId"
     JOIN "RootContentItem" rci ON rci."Id"::text = sf."RootContentItemId"
     JOIN "Client" cl ON rci."ClientId" = cl."Id"
     JOIN "ClientInProfitCenter" cpc ON cl."Id" = cpc."ClientId"
     JOIN "ProfitCenter" pc ON cpc."ProfitCenterId" = pc."Id"
  WHERE cpc."EndDate" = '9999-12-31'::date AND strpos(qva."Message", 'action(11)') > 0
  GROUP BY pc."Name", cl."Name", rci."ContentName", qvs."Username", (date_part('month'::text, qva."Timestamp")), (date_part('year'::text, qva."Timestamp"));

ALTER TABLE public."NYExportCounts"
    OWNER TO prmpgadmin;

CREATE OR REPLACE VIEW public."UserContentAccess" AS
 SELECT "AuditEvent"."Id",
        CASE
            WHEN ("AuditEvent"."EventData" #>> '{ContentItem,Id}'::text[]) IS NULL THEN "AuditEvent"."EventData" ->> 'ContentItem'::text
            ELSE "AuditEvent"."EventData" #>> '{ContentItem,Id}'::text[]
        END AS "ContentItemId",
        CASE
            WHEN ("AuditEvent"."EventData" #>> '{ContentItem,Id}'::text[]) IS NULL THEN "AuditEvent"."EventData" ->> 'SelectionGroup'::text
            ELSE "AuditEvent"."EventData" #>> '{SelectionGroup,Id}'::text[]
        END AS "SelectionGroupId",
    "AuditEvent"."SessionId",
    "AuditEvent"."TimeStampUtc" AS "TimeStampEasternTime",
    "AuditEvent"."User"
   FROM "AuditEvent"
  WHERE "AuditEvent"."EventCode" = 1008;

ALTER TABLE public."UserContentAccess"
    OWNER TO prmpgadmin;

CREATE OR REPLACE VIEW public."UserRoleListing" AS
 SELECT pc."Name" AS "ProfitCenterName",
    pc."ProfitCenterCode",
    cl."Name" AS "Client",
    usr."UserName",
    max(
        CASE rl."Name"
            WHEN 'Admin'::text THEN 1
            ELSE 0
        END)::boolean AS "Admin",
    max(
        CASE rl."Name"
            WHEN 'UserCreator'::text THEN 1
            ELSE 0
        END)::boolean AS "User Creator",
    max(
        CASE rl."Name"
            WHEN 'ContentAccessAdmin'::text THEN 1
            ELSE 0
        END)::boolean AS "Content Access Admin",
    max(
        CASE rl."Name"
            WHEN 'ContentPublisher'::text THEN 1
            ELSE 0
        END)::boolean AS "Content Publisher",
    max(
        CASE rl."Name"
            WHEN 'ContentUser'::text THEN 1
            ELSE 0
        END)::boolean AS "Content User",
    pc."Id" AS "ProfitCenterId",
    cl."Id" AS "ClientId"
   FROM "ProfitCenter" pc
     JOIN "ClientInProfitCenter" cpc ON pc."Id" = cpc."ProfitCenterId"
     JOIN "Client" cl ON cl."Id" = cpc."ClientId"
     JOIN map."UserRoleInClient" urc ON cl."Id" = urc."ClientId"
     JOIN "Users" usr ON usr."Id" = urc."UserId"
     LEFT JOIN map."AspNetRoles" rl ON urc."RoleId" = rl."Id"
  GROUP BY pc."Name", pc."ProfitCenterCode", cl."Name", usr."UserName", pc."Id", cl."Id";

ALTER TABLE public."UserRoleListing"
    OWNER TO prmpgadmin;

