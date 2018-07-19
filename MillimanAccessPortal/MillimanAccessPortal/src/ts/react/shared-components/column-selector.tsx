import '../../../scss/react/shared-components/column-selector.scss';

import * as React from 'react';

import { Column } from '../system-admin/interfaces';
import { ColumnSelectorOption } from './interfaces';

interface ColumnSelectorProps {
  columnOptions: ColumnSelectorOption[];
  setSelected: (id: Column) => void;
  selected: Column;
}

export class ColumnSelector extends React.Component<ColumnSelectorProps, {}> {
  public constructor(props) {
    super(props);
  }

  public render() {
    const options = this.props.columnOptions.map((option) => {
      const selectorClass = (this.props.selected === option.column)
        ? 'selected'
        : null;
      return (
        <div
          key={option.column}
          className={`content-option ${selectorClass}`}
          // tslint:disable-next-line:jsx-no-lambda
          onClick={() => this.props.setSelected(option.column)}
        >
          {option.displayName}
        </div>
      );
    });
    return (
      <div className="content-options">
        {options}
      </div>
    );
  }
}
