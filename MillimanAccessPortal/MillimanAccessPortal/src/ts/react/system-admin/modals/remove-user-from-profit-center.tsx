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
    {
      selectionValue: HitrustReasonEnum.ChangeInEmployeeResponsibilities,
      selectionLabel: 'Change in employee responsibilities',
    },
    { selectionValue: HitrustReasonEnum.ClientRemoval, selectionLabel: 'Client removal' },
    { selectionValue: HitrustReasonEnum.EmployeeTermination, selectionLabel: 'Employee termination' },
  ];

  public constructor(props: RemoveUserFromProfitCenterModalProps) {
    super(props);

    this.state = {
      reason: 0,
    };

    this.handleHitrustReasonChange = this.handleHitrustReasonChange.bind(this);
    this.handleSubmit = this.handleSubmit.bind(this);
  }

  public render() {
    return (
      <Modal
        ariaHideApp={false}
        {...this.props}
        className="modal"
        overlayClassName="modal-overlay"
        onRequestClose={() => {
          this.props.onRequestClose(null);
          this.setState({ reason: 0 });
        }}
      >
        <h3 className="title red">Remove Authorized User</h3>
        <span className="modal-text">Please select a valid reason to remove this authorized user.</span>
        <form onSubmit={this.handleSubmit}>
          <DropDown
            name="reason"
            label="Reason"
            placeholderText="Choose an option..."
            values={this.removeAuthorizedUserHitrustReasons}
            value={this.state.reason.toString()}
            onChange={this.handleHitrustReasonChange}
            error={null}
            autoFocus={true}
          />
          <div className="button-container">
            <button
              className="link-button"
              type="button"
              onClick={() => {
                this.props.onRequestClose(null);
                this.setState({ reason: 0 });
              }}
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
      alert('User removed from profit center.');
      this.props.onRequestClose(null);
    });
  }
}
