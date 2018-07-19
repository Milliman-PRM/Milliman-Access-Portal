import '../../../../scss/react/shared-components/content-panel.scss';

import * as React from 'react';

import { ContentPanel } from '../../shared-components/content-panel';
import { Column } from '../interfaces';
import { UserContentList } from '../user-content-list';

export class ProfitCenterContentPanel extends ContentPanel {
  public constructor(props) {
    super(props);

    this.columns = [
      {
        column: Column.User,
        displayName: 'Authorized Users',
      },
      {
        column: Column.Client,
        displayName: 'Clients',
      },
    ];
  }

  protected renderContentList(): JSX.Element {
    switch (this.props.selectedColumn) {
      case Column.User:
        return (
          <UserContentList
            action={'Users'}
            controller={this.props.controller}
            queryFilter={this.props.queryFilter}
            setQueryFilter={this.props.setQueryFilter}
          />
        );
      case Column.Client:
        return (
          <li />
        );
      default:
        return (
          <li />
        );
    }
  }

  protected getDefaultColumn(): Column {
    return Column.User;
  }
}
