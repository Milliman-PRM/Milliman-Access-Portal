import * as React from 'react';
import { connect } from 'react-redux';
import ReduxToastr from 'react-redux-toastr';

import { NavBar } from '../shared-components/navbar';
import * as AccountActionCreators from './redux/action-creators';
import { UpdateAccount } from './redux/actions';
import {
    allPasswordInputsModified, allPasswordInputsValid, allUserInputsValid, anyPasswordInputModified,
    anyUserInputModified, inputProps, updateProps, validProps,
} from './redux/selectors';
import { AccountState, ValidationState } from './redux/store';

interface AccountSettingsProps {
  inputs: {
    username: string;
    firstName: string;
    lastName: string;
    phone: string;
    employer: string;
    currentPassword: string;
    newPassword: string;
    confirmPassword: string;
  };
  valid: {
    firstName: ValidationState;
    lastName: ValidationState;
    phone: ValidationState;
    employer: ValidationState;
    currentPassword: ValidationState;
    newPassword: ValidationState;
    confirmPassword: ValidationState;
  };
  updateData: UpdateAccount['request'];
  isLocal: boolean;
  anyPasswordInputModified: boolean;
  discardButtonEnabled: boolean;
  submitButtonEnabled: boolean;
}
class AccountSettings extends React.Component<AccountSettingsProps & typeof AccountActionCreators> {
  public componentDidMount() {
    this.props.fetchUser({});
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
        {this.renderAccountSettingsForm()}
      </>
    );
  }

  private renderAccountSettingsForm() {
    return (
      <div id="account-settings-container">
        <div className="admin-panel-container flex-item-for-tablet-up-10-12 flex-item-for-desktop-up-5-12">
          {/* <ColumnSpinner /> */}
          <div className="admin-panel-content-container">
            <form autoComplete="off" className="admin-panel-content">
              <div className="form-section-container">
                {this.renderInformationSection()}
                {this.renderPasswordSection()}
                {this.renderSubmissionSection()}
              </div>
            </form>
          </div>
        </div>
      </div>
    );
  }

  private renderInformationSection() {
    const { username, firstName, lastName, phone, employer } = this.props.inputs;
    return (
      <>
        <div className="form-section" data-section="username">
          <h4 className="form-section-title">User Information</h4>
          <div className="form-input-container" style={{ marginBottom: 0 }}>
            <div className="form-input htmlForm-input-text flex-item-12-12">
              <label className="form-input-text-title" htmlFor="username">Username</label>
              <div>
                <input
                  type="text"
                  name="username"
                  value={username}
                  readOnly={true}
                />
              </div>
            </div>
          </div>
        </div>
        <div className="form-section" data-section="account">
          <div className="form-input-container">
            <div className="form-input form-input-text flex-item-for-tablet-up-6-12">
              <label className="form-input-text-title" htmlFor="firstName">First Name</label>
              <div>
                <input
                  type="text"
                  name="firstName"
                  value={firstName}
                  onChange={({ target }) => {
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
                />
                {this.props.valid.firstName.valid
                ? null
                : (
                  <span className="text-danger field-validation-valid">
                    {this.props.valid.firstName.message}
                  </span>
                )}
              </div>
            </div>
            <div className="form-input form-input-text flex-item-for-tablet-up-6-12">
              <label className="form-input-text-title" htmlFor="lastName">Last Name</label>
              <div>
                <input
                  type="text"
                  name="lastName"
                  value={lastName}
                  onChange={({ target }) => {
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
                />
                {this.props.valid.lastName.valid
                ? null
                : (
                  <span className="text-danger field-validation-valid">
                    {this.props.valid.lastName.message}
                  </span>
                )}
              </div>
            </div>
            <div className="form-input form-input-text flex-item-for-tablet-up-4-12">
              <label className="form-input-text-title" htmlFor="phone">Phone Number</label>
              <div>
                <input
                  placeholder="(###) ###-####"
                  type="tel"
                  name="phone"
                  value={phone}
                  onChange={({ target }) => {
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
                />
                <span className="text-danger field-validation-valid" />
              </div>
            </div>
            <div className="form-input form-input-text flex-item-for-tablet-up-8-12">
              <label className="form-input-text-title" htmlFor="employer">Employer</label>
              <div>
                <input
                  type="text"
                  name="employer"
                  value={employer}
                  onChange={({ target }) => {
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
                />
                <span className="text-danger field-validation-valid" />
              </div>
            </div>
          </div>
        </div>
      </>
    );
  }

  private renderPasswordSection() {
    const { isLocal } = this.props;
    const { currentPassword, newPassword, confirmPassword } = this.props.inputs;
    return isLocal
    ? (
      <div className="form-section">
        <h4 className="form-section-title">Update Password</h4>
        <div className="form-input-container">
          <div className="form-input htmlForm-input-text flex-item-12-12">
            <label className="form-input-text-title" htmlFor="currentPassword">Current Password</label>
            <div>
              <input
                type="password"
                name="currentPassword"
                value={currentPassword}
                onChange={({ target }) => {
                  this.props.setPendingTextInputValue({
                    inputName: 'current',
                    value: target.value,
                  });
                  this.props.validateInputPassword({
                    value: {
                      current: target.value,
                      new: this.props.inputs.newPassword,
                      confirm: this.props.inputs.confirmPassword,
                    },
                    inputName: 'current',
                  });
                }}
              />
              {this.props.valid.currentPassword.valid || !this.props.anyPasswordInputModified
              ? null
              : (
                <span className="text-danger field-validation-valid">
                  {this.props.valid.currentPassword.message}
                </span>
              )}
            </div>
          </div>
          <div className="form-input htmlForm-input-text flex-item-12-12">
            <label className="form-input-text-title" htmlFor="newPassword">New Password</label>
            <div>
              <input
                type="password"
                name="newPassword"
                value={newPassword}
                onChange={({ target }) => {
                  this.props.setPendingTextInputValue({
                    inputName: 'new',
                    value: target.value,
                  });
                  this.props.validateInputPassword({
                    value: {
                      current: this.props.inputs.currentPassword,
                      new: target.value,
                      confirm: this.props.inputs.confirmPassword,
                    },
                    inputName: 'new',
                  });
                }}
              />
              {this.props.valid.newPassword.valid || !this.props.anyPasswordInputModified
              ? null
              : (
                <span className="text-danger field-validation-valid">
                  {this.props.valid.newPassword.message}
                </span>
              )}
            </div>
          </div>
          <div className="form-input htmlForm-input-text flex-item-12-12">
            <label className="form-input-text-title" htmlFor="confirmPassword">Confirm password</label>
            <div>
              <input
                type="password"
                name="confirmPassword"
                value={confirmPassword}
                onChange={({ target }) => {
                  this.props.setPendingTextInputValue({
                    inputName: 'confirm',
                    value: target.value,
                  });
                  this.props.validateInputPassword({
                    value: {
                      current: this.props.inputs.currentPassword,
                      new: this.props.inputs.newPassword,
                      confirm: target.value,
                    },
                    inputName: 'confirm',
                  });
                }}
              />
              {this.props.valid.confirmPassword.valid || !this.props.anyPasswordInputModified
              ? null
              : (
                <span className="text-danger field-validation-valid">
                  {this.props.valid.confirmPassword.message}
                </span>
              )}
            </div>
          </div>
        </div>
      </div>
    )
    : null;
  }

  private renderSubmissionSection() {
    const { submitButtonEnabled } = this.props;
    return (
      <div className="form-submission-section">
        <div className="button-container button-container-update">
          {this.renderResetButton()}
          <button
            type="button"
            className={`button-submit blue-button${submitButtonEnabled ? '' : ' disabled'}`}
            onClick={() => this.props.updateAccount(this.props.updateData)}
          >
            Update Account
          </button>
        </div>
      </div>
    );
  }

  private renderResetButton() {
    const { discardButtonEnabled } = this.props;
    return discardButtonEnabled
    ? (
      <button
        type="button"
        className="button-reset link-button"
        onClick={() => this.props.resetForm({})}
      >
        Discard Changes
      </button>
    )
    : null;
  }
}

function mapStateToProps(state: AccountState): AccountSettingsProps {
  return {
    inputs: inputProps(state),
    valid: validProps(state),
    updateData: updateProps(state),
    anyPasswordInputModified: anyPasswordInputModified(state),
    discardButtonEnabled: anyUserInputModified(state) || anyPasswordInputModified(state),
    submitButtonEnabled: (anyUserInputModified(state) || anyPasswordInputModified(state))
      && (anyUserInputModified(state) ? allUserInputsValid(state) : true)
      && (anyPasswordInputModified(state)
        ? allPasswordInputsModified(state) && allPasswordInputsValid(state)
        : true),
    isLocal: state.data.user.isLocal,
  };
}

export const ConnectedAccountSettings = connect(
  mapStateToProps,
  AccountActionCreators,
)(AccountSettings);
