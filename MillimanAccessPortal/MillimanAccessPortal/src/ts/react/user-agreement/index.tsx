import '../../../images/map-logo.svg';
import '../../../scss/map.scss';

import * as React from 'react';
import * as ReactDOM from 'react-dom';

import { UserAgreement as Root } from './user-agreement';

let UserAgreement: typeof Root = require('./user-agreement')
  .UserAgreement;
