import '../../images/add.svg';
import '../../images/cancel.svg';
import '../../images/checkmark.svg';
import '../../images/edit.svg';
import '../../images/map-logo.svg';
import '../../images/upload.svg';

import * as React from 'react';
import * as ReactDOM from 'react-dom';

import { NavBar } from '../react/shared-components/navbar';
import { setup } from './dom-methods';

import $ = require('jquery');
require('tooltipster');
import toastr = require('toastr');

import 'toastr/toastr.scss';
import 'tooltipster/src/css/plugins/tooltipster/sideTip/tooltipster-sideTip.css';
import 'tooltipster/src/css/tooltipster.css';
import '../../scss/map.scss';

document.addEventListener('DOMContentLoaded', (event) => {
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
