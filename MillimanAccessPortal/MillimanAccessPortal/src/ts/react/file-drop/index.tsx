declare function require(moduleName: string): any;

import '../../../images/map-logo.svg';
import '../../../scss/map.scss';

import * as React from 'react';
import * as ReactDOM from 'react-dom';

import { FileDrop as Root } from './file-drop';

let FileDrop: typeof Root = require('./file-drop').FileDrop;

document.addEventListener('DOMContentLoaded', () => {
  ReactDOM.render(<FileDrop />, document.getElementById('content-container'));
});

if (module.hot) {
  module.hot.accept(['./file-drop'], () => {
    FileDrop = require('./file-drop').FileDrop;
  });
}
