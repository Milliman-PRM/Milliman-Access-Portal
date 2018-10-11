import '../../../scss/react/shared-components/toggle.scss';

import * as React from 'react';

import { getData, postData } from '../../shared';
import { QueryFilter } from './interfaces';

interface ImmediateToggleProps {
  queryFilter: QueryFilter;
  label: string;
  data?: any;
  checked: boolean;
  onFetch: (data?: any) => void;
  onPush: (data?: any) => void;
}

export class ImmediateToggle extends React.Component<ImmediateToggleProps> {
  public render() {
    return (
      <div className="switch-container">
        <div
          className="toggle-switch"
          onClick={() => this.props.onPush(this.props.data)}
        >
          <input
            type="checkbox"
            className="toggle-switch-checkbox"
            name={this.props.label}
            checked={this.props.checked}
            onChange={() => null}
          />
          <label className="toggle-switch-label" htmlFor={this.props.label}>
            <span className="toggle-switch-inner" />
            <span className="toggle-switch-switch" />
          </label>
        </div>
        <label className="switch-label">{this.props.label}</label>
      </div>
    );
  }
}
