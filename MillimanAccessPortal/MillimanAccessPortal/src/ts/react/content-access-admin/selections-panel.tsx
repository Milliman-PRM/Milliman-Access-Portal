import * as React from 'react';

import { Filter } from '../shared-components/filter';
import { Toggle } from '../shared-components/toggle';
import { Fieldset, FieldsetProps } from './fieldset';

export interface SelectionsPanelProps {
  isSuspended: boolean;
  doesReduce: boolean;
  isModified: boolean;
  isMaster: boolean;
  onIsMasterChange: (value: boolean) => void;
  fieldsets: FieldsetProps[];
}

export class SelectionsPanel extends React.Component<SelectionsPanelProps> {
  public render() {
    return (
      <div
        className="admin-panel-container flex-item-12-12 flex-item-for-tablet-up-4-12 flex-item-for-desktop-up-3-12"
      >
        <h3 className="admin-panel-header">Selections</h3>
        <div className="admin-panel-toolbar">
          <Filter
            placeholderText={''}
            setFilterText={() => null}
            filterText={''}
          />
          <div className="admin-panel-action-icons-container" />
        </div>
        <div className="admin-panel-list">
          <div className="admin-panel-content-container">
            <form className="admin-panel-content">
              <h2>Title 1</h2>
              <h3>Title 2</h3>
              <Toggle
                label={'Suspend Access'}
                checked={this.props.isSuspended}
                onClick={() => null}
              />
              {this.renderDoesReduceSection()}
            </form>
          </div>
        </div>
      </div>
    );
  }

  private renderDoesReduceSection() {
    const { doesReduce, isMaster, onIsMasterChange } = this.props;
    return doesReduce
      ? (
        <div className="selection-content">
          <hr />
          <Toggle
            label={'Unrestricted Access'}
            checked={isMaster}
            onClick={() => onIsMasterChange(!isMaster)}
          />
          {this.renderReductionSection()}
        </div>
      )
      : null;
  }

  private renderReductionSection() {
    return this.props.isMaster
      ? null
      : (
        <>
          {this.renderSelectionsModifiedBanner()}
          <div className="fieldset-container">
            {this.renderReductionFields()}
          </div>
          <button type="button" className="blue-button button-status-0 button-status-1 button-status-40">
            Submit
          </button>
          <button type="button" className="red-button button-status-10">Cancel</button>
        </>
      );
  }

  private renderSelectionsModifiedBanner() {
    return this.props.isModified
    ? (
      <h4>* selections have been modified</h4>
    )
    : null;
  }

  private renderReductionFields() {
    const { fieldsets } = this.props;
    return fieldsets.map((fieldset) => (
      <Fieldset key={fieldset.name} {...fieldset} />
    ));
  }
}
