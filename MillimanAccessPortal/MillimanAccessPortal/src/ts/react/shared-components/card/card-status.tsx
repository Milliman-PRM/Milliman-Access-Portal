import '../../../../scss/react/shared-components/card.scss';

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

interface CardStatusState {
  statusMessageDisplayed: boolean;
}

export class CardStatus extends React.Component<CardStatusProps, CardStatusState> {
  constructor(props: CardStatusProps) {
    super(props);
    this.state = {
      statusMessageDisplayed: false,
    };
  }

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
          title={taskStatusMessage}
        >
          <div className="status-top">
            {this.renderStatusTitle()}
            {this.renderExpansionToggle()}
          </div>
          <div>{this.renderReductionTaskStatus()}</div>
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
      <span className="status-name">
        {statusName}&nbsp;
        <span>{this.renderQueueString()}</span>
      </span>
    );
  }

  private renderExpansionToggle = () => {
    return this.hasStatusMessages()
      ? (
        <span
          className="status-message-toggle"
          onClick={(event: React.MouseEvent) => {
            event.stopPropagation();
            this.setState({ statusMessageDisplayed: !this.state.statusMessageDisplayed });
          }}
        >
          {this.state.statusMessageDisplayed ? 'Show Less' : 'Show More'}
        </span>
      )
      : null;
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

  private hasStatusMessages = () => {
    const { status } = this.props;
    return isPublicationRequest(status)
      && status.outcomeMetadata
      && (status.outcomeMetadata.reductionTaskSuccessOutcomeList.length > 0
        || status.outcomeMetadata.reductionTaskFailOutcomeList.length > 0
      );
  }

  private renderReductionTaskStatus = () => {
    const { status } = this.props;
    if (this.state.statusMessageDisplayed && this.hasStatusMessages()) {
      if (isPublicationRequest(status) && status.outcomeMetadata) {
        const taskList = status.outcomeMetadata.reductionTaskSuccessOutcomeList.concat(
          status.outcomeMetadata.reductionTaskFailOutcomeList,
        ).sort((a, b) => (a.processingStartedUtc > b.processingStartedUtc) ? 1 : -1);
        const taskListTable = taskList.filter((x) => x.selectionGroupName !== null).map((x) => (
          <>
            <tr>
              <td>{'Success'}</td>
              <td>
                <span className="selection-group-name">{x.selectionGroupName}</span>
                <p className="task-status-description">{x.userMessage}</p>
              </td>
            </tr>
          </>
        ));
        return (
          <table className="task-status-list">
            <thead>
              <tr>
                <th>Status</th>
                <th>Details</th>
              </tr>
            </thead>
            <tbody>
              {taskListTable}
            </tbody>
          </table>
        );
      }
      if (isReductionTask(status) && status.taskStatusMessage) {
        return null;
      }
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
      <>
        {initiatedBy}
        {
          user
            ? <>&nbsp;</>
            : null
        }
        <span title={when.toLocaleString()}>
          {when.fromNow()}
        </span>
      </>
    );
  }
}
