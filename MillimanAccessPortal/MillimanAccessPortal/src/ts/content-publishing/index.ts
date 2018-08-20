import '../../images/add.svg';
import '../../images/cancel.svg';
import '../../images/checkmark.svg';
import '../../images/edit.svg';
import '../../images/map-logo.svg';
import '../../images/upload.svg';
import '../../scss/map.scss';
import '../navbar';

import 'bootstrap/scss/bootstrap-reboot.scss';
import 'toastr/toastr.scss';
import 'tooltipster/src/css/plugins/tooltipster/sideTip/tooltipster-sideTip.css';
import 'tooltipster/src/css/tooltipster.css';

import { setup } from './dom-methods';

import $ = require('jquery');
require('tooltipster');
import toastr = require('toastr');

$(document).ready(() => {
  setup();
  toastr.info('Page loaded');  // TODO: Remove for production
});

if (module.hot) {
  module.hot.accept();
}
