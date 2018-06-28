import '../navbar';

import 'bootstrap/scss/bootstrap-reboot.scss';
import 'toastr/toastr.scss';
import 'tooltipster/src/css/plugins/tooltipster/sideTip/tooltipster-sideTip.css';
import 'tooltipster/src/css/tooltipster.css';
import '../../scss/map.scss';

import { setup } from './dom-methods';

import $ = require('jquery');
require('tooltipster');
import toastr = require('toastr');

require('../../images/map-logo.svg');
require('../../images/add.svg');
require('../../images/edit.svg');
require('../../images/upload.svg');
require('../../images/cancel.svg');
require('../../images/checkmark.svg')

$(document).ready(() => {
  setup();
  toastr.info('Page loaded');  // TODO: Remove for production
});

if (module.hot) {
  module.hot.accept();
}
