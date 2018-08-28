import '../../images/add.svg';
import '../../images/collapse-cards.svg';
import '../../images/expand-cards.svg';
import '../../images/map-logo.svg';

import * as $ from 'jquery';
import * as React from 'react';
import * as ReactDOM from 'react-dom';
import * as toastr from 'toastr';

import { NavBar } from '../react/shared-components/navbar';
import { setup } from './dom-methods';

import 'toastr/toastr.scss';
import 'tooltipster';
import 'tooltipster/src/css/plugins/tooltipster/sideTip/tooltipster-sideTip.css';
import 'tooltipster/src/css/tooltipster.css';
import '../../scss/map.scss';

document.addEventListener('DOMContentLoaded', () => {
  const view = document.getElementsByTagName('body')[0].getAttribute('data-nav-location');
  ReactDOM.render(<NavBar currentView={view} />, document.getElementById('navbar'));
});

$(document).ready(() => {
  setup();
  toastr.info('Page loaded');  // TODO: Remove for production
});

if (module.hot) {
  module.hot.accept();
}
