declare function require(moduleName: string): any;

import * as React from 'react';
import * as ReactDOM from 'react-dom';
import { Provider } from 'react-redux';

import { ConnectedClientAdmin as Root } from './client-admin';
import { store } from './redux/store';

import '../../../images/map-logo.svg';
import '../../../scss/map.scss';

let ConnectedClientAdmin: typeof Root = require('./client-admin').ConnectedClientAdmin;

document.addEventListener('DOMContentLoaded', () => {
  ReactDOM.render(
    <Provider store={store} >
      <ConnectedClientAdmin />
    </Provider>,
    document.getElementById('content-container'),
  );
});

if (module.hot) {
  module.hot.accept(['./client-admin'], () => {
    ConnectedClientAdmin = require('./client-admin').ConnectedClientAdmin;
  });
}
