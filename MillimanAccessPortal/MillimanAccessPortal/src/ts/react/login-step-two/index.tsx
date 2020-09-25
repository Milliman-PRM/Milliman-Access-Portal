declare function require(moduleName: string): any;

import * as React from 'react';
import * as ReactDOM from 'react-dom';

import { LoginStepTwo as Root } from './login-step-two';

let LoginStepTwo: typeof Root = require('./login-step-two').LoginStepTwo;

document.addEventListener('DOMContentLoaded', () => {
  ReactDOM.render(<LoginStepTwo />, document.getElementById('content-wrapper'));
});

if (module.hot) {
  module.hot.accept(['./login-step-two'], () => {
    LoginStepTwo = require('./login-step-two').LoginStepTwo;
  });
}
