import * as React from 'react';
const Joi = require('joi-browser');
import Form from "../shared-components/form";

import '../../../images/icons/checkmark.svg';

class LoginForm extends Form {
  state = {
    userConfirmed: false,
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

  doSubmit = () => {
    // Call the server
    console.log("Submitted");
  };

  render() {
    return (
      <form onSubmit={this.state.userConfirmed ? this.handleSubmit : this.checkUser}>
        {!this.state.userConfirmed ?
          this.renderInput("username", "Username", "text", { inputIcon: 'user', actionIcon: 'checkmark', actionIconEvent: this.checkUser, autoFocus: true }) :
          this.renderInput("username", "Username", "text", { inputIcon: 'user' })
        }
        {this.state.userConfirmed && this.renderInput("password", "Password", "password", { inputIcon: 'password', autoFocus: true })}
        {this.state.userConfirmed && this.renderRequestVerificationToken()}
        {this.state.userConfirmed && this.renderButton("Forgot Password", false, () => { window.location.href = "/Account/ForgotPassword" })}
        {this.state.userConfirmed && this.renderButton("Login")}
      </form>
    );
  }
}

export default LoginForm;
