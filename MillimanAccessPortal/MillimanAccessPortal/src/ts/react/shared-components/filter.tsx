import '../../../scss/react/shared-components/filter.scss';

import * as React from 'react';

import { FilterProps } from './interfaces';

import '../../../images/filter.svg';

export class Filter extends React.Component<FilterProps, {}> {
  public constructor(props) {
    super(props);

    this.handleChange = this.handleChange.bind(this);
  }

  public handleChange(event) {
    this.props.setFilterText(event.target.value);
  }

  public render() {
    return (
      <div className="filter-container">
        <input
          type="text"
          key={this.props.placeholderText}
          className="filter-input"
          placeholder={this.props.placeholderText}
          onChange={this.handleChange}
        />
        <svg className="filter-icon">
          <use xlinkHref="#filter" />
        </svg>
      </div>
    );
  }
}
