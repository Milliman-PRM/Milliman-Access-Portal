# User Stats Database

## User

**Data source:** MAP application database

| Column    | Type      | Notes       |
|-----------|-----------|-------------|
| Id        | uuid      | Primary key |
| UserName  | character |             |
| FirstName | text      |             |
| LastName  | text      |             |
| Employer  | text      |             |

## Selection Group

**Data source:** MAP application database

| Column             | Type | Notes                                                                                                                   |
|--------------------|------|-------------------------------------------------------------------------------------------------------------------------|
| Id                 | uuid | Primary Key  |
| ContentInstanceUrl | text | Will need to be prefixed with the path to the content share so that this can be aligned to QlikView Session log entries |
| GroupName          | text |                                                                                                                         |
| RootContentItemId  | uuid | Foreign key to Root Content Item Id                                                                                     |

## Root Content Item

**Data source:** MAP application database

| Column        | Type | Notes                          |
|---------------|------|--------------------------------|
| Id            | uuid | Primary Key |
| ClientId      | uuid | Foreign Key to Client Id       |
| ContentName   | text |                                |
| ContentTypeId | uuid | Foreign Key to Content Type Id |

## Content Type

**Data Source:** MAP application database

| Column | Type | Notes |
|--------|------|-------|
| Id     | uuid | Primary Key |
| Name   | text | |

## Client

**Data source:** MAP application database

| Column         | Type | Notes                             |
|----------------|------|-----------------------------------|
| Id             | uuid | Primary key                       |
| ClientCode     | text |                                   |
| Name           | text |                                   |
| ParentClientId | uuid | Nullable foreign key to Client Id |

## Client In Profit Center

**Data source:** MAP application database

| Column         | Type    | Notes                                                          |
|----------------|---------|----------------------------------------------------------------|
| Id             | integer | Auto-increment primary key                                     |
| ClientId       | uuid    | Foreign key to Client Id                                       |
| ProfitCenterId | uuid    | Foreign key to Profit Center Id                                |
| StartDate      | date    |                                                                |
| EndDate        | date    | Default value of 12-31-9999 indicates currently active record  |

## Profit Center

**Data source:** MAP application database

| Column         | Type    | Notes      |
|----------------|---------|------------|
| Id               | uuid | Primary key |
| ProfitCenterCode | text |             |
| Name             | text |  |

## Publishing History

**Data source:** MAP application database and MAP audit log database

| Column            | Type      | Notes                                                                                                    |
|-------------------|-----------|----------------------------------------------------------------------------------------------------------|
| Id                | integer   | Auto-incremented primary key                                                                             |
| RootContentItemId | uuid      | Foreign key to Root Content Item Id                                                                      |
| PublishingUser    | uuid      | Foreign key to User Id; Indicates the user who created the publishing request                            |
| RequestTimestamp  | timestamp | When the publishing request was created                                                                  |
| ApprovingUser     | uuid      | Foreign key to User Id; Indicates the user who approved the publication to go live; comes from Audit Log |
| ApprovalTimestamp | timestamp | When the publication was approved to go live; comes from Audit Log                                       |
| RequestStatus     | integer   |   |


## Audit Event

**Data source:** MAP audit log database

| Column       | Type                        | Notes                                                                                        |
|--------------|-----------------------------|----------------------------------------------------------------------------------------------|
| Id           | bignit                      | Primary Key                                                                         |
| EventCode    | integer                     |                                                                                              |
| EventData    | jsonb                       | Contains a variety of details about the event. Some may be broken out into separate columns. |
| EventType    | text                        |                                                                                              |
| SessionId    | text                        |                                                                                              |
| TimeStampUtc | timestamp without time zone |                                                                                              |
| User         | text                        | The username of the user who initiated the action being logged                               |

## QlikView Session

**Data source:** QlikView Server log files (plain text)

| Column           | Type      | Notes                                                                            |
|------------------|-----------|----------------------------------------------------------------------------------|
| Id               | integer   |  Auto-incremented primary key                                                    |
| Timestamp        | timestamp |                                                                                  |
| Document         | text      | File path of the QVW being accessed in the session                               |
| ExitReason       | text      | The reason the session ended (user closed session, timeout, etc.)                |
| SessionStartTime | timestamp |                                                                                  |
| SessionDuration  | timestamp |                                                                                  |
| SessionEndTime   | timestamp | Not provided in the text file; has to be calculated at the time of the data load |
| UserName         | text      |                                                                                  |
| CalType          | text      | The type of QlikView CAL used by the session (Document or Named User)            |
| Browser          | text      |                                                                                  |
| Session          | integer   | Session ID (new in QlikView 12)                                                  |
| LogFileName      | text      | The name of the log file the record was extracted from                           |
| LogFileLineNumber | integer  | The line number where the record appeared in the log file                        |

> The combination of LogFileName and LogFileLineNumber must be unique. This is implemented to protect against accidental duplication of log records in the database.

## QlikView Audit

**Data source:** QlikView Server log files (plain text)

| Column    | Type      | Notes                                                                                   |
|-----------|-----------|-----------------------------------------------------------------------------------------|
| Id        | integer   | Auto-incremented primary key                                                            |
| Session   | integer   | Foreign key to QlikView Session table's Session                                         |
| Timestamp | timestamp | The time the logged event occurred                                                      |
| Document  | text      | File path of the QVW being accessed in the session                                      |
| EventType | text      | Category of action taken                                                                |
| Message   | text      | Detailed information; may contain ePHI and must be sanitized before presenting to users |
| LogFileName      | text      | The name of the log file the record was extracted from                           |
| LogFileLineNumber | integer  | The line number where the record appeared in the log file                        |

> The combination of LogFileName and LogFileLineNumber must be unique. This is implemented to protect against accidental duplication of log records in the database.

## Selected Fields

This table is used to assist with sanitizing records in QlikView Audit before displaying to end users in a report or database view

**Data source:** Initially populated by copying data from legacy user stats database

| Column    | Type    | Notes                                                                                 |
|-----------|---------|---------------------------------------------------------------------------------------|
| Id        | integer | Auto-incremented primary key                                                          |
| FieldName | text    | The name of the field, which would appear within the Message column of QlikView Audit |