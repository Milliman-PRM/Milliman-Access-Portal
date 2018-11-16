import * as React from 'react';

import { ReductionStatus, isReductionActive } from '../../view-models/content-publishing';
import { PanelSectionContainer } from '../shared-components/card-panel/panel-sections';
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
    const { children, title, subtitle, isSuspended } = this.props;
    return (
      <PanelSectionContainer>
        <h3 className="admin-panel-header">Selections</h3>
        {children}
        <div className="admin-panel-form">
          <div className="admin-panel-content-container">
            <form className="admin-panel-content">
              <h2>{title}</h2>
              <h3>{subtitle}</h3>
              <Toggle
                label={'Suspend Access'}
                checked={isSuspended}
                onClick={() => null}
              />
              {this.renderDoesReduceSection()}
            </form>
          </div>
        </div>
      </PanelSectionContainer>
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
          ? <button type="button" className="blue-button">Submit</button>
          : null;
      case ReductionStatus.Queued:
        return <button type="button" className="red-button">Cancel</button>;
      default:
        return null;
    }
  }
}
