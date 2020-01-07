import '../../../../scss/react/shared-components/card.scss';

import '../../../../images/icons/checkmark.svg';
import '../../../../images/icons/error.svg';
import '../../../../images/icons/expand-card.svg';
import '../../../../images/icons/information.svg';

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
    const taskStatusMessage = isReductionTask(status)
      && (status.taskStatus === ReductionStatus.Warning
        || status.taskStatus === ReductionStatus.Error)
      && status.taskStatusMessage;

    return isActive
      || (!isPublicationRequest(status)
      && (status.taskStatus === ReductionStatus.Error || status.taskStatus === ReductionStatus.Warning))
      || (isPublicationRequest(status) && status.requestStatus === PublicationStatus.Error)
      ? (
        <div
          className={`card-status-container status-${statusValue}`}
        >
          <div className="status-top">
            {this.renderStatusTitle()}
          </div>
          <div>
            {this.renderReductionTaskStatus()}
          </div>
          <div className="status-bot">
            {this.renderStatusMessage()}
          </div>
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
      <span className="status-name">
        {statusName}&nbsp;
        <span>{this.renderQueueString()}</span>
      </span>
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
        queueString = (position > 0)
          ? `(behind ${position} other publication${s(position)})`
          : '';
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
      }
    }

    return queueString;
  }

  private renderReductionTaskStatus = () => {
    const { status } = this.props;
    if (isPublicationRequest(status)
      && status.requestStatus === PublicationStatus.Error
      && status.outcomeMetadata
      && (status.outcomeMetadata.reductionTaskFailOutcomeList.length > 0
      || status.outcomeMetadata.userMessage)
    ) {
      if (status.outcomeMetadata.reductionTaskFailOutcomeList.length > 0) {
        return status.outcomeMetadata.reductionTaskFailOutcomeList.map((x) => (
          <>
            <span className="task-status-message">
              {x.selectionGroupName
                ? <>In Selection Group <strong>{x.selectionGroupName}</strong>: </>
                : null
              } {x.userMessage}
            </span>
            <br />
          </>
        ));
      } else {
        return <span className="task-status-message">{status.outcomeMetadata.userMessage}</span>;
      }
    } else if (isReductionTask(status)
      && (status.taskStatus === ReductionStatus.Error
      || status.taskStatus === ReductionStatus.Warning)
      && status.taskStatusMessage) {
      return (
        <span className="task-status-message">{status.taskStatusMessage}</span>
      );
    } else {
      return null;
    }
  }

  private renderStatusMessage = () => {
    const { status } = this.props;
    const user = status.applicationUser;
    const initiatedBy = user ? `Initiated by ${user.firstName[0]}. ${user.lastName}` : '';

    const when = moment(status.createDateTimeUtc);

    return (
      <span className="initiated-by">
        {initiatedBy}
        {
          user
            ? <>&nbsp;</>
            : null
        }
        <span title={when.toLocaleString()}>
          {when.fromNow()}
        </span>
      </span>
    );
  }
}
