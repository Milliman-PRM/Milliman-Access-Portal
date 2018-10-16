import '../../../scss/react/shared-components/toggle.scss';

import * as React from 'react';

interface ToggleProps {
  label: string;
  checked: boolean;
  onChange: (event: React.ChangeEvent<HTMLInputElement>) => void;
}

export class Toggle extends React.Component<ToggleProps> {
  public render() {
    return (
      <div className="switch-container">
        <div className="toggle-switch">
          <input
            type="checkbox"
            className="toggle-switch-checkbox"
            name={this.props.label}
            checked={this.props.checked}
            onChange={this.props.onChange}
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
