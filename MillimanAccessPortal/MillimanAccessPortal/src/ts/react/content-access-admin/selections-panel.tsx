import * as React from 'react';

import { Filter } from '../shared-components/filter';
import { Fieldset, FieldsetProps } from './fieldset';

export interface SelectionsPanelProps {
  doesReduce: boolean;
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
              <div className="switch-container">
                <div className="toggle-switch">
                  <input type="checkbox" className="toggle-switch-checkbox" name="IsSuspended" id="IsSuspended" />
                  <label className="toggle-switch-label" htmlFor="IsSuspended">
                    <span className="toggle-switch-inner" />
                    <span className="toggle-switch-switch" />
                  </label>
                </div>
                <label className="switch-label">Suspend Access</label>
              </div>
              {this.renderDoesReduceSection()}
            </form>
          </div>
        </div>
      </div>
    );
  }

  private renderDoesReduceSection() {
    return this.props.doesReduce
      ? (
        <div className="selection-content">
          <hr />
          <div className="switch-container">
            <div className="toggle-switch">
              <input type="checkbox" className="toggle-switch-checkbox" name="IsMaster" id="IsMaster" />
              <label className="toggle-switch-label" htmlFor="IsMaster">
                <span className="toggle-switch-inner" />
                <span className="toggle-switch-switch" />
              </label>
            </div>
            <label className="switch-label">Unrestricted Access</label>
          </div>
          <div className="fieldset-container">
            {this.renderReductionFields()}
          </div>
          <button type="button" className="blue-button button-status-0 button-status-1 button-status-40">
            Submit
          </button>
          <button type="button" className="red-button button-status-10">Cancel</button>
        </div>
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
