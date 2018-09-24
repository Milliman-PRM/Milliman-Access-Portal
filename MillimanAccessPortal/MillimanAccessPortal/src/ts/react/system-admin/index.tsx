import '../../../images/map-logo.svg';
import '../../../scss/map.scss';

// # sourceMappingURL=authorized-content.js.map
import * as React from 'react';
import * as ReactDOM from 'react-dom';

import { StatusMonitor } from '../../status-monitor';
import { SystemAdmin } from './system-admin';

document.addEventListener('DOMContentLoaded', () => {
  ReactDOM.render(<SystemAdmin />, document.getElementById('content-container'));
});

const statusMonitor = new StatusMonitor('/Account/SessionStatus', () => null, 60);
statusMonitor.start();

if (module.hot) {
  module.hot.accept();
}
