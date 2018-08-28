import '../../../images/map-logo.svg';
// # sourceMappingURL=authorized-content.js.map

import * as React from 'react';
import * as ReactDOM from 'react-dom';

import { SystemAdmin } from './system-admin';

import '../../../scss/map.scss';

document.addEventListener('DOMContentLoaded', () => {
  ReactDOM.render(<SystemAdmin />, document.getElementById('content-container'));
});

if (module.hot) {
  module.hot.accept();
}
