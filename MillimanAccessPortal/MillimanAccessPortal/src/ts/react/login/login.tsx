import '../../../scss/react/login/login.scss';

import * as React from 'react';

import { BrowserSupportBanner } from '../shared-components/browser-support-banner';
import { LoginForm } from './login-form';

import '../../../images/map-logo.svg';
import '../../../scss/map.scss';

export class Login extends React.Component {
  public render() {
    return (
      <>
        <div id="login-splash-panel" />
        <div id="login-wrapper">
          <section id="login-container">
            <div id="login-form-container">
              <div id="login-logo-container">
                <svg id="login-logo">
                  <use xlinkHref={'#map-logo'} />
                </svg>
              </div>
              <LoginForm />
            </div>
          </section>
        </div>
        <BrowserSupportBanner />
      </>
    );
  }
}
