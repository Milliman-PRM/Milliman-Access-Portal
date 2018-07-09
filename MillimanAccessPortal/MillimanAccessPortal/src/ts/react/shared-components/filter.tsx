import '../../../scss/react/shared-components/filter.scss';

import * as React from 'react';

import { FilterProps } from './interfaces';

export class Filter extends React.Component<FilterProps, {}> {
  public constructor(props) {
    super(props);
  }

  public render() {
    return (
      <div className="filter-container">
      </div>
    );
  }
}
