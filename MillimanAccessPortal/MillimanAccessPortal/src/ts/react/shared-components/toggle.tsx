import '../../../scss/react/shared-components/toggle.scss';

import * as React from 'react';

interface ToggleProps {
  label: string;
  checked: boolean;
  onClick: (event: React.MouseEvent<HTMLDivElement>) => void;
}

export class Toggle extends React.Component<ToggleProps> {
  public render() {
    const labelClass = 'toggle-switch-label' + (this.props.checked ? ' checked' : '');
    return (
      <div className="switch-container-react">
        <div className="toggle-switch" onClick={this.props.onClick}>
          <label className={labelClass}>
            <span className="toggle-switch-inner" />
            <span className="toggle-switch-switch" />
          </label>
        </div>
        <label className="switch-label">{this.props.label}</label>
      </div>
    );
  }
}
