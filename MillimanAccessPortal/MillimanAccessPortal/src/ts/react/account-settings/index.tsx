declare function require(moduleName: string): any;

import '../../../images/map-logo.svg';
import '../../../scss/map.scss';

import * as React from 'react';
import * as ReactDOM from 'react-dom';
import { Provider } from 'react-redux';

import { ConnectedAccountSettings as Root } from './account-settings';
import { store } from './redux/store';

let ConnectedAccountSettings: typeof Root = require('./account-settings').ConnectedAccountSettings;

document.addEventListener('DOMContentLoaded', () => {
  ReactDOM.render(
    <Provider store={store} >
      <ConnectedAccountSettings />
    </Provider>,
    document.getElementById('content-container'),
  );
});

if (module.hot) {
  module.hot.accept(['./account-settings'], () => {
    ConnectedAccountSettings = require('./account-settings').ConnectedAccountSettings;
  });
}
