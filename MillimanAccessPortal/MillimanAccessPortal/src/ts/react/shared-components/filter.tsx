import '../../../scss/react/shared-components/filter.scss';

import * as React from 'react';

import '../../../images/filter.svg';

export interface FilterProps {
  filterText: string;
  setFilterText: (filterString: string) => void;
  placeholderText: string;
}

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
          value={this.props.filterText}
          className="filter-input"
          placeholder={this.props.placeholderText || 'Filter...'}
          onChange={this.handleChange}
          disabled={!this.props.placeholderText}
        />
        <svg className="filter-icon">
          <use xlinkHref="#filter" />
        </svg>
      </div>
    );
  }
}
