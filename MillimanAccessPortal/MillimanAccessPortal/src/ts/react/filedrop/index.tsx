declare function require(moduleName: string): any;

import '../../../images/map-logo.svg';
import '../../../scss/map.scss';

import * as React from 'react';
import * as ReactDOM from 'react-dom';

import { FileDrop as Root } from './filedrop';

let FileDrop: typeof Root = require('./filedrop').FileDrop;

document.addEventListener('DOMContentLoaded', () => {
  ReactDOM.render(<FileDrop />, document.getElementById('content-container'));
});

if (module.hot) {
  module.hot.accept(['./filedrop'], () => {
    FileDrop = require('./filedrop').FileDrop;
  });
}
