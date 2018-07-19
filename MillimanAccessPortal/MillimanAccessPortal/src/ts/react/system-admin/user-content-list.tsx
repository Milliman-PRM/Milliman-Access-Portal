import '../../../scss/react/shared-components/content-panel.scss';

import * as React from 'react';

import { ContentList } from '../shared-components/content-list';
import { QueryFilter } from '../shared-components/interfaces';
import { UserInfo } from './interfaces';
import { UserCard } from './user-card';

export class UserContentList extends ContentList<UserInfo, UserCard> {
  public constructor(props) {
    super(props);
  }

  protected renderQueryFilter(id: number): QueryFilter {
    const queryFilter = {...this.props.queryFilter};
    queryFilter.userId = id;
    return queryFilter;
  }
}
