import '../../../scss/react/shared-components/form-elements.scss';

import '../../../images/icons/login.svg';
import '../../../images/icons/password.svg';
import '../../../images/icons/user.svg';

import * as Joi from 'joi';
import * as React from 'react';

import { postData } from '../../shared';
import { BaseFormState, Form } from '../shared-components/form';
import { Input } from '../shared-components/input';

interface LoginFormState extends BaseFormState {
  userConfirmed: boolean;
  awaitingConfirmation: boolean;
  loginWarning: string;
}

export class LoginForm extends Form<{}, LoginFormState> {

  protected schema = {
    username: Joi.string()
      .email()
      .required()
      .label('Username'),
    password: Joi.string()
      .required()
      .label('Password'),
  };

  public constructor(props: {}) {
    super(props);

    this.state = {
      userConfirmed: false,
      awaitingConfirmation: false,
      loginWarning: null,
      data: { username: '', password: '' },
      errors: {},
    };
  }

  public render() {
    const { data, errors, userConfirmed, awaitingConfirmation, loginWarning } = this.state;
    let actionButton;
    if (!userConfirmed && !awaitingConfirmation) {
      actionButton = (
        <button type="submit" className="action-icon-label" onClick={this.checkUser}>
          <svg className="action-icon"><use xlinkHref="#login" /></svg>
        </button>
      );
    } else if (awaitingConfirmation) {
      actionButton = (
        <div className="action-icon-label waiting">
          <svg className="action-icon"><use xlinkHref="#login" /></svg>
        </div>
      );
    } else {
      actionButton = null;
    }
    return (
      <form onSubmit={!userConfirmed ? this.checkUser : this.handleSubmit}>
        <Input
          name="username"
          label="Username"
          type="text"
          value={data.username}
          onChange={this.handleChange}
          onBlur={this.handleBlur}
          onClick={userConfirmed ? this.handleUsernameClick : undefined}
          error={errors.username}
          autoFocus={!userConfirmed}
          inputIcon="user"
          readOnly={userConfirmed}
        >
          {actionButton}
        </Input>
        <Input
          name="password"
          label="Password"
          type="password"
          value={data.password}
          onChange={this.handleChange}
          onBlur={this.handleBlur}
          error={errors.password}
          autoFocus={userConfirmed}
          inputIcon="password"
          hidden={!userConfirmed}
        />
        {loginWarning && <div className="login-warning">{loginWarning}</div>}
        <div className={'button-container' + (userConfirmed ? ' visible' : ' hidden')}>
          <a href="/Account/ForgotPassword" className="link-button">Forgot Password</a>
          <button
            type={userConfirmed ? 'submit' : 'button'}
            disabled={userConfirmed && (this.validate()) ? true : false}
            className="blue-button"
            onClick={userConfirmed ? this.handleSubmit : undefined}
          >
            Login
          </button>
        </div>
      </form>
    );
  }

  protected handleSubmit = (e: React.MouseEvent<HTMLButtonElement> | React.FormEvent<HTMLFormElement>) => {
    e.preventDefault();

    const errors = this.validate();
    this.setState({ errors: errors || {} });
    if (errors) {
      return;
    }

    postData(window.location.href, this.state.data, true)
      .then((response) => {
        const loginWarning = response.headers.get('Warning');
        if (loginWarning) {
          const data = { ...this.state.data };
          data.password = '';
          this.setState({ data, loginWarning });
          return;
        } else if (response.redirected === true || response.status === 302) {
          window.location.replace(response.url);
        } else if (response.status === 200) {
          window.location.reload();
        }
      });
  }

  protected handleUsernameClick = () => {
    this.setState({ userConfirmed: false });
  }

  private checkUser = (e: React.MouseEvent<HTMLButtonElement> | React.FormEvent<HTMLFormElement>) => {
    e.preventDefault();

    const errors = { ...this.state.errors };
    const errorMessage = this.validateProperty({ name: 'username', value: this.state.data.username });
    if (errorMessage) {
      errors.username = errorMessage;
      this.setState({ errors });
      return;
    } else {
      delete errors.username;
    }

    // hold for
    this.setState({ awaitingConfirmation: true }, () => {
      setTimeout(() => {
        this.setState({
          userConfirmed: true,
          awaitingConfirmation: false,
        });
      }, 2000);
    });
  }
}
