import * as React from 'react';

import { SelectionGroupSummary } from '../../view-models/content-publishing';

interface SelectionGroupDetailsProps {
  selectionGroup: SelectionGroupSummary;
}

export class SelectionGroupDetails extends React.Component<SelectionGroupDetailsProps, {}> {
  public render() {
    const { selectionGroup } = this.props;
    return (
      <div className="selection-group-detail">
      </div>
    );
  }
}
