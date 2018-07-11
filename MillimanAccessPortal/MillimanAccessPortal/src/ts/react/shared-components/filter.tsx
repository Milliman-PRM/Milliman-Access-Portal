import '../../../scss/react/shared-components/filter.scss';

import * as React from 'react';

import { FilterProps } from './interfaces';

export class Filter extends React.Component<FilterProps, {}> {
  public constructor(props) {
    super(props);
  }

  handleChange(event) {
    this.props.updateFilterString(event.target.value);
  }

  public render() {
    return (
      <div className="filter-container">
        <input type="text" placeholder={this.props.placeholderText} onChange={this.handleChange.bind(this)} />
      </div>
    );
  }
}
