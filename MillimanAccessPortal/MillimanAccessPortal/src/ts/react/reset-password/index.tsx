declare function require(moduleName: string): any;

import '../../../images/map-logo.svg';
import '../../../scss/map.scss';

import * as React from 'react';
import * as ReactDOM from 'react-dom';

import { ResetPasswordForm as Root } from './reset-password-form';

let ResetPasswordForm: typeof Root = require('./reset-password-form')
  .ResetPasswordForm;

document.addEventListener('DOMContentLoaded', () => {
  ReactDOM.render(
    <ResetPasswordForm />,
    document.getElementById('content-container'),
  );
});

if (module.hot) {
  module.hot.accept(['./reset-password-form'], () => {
    ResetPasswordForm = require('./reset-password-form').ResetPasswordForm;
  });
}
