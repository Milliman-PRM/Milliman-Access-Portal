import '../../../scss/react/authorized-content/authorized-content.scss';

import * as React from 'react';

import { getData } from '../../shared';

import { LoginState } from './interfaces';


import '../../../images/map-logo.svg';
import '../../../scss/map.scss';


export class Login extends React.Component <{}, LoginState > {
  public constructor(props) {
    super(props);

    this.state = {
      Username: null,
      Password: null,
    };

    this.handleUsernameChange = this.handleUsernameChange.bind(this);
    this.handlePasswordChange = this.handlePasswordChange.bind(this);
    this.handleSubmit = this.handleSubmit.bind(this);
  }

  private handleUsernameChange(event) {
    this.setState({ Username: event.target.value });
  }

  private handlePasswordChange(event) {
    this.setState({ Password: event.target.value });
  }

  private handleSubmit(event) {
    console.log('Form Submitted');
    event.preventDefault
  }

  public render() {
    return (
      <div id="login-wrapper">
        <section id="login-container">
          <div id="login-form-container">
            <div id="login-logo-container">
              <svg id="login-logo">
                <use xlinkHref={"#map-logo"} />
              </svg>
            </div>
            <form id="login-form" autoComplete="off" method="post">
              <div className="login-form-input-container">
                <input type="text" value={this.state.Username} onChange={this.handleUsernameChange} placeholder="Username" autoFocus />
              </div>
              <div className="login-form-input-container">
                <input type="password" value={this.state.Password} onChange={this.handlePasswordChange} placeholder="Password" autoFocus />
              </div>
              <div asp-validation-summary="All" className="text-danger"></div>
              <div className="login-form-button-container">
                <a asp-action="ForgotPassword" className="link-button">Forgot Password</a>
                <button type="submit" className="blue-button">Login</button>
              </div>
            </form>
          </div>
        </section>
      </div>
    )
  }
}