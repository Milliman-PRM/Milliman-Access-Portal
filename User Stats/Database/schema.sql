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
)

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
