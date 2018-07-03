import '../../../scss/react/shared-components/columnSelector.scss';

import * as React from 'react';

import { columnSelectorProps } from './interfaces';

export class SystemAdmin extends React.Component<columnSelectorProps, {}> {

  public render() {
    return (
      <div className="content-options">
        {this.props.colContentOptions.map(function (option, index) {
          let selectorClass = (option === this.props.colContent) ? 'selected' : null;
          return (
            <div key={index}
              className="content-option `${selectorClass}`"
              onClick={() => this.props.colContentSelection(option)}>
              {option}
            </div>
           );
          }
        )}
      </div>
    );
  }
}
