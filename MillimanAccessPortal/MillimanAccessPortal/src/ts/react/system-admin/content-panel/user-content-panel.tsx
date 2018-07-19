import '../../../../scss/react/shared-components/content-panel.scss';

import * as React from 'react';

import { ContentPanel } from '../../shared-components/content-panel';
import { Column } from '../interfaces';

export class UserContentPanel extends ContentPanel {
  public constructor(props) {
    super(props);

    this.columns = [
      {
        column: Column.Client,
        displayName: 'Clients',
      },
      {
        column: Column.RootContentItem,
        displayName: 'Authorized Content',
      },
    ];
  }

  protected renderContentList(): JSX.Element {
    switch (this.props.selectedColumn) {
      case Column.Client:
        return (
          <li />
        );
      case Column.RootContentItem:
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
    return Column.Client;
  }
}
