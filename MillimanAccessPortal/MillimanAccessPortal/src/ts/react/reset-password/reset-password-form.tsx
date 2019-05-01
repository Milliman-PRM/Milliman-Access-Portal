import '../../../scss/react/shared-components/form-elements.scss';

import * as React from 'react';
import { connect } from 'react-redux';
import ReduxToastr from 'react-redux-toastr';

import { Input } from '../shared-components/input';
import * as AccountActionCreators from './redux/action-creators';
import { UpdateAccount } from './redux/actions';
import {
    allPasswordInputsModified, allPasswordInputsValid,anyPasswordInputModified,
    anyUserInputModified, inputProps, updateProps, validProps,
} from './redux/selectors';
import { AccountState, ValidationState } from './redux/store';

interface ResetPasswordProps {
  inputs: {
    newPassword: string;
    confirmPassword: string;
  };
  valid: {
    newPassword: ValidationState;
    confirmPassword: ValidationState;
  };
  updateData: UpdateAccount['request'];
  isLocal: boolean;
  anyPasswordInputModified: boolean;
  submitButtonEnabled: boolean;
}
class ResetPasswordForm extends React.Component {

  public render() {
    return (
      <>
        <ReduxToastr
          timeOut={5000}
          newestOnTop={false}
          position="bottom-right"
          transitionIn="fadeIn"
          transitionOut="fadeOut"
        />
      </>
    );
  }

  private renderResetPasswordForm() {
    return (
      <div className="form-content-container flex-item-for-tablet-up-10-12 flex-item-for-desktop-up-5-12">
        <form autoComplete="off">
          {this.renderPasswordSection()}
          {this.renderSubmissionSection()}
        </form>
      </div>
    );
  }

  private renderPasswordSection() {
    const { isLocal } = this.props;
    const { newPassword, confirmPassword } = this.props.inputs;
    return isLocal
    ? (
      <div className="form-section">
        <h3 className="form-section-title">Reset your Password</h3>
        <Input
          name="newPassword"
          label="New Password"
          type="password"
          value={newPassword}
          onChange={({ currentTarget: target }: React.FormEvent<HTMLInputElement>) => {
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
            if (this.props.inputs.confirmPassword) {
              this.props.validateInputPassword({
                value: {
                  current: this.props.inputs.currentPassword,
                  new: target.value,
                  confirm: this.props.inputs.confirmPassword,
                },
                inputName: 'confirm',
              });
            }
          }}
          onBlur={() => { return; }}
          error={this.props.valid.newPassword.valid || !this.props.anyPasswordInputModified
            ? null
            : this.props.valid.newPassword.message
          }
        />
        <Input
          name="confirmPassword"
          label="Confirm Password"
          type="password"
          value={confirmPassword}
          onChange={({ currentTarget: target }: React.FormEvent<HTMLInputElement>) => {
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
          onBlur={() => { return; }}
          error={this.props.valid.confirmPassword.valid || !this.props.anyPasswordInputModified
            ? null
            : this.props.valid.confirmPassword.message
          }
        />
      </div>
    )
    : null;
  }

  private renderSubmissionSection() {
    const { submitButtonEnabled } = this.props;
    return (
      <div className="button-container">
        {this.renderResetButton()}
        <button
          type="submit"
          className="blue-button"
          disabled={(submitButtonEnabled &&
            (this.props.inputs.newPassword === this.props.inputs.confirmPassword)) ? false : true}
          onClick={(event: React.FormEvent) => {
            event.preventDefault();
            if (submitButtonEnabled) {
              this.props.updateAccount(this.props.updateData);
            }
          }}
        >
          Reset Password
        </button>
      </div>
    );
  }
}

function mapStateToProps(state: AccountState): ResetPasswordProps {
  return {
    inputs: inputProps(state),
    valid: validProps(state),
    updateData: updateProps(state),
    anyPasswordInputModified: anyPasswordInputModified(state),
    submitButtonEnabled: (anyUserInputModified(state) || anyPasswordInputModified(state))
      && (anyUserInputModified(state) ? allUserInputsValid(state) : true)
      && (anyPasswordInputModified(state)
        ? allPasswordInputsModified(state) && allPasswordInputsValid(state)
        : true),
    isLocal: state.data.user.isLocal,
  };
}

export const ConnectedResetPassword = connect(
  mapStateToProps,
  AccountActionCreators,
)(ResetPasswordForm);
