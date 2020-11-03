import '../../../../scss/react/shared-components/modal.scss';

import * as React from 'react';
import * as Modal from 'react-modal';

import { isEmailAddressValid, postData } from '../../../shared';
import { Input } from '../../shared-components/form/input';
import { DropDown } from '../../shared-components/form/select';
import { Guid, HitrustReasonEnum } from '../../shared-components/interfaces';

export interface AddUserToProfitCenterModalProps extends Modal.Props {
  profitCenterId: Guid;
}

interface AddUserToProfitCenterModalState {
  userText: string;
  reason: HitrustReasonEnum;
  emailError: boolean;
}

export class AddUserToProfitCenterModal
    extends React.Component<AddUserToProfitCenterModalProps, AddUserToProfitCenterModalState> {

  private url: string = 'SystemAdmin/AddUserToProfitCenter';
  private readonly addAuthorizedUserHitrustReasons: Array<{ selectionValue: number, selectionLabel: string }> = [
    { selectionValue: HitrustReasonEnum.NewMapClient, selectionLabel: 'New MAP Client' },
    {
      selectionValue: HitrustReasonEnum.ChangeInEmployeeResponsibilities,
      selectionLabel: 'Change in employee responsibilities',
    },
  ];

  public constructor(props: AddUserToProfitCenterModalProps) {
    super(props);

    this.state = {
      userText: '',
      reason: null,
      emailError: false,
    };

    this.handleChange = this.handleChange.bind(this);
    this.handleEmailBlur = this.handleEmailBlur.bind(this);
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
        <h3 className="title blue">Add Authorized User</h3>
        <span className="modal-text">User to add:</span>
        <form onSubmit={this.handleSubmit}>
          <Input
            name="email"
            label="Email address"
            value={this.state.userText}
            type="text"
            placeholderText="Email address"
            onChange={this.handleChange}
            onBlur={this.handleEmailBlur}
            error={this.state.emailError ? 'Please enter a valid email address.' : null}
          />
          <DropDown
            name="reason"
            label="Reason"
            placeholderText="Choose an option..."
            values={this.addAuthorizedUserHitrustReasons}
            value={this.state.reason}
            onChange={this.handleHitrustReasonChange}
            error={null}
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
              className="blue-button"
              type="submit"
              disabled={
                !this.state.userText.trim()
                || !isEmailAddressValid(this.state.userText)
                || this.state.emailError
                || !this.state.reason
              }
            >
              Add User
            </button>
          </div>
        </form>
      </Modal>
    );
  }

  private handleChange(event: React.ChangeEvent<HTMLInputElement>) {
    this.setState({
      userText: event.target.value,
      emailError: false,
    });
  }

  private handleEmailBlur() {
    this.setState({
      emailError: !this.state.userText.trim() || !isEmailAddressValid(this.state.userText),
    });
  }

  private handleHitrustReasonChange(event: React.FormEvent<HTMLSelectElement>) {
    this.setState({
      reason: parseInt(event.currentTarget.value, 10),
    });
  }

  private handleSubmit(event: React.FormEvent<HTMLFormElement>) {
    event.preventDefault();
    postData(this.url, {
      email: this.state.userText,
      profitCenterId: this.props.profitCenterId,
      reason: this.state.reason,
    }).then(() => {
      alert('User added to profit center.');
      this.props.onRequestClose(null);
    });
  }

  private resetState() {
    this.setState({
      userText: '',
      reason: null,
      emailError: false,
    });
  }
}
