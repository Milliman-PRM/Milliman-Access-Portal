import '../../../scss/react/login/login.scss';

import * as React from 'react';

import { getData } from '../../shared';

import { LoginState } from './interfaces';
import LoginForm from './login-form';
import Input from '../shared-components/input';


import '../../../images/map-logo.svg';
import '../../../images/icons/user.svg';
import '../../../images/icons/password.svg';
import '../../../scss/map.scss';


export class Login extends React.Component <{}, LoginState > {
  public constructor(props) {
    super(props);

    this.state = {};
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
              <LoginForm />
            </div>
          </section>
        </div>
      </>
    )
  }
}


//<div className = "login-form-input-container" >
//  <input name="Username" type="text" value={this.state.Username} onChange={this.handleChange} onBlur={this.handleBlur} placeholder="Username" autoFocus />
//</div>
//<div className="login-form-input-container">
//  <input name="Password" type="password" value={this.state.Password} onChange={this.handleChange} placeholder="Password" />
//</div>



//<form id="login-form" autoComplete="off" action="/Account/Login?returnurl=%2F" method="post">
//  <Input
//    name="Username"
//    label="Username"
//    placeholderText="Username"
//    inputIcon="user"
//    value={this.state.Username}
//    handleChange={this.handleChange}
//    type="text"
//  />
//  <Input
//    name="Password"
//    label="Password"
//    inputIcon="password"
//    value={this.state.Password}
//    handleChange={this.handleChange}
//    type="password"
//  />
//  <input name="__RequestVerificationToken" type="hidden" value={this.state.RequestValidationToken} />
//  <div className="login-form-button-container">
//    <a href="/Account/ForgotPassword" className="link-button">Forgot Password</a>
//    <button type="submit" className="blue-button">Login</button>
//  </div>
//</form>
