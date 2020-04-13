import '../../../images/icons/add-group.svg';
import '../../../images/icons/add-user.svg';
import '../../../images/icons/group.svg';
import '../../../images/icons/user.svg';

import '../../../scss/react/file-drop/activity-log-table.scss';

import * as moment from 'moment';
import * as React from 'react';

import {
  FDEventCreated, FDEventUpdated, FileDropEvent, FileDropLogEventEnum,
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
              case FileDropLogEventEnum.Created:
                return this.renderFileDropCreatedRow(logE);
                break;
              case FileDropLogEventEnum.Updated:
                return this.renderFileDropUpdatedRow(logE);
                break;
            }
          })
        }
        </tbody>
      </table>
    );
  }

  // Render rows by Log Event type
  public renderFileDropCreatedRow(logEvent: FDEventCreated) {
    const { FileDrop } = logEvent.eventDataObject;
    const details = [];
    details.push((
      <tr className="event-details">
        <td colSpan={4}>
          File Drop name: <strong>{FileDrop.Name}</strong>
        </td>
      </tr>
    ));
    if (FileDrop.Description) {
      details.push((
        <tr className="event-details">
          <td colSpan={4}>
            File Drop description: <strong>{FileDrop.Description}</strong>
          </td>
        </tr>
      ));
    }
    details.push((
      <tr className="spacer" />
    ));
    return (
      <>
        <tr className="event-row">
          <td/>
          <td><strong>File Drop created</strong></td>
          <td>{logEvent.user}</td>
          <td>{this.localizeUtcTimeStamp(logEvent.timeStampUtc)}</td>
        </tr>
        {details}
      </>
    );
  }

  public renderFileDropUpdatedRow(logEvent: FDEventUpdated) {
    const { OldFileDrop, NewFileDrop } = logEvent.eventDataObject;
    const details = [];
    if (OldFileDrop.Name !== NewFileDrop.Name) {
      details.push((
        <tr className="event-details">
          <td colSpan={4}>
            File Drop name changed to <strong>{NewFileDrop.Name}</strong>
          </td>
        </tr>
      ));
    }
    if (OldFileDrop.Description !== NewFileDrop.Description) {
      details.push((
        <tr className="event-details">
          <td colSpan={4}>
            File Drop description changed to <strong>{NewFileDrop.Description}</strong>
          </td>
        </tr>
      ));
    }
    details.push((
      <tr className="spacer" />
    ));
    return (
      <>
        <tr className="event-row">
          <td />
          <td><strong>File Drop updated</strong></td>
          <td>{logEvent.user}</td>
          <td>
              {
                this.localizeUtcTimeStamp(logEvent.timeStampUtc)
              }
          </td>
        </tr>
        {details}
      </>
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
