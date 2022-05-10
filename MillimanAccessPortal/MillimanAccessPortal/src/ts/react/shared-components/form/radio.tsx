import '../../../../images/icons/add-circle.svg';
import '../../../../images/icons/remove-circle.svg';

import * as React from 'react';

export interface RadioButtonData {
  id: string;
  group: string;
  selected: boolean;
  value: any;
  onSelect: (value: any) => void;
  hoverText?: string;
  description?: string;
  labelText?: string;
}
export interface RadioButtonProps extends RadioButtonData {
  readOnly: boolean;
}

export class RadioButton extends React.Component<RadioButtonProps> {
  public render() {
    const { id, group, labelText, selected, readOnly, description } = this.props;
    return (
      <label className={`selection-option-label-radio${readOnly ? ' readonly' : ''}`} htmlFor={id}>
        {labelText ? labelText : null}
        {description ? <span className="description-text">{description}</span> : null}
        <input
          type="radio"
          className="selection-option-radio"
          checked={selected}
          onClick={(_event) => this.onSelect()}
          id={id}
          name={group}
          onChange={() => null}
        />
      </label>
    );
  }

  private onSelect = () => {
    const { onSelect, readOnly, value } = this.props;
    if (!readOnly) {
      onSelect(value);
    }
  }
}
