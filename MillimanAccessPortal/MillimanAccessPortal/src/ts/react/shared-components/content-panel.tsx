import '../../../scss/react/shared-components/content-panel.scss';

import * as React from 'react';

import { ColumnSelector } from './column-selector';
import { ContentList } from './content-list';
import { ColumnSelectorOption, QueryFilter } from './interfaces';

export interface ContentPanelProps {
  setQueryFilter: (queryFilter: QueryFilter) => void;
  queryFilter: QueryFilter;
  controller: string;
}
interface ContentPanelState {
  action: string;
  selectedColumn: number;
}

export abstract class ContentPanel extends React.Component<ContentPanelProps, ContentPanelState> {
  protected _options: ColumnSelectorOption[];
  private get options() {
    return this._options;
  }

  public constructor(props) {
    super(props);

    this.state = {
      action: '',
      selectedColumn: 0,
    };

    this.selectColumn = this.selectColumn.bind(this);
  }

  public render() {
    return (
      <div
        className="admin-panel-container flex-item-12-12 flex-item-for-tablet-up-4-12 flex-item-for-desktop-up-3-12"
      >
        <ColumnSelector
          options={this.options}
          select={this.selectColumn}
          selected={this.state.selectedColumn}
        />
        {this.options[this.state.selectedColumn].contentList}
      </div>
    );
  }

  private selectColumn(id: number) {
    this.setState({
      selectedColumn: id,
    });
  }
}
