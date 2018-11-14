import * as React from 'react';

import { isPublicationActive, isReductionActive } from '../../../view-models/content-publishing';
import {
  isPublicationRequest, PublicationWithQueueDetails, ReductionWithQueueDetails,
} from '../../models';

export interface CardStatusProps {
  status: PublicationWithQueueDetails | ReductionWithQueueDetails;
}
export class CardStatus extends React.Component<CardStatusProps> {
  public render() {
    const { status } = this.props;
    const [statusValue, isActive] = isPublicationRequest(status)
      ? [status.requestStatus, isPublicationActive(status.requestStatus)]
      : [status.taskStatus, isReductionActive(status.taskStatus)];
    return isActive
      ? (
        <div className={`card-status-container status-${statusValue}`}>
          <span className="status-top">{statusValue}</span>
          <span className="status-bot">
            Initiated by {status.applicationUserId} {status.queueDetails.queuedDurationMs}ms ago
          </span>
        </div>
      )
      : null;
  }
}
