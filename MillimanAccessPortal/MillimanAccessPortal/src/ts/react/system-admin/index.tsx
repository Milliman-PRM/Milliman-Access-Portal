import 'bootstrap/scss/bootstrap-reboot.scss';
import '../../../images/map-logo.svg';
import '../../navbar';
// # sourceMappingURL=authorized-content.js.map

import * as React from 'react';
import * as ReactDOM from 'react-dom';

import { SystemAdmin } from './system-admin';

document.addEventListener('DOMContentLoaded', () => {
  ReactDOM.render(<SystemAdmin />, document.getElementById('content-container'));
});

if (module.hot) {
  module.hot.accept();
}
