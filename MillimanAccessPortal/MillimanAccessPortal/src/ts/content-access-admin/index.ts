import '../../images/add.svg';
import '../../images/collapse-cards.svg';
import '../../images/expand-cards.svg';
import '../../images/map-logo.svg';
import '../../scss/map.scss';
import '../navbar';

import 'bootstrap/scss/bootstrap-reboot.scss';
import 'toastr/toastr.scss';
import 'tooltipster';
import 'tooltipster/src/css/plugins/tooltipster/sideTip/tooltipster-sideTip.css';
import 'tooltipster/src/css/tooltipster.css';

import * as $ from 'jquery';
import * as toastr from 'toastr';

import { setup } from './dom-methods';

$(document).ready(() => {
  setup();
  toastr.info('Page loaded');  // TODO: Remove for production
});

if (module.hot) {
  module.hot.accept();
}
