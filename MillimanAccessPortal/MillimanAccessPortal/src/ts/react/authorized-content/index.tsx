import * as React from 'react';
import * as ReactDOM from 'react-dom';
import { ContentCollection } from './content-collection';

require('jquery');
require('jquery-validation');
require('jquery-validation-unobtrusive');
require('../../navbar');
require('toastr');
require('tooltipster');
require('vex-js');
require('bootstrap/scss/bootstrap-reboot.scss');
require('toastr/toastr.scss');
require('tooltipster/src/css/tooltipster.css');
require('vex-js/sass/vex.sass');
require('../../../scss/map.scss');
//# sourceMappingURL=authorized-content.js.map

document.addEventListener("DOMContentLoaded", (event) => {
  ReactDOM.render(<ContentCollection />, document.getElementById('content-container'));
});
