import '../../../scss/react/shared-components/form-elements.scss';

import * as React from 'react';
import * as Yup from 'yup';

import { BaseFormState, Form } from '../shared-components/form';
import { Input } from '../shared-components/input';

export class ResetPasswordForm extends Form<{}, BaseFormState> {
  protected schema = Yup.object({
    newPassword: Yup.string()
      .required()
      .label('New Password'),
    confirmPassword: Yup.string()
      .required()
      .label('Confirm Password'),
  });

  public constructor(props: {}) {
    super(props);

    this.state = {
      data: {
        newPassword: '',
        confirmPassword: '',
      },
      errors: {},
      formIsValid: false,
    };
  }

  private renderResetPasswordForm() {
    return (
      <div className="form-content-container flex-item-for-tablet-up-10-12 flex-item-for-desktop-up-5-12">
        <form autoComplete="off">
          {this.renderPasswordSection()}
        </form>
      </div>
    );
  }

  private renderPasswordSection() {
    const { data, errors, formIsValid } = this.state;
    return (
      <div className="form-section">
        <h3 className="form-section-title">Reset your Password</h3>
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
    );
  }
}
