import '../../../scss/react/shared-components/form-elements.scss';

import '../../../images/icons/user.svg';

import * as React from 'react';
import * as Yup from 'yup';

import { BaseFormState, Form } from '../shared-components/form';
import { Input } from '../shared-components/input';

export class ForgotPasswordForm extends Form<{}, BaseFormState> {
  protected schema = Yup.object({
    email: Yup.string()
      .email()
      .required()
      .label('Email'),
  });

  public constructor(props: {}) {
    super(props);

    this.state = {
      data: { email: '' },
      errors: {},
      formIsValid: false,
    };
  }

  public render() {
    const { data, errors, formIsValid } = this.state;

    return (
      <div className="form-content-container flex-item-for-tablet-up-10-12 flex-item-for-desktop-up-5-12">
        <div className="form-section">
          <form action="/Account/ForgotPassword" method="post">
          <h3 className="form-section-title">Enter your Email Address</h3>
            <Input
              name="email"
              label="Email"
              type="text"
              value={data.email}
              onChange={this.handleChange}
              onBlur={this.handleBlur}
              error={errors.email}
              autoFocus={true}
              inputIcon="user"
            />
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
      </div>
    );
  }
}
