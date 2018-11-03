declare function require(moduleName: string): any;

import '../../../images/map-logo.svg';
import '../../../scss/map.scss';

import * as React from 'react';
import * as ReactDOM from 'react-dom';
import { Provider } from 'react-redux';

import { StatusMonitor } from '../../status-monitor';
import { ConnectedContentAccessAdmin as Root } from './content-access-admin';
import { store } from './redux/store';

let ConnectedContentAccessAdmin: typeof Root = require('./content-access-admin').ConnectedContentAccessAdmin;

document.addEventListener('DOMContentLoaded', () => {

  ReactDOM.render(
    <Provider store={store} >
      <ConnectedContentAccessAdmin />
    </Provider>,
    document.getElementById('content-container'),
  );
});

// const statusMonitor = new StatusMonitor('/Account/SessionStatus', () => null, 60000);
// statusMonitor.start();

if (module.hot) {
  module.hot.accept(['./content-access-admin'], () => {
    ConnectedContentAccessAdmin = require('./content-access-admin').ConnectedContentAccessAdmin;
  });
}
