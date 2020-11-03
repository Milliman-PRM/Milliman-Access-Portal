import '../../../../scss/react/shared-components/modal.scss';

import * as React from 'react';
import * as Modal from 'react-modal';

import { postData } from '../../../shared';
import { DropDown } from '../../shared-components/form/select';
import { Guid, HitrustReasonEnum } from '../../shared-components/interfaces';

export interface RemoveUserFromProfitCenterModalProps extends Modal.Props {
  profitCenterId: Guid;
  userId: Guid;
}

interface RemoveUserFromProfitCenterModalState {
  reason: HitrustReasonEnum;
}

export class RemoveUserFromProfitCenterModal
  extends React.Component<RemoveUserFromProfitCenterModalProps, RemoveUserFromProfitCenterModalState> {

  private url: string = 'SystemAdmin/RemoveUserFromProfitCenter';
  private readonly removeAuthorizedUserHitrustReasons: Array<{ selectionValue: number, selectionLabel: string }> = [
    { selectionValue: HitrustReasonEnum.EmployeeTermination, selectionLabel: 'Employee termination' },
    {
      selectionValue: HitrustReasonEnum.ChangeInEmployeeResponsibilities,
      selectionLabel: 'Change in employee responsibilities',
    },
    { selectionValue: HitrustReasonEnum.ClientRemoval, selectionLabel: 'Client removal' },
  ];

  public constructor(props: RemoveUserFromProfitCenterModalProps) {
    super(props);

    this.state = {
      reason: null,
    };

    this.handleHitrustReasonChange = this.handleHitrustReasonChange.bind(this);
    this.handleSubmit = this.handleSubmit.bind(this);
    this.resetState = this.resetState.bind(this);
  }

  public render() {
    return (
      <Modal
        ariaHideApp={false}
        {...this.props}
        className="modal"
        overlayClassName="modal-overlay"
        onRequestClose={this.resetState}
      >
        <h3 className="title red">Remove Authorized User</h3>
        <span className="modal-text">Please select a valid reason to remove this authorized user.</span>
        <form onSubmit={this.handleSubmit}>
          <DropDown
            name="reason"
            label="Reason"
            placeholderText="Choose an option..."
            values={this.removeAuthorizedUserHitrustReasons}
            value={this.state.reason as number}
            onChange={this.handleHitrustReasonChange}
            error={null}
            autoFocus={true}
          />
          <div className="button-container">
            <button
              className="link-button"
              type="button"
              onClick={() => this.props.onRequestClose(null)}
            >
              Cancel
            </button>
            <button
              className="red-button"
              type="submit"
              disabled={!this.state.reason}
            >
              Remove User
            </button>
          </div>
        </form>
      </Modal>
    );
  }

  private handleHitrustReasonChange(event: React.FormEvent<HTMLSelectElement>) {
    this.setState({
      reason: parseInt(event.currentTarget.value, 10),
    });
  }

  private handleSubmit(event: React.FormEvent<HTMLFormElement>) {
    event.preventDefault();
    postData(this.url, {
      userId: this.props.userId,
      profitCenterId: this.props.profitCenterId,
      reason: this.state.reason,
    }).then(() => {
      alert('User removed profit center.');
      this.props.onRequestClose(null);
    });
  }

  private resetState() {
    this.setState({
      reason: null,
    });
  }
}
