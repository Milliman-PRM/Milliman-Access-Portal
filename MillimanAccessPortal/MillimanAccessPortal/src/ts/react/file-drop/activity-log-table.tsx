import '../../../images/icons/add-group.svg';
import '../../../images/icons/add-user.svg';
import '../../../images/icons/group.svg';
import '../../../images/icons/user.svg';

import '../../../scss/react/file-drop/activity-log-table.scss';

import * as moment from 'moment';
import * as React from 'react';

import {
  FDEventAccountAddedToPG,
  FDEventAccountCreated,
  FDEventAccountRemovedFromPG,
  FDEventDirectoryCreated,
  FDEventDirectoryRemoved,
  FDEventFDCreated,
  FDEventFDUpdated,
  FDEventFileDeleteAuthorized,
  FDEventFileOrDirectoryRenamed,
  FDEventFileReadAuthorized,
  FDEventFileWriteAuthorized,
  FDEventPGCreated,
  FDEventPGDeleted,
  FDEventPGUpdated,
  FileDropEvent,
  FileDropLogEventEnum,
} from '../models';

interface ActivityLogTableProps {
  activityLogData: FileDropEvent[];
}

export class ActivityLogTable extends React.Component<ActivityLogTableProps> {

  public render() {

    return (
      <table className="activity-log-table">
        <thead>
          <tr>
            <th className="col-date">Date</th>
            <th className="col-author">Author</th>
            <th className="col-action">Action</th>
            <th className="col-description">Description</th>
          </tr>
        </thead>
        <tbody>
          {
            this.props.activityLogData.map((logEvent) => (
              <tr className="event-row" key={`${logEvent.timeStampUtc}`}>
                <td>
                  <span title={moment(logEvent.timeStampUtc).local().format('MM/DD/YYYY h:mm:ss A')}>
                    {
                      moment(logEvent.timeStampUtc).local().format('M/D/YY \nh:mm A')
                    }
                  </span>
                </td>
                <td>
                  {logEvent.fullName}<br />
                  <span className="username">{logEvent.userName}</span>
                </td>
                <td>{logEvent.eventType}</td>
                <td>
                  {
                    ((logE: FileDropEvent) => {
                      switch (logE.eventCode) {
                        case FileDropLogEventEnum.FDCreated:
                          return this.renderFileDropCreatedDetails(logE);
                          break;
                        case FileDropLogEventEnum.FDUpdated:
                          return this.renderFileDropUpdatedDetails(logE);
                          break;
                        case FileDropLogEventEnum.PGCreated:
                          return this.renderPermissionGroupCreatedDetails(logE);
                          break;
                        case FileDropLogEventEnum.PGDeleted:
                          return this.renderPermissionGroupDeletedDetails(logE);
                          break;
                        case FileDropLogEventEnum.PGUpdated:
                          return this.renderPermissionGroupUpdatedDetails(logE);
                          break;
                        case FileDropLogEventEnum.AccountCreated:
                          return this.renderAccountCreatedDetails(logE);
                          break;
                        case FileDropLogEventEnum.AccountAddedToPG:
                          return this.renderAccountAddedToPGDetails(logE);
                          break;
                        case FileDropLogEventEnum.AccountRemovedFromPG:
                          return this.renderAccountRemovedFromPGDetails(logE);
                          break;
                        case FileDropLogEventEnum.DirectoryCreated:
                          return this.renderDirectoryCreatedDetails(logE);
                          break;
                        case FileDropLogEventEnum.DirectoryRemoved:
                          return this.renderDirectoryRemovedDetails(logE);
                          break;
                        case FileDropLogEventEnum.FileOrDirectoryRenamed:
                          return this.renderFileOrDirectoryRenamedDetails(logE);
                          break;
                        case FileDropLogEventEnum.FileWriteAuthorized:
                          return this.renderFileWriteAuthorizedDetails(logE);
                          break;
                        case FileDropLogEventEnum.FileReadAuthorized:
                          return this.renderFileReadAuthorizedDetails(logE);
                          break;
                        case FileDropLogEventEnum.FileDeleteAuthorized:
                          return this.renderFileDeleteAuthorizedDetails(logE);
                          break;
                        default:
                          return null;
                      }
                    })(logEvent)
                  }
                </td>
              </tr>
            ))
          }
        </tbody>
      </table>
    );
  }

