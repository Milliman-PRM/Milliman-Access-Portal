import '../../../scss/react/shared-components/column-selector.scss';

import * as React from 'react';

import { ColumnSelectorProps } from './interfaces';

export class ColumnSelector extends React.Component<ColumnSelectorProps, {}> {
  public constructor(props) {
    super(props);
  }

  public render() {
    const options = this.props.colContentOptions.map((option, index) => {
      const selectorClass = (option.value === this.props.colContent)
        ? 'selected'
        : null;
      return (
        <div
          key={index}
          className={`content-option ${selectorClass}`}
          // tslint:disable-next-line:jsx-no-lambda
          onClick={() => this.props.colContentSelection(option)}
        >
          {option.label}
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
