import 'tooltipster';
import 'tooltipster/src/css/tooltipster.css';

import * as React from 'react';

import { Card } from '../shared-components/card';
import { UserInfo } from './interfaces';

export class UserCard extends Card<UserInfo> {
  public constructor(props) {
    super(props);
  }
}