  // Render rows by Log Event type
  public renderFileDropCreatedDetails(logEvent: FDEventFDCreated) {
    const { FileDrop } = logEvent.eventData;
    const details = [];

    details.push(this.renderEventDetail(<>File Drop name: <strong>{FileDrop.Name}</strong></>));
    if (FileDrop.Description) {
      details.push(this.renderEventDetail(<>File Drop description: <strong>{FileDrop.Description}</strong></>));
    }

    return details;
  }

  public renderFileDropUpdatedDetails(logEvent: FDEventFDUpdated) {
    const { OldFileDrop, NewFileDrop } = logEvent.eventData;
    const details = [];

    if (OldFileDrop.Name !== NewFileDrop.Name) {
      details.push(this.renderEventDetail(<>File Drop name changed to <strong>{NewFileDrop.Name}</strong></>));
    }
    if (OldFileDrop.Description !== NewFileDrop.Description) {
      details.push(
        this.renderEventDetail(<>File Drop description changed to <strong>{NewFileDrop.Description}</strong></>),
      );
    }

    return details;
  }

  public renderPermissionGroupCreatedDetails(logEvent: FDEventPGCreated) {
    const { PermissionGroup } = logEvent.eventData;
    const details = [];

    if (PermissionGroup.IsPersonalGroup) {
      details.push(this.renderEventDetail(<>Personal Permission Group for <strong>{PermissionGroup.Name}</strong></>));
    } else {
      details.push(this.renderEventDetail(<>Permission Group name: <strong>{PermissionGroup.Name}</strong></>));
    }
    if (PermissionGroup.ReadAccess) {
      details.push(this.renderEventDetail(<>Read Access Granted</>));
    }
    if (PermissionGroup.WriteAccess) {
      details.push(this.renderEventDetail(<>Write Access Granted</>));
    }
    if (PermissionGroup.DeleteAccess) {
      details.push(this.renderEventDetail(<>Delete Access Granted</>));
    }

    return details;
  }

  public renderPermissionGroupDeletedDetails(logEvent: FDEventPGDeleted) {
    const { PermissionGroup } = logEvent.eventData;
    const details = [];

    if (PermissionGroup.IsPersonalGroup) {
      details.push(this.renderEventDetail(<><strong>{PermissionGroup.Name}</strong> personal group deleted</>));
    } else {
      details.push(this.renderEventDetail(<>Permission Group <strong>{PermissionGroup.Name}</strong> deleted</>));
    }

    return details;
  }

  public renderPermissionGroupUpdatedDetails(logEvent: FDEventPGUpdated) {
    const { PermissionGroup, PreviousProperties, UpdatedProperties } = logEvent.eventData;
    const details = [];

    if (!PermissionGroup.IsPersonalGroup && UpdatedProperties.Name !== PreviousProperties.Name) {
      details.push(
        this.renderEventDetail(
          <>
            <strong>{PreviousProperties.Name}</strong> renamed to <strong>{UpdatedProperties.Name}</strong>
          </>,
        ),
      );
    }
    if (UpdatedProperties.ReadAccess && !PreviousProperties.ReadAccess) {
      details.push(this.renderEventDetail(<>Read Access Granted</>));
    }
    if (UpdatedProperties.WriteAccess && !PreviousProperties.WriteAccess) {
      details.push(this.renderEventDetail(<>Write Access Granted</>));
    }
    if (UpdatedProperties.DeleteAccess && !PreviousProperties.DeleteAccess) {
      details.push(this.renderEventDetail(<>Delete Access Granted</>));
    }
    if (!UpdatedProperties.ReadAccess && PreviousProperties.ReadAccess) {
      details.push(this.renderEventDetail(<>Read Access Revoked</>));
    }
    if (!UpdatedProperties.WriteAccess && PreviousProperties.WriteAccess) {
      details.push(this.renderEventDetail(<>Write Access Revoked</>));
    }
    if (!UpdatedProperties.DeleteAccess && PreviousProperties.DeleteAccess) {
      details.push(this.renderEventDetail(<>Delete Access Revoked</>));
    }

    return details;
  }

