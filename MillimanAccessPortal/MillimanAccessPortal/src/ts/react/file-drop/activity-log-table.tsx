import '../../../images/icons/add-group.svg';
import '../../../images/icons/add-user.svg';
import '../../../images/icons/group.svg';
import '../../../images/icons/user.svg';

import '../../../scss/react/file-drop/activity-log-table.scss';

import * as moment from 'moment';
import * as React from 'react';

import {
  FileDropEvent,
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
                <td className="date-width">
                  <span title={moment(logEvent.timeStampUtc).local().format('MM/DD/YYYY h:mm:ss A')}>
                    {
                      moment(logEvent.timeStampUtc).local().format('M/D/YY \nh:mmA')
                    }
                  </span>
                </td>
                <td className="name-max-width">
                  <span
                    title={logEvent.fullName}
                  >
                    {logEvent.fullName}
                  </span>
                  <br />
                  <span
                    className="username"
                    title={logEvent.userName}
                  >
                    {logEvent.userName}
                  </span>
                </td>
                <td>{logEvent.eventType}</td>
                <td>{logEvent.description}
                </td>
              </tr>
            ))
          }
        </tbody>
      </table>
    );
  }

  public renderEventDetail(eventDetail: JSX.Element) {
    return (
      <div className="event-details">
        {eventDetail}
      </div>
    );
  }
}
