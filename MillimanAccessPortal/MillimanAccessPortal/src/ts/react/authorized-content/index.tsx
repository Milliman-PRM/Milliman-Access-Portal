declare function require(moduleName: string): any;

import '../../../images/map-logo.svg';

import * as React from 'react';
import * as ReactDOM from 'react-dom';

import { StatusMonitor } from '../../status-monitor';
import { AuthorizedContent as Root } from './authorized-content';

import 'jquery';
import 'jquery-validation';
import 'jquery-validation-unobtrusive';
import 'toastr';
import 'tooltipster';
import 'vex-js';

import 'toastr/toastr.scss';
import 'tooltipster/src/css/tooltipster.css';
import 'vex-js/sass/vex.sass';
import '../../../scss/map.scss';
// # sourceMappingURL=authorized-content.js.map

let AuthorizedContent: typeof Root = require('./authorized-content').AuthorizedContent;

document.addEventListener('DOMContentLoaded', () => {
  ReactDOM.render(<AuthorizedContent />, document.getElementById('content-container'));
});

const statusMonitor = new StatusMonitor('/Account/SessionStatus', () => null, 60000);
statusMonitor.start();

if (module.hot) {
  module.hot.accept(['./authorized-content'], () => {
    AuthorizedContent = require('./authorized-content').AuthorizedContent;
  });
}
