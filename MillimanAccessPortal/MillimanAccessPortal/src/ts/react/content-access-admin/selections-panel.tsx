import * as React from 'react';

import { isReductionActive, ReductionStatus } from '../../view-models/content-publishing';
import { PanelSectionContainer } from '../shared-components/card-panel/panel-sections';
import { LoadingSpinner } from '../shared-components/loading-spinner';
import { Toggle } from '../shared-components/toggle';
import { Fieldset, FieldsetData } from './fieldset';

export interface SelectionsPanelProps {
  isSuspended: boolean;
  onIsSuspendedChange: (value: boolean) => void;
  doesReduce: boolean;
  isModified: boolean;
  isMaster: boolean;
  onIsMasterChange: (value: boolean) => void;
  title: string;
  subtitle: string;
  status: ReductionStatus;
  onBeginReduction: () => void;
  onCancelReduction: () => void;
  loading?: boolean;
  fieldsets: FieldsetData[];
}

export class SelectionsPanel extends React.Component<SelectionsPanelProps> {
  public render() {
    const { children } = this.props;
    return (
      <PanelSectionContainer>
        <h3 className="admin-panel-header">Selections</h3>
        {children}
        {this.renderFormSection()}
      </PanelSectionContainer>
    );
  }

  private renderFormSection() {
    const { title, subtitle, isSuspended, loading } = this.props;
    return loading
      ? <LoadingSpinner />
      : (
        <div className="admin-panel-form">
          <div className="admin-panel-content-container">
            <form className="admin-panel-content">
              <h2>{title}</h2>
              <h3>{subtitle}</h3>
              <Toggle
                label={'Suspend Access'}
                checked={isSuspended}
                onClick={() => this.props.onIsSuspendedChange(!isSuspended)}
              />
              {this.renderDoesReduceSection()}
            </form>
          </div>
        </div>
      );
  }

  private renderDoesReduceSection() {
    const { doesReduce, isMaster, onIsMasterChange, status } = this.props;
    return doesReduce
      ? (
        <div className="selection-content">
          <hr />
          <Toggle
            label={'Unrestricted Access'}
            checked={isMaster}
            onClick={() => !isReductionActive(status) && onIsMasterChange(!isMaster)}
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
        readOnly={isReductionActive(status)}
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
          ? (
            <button
              type="button"
              className="blue-button"
              onClick={this.props.onBeginReduction}
            >
              Submit
            </button>
          )
          : null;
      case ReductionStatus.Queued:
        return (
          <button
            type="button"
            className="red-button"
            onClick={this.props.onCancelReduction}
          >
            Cancel
          </button>
        );
      default:
        return null;
    }
  }
}
