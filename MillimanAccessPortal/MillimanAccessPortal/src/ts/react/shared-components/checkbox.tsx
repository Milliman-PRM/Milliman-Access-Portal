import * as React from 'react';

export interface CheckboxProps {
  name: string;
  selected: boolean;
  onChange: (selected: boolean) => void;
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
    this.props.onChange(event.target.checked);
  }
}
