import * as React from 'react';

import {
  ContentReductionHierarchy, FieldValueChange, FieldValueChangeName, ReductionField,
  ReductionFieldValue,
} from '../../view-models/content-publishing';

interface HierarchyDiffsProps {
  changedOnly: boolean;
  hierarchy: ContentReductionHierarchy<ReductionFieldValue>;
}

export class HierarchyDiffs extends React.Component<HierarchyDiffsProps, {}> {

  public render() {
    const { hierarchy } = this.props;
    const hierarchyValues = hierarchy.fields.map((item) => (
      <>
        {this.renderFieldValues(item)}
      </>
    ));
    return (
      <div className="hierarchy-diffs">
        {hierarchyValues}
      </div>
    );
  }

  private renderFieldValues(field: ReductionField<ReductionFieldValue>) {
    const fieldValues = (this.props.changedOnly)
      ? field.values.map((item, key) =>
        item.valueChange !== FieldValueChange.noChange && (
          <tr key={key} className={`status-${item.valueChange}`}>
            <td>{FieldValueChangeName[item.valueChange]}</td>
            <td>{item.value}</td>
          </tr>),
      )
      : field.values.map((item, key) => (
        <tr key={key} className={`status-${item.valueChange}`}>
          <td>{FieldValueChangeName[item.valueChange]}</td>
          <td>{item.value}</td>
        </tr>),
      );

    return (
      <>
        <h4>{field.displayName}</h4>
        <table>
          <tr>
            <th className="header-status">Status</th>
            <th className="header-value">Value</th>
          </tr>
          {fieldValues}
        </table>
      </>
    );
  }

}
