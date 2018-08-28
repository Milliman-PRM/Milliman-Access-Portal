import * as React from 'react';
import * as ReactDOM from 'react-dom';

import '../../../images/map-logo.svg';

import { AuthorizedContent } from './authorized-content';

require('jquery');
require('jquery-validation');
require('jquery-validation-unobtrusive');
require('toastr');
require('tooltipster');
require('vex-js');
require('toastr/toastr.scss');
require('tooltipster/src/css/tooltipster.css');
require('vex-js/sass/vex.sass');
require('../../../scss/map.scss');
// # sourceMappingURL=authorized-content.js.map

document.addEventListener('DOMContentLoaded', () => {
  ReactDOM.render(<AuthorizedContent />, document.getElementById('content-container'));
});
