import '../../../scss/react/shared-components/column-selector.scss';

import * as React from 'react';
import { ColumnSelectorOption } from './interfaces';

interface ColumnSelectorProps {
  options: ColumnSelectorOption[];
  select: (id: number) => void;
  selected: number;
}

export class ColumnSelector extends React.Component<ColumnSelectorProps, {}> {
  public constructor(props) {
    super(props);
  }

  public render() {
    const options = this.props.options.map((option, index) => {
      const selectorClass = (this.props.selected === index)
        ? 'selected'
        : null;
      return (
        <div
          key={index}
          className={`content-option ${selectorClass}`}
          // tslint:disable-next-line:jsx-no-lambda
          onClick={() => this.props.select(index)}
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
