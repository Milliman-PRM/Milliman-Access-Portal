declare function require(moduleName: string): any;

import '../../../images/map-logo.svg';
import '../../../scss/map.scss';

import * as React from 'react';
import * as ReactDOM from 'react-dom';
import { Provider } from 'react-redux';

import { ConnectedClientAccessReview as Root } from './client-access-review';
import { store } from './redux/store';

let ConnectedClientAccessReview: typeof Root = require('./client-access-review').ConnectedClientAccessReview;

document.addEventListener('DOMContentLoaded', () => {

  ReactDOM.render(
    <Provider store={store} >
      <ConnectedClientAccessReview />
    </Provider>,
    document.getElementById('content-container'),
  );
});

if (module.hot) {
  module.hot.accept(['./client-access-review'], () => {
    ConnectedClientAccessReview = require('./client-access-review').ConnectedClientAccessReview;
  });
}
