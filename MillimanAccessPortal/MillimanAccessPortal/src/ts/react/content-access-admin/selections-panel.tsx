import * as React from 'react';

import { isReductionActive, ReductionStatus } from '../../view-models/content-publishing';
import { ButtonSpinner } from '../shared-components/button-spinner';
import { PanelSectionContainer } from '../shared-components/card-panel/panel-sections';
import { ColumnSpinner } from '../shared-components/column-spinner';
import { Toggle } from '../shared-components/toggle';
import { Fieldset, FieldsetData } from './fieldset';

export interface SelectionsPanelProps {
  isSuspended: boolean;
  onIsSuspendedChange: (value: boolean) => void;
  doesReduce: boolean;
  isAllValuesSelected: boolean;
  isAllValuesDeselected: boolean;
  isModified: boolean;
  isValuesModified: boolean;
  isMaster: boolean;
  onIsMasterChange: (value: boolean) => void;
  title: string;
  subtitle: string;
  status: ReductionStatus;
  onBeginReduction: () => void;
  onCancelReduction: () => void;
  loading?: boolean;
  onSetPendingAllSelectionsOff: () => void;
  onSetPendingAllSelectionsOn: () => void;
  onSetPendingAllSelectionsReset: () => void;
  submitting: boolean;
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
      ? <ColumnSpinner />
      : (
        <div className="admin-panel-form" style={{ display: 'flex', flex: 'auto' }}>
          <div className="admin-panel-content-container">
            <form
              className="admin-panel-content"
              style={{
                display: 'flex',
                flexDirection: 'column',
                flex: 'auto',
                height: 'calc(100% - 1.5em)',
              }}
            >
              <h2
                style={{
                  margin: 0,
                }}
              >
                {title}
              </h2>
              <h3
                style={{
                  marginBottom: '1rem',
                }}
              >
                {subtitle}
              </h3>
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
        <div
          className="selection-content"
          style={{
            flex: 'auto',
            display: 'flex',
            flexDirection: 'column',
          }}
        >
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
    const { status } = this.props;
    return this.props.isMaster
      ? null
      : (
        <div className="fieldset-container" style={{ flex: '1 1 1px', overflowY: 'auto' }}>
          <h4>Reduction Values</h4>
          {!isReductionActive(status) && (
            <div className="fieldset-options-container">
              <div
                className={`fieldset-option${this.props.isAllValuesSelected ? ' disabled' : ''}`}
                onClick={this.props.onSetPendingAllSelectionsOn}
              >
                Select All
              </div>
              <div
                className={`fieldset-option${this.props.isAllValuesDeselected ? ' disabled' : ''}`}
                onClick={this.props.onSetPendingAllSelectionsOff}
              >
                Clear All
              </div>
              <div
                className={`fieldset-option${!this.props.isValuesModified ? ' disabled' : ''}`}
                onClick={this.props.onSetPendingAllSelectionsReset}
              >
                Reset
              </div>
            </div>
          )}
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
              style={{ alignSelf: 'flex-end' }}
            >
              Submit
              {this.props.submitting
                ? <ButtonSpinner version="circle" />
                : null
              }
            </button>
          )
          : null;
      case ReductionStatus.Queued:
        return (
          <button
            type="button"
            className="red-button"
            onClick={this.props.onCancelReduction}
            style={{ alignSelf: 'flex-end' }}
          >
            Cancel
            {this.props.submitting
              ? <ButtonSpinner version="circle" />
              : null
            }
          </button>
        );
      default:
        return null;
    }
  }
}
