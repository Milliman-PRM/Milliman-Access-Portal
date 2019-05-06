import '../../../scss/react/shared-components/form-elements.scss';

import * as React from 'react';
import * as Yup from 'yup';

import { BaseFormState, Form } from '../shared-components/form';
import { Input } from '../shared-components/input';

import { PasswordValidation } from '../../../ts/react/models';
import { postJsonData } from '../../../ts/shared';

interface ResetPasswordState extends BaseFormState {
  requestVerificationToken: string;
}

const validatePassword = async (requestModel: { proposedPassword: string }) =>
  await postJsonData<PasswordValidation>('/Account/CheckPasswordValidity2', requestModel);

let msg: string = null;

export class ResetPasswordForm extends Form<{}, ResetPasswordState> {
  protected schema = Yup.object({
    newPassword: Yup.string()
      .required('This field is required')
      .label('New Password')
      .test('new-password-is-valid', () => msg, (value) =>
        validatePassword({ proposedPassword: value })
          .then((response) => {
            msg = response.messages
              ? response.messages.join('\r\n')
              : null;
            return response.valid;
          })),
    confirmPassword: Yup.string()
      .required('This field is required')
      .label('Confirm Password')
      .test(
        'confirm-password-matches-new',
        'Does not match new password',
        () => this.state.data.confirmPassword === this.state.data.newPassword,
      ),
    email: Yup.string()
      .required(),
    passwordResetToken: Yup.string()
      .required(),
    requestVerificationToken: Yup.string()
      .required(),
  }).notRequired();

  public constructor(props: {}) {
    super(props);

    this.state = {
      data: {
        newPassword: '',
        confirmPassword: '',
        email: '',
        passwordResetToken: '',
      },
      errors: {
        newPassword: '',
        confirmPassword: '',
      },
      formIsValid: false,
      requestVerificationToken: '',
    };

    this.handleChange = this.handleChange.bind(this);
  }

  public componentDidMount() {
    const antiforgeryToken = document
      .querySelector('input[name="__RequestVerificationToken"]')
      .getAttribute('value');
    const passwordResetToken = document
      .querySelector('input[name="PasswordResetToken"]')
      .getAttribute('value');
    const email = document
      .querySelector('input[name="Email"]')
      .getAttribute('value');
  }

  public render() {
    const { data, errors, formIsValid, requestVerificationToken } = this.state;

    return (
      <div className="form-content-container flex-item-for-tablet-up-10-12 flex-item-for-desktop-up-5-12">
        <form autoComplete="off" action="ResetPassword" method="POST">
          <div className="form-section">
            <h3 className="form-section-title">Reset your Password</h3>
            <input
              readOnly={true}
              name="__RequestVerificationToken"
              value={requestVerificationToken}
              style={{display: 'none'}}
            />
            <input
              readOnly={true}
              value={data.email}
              style={{display: 'none'}}
            />
            <input
              readOnly={true}
              value={data.passwordResetToken}
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
              disabled={(
                data.newPassword === '' ||
                errors.newPassword ||
                errors.confirmPassword ||
                data.newPassword !== data.confirmPassword) ? true : false}
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
