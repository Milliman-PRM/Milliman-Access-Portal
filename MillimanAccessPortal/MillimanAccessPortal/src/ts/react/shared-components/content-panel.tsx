import '../../../scss/react/shared-components/content-panel.scss';

import * as React from 'react';

import { Column } from '../system-admin/interfaces';
import { ColumnSelector } from './column-selector';
import { ColumnSelectorOption, QueryFilter } from './interfaces';

export interface ContentPanelProps {
  controller: string;
  setQueryFilter: (queryFilter: QueryFilter) => void;
  queryFilter: QueryFilter;
  setSelectedColumn: (column: Column) => void;
  selectedColumn: Column;
}
interface ContentPanelState {
  action: string;
}

export abstract class ContentPanel extends React.Component<ContentPanelProps, ContentPanelState> {
  protected columns: ColumnSelectorOption[];

  public constructor(props) {
    super(props);

    this.state = {
      action: '',
    };

    this.selectColumn = this.selectColumn.bind(this);
  }

  public componentDidMount() {
    this.props.setSelectedColumn(this.getDefaultColumn());
  }

  public render() {
    return (
      <div
        className="admin-panel-container flex-item-12-12 flex-item-for-tablet-up-4-12 flex-item-for-desktop-up-3-12"
      >
        <ColumnSelector
          columnOptions={this.columns}
          setSelected={this.selectColumn}
          selected={this.props.selectedColumn}
        />
        {this.renderContentList()}
      </div>
    );
  }

  protected abstract renderContentList(): JSX.Element;
  protected abstract getDefaultColumn(): Column;

  private selectColumn(id: Column) {
    this.props.setSelectedColumn(id);
  }
}
