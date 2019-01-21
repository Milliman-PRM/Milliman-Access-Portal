import * as React from 'react';
const Joi = require('joi-browser');
import Form from "../shared-components/form";
import { getData, postData } from "../../shared";

import '../../../images/icons/login.svg';
//import { LoginFormState } from './interfaces';

class LoginForm extends Form {

  state = {
    userConfirmed: false,
    loginWarning: null,
    data: { username: "", password: "" },
    errors: {}
  };

  schema = {
    username: Joi.string()
      .email()
      .required()
      .label("Username"),
    password: Joi.string()
      .required()
      .label("Password")
  };

  checkUser = e => {
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

  handleSubmit = (e) => {
    e.preventDefault();

    const errors = this.validate();
    this.setState({ errors: errors || {} });
    if (errors) return;

    postData(window.location.href, this.state.data, true)
      .then(response => {
        console.log(response);
        let loginWarning = response.headers.get('Warning');
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
    return (
      <form onSubmit={!this.state.userConfirmed ? this.checkUser : this.handleSubmit}>
        {!this.state.userConfirmed ?
          this.renderInput("username", "Username", "text", { inputIcon: 'user', actionIcon: 'login', actionIconEvent: this.checkUser, autoFocus: true }) :
          this.renderInput("username", "Username", "text", { inputIcon: 'user' })
        }
        {this.state.userConfirmed && this.renderInput("password", "Password", "password", { inputIcon: 'password', autoFocus: true })}
        {this.state.userConfirmed && this.renderRequestVerificationToken()}
        {this.state.loginWarning && <div className="login-warning">{this.state.loginWarning}</div>}
        {this.state.userConfirmed && this.renderButton("Forgot Password", false, () => { window.location.href = "/Account/ForgotPassword" })}
        {this.state.userConfirmed && this.renderButton("Login")}
      </form>
    );
  }
}

export default LoginForm;
