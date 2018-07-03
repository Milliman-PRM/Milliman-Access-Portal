import '../../../scss/react/shared-components/column-selector.scss';

import * as React from 'react';

import { ColumnSelectorProps } from './interfaces';

export class ColumnSelector extends React.Component<ColumnSelectorProps, {}> {
  public constructor(props) {
    super(props);
  }

  public render() {
    return (
      <div className="content-options">
        {this.props.colContentOptions.map((option, index) => {
          let selectorClass = (option === this.props.colContent) ? 'selected' : null;
          return (
            <div key={index}
              className={`content-option ${selectorClass}`}
              onClick={() => this.props.colContentSelection(option, this.props.primaryColumn)}>
              {option}
            </div>
           );
          }
        )}
      </div>
    );
  }
}
