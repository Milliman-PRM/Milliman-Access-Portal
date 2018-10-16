import '../../../scss/react/shared-components/column-selector.scss';

import * as React from 'react';

export interface ColumnIndicator {
  id: string;
  name: string;
}

export interface ColumnSelectorProps {
  columns: ColumnIndicator[];
  onColumnSelect: (id: string) => void;
  selectedColumn: ColumnIndicator;
}

export class ColumnSelector extends React.Component<ColumnSelectorProps> {
  public render() {
    return (
      <div className="content-options">
        {this.renderColumns()}
      </div>
    );
  }

  private renderColumns() {
    const { columns, onColumnSelect, selectedColumn } = this.props;
    return columns.map((column) => (
      <div
        key={column.id}
        className={`content-option${column.id === (selectedColumn && selectedColumn.id) ? ' selected' : ''}`}
        onClick={() => onColumnSelect(column.id)}
      >
        {column.name}
      </div>
    ));
  }
}
