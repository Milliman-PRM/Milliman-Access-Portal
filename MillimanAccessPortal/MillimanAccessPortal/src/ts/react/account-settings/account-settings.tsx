import * as React from 'react';
import { connect } from 'react-redux';
import ReduxToastr from 'react-redux-toastr';

import { ColumnSpinner } from '../shared-components/column-spinner';
import { NavBar } from '../shared-components/navbar';
import * as AccountActionCreators from './redux/action-creators';
import { AccountState } from './redux/store';

// tslint:disable-next-line
interface AccountSettingsProps {
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
    return (
      <>
        <div className="form-section" data-section="username">
          <h4 className="form-section-title">User Information</h4>
          <div className="form-input-container" style={{ marginBottom: 0 }}>
            <div className="form-input htmlForm-input-text flex-item-12-12">
              <label className="form-input-text-title" htmlFor="UserName">Username</label>
              <div>
                <input
                  type="text"
                  name="UserName"
                  value="joseph.sweeney@milliman.com"
                  readOnly={true}
                />
              </div>
            </div>
          </div>
        </div>
        <div className="form-section" data-section="account">
          <div className="form-input-container">
            <div className="form-input form-input-text flex-item-for-tablet-up-6-12">
              <label className="form-input-text-title" htmlFor="FirstName">First Name</label>
              <div>
                <input
                  type="text"
                  name="FirstName"
                  value="Joseph"
                  autoFocus={true}
                />
                <span className="text-danger field-validation-valid" />
              </div>
            </div>
            <div className="form-input form-input-text flex-item-for-tablet-up-6-12">
              <label className="form-input-text-title" htmlFor="LastName">Last Name</label>
              <div>
                <input type="text" name="LastName" value="Sweeney" />
                <span className="text-danger field-validation-valid" />
              </div>
            </div>
            <div className="form-input htmlForm-input-text flex-item-12-12" style={{ display: 'none' }}>
              <label className="form-input-text-title" htmlFor="Email">Email Address</label>
              <div>
                <input type="email" name="Email" value="joseph.sweeney@milliman.com" />
                <span className="text-danger field-validation-valid" />
              </div>
            </div>
            <div className="form-input form-input-text flex-item-for-tablet-up-4-12">
              <label className="form-input-text-title" htmlFor="PhoneNumber">Phone Number</label>
              <div>
                <input
                  placeholder="(###) ###-####"
                  type="tel"
                  name="PhoneNumber"
                  value="don't call me please"
                />
                <span className="text-danger field-validation-valid" />
              </div>
            </div>
            <div className="form-input form-input-text flex-item-for-tablet-up-8-12">
              <label className="form-input-text-title" htmlFor="Employer">Employer</label>
              <div>
                <input type="text" name="Employer" value="Milliman" />
                <span className="text-danger field-validation-valid" />
              </div>
            </div>
          </div>
        </div>
      </>
    );
  }

  private renderPasswordSection() {
    return (
      <div className="form-section">
        <h4 className="form-section-title">Update Password</h4>
        <div className="form-input-container">
          <div className="form-input htmlForm-input-text flex-item-12-12">
            <label className="form-input-text-title" htmlFor="CurrentPassword">Current Password</label>
            <div>
              <input type="password" name="CurrentPassword" />
            </div>
          </div>
          <div className="form-input htmlForm-input-text flex-item-12-12">
            <label className="form-input-text-title" htmlFor="NewPassword">New Password</label>
            <div>
              <input type="password" name="NewPassword" />
              <span className="text-danger field-validation-valid" />
            </div>
          </div>
          <div className="form-input htmlForm-input-text flex-item-12-12">
            <label className="form-input-text-title" htmlFor="ConfirmNewPassword">Confirm password</label>
            <div>
              <input type="password" name="ConfirmNewPassword" />
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
  const s = state;
  return {};
}

export const ConnectedAccountSettings = connect(
  mapStateToProps,
  AccountActionCreators,
)(AccountSettings);
