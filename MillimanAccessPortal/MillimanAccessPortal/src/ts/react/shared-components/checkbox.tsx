import * as React from 'react';

export interface CheckboxData {
  name: string;
  selected: boolean;
  onChange: (selected: boolean) => void;
}
export interface CheckboxProps extends CheckboxData {
  readOnly: boolean;
}

export class Checkbox extends React.Component<CheckboxProps> {
  public render() {
    const { name, selected } = this.props;
    return (
      <div className="selection-option-container">
        <label className="selection-option-label">
          {name}
          <input
            type="checkbox"
            className="selection-option-value"
            checked={selected}
            onChange={this.onChange}
          />
          <span className="selection-option-checkmark" />
        </label>
      </div>
    );
  }

  private onChange = (event: React.ChangeEvent<HTMLInputElement>) => {
    const { onChange, readOnly } = this.props;
    if (!readOnly) {
      onChange(event.target.checked);
    }
  }
}
