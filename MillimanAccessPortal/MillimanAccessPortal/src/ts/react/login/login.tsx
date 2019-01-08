import '../../../scss/react/login/login.scss';

import * as React from 'react';

import { getData } from '../../shared';

import { LoginState } from './interfaces';


import '../../../images/map-logo.svg';
import '../../../scss/map.scss';


export class Login extends React.Component <{}, LoginState > {
  public constructor(props) {
    super(props);

    this.state = {
      Username: '',
      Password: '',
      ShowPassword: false,
      RequestValidationToken: ''
    };

    this.handleChange = this.handleChange.bind(this);
    this.handleBlur = this.handleBlur.bind(this);
    this.handleSubmit = this.handleSubmit.bind(this);
  }

  componentDidMount() {
    const RequestValidationToken = document.querySelector('input[name="__RequestVerificationToken"]').getAttribute('value');
    this.setState({ RequestValidationToken });
  }

  private handleChange(event) {
    const loginFieldValue = {};
    loginFieldValue[event.target.name] = event.target.value
    this.setState(loginFieldValue);
    this.setState({ ShowPassword: true });
  }

  private handleBlur() {
    this.setState({ ShowPassword: true });
  }

  private handleSubmit(event) {
    console.log('Form Submitted');
    event.preventDefault
  }

  public render() {
    return (
      <>
        <div id="login-splash-panel">
        </div>
        <div id="login-wrapper">
          <section id="login-container">
            <div id="login-form-container">
              <div id="login-logo-container">
                <svg id="login-logo">
                  <use xlinkHref={"#map-logo"} />
                </svg>
              </div>
              <form id="login-form" autoComplete="off" action="/Account/Login?returnurl=%2F" method="post">
                <div className="login-form-input-container">
                  <input name="Username" type="text" value={this.state.Username} onChange={this.handleChange} onBlur={this.handleBlur} placeholder="Username" autoFocus />
                </div>
                <div className="login-form-input-container">
                  <input name="Password" type="password" value={this.state.Password} onChange={this.handleChange} placeholder="Password" />
                </div>
                <input name="__RequestVerificationToken" type="hidden" value={this.state.RequestValidationToken} />
                <div className="login-form-button-container">
                  <a href="/Account/ForgotPassword" className="link-button">Forgot Password</a>
                  <button type="submit" className="blue-button">Login</button>
                </div>
              </form>
            </div>
          </section>
        </div>
      </>
    )
  }
}
