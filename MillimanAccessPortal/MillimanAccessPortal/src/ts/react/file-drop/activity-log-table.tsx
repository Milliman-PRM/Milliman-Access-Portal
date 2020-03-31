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
            <th className="col-name">Name</th>
            <th className="col-date">Date</th>
            <th className="col-action">Action</th>
            <th className="col-expand-icon" />
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
    return (
      <tr>
        <td/>
        <td>{logEvent.user}</td>
        <td>{this.localizeUtcTimeStamp(logEvent.timeStampUtc)}</td>
        <td>File Drop created</td>
        <td />
      </tr>
    );
  }

  public renderFileDropUpdatedRow(logEvent: FDEventUpdated) {
    return (
      <tr>
        <td />
        <td>{logEvent.user}</td>
        <td>{this.localizeUtcTimeStamp(logEvent.timeStampUtc)}</td>
        <td>File Drop updated</td>
        <td />
      </tr>
    );
  }

  public localizeUtcTimeStamp(timeStamp: string) {
    return moment(timeStamp).local().format('MM/DD/YYYY h:mm:ss A');
  }
}
