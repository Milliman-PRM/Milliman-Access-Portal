import '../../../scss/react/shared-components/form-elements.scss';

import * as React from 'react';
import { connect } from 'react-redux';
import ReduxToastr from 'react-redux-toastr';

import { BrowserSupportBanner } from '../shared-components/browser-support-banner';
import { Input } from '../shared-components/form/input';
import { NavBar } from '../shared-components/navbar';
import * as AccountActionCreators from './redux/action-creators';
import { UpdateAccount } from './redux/actions';
import {
    allPasswordInputsValid, allUserInputsValid,
    anyUserInputModified, inputProps, updateProps, validProps,
} from './redux/selectors';
import { AccountState, ValidationState } from './redux/store';

interface AccountSettingsProps {
  inputs: {
    userName: string;
    firstName: string;
    lastName: string;
    phone: string;
    employer: string;
  };
  valid: {
    firstName: ValidationState;
    lastName: ValidationState;
    phone: ValidationState;
    employer: ValidationState;
  };
  updateData: UpdateAccount['request'];
  isLocal: boolean;
  discardButtonEnabled: boolean;
  submitButtonEnabled: boolean;
}
class AccountSettings extends React.Component<AccountSettingsProps & typeof AccountActionCreators> {
  public componentDidMount() {
    this.props.fetchUser({});
    this.props.scheduleSessionCheck({ delay: 0 });
  }

  public render() {
    return (
      <>
        <ReduxToastr
          timeOut={5000}
          newestOnTop={false}
          position="bottom-center"
          transitionIn="fadeIn"
          transitionOut="fadeOut"
        />
        <NavBar currentView="AccountSettings" />
        <BrowserSupportBanner />
        {this.renderAccountSettingsForm()}
      </>
    );
  }

  private renderAccountSettingsForm() {
    return (
      <div className="form-content-container flex-item-for-tablet-up-10-12 flex-item-for-desktop-up-6-12">
        <form autoComplete="off" className="admin-panel-content">
          {this.renderInformationSection()}
          {this.renderSubmissionSection()}
        </form>
      </div>
    );
  }

  private renderInformationSection() {
    const { userName, firstName, lastName, phone, employer } = this.props.inputs;
    return (
      <>
        <div className="form-section" data-section="username">
          <h3 className="form-section-title">User Information</h3>
          <Input
            name="userName"
            label="Username"
            type="text"
            value={userName}
            onChange={() => { return; }}
            onBlur={() => { return; }}
            error={null}
            readOnly={true}
          />
        </div>
        <div className="form-section" data-section="account">
          <Input
            name="firstName"
            label="First"
            type="text"
            value={firstName}
            onChange={({ currentTarget: target }: React.FormEvent<HTMLInputElement>) => {
              this.props.setPendingTextInputValue({
                inputName: 'firstName',
                value: target.value,
              });
              this.props.validateInputUser({
                value: {
                  ...this.props.inputs,
                  firstName: target.value,
                },
                inputName: 'firstName',
              });
            }}
            autoFocus={true}
            onBlur={() => { return; }}
            error={this.props.valid.firstName.valid ? null : this.props.valid.firstName.message}
          />
          <Input
            name="lastName"
            label="Last"
            type="text"
            value={lastName}
            onChange={({ currentTarget: target }: React.FormEvent<HTMLInputElement>) => {
              this.props.setPendingTextInputValue({
                inputName: 'lastName',
                value: target.value,
              });
              this.props.validateInputUser({
                value: {
                  ...this.props.inputs,
                  lastName: target.value,
                },
                inputName: 'lastName',
              });
            }}
            onBlur={() => { return; }}
            error={this.props.valid.lastName.valid ? null : this.props.valid.lastName.message}
          />
          <Input
            name="phone"
            label="Phone Number"
            type="phone"
            value={phone}
            onChange={({ currentTarget: target }: React.FormEvent<HTMLInputElement>) => {
              this.props.setPendingTextInputValue({
                inputName: 'phone',
                value: target.value,
              });
              this.props.validateInputUser({
                value: {
                  ...this.props.inputs,
                  phone: target.value,
                },
                inputName: 'phone',
              });
            }}
            onBlur={() => { return; }}
            error={this.props.valid.phone.valid ? null : this.props.valid.phone.message}
          />
          <Input
            name="employer"
            label="Employer"
            type="text"
            value={employer}
            onChange={({ currentTarget: target }: React.FormEvent<HTMLInputElement>) => {
              this.props.setPendingTextInputValue({
                inputName: 'employer',
                value: target.value,
              });
              this.props.validateInputUser({
                value: {
                  ...this.props.inputs,
                  employer: target.value,
                },
                inputName: 'employer',
              });
            }}
            onBlur={() => { return; }}
            error={this.props.valid.employer.valid ? null : this.props.valid.employer.message}
          />
        </div>
      </>
    );
  }

  private renderSubmissionSection() {
    const { submitButtonEnabled } = this.props;
    return (
      <div className="form-submission-section">
        <div className="button-container button-container-update">
          {this.renderPasswordResetButton()}
          {this.renderResetButton()}
          <button
            type="submit"
            className="button-submit blue-button"
            disabled={!submitButtonEnabled}
            onClick={(event: React.FormEvent) => {
              event.preventDefault();
              if (submitButtonEnabled) {
                this.props.updateAccount(this.props.updateData);
              }
            }}
          >
            Update Account
          </button>
        </div>
      </div>
    );
  }

  private renderPasswordResetButton() {
    const { isLocal } = this.props;
    return isLocal ?
      (
        <button
          className="button-left link-button"
          onClick={(event: React.FormEvent) => {
            event.preventDefault();
            this.props.requestPasswordReset({});
          }}
        >
          Update Password
        </button>
      )
      : null;
  }

  private renderResetButton() {
    const { discardButtonEnabled } = this.props;
    return discardButtonEnabled ? (
      <button
        type="button"
        className="button-reset link-button"
        onClick={() => this.props.resetForm({})}
      >
        Discard Changes
      </button>
    ) : null;
  }
}

function mapStateToProps(state: AccountState): AccountSettingsProps {
  return {
    inputs: inputProps(state),
    valid: validProps(state),
    updateData: updateProps(state),
    discardButtonEnabled: anyUserInputModified(state),
    submitButtonEnabled: (anyUserInputModified(state) &&
      (anyUserInputModified(state) ? allUserInputsValid(state) : true)),
    isLocal: state.data.user.isLocal,
  };
}

export const ConnectedAccountSettings = connect(
  mapStateToProps,
  AccountActionCreators,
)(AccountSettings);
