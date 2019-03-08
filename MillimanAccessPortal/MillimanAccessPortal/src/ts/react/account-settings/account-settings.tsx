import * as React from 'react';
import { connect } from 'react-redux';
import ReduxToastr from 'react-redux-toastr';

import { ColumnSpinner } from '../shared-components/column-spinner';
import { NavBar } from '../shared-components/navbar';
import * as AccountActionCreators from './redux/action-creators';
import { AccountState } from './redux/store';

// tslint:disable-next-line
interface AccountSettingsProps {
  data: {
    username: string;
    firstName: string;
    lastName: string;
    phone: string;
    employer: string;
    currentPassword: string;
    newPassword: string;
    confirmPassword: string;
  };
}
class AccountSettings extends React.Component<AccountSettingsProps & typeof AccountActionCreators> {
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
    const { username, firstName, lastName, phone, employer } = this.props.data;
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
                  onChange={({ target }) => this.props.setPendingTextInputValue({
                    inputName: 'firstName',
                    value: target.value,
                  })}
                  autoFocus={true}
                />
                <span className="text-danger field-validation-valid" />
              </div>
            </div>
            <div className="form-input form-input-text flex-item-for-tablet-up-6-12">
              <label className="form-input-text-title" htmlFor="lastName">Last Name</label>
              <div>
                <input
                  type="text"
                  name="lastName"
                  value={lastName}
                  onChange={({ target }) => this.props.setPendingTextInputValue({
                    inputName: 'lastName',
                    value: target.value,
                  })}
                />
                <span className="text-danger field-validation-valid" />
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
                  onChange={({ target }) => this.props.setPendingTextInputValue({
                    inputName: 'phone',
                    value: target.value,
                  })}
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
                  onChange={({ target }) => this.props.setPendingTextInputValue({
                    inputName: 'employer',
                    value: target.value,
                  })}
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
    const { currentPassword, newPassword, confirmPassword } = this.props.data;
    return (
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
                onChange={({ target }) => this.props.setPendingTextInputValue({
                  inputName: 'currentPassword',
                  value: target.value,
                })}
              />
            </div>
          </div>
          <div className="form-input htmlForm-input-text flex-item-12-12">
            <label className="form-input-text-title" htmlFor="newPassword">New Password</label>
            <div>
              <input
                type="password"
                name="newPassword"
                value={newPassword}
                onChange={({ target }) => this.props.setPendingTextInputValue({
                  inputName: 'newPassword',
                  value: target.value,
                })}
              />
              <span className="text-danger field-validation-valid" />
            </div>
          </div>
          <div className="form-input htmlForm-input-text flex-item-12-12">
            <label className="form-input-text-title" htmlFor="confirmPassword">Confirm password</label>
            <div>
              <input
                type="password"
                name="confirmPassword"
                value={confirmPassword}
                onChange={({ target }) => this.props.setPendingTextInputValue({
                  inputName: 'confirmPassword',
                  value: target.value,
                })}
              />
              <span className="text-danger field-validation-valid" />
            </div>
          </div>
        </div>
      </div>
    );
  }

  private renderSubmissionSection() {
    return (
      <div className="form-submission-section">
        <div className="button-container button-container-update">
          <button
            type="button"
            className="button-reset link-button"
          >
            Discard Changes
          </button>
          <button
            type="submit"
            className="button-submit blue-button"
          >
            Update Account
          </button>
        </div>
      </div>
    );
  }
}

function mapStateToProps(state: AccountState): AccountSettingsProps {
  return {
    data: {
      username: state.pending.fields.userName,
      firstName: state.pending.fields.firstName,
      lastName: state.pending.fields.lastName,
      phone: state.pending.fields.phone,
      employer: state.pending.fields.employer,
      currentPassword: state.pending.fields.current,
      newPassword: state.pending.fields.new,
      confirmPassword: state.pending.fields.confirm,
    },
  };
}

export const ConnectedAccountSettings = connect(
  mapStateToProps,
  AccountActionCreators,
)(AccountSettings);
