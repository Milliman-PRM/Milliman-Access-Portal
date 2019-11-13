import * as React from 'react';

interface HierarchyDiffsProps {
  changedOnly: boolean;
  hierarchy: []
}

enum FieldValueChange {
  noChange = 0,
  added = 1,
  removed = 2,
}

export class HierarchyDiffs extends React.Component<HierarchyDiffsProps, {}> {
  public render() {
    const { changedOnly, hierarchy } = this.props;
    return (
      <div className="hierarchy-diffs">
      </div>
    );
  }
}
