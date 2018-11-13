import * as React from 'react';

import { ReductionStatus } from '../../view-models/content-publishing';
import { Filter } from '../shared-components/filter';
import { Toggle } from '../shared-components/toggle';
import { Fieldset, FieldsetData } from './fieldset';

export interface SelectionsPanelProps {
  isSuspended: boolean;
  doesReduce: boolean;
  isModified: boolean;
  isMaster: boolean;
  onIsMasterChange: (value: boolean) => void;
  title: string;
  subtitle: string;
  status: ReductionStatus;
  fieldsets: FieldsetData[];
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
              <h2>{this.props.title}</h2>
              <h3>{this.props.subtitle}</h3>
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
          {this.renderButtonSection()}
        </div>
      )
      : null;
  }

  private renderReductionSection() {
    return this.props.isMaster
      ? null
      : (
        <div className="fieldset-container">
          {this.renderReductionFields()}
        </div>
      );
  }

  private renderReductionFields() {
    const { fieldsets, status } = this.props;
    return fieldsets.map((fieldset) => (
      <Fieldset
        key={fieldset.name}
        readOnly={status === ReductionStatus.Queued}
        {...fieldset}
      />
    ));
  }

  private renderButtonSection() {
    switch (this.props.status) {
      case ReductionStatus.Unspecified:
      case ReductionStatus.Canceled:
      case ReductionStatus.Rejected:
      case ReductionStatus.Live:
      case ReductionStatus.Error:
        return this.props.isModified
          ? <button type="button" className="blue-button">Submit</button>
          : null;
      case ReductionStatus.Queued:
        return <button type="button" className="red-button">Cancel</button>;
      default:
        return null;
    }
  }
}
