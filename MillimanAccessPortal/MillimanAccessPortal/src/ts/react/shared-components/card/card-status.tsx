import * as moment from 'moment';
import * as React from 'react';
import { toastr } from 'react-redux-toastr';

import {
    isPublicationActive, isReductionActive, PublicationStatus, ReductionStatus,
} from '../../../view-models/content-publishing';
import {
    isPublicationRequest, isReductionTask, PublicationWithQueueDetails, ReductionWithQueueDetails,
    User,
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
    return isActive || (!isPublicationRequest(status) && status.taskStatus === ReductionStatus.Error)
      ? (
        <div
          className={`card-status-container status-${statusValue}`}
          onClick={(event) => {
            event.stopPropagation();
            if (isReductionTask(status) && status.taskStatus === ReductionStatus.Error && status.taskStatusMessage) {
              toastr.error('', status.taskStatusMessage);
            }
          }}
        >
          <div className="status-top">{this.renderStatusTitle()}</div>
          <div className="status-bot">{this.renderStatusMessage()}</div>
        </div>
      )
      : null;
  }

  private renderStatusTitle = () => {
    const { status } = this.props;
    const statusName = isPublicationRequest(status)
      ? status.requestStatusName
      : status.taskStatusName;

    return (
      <>
        {statusName}&nbsp;
        <span>{this.renderQueueString()}</span>
      </>
    );
  }

  private renderQueueString = () => {
    const { status } = this.props;

    const s = (count: number) => count === 1 ? '' : 's';

    let queueString = '';
    if (!status.queueDetails) {
      return queueString;
    }
    if (isPublicationRequest(status)) {
      const { requestStatus, queueDetails } = status;
      if (requestStatus === PublicationStatus.Queued) {
        const { queuePosition: position } = queueDetails;
        queueString = `(behind ${position} other publication${s(position)})`;
      } else if (requestStatus === PublicationStatus.Processing) {
        const { reductionsCompleted: completed, reductionsTotal: total } = queueDetails;
        if (total > 0) {
          queueString = `(${completed}/${total} reductions completed)`;
        }
      } else if (requestStatus === PublicationStatus.Processed) {
        queueString = '(awaiting approval)';
      }
    } else {
      const { taskStatus, queueDetails } = status;
      if (taskStatus === ReductionStatus.Queued) {
        const { queuePosition: position } = queueDetails;
        queueString = `(behind ${position} other reduction${s(position)})`;
      } else if (taskStatus === ReductionStatus.Error) {
        queueString = status.taskStatusMessage
          ? '(click for details)'
          : '';
      }
    }

    return queueString;
  }

  private renderStatusMessage = () => {
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