  public renderAccountCreatedDetails(logEvent: FDEventAccountCreated) {
    const { MapUser } = logEvent.eventData;
    const details = [];

    details.push(this.renderEventDetail(<>SFTP account created for <strong>{MapUser.UserName}</strong></>));

    return details;
  }

  public renderAccountAddedToPGDetails(logEvent: FDEventAccountAddedToPG) {
    const { MapUser, PermissionGroup } = logEvent.eventData;
    const details = [];

    details.push(
      this.renderEventDetail(
        <><strong>{MapUser.UserName}</strong> assigned to Permission Group <strong>{PermissionGroup.Name}</strong></>,
      ),
    );

    return details;
  }

  public renderAccountRemovedFromPGDetails(logEvent: FDEventAccountRemovedFromPG) {
    const { MapUser, PermissionGroup } = logEvent.eventData;
    const details = [];

    details.push(
      this.renderEventDetail(
        <><strong>{MapUser.UserName}</strong> removed from Permission Group <strong>{PermissionGroup.Name}</strong></>,
      ),
    );

    return details;
  }

  public renderDirectoryCreatedDetails(logEvent: FDEventDirectoryCreated) {
    const { FileDropDirectory } = logEvent.eventData;
    const details = [];

    details.push(
      this.renderEventDetail(
        <>Directory created: <strong>{FileDropDirectory.CanonicalFileDropPath}</strong></>,
      ),
    );

    return details;
  }

  public renderDirectoryRemovedDetails(logEvent: FDEventDirectoryRemoved) {
    const { FileDropDirectory } = logEvent.eventData;
    const details = [];

    details.push(
      this.renderEventDetail(
        <>Directory removed: <strong>{FileDropDirectory.CanonicalFileDropPath}</strong></>,
      ),
    );

    return details;
  }

  public renderFileOrDirectoryRenamedDetails(logEvent: FDEventFileOrDirectoryRenamed) {
    const { Type, From, To } = logEvent.eventData;
    const details = [];

    details.push(
      this.renderEventDetail(
        <>{Type} rename: <strong>{From} &gt; {To}</strong></>,
      ),
    );

    return details;
  }

  public renderFileWriteAuthorizedDetails(logEvent: FDEventFileWriteAuthorized) {
    const { FileDropDirectory, FileName } = logEvent.eventData;
    const details = [];

    details.push(
      this.renderEventDetail(
        <>File <strong>{FileName}</strong> written to <strong>{FileDropDirectory}</strong></>,
      ),
    );

    return details;
  }

  public renderFileReadAuthorizedDetails(logEvent: FDEventFileReadAuthorized) {
    const { FileDropDirectory, FileName } = logEvent.eventData;
    const details = [];

    details.push(
      this.renderEventDetail(
        <>File <strong>{FileName}</strong> downloaded from <strong>{FileDropDirectory}</strong></>,
      ),
    );

    return details;
  }

  public renderFileDeleteAuthorizedDetails(logEvent: FDEventFileDeleteAuthorized) {
    const { FileDropDirectory, FileName } = logEvent.eventData;
    const details = [];

    details.push(
      this.renderEventDetail(
        <>File <strong>{FileName}</strong> deleted from <strong>{FileDropDirectory}</strong></>,
      ),
    );

    return details;
  }

  public renderEventDetail(eventDetail: JSX.Element) {
    return (
      <div className="event-details">
        {eventDetail}
      </div>
    );
  }
}
