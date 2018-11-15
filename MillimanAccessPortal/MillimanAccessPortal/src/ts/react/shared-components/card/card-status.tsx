import * as moment from 'moment';
import * as React from 'react';

import { isPublicationActive, isReductionActive } from '../../../view-models/content-publishing';
import {
  isPublicationRequest, PublicationWithQueueDetails, ReductionWithQueueDetails, User,
} from '../../models';

export interface CardStatusProps {
  status: PublicationWithQueueDetails | ReductionWithQueueDetails;
}
export class CardStatus extends React.Component<CardStatusProps> {
  public render() {
    const { status } = this.props;
    const [statusValue, statusName, isActive] = isPublicationRequest(status)
      ? [status.requestStatus, status.requestStatusName, isPublicationActive(status.requestStatus)]
      : [status.taskStatus, status.taskStatusName, isReductionActive(status.taskStatus)];
    return isActive
      ? (
        <div className={`card-status-container status-${statusValue}`}>
          <div className="status-top">{statusName}</div>
          <div className="status-bot">
            {this.renderStatusMessage()}
          </div>
        </div>
      )
      : null;
  }

  private renderStatusMessage() {
    const { status } = this.props;
    const user = status.applicationUser;
    const userAbbreviation = `${user.firstName[0]}. ${user.lastName}`;

    const when = moment(status.createDateTimeUtc);

    return (
      <>
        Initiated by {userAbbreviation}&nbsp;
        <span title={when.toLocaleString()}>
          {when.fromNow()}
        </span>
      </>
    );
  }
}
