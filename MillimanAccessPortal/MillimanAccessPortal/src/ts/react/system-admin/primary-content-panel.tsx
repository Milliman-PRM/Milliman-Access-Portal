import '../../../scss/react/shared-components/content-panel.scss';

import * as React from 'react';

import { ContentPanel } from '../shared-components/content-panel';
import { UserContentList } from './user-content-list';

export class PrimaryContentPanel extends ContentPanel {
  public constructor(props) {
    super(props);

    this._options = [
      {
        contentList: (
          <UserContentList
            action={'Users'}
            controller={this.props.controller}
            queryFilter={this.props.queryFilter}
            setQueryFilter={this.props.setQueryFilter}
          />
        ),
        displayName: 'Users',
      },
      {
        contentList: (
          <li />
        ),
        displayName: 'Clients',
      },
      {
        contentList: (
          <li />
        ),
        displayName: 'PCs',
      },
    ];
  }
}
