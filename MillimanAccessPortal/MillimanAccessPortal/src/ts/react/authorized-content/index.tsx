declare function require(moduleName: string): any;

import '../../../images/map-logo.svg';

import * as React from 'react';
import * as ReactDOM from 'react-dom';

import { AuthorizedContent as Root } from './authorized-content';

import 'tooltipster';

import 'tooltipster/src/css/tooltipster.css';
import '../../../scss/map.scss';

let AuthorizedContent: typeof Root = require('./authorized-content').AuthorizedContent;

document.addEventListener('DOMContentLoaded', () => {
  ReactDOM.render(<AuthorizedContent />, document.getElementById('content-container'));
});

if (module.hot) {
  module.hot.accept(['./authorized-content'], () => {
    AuthorizedContent = require('./authorized-content').AuthorizedContent;
  });
}
