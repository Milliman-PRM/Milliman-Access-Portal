import '../../../../images/icons/add-circle.svg';
import '../../../../images/icons/remove-circle.svg';

import * as React from 'react';

export interface RadioButtonData {
  id: string;
  name?: string;
  selected: boolean;
  onChange: (selected: boolean) => void;
  hoverText?: string;
  description?: string;

}
export interface RadioButtonProps extends RadioButtonData {
  readOnly: boolean;
}

export class RadioButton extends React.Component<RadioButtonProps> {
  public render() {
    const { id, name, selected, readOnly, description } = this.props;
    return (
      <label className={`selection-option-label${readOnly ? ' readonly' : ''}`} htmlFor={id}>
        {name ? name : null}
        {description ? <span className="description-text">{description}</span> : null}
        <input
          type="radio"
          className="selection-option-value"
          checked={selected}
          onChange={this.onChange}
          id={id}
        />
      </label>
    );
  }

  private onChange = (event: React.ChangeEvent<HTMLInputElement>) => {
    const { onChange, readOnly } = this.props;
    if (!readOnly) {
      onChange(event.target.checked);
    }
  }
}
