import * as React from 'react';
import * as Yup from 'yup';

import { Guid } from '../models';
import { BaseFormState, Form } from '../shared-components/form/form';
import { Input } from '../shared-components/form/input';
import { DropDown } from '../shared-components/form/select';

import { PasswordValidation } from '../../../ts/react/models';
import { postJsonData } from '../../../ts/shared';

const validatePassword = async (requestModel: { proposedPassword: string }) =>
  await postJsonData<PasswordValidation>('/Account/CheckPasswordValidity2', requestModel);

interface EnableAccountState extends BaseFormState {
  pageData: {
    requestVerificationToken: string;
    id: Guid;
    code: string;
    isLocalAccount: boolean;
    timeZones: Array<{ selectionValue: string | number, selectionLabel: string }>;
  };
  data: {
    username: string;
    firstName: string;
    lastName: string;
    phone: string;
    employer: string;
    timeZoneId: string;
    newPassword: string;
    confirmNewPassword: string;
  };
  errors: {
    firstName: string;
    lastName: string;
    phone: string;
    employer: string;
    timeZoneId: string;
    newPassword: string;
    confirmNewPassword: string;
  };
}

export class EnableAccount extends Form<{}, EnableAccountState> {

  protected schema = Yup.object({
    username: Yup.string().email().required(),
    firstName: Yup.string().required(),
    lastName: Yup.string().required(),
    phone: Yup.string().required(),
    employer: Yup.string().required(),
    timeZoneId: Yup.string().required(),
    newPassword: Yup.string()
      .required('This field is required')
      .label('New Password')
      .test('new-password-is-valid', () => this.msg, (value) =>
        validatePassword({ proposedPassword: value })
          .then((response) => {
            this.msg = response.messages
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
  }).notRequired();

  private msg: string = null;

  public constructor(props: {}) {
    super(props);

    this.state = {
      pageData: {
        requestVerificationToken: null,
        id: null,
        code: null,
        isLocalAccount: true,
        timeZones: [],
      },
      data: {
        username: null,
        firstName: null,
        lastName: null,
        phone: null,
        employer: null,
        timeZoneId: 'UTC',
        newPassword: null,
        confirmNewPassword: null,
      },
      errors: {
        firstName: null,
        lastName: null,
        phone: null,
        employer: null,
        timeZoneId: null,
        newPassword: null,
        confirmNewPassword: null,
      },
      formIsValid: false,
    };
  }

  public componentDidMount() {
    const requestVerificationToken = document
      .querySelector('input[name="__RequestVerificationToken"]')
      .getAttribute('value');
    const id = document
      .querySelector('input[name="__Id"]')
      .getAttribute('value');
    const code = document
      .querySelector('input[name="__Code"]')
      .getAttribute('value');
    const isLocalAccountString = document
      .querySelector('input[name="__IsLocalAccount"]')
      .getAttribute('value');
    const username = document
      .querySelector('input[name="__Username"]')
      .getAttribute('value');
    const timeZonesRaw: Array<{ DisplayName: string; Id: string; }> = JSON.parse(
      document
        .querySelector('input[name="__TimeZones"]')
        .getAttribute('value'));
    const timeZones: Array<{ selectionValue: string | number; selectionLabel: string }> =
      timeZonesRaw.map((x) => {
        return {
          selectionValue: x.Id,
          selectionLabel: x.DisplayName,
        };
      });
    validatePassword({ proposedPassword: '' })
      .catch(() => {
        location.reload(true);
      });
    this.setState({
      pageData: {
        ...this.state.pageData,
        requestVerificationToken,
        id,
        code,
        isLocalAccount: isLocalAccountString === 'True' ? true : false,
        timeZones,
      },
      data: {
        ...this.state.data,
        username,
      },
      errors: {
        ...this.state.errors,
      },
    });
  }

  public renderUserInformationSection() {
    const { data, errors } = this.state;
    return (
      <div className="form-section-container">
        <div className="form-section">
          <h3 className="form-section-title">User Information</h3>
          <div className="form-input-container">
            <div className="form-input form-input-text flex-item-for-phone-only-12-12 flex-item-for-tablet-up-12-12">
              <Input
                name="userName"
                label="Username"
                type="text"
                value={data.username}
                error={null}
                readOnly={true}
              />
            </div>
            <div className="form-input form-input-text flex-item-for-phone-only-12-12 flex-item-for-tablet-up-6-12">
              <Input
                name="firstName"
                label="First Name *"
                type="text"
                autoFocus={true}
                value={data.firstName}
                error={errors.firstName}
                onChange={({ currentTarget: target }: React.FormEvent<HTMLInputElement>) => {
                  this.setState({
                    data: { ...data, firstName: target.value },
                  });
                }}
              />
            </div>
            <div className="form-input form-input-text flex-item-for-phone-only-12-12 flex-item-for-tablet-up-6-12">
              <Input
                name="lastName"
                label="Last Name *"
                type="text"
                value={data.lastName}
                error={errors.lastName}
                onChange={({ currentTarget: target }: React.FormEvent<HTMLInputElement>) => {
                  this.setState({
                    data: { ...data, lastName: target.value },
                  });
                }}
              />
            </div>
            <div className="form-input form-input-text flex-item-for-phone-only-12-12 flex-item-for-tablet-up-6-12">
              <Input
                name="phone"
                label="Phone Number *"
                type="phone"
                value={data.phone}
                error={errors.phone}
                onChange={({ currentTarget: target }: React.FormEvent<HTMLInputElement>) => {
                  this.setState({
                    data: { ...data, phone: target.value },
                  });
                }}
              />
            </div>
            <div className="form-input form-input-text flex-item-for-phone-only-12-12 flex-item-for-tablet-up-6-12">
              <Input
                name="employer"
                label="Employer *"
                type="text"
                value={data.employer}
                error={errors.employer}
                onChange={({ currentTarget: target }: React.FormEvent<HTMLInputElement>) => {
                  this.setState({
                    data: { ...data, employer: target.value },
                  });
                }}
              />
            </div>
            <div className="form-input form-input-text flex-item-for-phone-only-12-12 flex-item-for-tablet-up-12-12">
              <DropDown
                name="timezone"
                label="Timezone *"
                value={data.timeZoneId}
                values={this.state.pageData.timeZones}
                error={errors.timeZoneId}
                onChange={({ currentTarget: target }: React.FormEvent<HTMLSelectElement>) => {
                  const timezoneValue = target.value ? target.value : null;
                  this.setState({
                    data: { ...data, timeZoneId: timezoneValue },
                  });
                }}
              />
            </div>
          </div>
        </div>
      </div>
    );
  }

  public renderNewPasswordSection() {
    const { newPassword, confirmNewPassword } = this.state.data;
    const { newPassword: newPasswordError, confirmNewPassword: confirmNewPasswordError } = this.state.errors;
    return (
      <div className="form-section-container">
        <div className="form-section">
          <h3 className="form-section-title">Password</h3>
          <div className="form-input-container">
            <div className="form-input form-input-text flex-item-for-phone-only-12-12 flex-item-for-tablet-up-12-12">
              <Input
                name="newPassword"
                label="New Password *"
                type="password"
                value={newPassword}
                error={newPasswordError}
                onChange={({ currentTarget: target }: React.FormEvent<HTMLInputElement>) => {
                  const { name, value } = target;
                  const { data, errors } = Object.assign({}, this.state);
                  data.newPassword = value;

                  this.setState({ data }, async () => {
                    const errorMessage = await this.validateProperty(target);
                    this.validate();

                    if (errorMessage && value) {
                      errors.newPassword = errorMessage.newPassword;
                    } else {
                      errors.newPassword = null;
                    }
                    this.setState({ errors });
                  });
                }}
              />
            </div>
            <div className="form-input form-input-text flex-item-for-phone-only-12-12 flex-item-for-tablet-up-12-12">
              <Input
                name="confirmNewPassword"
                label="Confirm New Password *"
                type="password"
                value={confirmNewPassword}
                error={confirmNewPasswordError}
                onChange={({ currentTarget: target }: React.FormEvent<HTMLInputElement>) => {
                  this.setState({
                    data: { ...this.state.data, confirmNewPassword: target.value },
                  });
                }}
              />
            </div>
          </div>
        </div>
      </div>
    );
  }

  public renderButtonSection() {
    const { formIsValid } = this.state;
    return (
      <div className="button-container">
        <button
          type="submit"
          disabled={!formIsValid}
          className="blue-button"
        >
          Activate Account
        </button>
      </div>
    );
  }

  public render() {
    const { isLocalAccount } = this.state.pageData;
    return (
      <div className="form-content-container flex-item-for-tablet-up-10-12 flex-item-for-desktop-up-6-12">
        <form autoComplete="off" className="enable-account-form">
          {this.renderUserInformationSection()}
          {isLocalAccount && this.renderNewPasswordSection()}
          {this.renderButtonSection()}
        </form>
      </div>
    );
  }
}
