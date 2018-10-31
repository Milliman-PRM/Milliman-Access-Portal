import '../../../images/map-logo.svg';
import '../../../scss/map.scss';

import * as React from 'react';
import * as ReactDOM from 'react-dom';

import { StatusMonitor } from '../../status-monitor';

document.addEventListener('DOMContentLoaded', () => {
  ReactDOM.render(
    <div />,
    document.getElementById('content-container'),
  );
});

// const statusMonitor = new StatusMonitor('/Account/SessionStatus', () => null, 60000);
// statusMonitor.start();

if (module.hot) {
  module.hot.accept();
}
