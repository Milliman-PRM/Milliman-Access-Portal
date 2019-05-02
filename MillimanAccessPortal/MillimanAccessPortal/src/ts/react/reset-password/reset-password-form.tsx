import '../../../scss/react/shared-components/form-elements.scss';

import * as React from 'react';
import * as Yup from 'yup';

import { BaseFormState, Form } from '../shared-components/form';
import { Input } from '../shared-components/input';

interface ResetPasswordState extends BaseFormState {
  requestVerificationToken: string;
}

export class ResetPasswordForm extends Form<{}, ResetPasswordState> {
  protected schema = Yup.object({
    newPassword: Yup.string()
      .required()
      .label('New Password'),
    confirmPassword: Yup.string()
      .required()
      .label('Confirm Password'),
    email: Yup.string()
      .required(),
    passwordResetToken: Yup.string()
      .required(),
    requestVerificationToken: Yup.string()
      .required(),
  });

  public constructor(props: {}) {
    super(props);

    this.state = {
      data: {
        newPassword: '',
        confirmPassword: '',
        email: '',
        passwordResetToken: '',
      },
      errors: {},
      formIsValid: false,
      requestVerificationToken: '',
    };
  }

  public componentDidMount() {
    const antiforgeryToken = document
      .querySelector('input[name="__RequestVerificationToken"]')
      .getAttribute('value');
    this.setState({ requestVerificationToken: antiforgeryToken });
  }

  public render() {
    const { data, errors, formIsValid } = this.state;

    return (
      <div className="form-content-container flex-item-for-tablet-up-10-12 flex-item-for-desktop-up-5-12">
        <form autoComplete="off" action="ResetPassword" method="POST">
          <div className="form-section">
            <h3 className="form-section-title">Reset your Password</h3>
            <input
              readOnly={true}
              name="__RequestVerificationToken"
              value={this.state.requestVerificationToken}
              style={{display: 'none'}}
            />
            <Input
              name="newPassword"
              label="New Password"
              type="password"
              value={data.newPassword}
              onChange={this.handleChange}
              onBlur={this.handleBlur}
              error={errors.newPassword}
            />
            <Input
              name="confirmPassword"
              label="Confirm Password"
              type="password"
              value={data.confirmPassword}
              onChange={this.handleChange}
              onBlur={this.handleBlur}
              error={errors.confirmPassword}
            />
          </div>
          <div className="button-container">
              <button
                type="submit"
                disabled={!formIsValid}
                className="blue-button"
              >
                Reset Password
              </button>
            </div>
        </form>
      </div>
    );
  }
}
