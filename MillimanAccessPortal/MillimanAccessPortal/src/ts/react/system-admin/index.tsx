import * as React from 'react';
import * as ReactDOM from 'react-dom';

import { SystemAdmin } from './system-admin.1';

require('jquery');
require('../../navbar');
require('toastr');
require('tooltipster');
require('vex-js');
require('bootstrap/scss/bootstrap-reboot.scss');
require('toastr/toastr.scss');
require('tooltipster/src/css/tooltipster.css');
require('vex-js/sass/vex.sass');
require('../../../scss/map.scss');
// # sourceMappingURL=authorized-content.js.map

require('../../../images/map-logo.svg');

document.addEventListener('DOMContentLoaded', (event) => {
  ReactDOM.render(<SystemAdmin />, document.getElementById('content-container'));
});

if (module.hot) {
  module.hot.accept();
}
