import '../../../scss/react/authorized-content/content-card.scss';

import * as React from 'react';

require('tooltipster');
require('tooltipster/src/css/tooltipster.css');

interface FilterBarProps {
  onFilterStringChanged: (filterString: string) => void;
}

export class FilterBar extends Component<FilterBarProps, {}> {
  public constructor(props) {
    super(props);
    this.handleFilterStringChange = this.handleFilterStringChange.bind(this);
  }

  public render() {
    return (
      <input
        id="authorized-content-filter"
        name="authorizedContentFilter"
        type="text"
        placeholder="Filter content"
        onKeyUp={this.handleFilterStringChange}
        />
    )
  }

  private handleFilterStringChange(event: React.SyntheticEvent<EventTarget>) {
    this.props.onFilterStringChanged((event.target as HTMLInputElement).value);
  }
}
