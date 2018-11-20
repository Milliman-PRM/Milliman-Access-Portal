declare function require(moduleName: string): any;

import '../../../images/map-logo.svg';
import '../../../scss/map.scss';

// # sourceMappingURL=authorized-content.js.map
import * as React from 'react';
import * as ReactDOM from 'react-dom';

import { StatusMonitor } from '../../status-monitor';
import { SystemAdmin as Root } from './system-admin';

let SystemAdmin: typeof Root = require('./system-admin').SystemAdmin;

document.addEventListener('DOMContentLoaded', () => {
  ReactDOM.render(<SystemAdmin />, document.getElementById('content-container'));
});

const statusMonitor = new StatusMonitor('/Account/SessionStatus', () => null, 60000);
statusMonitor.start();

if (module.hot) {
  module.hot.accept(['./system-admin'], () => {
    SystemAdmin = require('./system-admin').SystemAdmin;
  });
}
