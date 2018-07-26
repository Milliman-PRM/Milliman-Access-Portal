import * as React from 'react';
import * as ReactDOM from 'react-dom';

import { SystemAdmin } from './system-admin';

require('../../navbar');
require('bootstrap/scss/bootstrap-reboot.scss');
require('../../../scss/map.scss');
// # sourceMappingURL=authorized-content.js.map

require('../../../images/map-logo.svg');

document.addEventListener('DOMContentLoaded', (event) => {
  ReactDOM.render(<SystemAdmin />, document.getElementById('content-container'));
});

if (module.hot) {
  module.hot.accept();
}
