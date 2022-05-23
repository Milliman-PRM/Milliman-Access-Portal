import '../../../../scss/react/shared-components/toggle.scss';

import * as React from 'react';

interface ToggleProps {
  label: string;
  checked: boolean;
  readOnly?: boolean;
  onClick: (event: React.MouseEvent<HTMLDivElement>) => void;
}

export class Toggle extends React.Component<ToggleProps> {
  public render() {
    const labelClass = 'toggle-switch-label' + (this.props.checked ? ' checked' : '');
    return (
      <div className={`switch-container-react${this.props.readOnly ? ' disabled' : ''}`}>
        <div className="toggle-switch" onClick={this.onClick}>
          <label className={labelClass}>
            <span className="toggle-switch-inner" />
            <span className="toggle-switch-switch" />
          </label>
        </div>
        <label className="switch-label">{this.props.label}</label>
      </div>
    );
  }

  private onClick = (event: React.MouseEvent<HTMLInputElement>) => {
    const { onClick, readOnly } = this.props;
    if (!readOnly) {
      onClick(event);
    }
  }
}
