declare function require(moduleName: string): any;

import '../../../images/map-logo.svg';
import '../../../scss/map.scss';

import * as React from 'react';
import * as ReactDOM from 'react-dom';
import { Provider } from 'react-redux';

import { ConnectedFileDrop as Root } from './file-drop';
import { store } from './redux/store';

let ConnectedFileDrop: typeof Root = require('./file-drop').ConnectedFileDrop;

document.addEventListener('DOMContentLoaded', () => {
  ReactDOM.render(
    <Provider store={store}>
      <ConnectedFileDrop />
    </Provider>,
    document.getElementById('content-container'));
});

if (module.hot) {
  module.hot.accept(['./file-drop'], () => {
    ConnectedFileDrop = require('./file-drop').ConnectedFileDrop;
  });
}
