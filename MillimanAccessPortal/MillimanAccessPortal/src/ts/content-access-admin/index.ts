import '../navbar';

import 'bootstrap/scss/bootstrap-reboot.scss';
import 'toastr/toastr.scss';
import 'tooltipster';
import 'tooltipster/src/css/plugins/tooltipster/sideTip/tooltipster-sideTip.css';
import 'tooltipster/src/css/tooltipster.css';
import '../../scss/map.scss';

import * as $ from 'jquery';
import * as toastr from 'toastr';

import { setup } from './dom-methods';

require('../../images/map-logo.svg');
require('../../images/add.svg');
require('../../images/collapse-cards.svg');
require('../../images/expand-cards.svg');

$(document).ready(() => {
  setup();
  toastr.info('Page loaded');  // TODO: Remove for production
});

if (module.hot) {
  module.hot.accept();
}
