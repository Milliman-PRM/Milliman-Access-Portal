import '../../../scss/react/shared-components/form-elements.scss';

import * as React from 'react';
import * as Yup from 'yup';

import { BaseFormState, Form } from '../shared-components/form/form';
import { Input } from '../shared-components/form/input';

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
    confirmNewPassword: Yup.string()
      .required('This field is required')
      .label('Confirm Password')
      .test(
        'confirm-password-matches-new',
        'Does not match new password',
      () => this.state.data.confirmNewPassword === this.state.data.newPassword,
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
        email: '',
        passwordResetToken: '',
        newPassword: '',
        confirmNewPassword: '',
      },
      errors: {},
      formIsValid: false,
      requestVerificationToken: '',
    };

    this.handleChange = this.handleChange.bind(this);
  }

  public componentDidMount() {
    const requestVerificationToken = document
      .querySelector('input[name="__RequestVerificationToken"]')
      .getAttribute('value');
    const passwordResetToken = document
      .querySelector('input[name="__PasswordResetToken"]')
      .getAttribute('value');
    const email = document
      .querySelector('input[name="__Email"]')
      .getAttribute('value');
    const serverSideErrorMessage = document
      .querySelector('input[name="__Message"]')
      .getAttribute('value');
    this.setState({
      requestVerificationToken,
      data: {
        email,
        passwordResetToken,
        newPassword: this.state.data.newPassword,
        confirmNewPassword: this.state.data.confirmNewPassword,
      },
      errors: {
        newPassword: serverSideErrorMessage,
      },
    });
  }

  public handlePasswordChange = async ({ currentTarget: input }: React.FormEvent<HTMLInputElement>) => {
    const { name, value } = input;
    const { data, errors } = Object.assign({}, this.state);
    data[name] = value;

    this.setState({ data }, async () => {
      const errorMessage = await this.validateProperty(input);
      this.validate();

      if (errorMessage && value) {
        errors[name] = errorMessage[name];
      } else {
        delete errors[name];
      }
      this.setState({ errors });
    });
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
              data-lpignore="true"
            />
            <input
              readOnly={true}
              name="email"
              value={data.email}
              style={{display: 'none'}}
              data-lpignore="true"
            />
            <input
              readOnly={true}
              name="passwordResetToken"
              value={data.passwordResetToken}
              style={{display: 'none'}}
              data-lpignore="true"
            />
            <Input
              name="newPassword"
              label="New Password"
              type="password"
              value={data.newPassword}
              onChange={this.handlePasswordChange}
              onBlur={this.handleBlur}
              error={errors.newPassword}
            />
            <Input
              name="confirmNewPassword"
              label="Confirm Password"
              type="password"
              value={data.confirmNewPassword}
              onChange={this.handleChange}
              onBlur={this.handleBlur}
              error={errors.confirmNewPassword}
            />
          </div>
          <div className="button-container">
            <button
              type="submit"
              disabled={(
                data.newPassword === '' ||
                errors.newPassword ||
                errors.confirmNewPassword ||
                data.newPassword !== data.confirmNewPassword) ? true : false}
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
