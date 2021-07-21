import '../../../../scss/react/shared-components/modal.scss';

import * as React from 'react';
import * as Modal from 'react-modal';

import { Guid } from '../../models';
import { DropDown } from '../../shared-components/form/select';
import { EnableDisabledAccountReasonEnum } from '../../shared-components/interfaces';

export interface ReenableUserModalProps extends Modal.Props {
  targetUserId: Guid;
  targetUserEmail: string;
  handleSubmit: (userId: Guid, reason: EnableDisabledAccountReasonEnum) => void;
}

interface ReenableUserModalState {
  reason: EnableDisabledAccountReasonEnum;
}

export class ReenableUserModal extends React.Component<ReenableUserModalProps, ReenableUserModalState> {
  private readonly addReenableUserReasons: Array<{ selectionValue: number, selectionLabel: string }> = [
    {
      selectionValue: EnableDisabledAccountReasonEnum.ChangeInEmployeeResponsibilities,
      selectionLabel: 'Change in employee responsibilities',
    },
    {
      selectionValue: EnableDisabledAccountReasonEnum.ReturningEmployee,
      selectionLabel: 'Returning employee',
    },
  ];

  public constructor(props: ReenableUserModalProps) {
    super(props);

    this.state = {
      reason: 0,
    };

    this.handleReenableReasonChange = this.handleReenableReasonChange.bind(this);
    this.resetState = this.resetState.bind(this);
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
          this.resetState();
        }}
      >
        <h3 className="title blue">Re-enable User</h3>
        <span className="modal-text">Reenable user account <strong>{this.props.targetUserEmail}</strong></span>
        <span className="modal-text">Please enter the same reason that the requestor used in their request.</span>
        <form
          onSubmit={(_event) => {
            this.props.handleSubmit(this.props.targetUserId, this.state.reason);
            this.props.onRequestClose(null);
          }}
        >
          <DropDown
            name="reason"
            label="Reason"
            placeholderText="Choose an option..."
            values={this.addReenableUserReasons}
            value={this.state.reason.toString()}
            onChange={this.handleReenableReasonChange}
            error={null}
          />
          <div className="button-container">
            <button
              className="link-button"
              type="button"
              onClick={() => {
                this.props.onRequestClose(null);
                this.resetState();
              }}
            >
              Cancel
            </button>
            <button
              className="blue-button"
              type="submit"
              disabled={!this.state.reason}
            >
              Add User
            </button>
          </div>
        </form>
      </Modal>
    );
  }

  private handleReenableReasonChange(event: React.FormEvent<HTMLSelectElement>) {
    this.setState({
      reason: parseInt(event.currentTarget.value, 10),
    });
  }

  private resetState() {
    this.setState({
      reason: 0,
    });
  }
}
