declare function require(moduleName: string): any;

import '../../images/icons/add.svg';
import '../../images/icons/cancel.svg';
import '../../images/icons/checkmark.svg';
import '../../images/icons/edit.svg';
import '../../images/icons/upload.svg';
import '../../images/map-logo.svg';

import * as React from 'react';
import * as ReactDOM from 'react-dom';

import { NavBar } from '../react/shared-components/navbar';
import { setup as Root } from './dom-methods';

import $ = require('jquery');
require('tooltipster');
import toastr = require('toastr');

import 'toastr/toastr.scss';
import 'tooltipster/src/css/plugins/tooltipster/sideTip/tooltipster-sideTip.css';
import 'tooltipster/src/css/tooltipster.css';
import '../../scss/map.scss';

let setup: typeof Root = require('./dom-methods').setup;

document.addEventListener('DOMContentLoaded', () => {
  const view = document.getElementsByTagName('body')[0].getAttribute('data-nav-location');
  ReactDOM.render(<NavBar currentView={view} />, document.getElementById('navbar'));
});

$(() => {
  setup();
});

if (module.hot) {
  module.hot.accept(['./dom-methods'], () => {
    setup = require('./dom-methods').setup;
  });
}
