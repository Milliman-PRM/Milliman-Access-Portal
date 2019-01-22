import * as React from 'react';
import * as Joi from 'joi-browser';
import { Form } from '../shared-components/form';
import { getData, postData } from '../../shared';

import { BaseFormState } from '../shared-components/form';
import { Input } from '../shared-components/input';

import '../../../images/icons/user.svg';
import '../../../images/icons/password.svg';
import '../../../images/icons/login.svg';

interface LoginFormState extends BaseFormState {
  userConfirmed: boolean;
  loginWarning: string;
}

export class LoginForm extends Form<{}, LoginFormState> {
  public constructor(props) {
    super(props);

    this.state = {
      userConfirmed: false,
      loginWarning: null,
      data: { username: "", password: "" },
      errors: {}
    };
  }

  protected schema = {
    username: Joi.string()
      .email()
      .required()
      .label("Username"),
    password: Joi.string()
      .required()
      .label("Password")
  };

  private checkUser = (e) => {
    e.preventDefault();

    const errors = { ...this.state.errors };
    const errorMessage = this.validateProperty({ name: "username", value: this.state.data.username });
    if (errorMessage) {
      errors["username"] = errorMessage;
      this.setState({ errors });
      return;
    } else {
      delete errors["username"];
    }

    this.setState({ userConfirmed: true });
  }

  protected handleSubmit = (e: React.MouseEvent<HTMLButtonElement> | React.FormEvent<HTMLFormElement>) => {
    e.preventDefault();

    const errors = this.validate();
    this.setState({ errors: errors || {} });
    if (errors) return;

    postData(window.location.href, this.state.data, true)
      .then(response => {
        console.log(response);
        const loginWarning = response.headers.get('Warning');
        if (loginWarning) {
          const data = { ...this.state.data };
          data["password"] = "";
          this.setState({ data, loginWarning });
          return;
        } else if (response.redirected == true || response.status == 302) {
          window.location.replace(response.url);
        } else if (response.status == 200) {
          window.location.reload();
        }
      })
  };

  render() {
    const { userConfirmed, loginWarning } = this.state;
    return (
      <form onSubmit={!this.state.userConfirmed ? this.checkUser : this.handleSubmit}>
        {!this.state.userConfirmed ?
          (
            <Input
              name="username"
              label="Username"
              type="text"
              value={this.state.data.username}
              onChange={this.handleChange}
              onBlur={this.handleBlur}
              error={this.state.errors.username}
              autoFocus={true}
              inputIcon="user"
            >
              <div className="action-icon-label" onClick={this.checkUser}>
                <svg className="action-icon"><use xlinkHref="#login" /></svg>
              </div>
            </Input>
          ) : (
            <Input
              name="username"
              label="Username"
              type="text"
              value={this.state.data.username}
              onChange={this.handleChange}
              onBlur={this.handleBlur}
              error={this.state.errors.username}
              inputIcon="user" />
          )
        }
        {userConfirmed &&
          <>
            <Input
              name="password"
              label="Password"
              type="password"
              value={this.state.data.password}
              onChange={this.handleChange}
              onBlur={this.handleBlur}
              error={this.state.errors.password}
              autoFocus={true}
              inputIcon="password"
            />
            {loginWarning && <div className="login-warning">{this.state.loginWarning}</div>}
            <a href="/Account/ForgotPassword" className="link-button">Forgot Password</a>
            <button
              type="submit"
              disabled={(this.validate()) ? true : false}
              className="blue-button"
              onClick={this.handleSubmit}>
              Login
            </button>
          </>
        }
      </form>
    );
  }
}
