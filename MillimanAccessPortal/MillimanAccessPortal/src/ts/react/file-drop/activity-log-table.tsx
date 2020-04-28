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
            <th className="col-group-action-icon" />
            <th className="col-action">Action</th>
            <th className="col-name">Name</th>
            <th className="col-date">Date</th>
          </tr>
        </thead>
        <tbody>
        {
          this.props.activityLogData.map((logE) => {
            switch (logE.eventCode) {
              case FileDropLogEventEnum.FDCreated:
                return this.renderFileDropCreatedRow(logE);
                break;
              case FileDropLogEventEnum.FDUpdated:
                return this.renderFileDropUpdatedRow(logE);
                break;
              case FileDropLogEventEnum.PGCreated:
                return this.renderPermissionGroupCreatedRow(logE);
                break;
              case FileDropLogEventEnum.PGDeleted:
                return this.renderPermissionGroupDeletedRow(logE);
                break;
              case FileDropLogEventEnum.PGUpdated:
                return this.renderPermissionGroupUpdatedRow(logE);
                break;
              case FileDropLogEventEnum.AccountCreated:
                return this.renderAccountCreatedRow(logE);
                break;
              case FileDropLogEventEnum.AccountAddedToPG:
                return this.renderAccountAddedToPGRow(logE);
                break;
              case FileDropLogEventEnum.AccountRemovedFromPG:
                return this.renderAccountRemovedFromPGRow(logE);
                break;
              case FileDropLogEventEnum.DirectoryCreated:
                return this.renderDirectoryCreatedRow(logE);
                break;
              case FileDropLogEventEnum.DirectoryRemoved:
                return this.renderDirectoryRemovedRow(logE);
                break;
              case FileDropLogEventEnum.FileOrDirectoryRenamed:
                return this.renderFileOrDirectoryRenamedRow(logE);
                break;
              case FileDropLogEventEnum.FileWriteAuthorized:
                return this.renderFileWriteAuthorizedRow(logE);
                break;
              case FileDropLogEventEnum.FileReadAuthorized:
                return this.renderFileReadAuthorizedRow(logE);
                break;
              case FileDropLogEventEnum.FileDeleteAuthorized:
                return this.renderFileDeleteAuthorizedRow(logE);
                break;
              default:
                return null;
            }
          })
        }
        </tbody>
      </table>
    );
  }

  // Render rows by Log Event type
  public renderFileDropCreatedRow(logEvent: FDEventFDCreated) {
    const { FileDrop } = logEvent.eventData;
    const details = [];

    details.push(this.renderEventDetailRow(<>File Drop name: <strong>{FileDrop.Name}</strong></>));
    if (FileDrop.Description) {
      details.push(this.renderEventDetailRow(<>File Drop description: <strong>{FileDrop.Description}</strong></>));
    }
    details.push(this.renderSpacer());

    return (
      <>
        {this.renderEventRow(logEvent)}
        {details}
      </>
    );
  }

  public renderFileDropUpdatedRow(logEvent: FDEventFDUpdated) {
    const { OldFileDrop, NewFileDrop } = logEvent.eventData;
    const details = [];

    if (OldFileDrop.Name !== NewFileDrop.Name) {
      details.push(this.renderEventDetailRow(<>File Drop name changed to <strong>{NewFileDrop.Name}</strong></>));
    }
    if (OldFileDrop.Description !== NewFileDrop.Description) {
      details.push(
        this.renderEventDetailRow(<>File Drop description changed to <strong>{NewFileDrop.Description}</strong></>),
      );
    }
    details.push(this.renderSpacer());

    return (
      <>
        {this.renderEventRow(logEvent)}
        {details}
      </>
    );
  }

  public renderPermissionGroupCreatedRow(logEvent: FDEventPGCreated) {
    const { PermissionGroup } = logEvent.eventData;
    const details = [];

    if (PermissionGroup.IsPersonalGroup) {
      details.push(this.renderEventDetailRow(<>User: <strong>{PermissionGroup.Name}</strong></>));
    } else {
      details.push(this.renderEventDetailRow(<>Permission Group name: <strong>{PermissionGroup.Name}</strong></>));
    }
    if (PermissionGroup.ReadAccess) {
      details.push(this.renderEventDetailRow(<>Read Access Granted</>));
    }
    if (PermissionGroup.WriteAccess) {
      details.push(this.renderEventDetailRow(<>Write Access Granted</>));
    }
    if (PermissionGroup.DeleteAccess) {
      details.push(this.renderEventDetailRow(<>Delete Access Granted</>));
    }
    details.push(this.renderSpacer());

    return (
      <>
        {this.renderEventRow(logEvent)}
        {details}
      </>
    );
  }

  public renderPermissionGroupDeletedRow(logEvent: FDEventPGDeleted) {
    const { PermissionGroup } = logEvent.eventData;
    const details = [];

    if (PermissionGroup.IsPersonalGroup) {
      details.push(this.renderEventDetailRow(<><strong>{PermissionGroup.Name}</strong> personal group deleted</>));
    } else {
      details.push(this.renderEventDetailRow(<>Permission Group <strong>{PermissionGroup.Name}</strong> deleted</>));
    }
    details.push(this.renderSpacer());

    return (
      <>
        {this.renderEventRow(logEvent)}
        {details}
      </>
    );
  }

  public renderPermissionGroupUpdatedRow(logEvent: FDEventPGUpdated) {
    const { PermissionGroup, PreviousProperties, UpdatedProperties } = logEvent.eventData;
    const details = [];

    if (!PermissionGroup.IsPersonalGroup && UpdatedProperties.Name !== PreviousProperties.Name) {
      details.push(
        this.renderEventDetailRow(
          <>
            <strong>{PreviousProperties.Name}</strong> renamed to <strong>{UpdatedProperties.Name}</strong>
          </>,
        ),
      );
    }
    if (UpdatedProperties.ReadAccess && !PreviousProperties.ReadAccess) {
      details.push(this.renderEventDetailRow(<>Read Access Granted</>));
    }
    if (UpdatedProperties.WriteAccess && !PreviousProperties.WriteAccess) {
      details.push(this.renderEventDetailRow(<>Write Access Granted</>));
    }
    if (UpdatedProperties.DeleteAccess && !PreviousProperties.DeleteAccess) {
      details.push(this.renderEventDetailRow(<>Delete Access Granted</>));
    }
    if (!UpdatedProperties.ReadAccess && PreviousProperties.ReadAccess) {
      details.push(this.renderEventDetailRow(<>Read Access Revoked</>));
    }
    if (!UpdatedProperties.WriteAccess && PreviousProperties.WriteAccess) {
      details.push(this.renderEventDetailRow(<>Write Access Revoked</>));
    }
    if (!UpdatedProperties.DeleteAccess && PreviousProperties.DeleteAccess) {
      details.push(this.renderEventDetailRow(<>Delete Access Revoked</>));
    }
    details.push(this.renderSpacer());

    return (
      <>
        {this.renderEventRow(logEvent)}
        {details}
      </>
    );
  }

  public renderAccountCreatedRow(logEvent: FDEventAccountCreated) {
    const { MapUser } = logEvent.eventData;
    const details = [];

    details.push(this.renderEventDetailRow(<>SFTP Account Created for <strong>{MapUser.UserName}</strong></>));
    details.push(this.renderSpacer());

    return (
      <>
        {this.renderEventRow(logEvent)}
        {details}
      </>
    );
  }

  public renderAccountAddedToPGRow(logEvent: FDEventAccountAddedToPG) {
    const { MapUser, PermissionGroup } = logEvent.eventData;
    const details = [];

    details.push(
      this.renderEventDetailRow(
        <><strong>{MapUser.UserName}</strong> assigned to Permission Group <strong>{PermissionGroup.Name}</strong></>,
      ),
    );
    details.push(this.renderSpacer());

    return (
      <>
        {this.renderEventRow(logEvent)}
        {details}
      </>
    );
  }

  public renderAccountRemovedFromPGRow(logEvent: FDEventAccountRemovedFromPG) {
    const { MapUser, PermissionGroup } = logEvent.eventData;
    const details = [];

    details.push(
      this.renderEventDetailRow(
        <><strong>{MapUser.UserName}</strong> removed from Permission Group <strong>{PermissionGroup.Name}</strong></>,
      ),
    );
    details.push(this.renderSpacer());

    return (
      <>
        {this.renderEventRow(logEvent)}
        {details}
      </>
    );
  }

  public renderDirectoryCreatedRow(logEvent: FDEventDirectoryCreated) {
    const { FileDropDirectory } = logEvent.eventData;
    const details = [];

    details.push(
      this.renderEventDetailRow(
        <>Directory created: <strong>{FileDropDirectory.CanonicalFileDropPath}</strong></>,
      ),
    );
    details.push(this.renderSpacer());

    return (
      <>
        {this.renderEventRow(logEvent)}
        {details}
      </>
    );
  }

  public renderDirectoryRemovedRow(logEvent: FDEventDirectoryRemoved) {
    const { FileDropDirectory } = logEvent.eventData;
    const details = [];

    details.push(
      this.renderEventDetailRow(
        <>Directory removed: <strong>{FileDropDirectory.CanonicalFileDropPath}</strong></>,
      ),
    );
    details.push(this.renderSpacer());

    return (
      <>
        {this.renderEventRow(logEvent)}
        {details}
      </>
    );
  }

  public renderFileOrDirectoryRenamedRow(logEvent: FDEventFileOrDirectoryRenamed) {
    const { Type, From, To } = logEvent.eventData;
    const details = [];

    details.push(
      this.renderEventDetailRow(
        <>{Type} rename: <strong>{From} &gt; {To}</strong></>,
      ),
    );
    details.push(this.renderSpacer());

    return (
      <>
        {this.renderEventRow(logEvent)}
        {details}
      </>
    );
  }

  public renderFileWriteAuthorizedRow(logEvent: FDEventFileWriteAuthorized) {
    const { FileDropDirectory, FileName } = logEvent.eventData;
    const details = [];

    details.push(
      this.renderEventDetailRow(
        <>File <strong>{FileName}</strong> written to <strong>{FileDropDirectory}</strong></>,
      ),
    );
    details.push(this.renderSpacer());

    return (
      <>
        {this.renderEventRow(logEvent)}
        {details}
      </>
    );
  }

  public renderFileReadAuthorizedRow(logEvent: FDEventFileReadAuthorized) {
    const { FileDropDirectory, FileName } = logEvent.eventData;
    const details = [];

    details.push(
      this.renderEventDetailRow(
        <>File <strong>{FileName}</strong> downloaded from <strong>{FileDropDirectory}</strong></>,
      ),
    );
    details.push(this.renderSpacer());

    return (
      <>
        {this.renderEventRow(logEvent)}
        {details}
      </>
    );
  }

  public renderFileDeleteAuthorizedRow(logEvent: FDEventFileDeleteAuthorized) {
    const { FileDropDirectory, FileName } = logEvent.eventData;
    const details = [];

    details.push(
      this.renderEventDetailRow(
        <>File <strong>{FileName}</strong> deleted from <strong>{FileDropDirectory}</strong></>,
      ),
    );
    details.push(this.renderSpacer());

    return (
      <>
        {this.renderEventRow(logEvent)}
        {details}
      </>
    );
  }

  public renderEventRow(logEvent: FileDropEvent) {
    return (
      <tr className="event-row">
        <td />
        <td><strong>{logEvent.eventType}</strong></td>
        <td>
          <strong>{logEvent.fullName}</strong><br />
          <span className="username">{logEvent.userName}</span>
        </td>
        <td>
          {
            this.localizeUtcTimeStamp(logEvent.timeStampUtc)
          }
        </td>
      </tr>
    );
  }

  public renderEventDetailRow(eventDetail: JSX.Element) {
    return (
      <tr className="event-details">
        <td colSpan={4}>
          {eventDetail}
        </td>
      </tr>
    );
  }

  public renderSpacer() {
    return (
      <tr className="spacer" />
    );
  }

  public localizeUtcTimeStamp(timeStamp: string) {
    return (
      <span title={moment(timeStamp).local().format('MM/DD/YYYY h:mm:ss A')}>
        {
          moment(timeStamp).local().format('M/D/YY h:mm A')
        }
      </span>
    );
  }
}
