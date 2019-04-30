declare function require(moduleName: string): any;

import '../../../images/map-logo.svg';
import '../../../scss/map.scss';

import * as React from 'react';
import * as ReactDOM from 'react-dom';

import { ForgotPasswordForm as Root } from './forgot-password-form';

let ForgotPasswordForm: typeof Root = require('./forgot-password-form').ForgotPasswordForm;

document.addEventListener('DOMContentLoaded', () => {
  ReactDOM.render(<ForgotPasswordForm />, document.getElementById('content-container'));
});

if (module.hot) {
  module.hot.accept(['./forgot-password-form'], () => {
    ForgotPasswordForm = require('./forgot-password-form').ForgotPasswordForm;
  });
}
