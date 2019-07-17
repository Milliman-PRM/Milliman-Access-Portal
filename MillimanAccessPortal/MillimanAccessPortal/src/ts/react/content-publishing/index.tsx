declare function require(moduleName: string): any;

import '../../../images/map-logo.svg';
import '../../../scss/map.scss';

import * as React from 'react';
import * as ReactDOM from 'react-dom';
import { Provider } from 'react-redux';

import { ConnectedContentPublishing as Root } from './content-publishing';
import { store } from './redux/store';

let ConnectedContentPublishing: typeof Root = require('./content-publishing').ConnectedContentPublishing;

document.addEventListener('DOMContentLoaded', () => {

  ReactDOM.render(
    <Provider store={store} >
      <ConnectedContentPublishing />
    </Provider>,
    document.getElementById('content-container'),
  );
});

if (module.hot) {
  module.hot.accept(['./content-publishing'], () => {
    ConnectedContentPublishing = require('./content-publishing').ConnectedContentPublishing;
  });
}
