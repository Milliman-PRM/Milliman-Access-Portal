import * as React from 'react';

import { SelectionGroupSummary } from '../../view-models/content-publishing';
import { HierarchyDiffs } from './hierarchy-diffs';

interface SelectionGroupDetailsProps {
  selectionGroup: SelectionGroupSummary;
  changedOnly: boolean;
  key: number | string;
}

export class SelectionGroupDetails extends React.Component<SelectionGroupDetailsProps, {}> {
  public render() {
    const { changedOnly, key, selectionGroup } = this.props;
    const prevStatus = selectionGroup.wasInactive
      ? <>[<span className="inactive-status">INACTIVE</span>]</>
      : <>[<span className="active-status">ACTIVE</span>]</>;
    const newStatus = selectionGroup.isInactive
      ? <>[<span className="inactive-status">INACTIVE</span>]</>
      : <>[<span className="active-status">ACTIVE</span>]</>;
    const statusDetail = (selectionGroup.wasInactive !== selectionGroup.isInactive)
      ? <span className="status">{prevStatus} > {newStatus}</span>
      : <span className="status">{newStatus}</span>;
    return (
      <div key={key} className="selection-group-detail">
        <h3>{selectionGroup.name} {statusDetail}</h3>
        {
          selectionGroup.selectionChanges && (
            <HierarchyDiffs
              hierarchy={selectionGroup.selectionChanges}
              changedOnly={changedOnly}
            />
          )
        }
      </div>
    );
  }
}
