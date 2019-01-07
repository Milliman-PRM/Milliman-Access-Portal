declare function require(moduleName: string): any;

import * as React from 'react';
import * as ReactDOM from 'react-dom';

import { Login as Root } from './login';

let Login: typeof Root = require('./login').Login;

document.addEventListener('DOMContentLoaded', () => {
  ReactDOM.render(<Login />, document.getElementById('content-container'));
});

if (module.hot) {
  module.hot.accept(['./login'], () => {
    Login = require('./login').Login;
  });
}
