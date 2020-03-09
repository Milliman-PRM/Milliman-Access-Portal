import '../../../../images/icons/add-circle.svg';
import '../../../../images/icons/remove-circle.svg';

import * as React from 'react';

export interface CheckboxData {
  name?: string;
  selected: boolean;
  modified?: boolean;
  onChange: (selected: boolean) => void;
  hoverText?: string;
}
export interface CheckboxProps extends CheckboxData {
  readOnly: boolean;
}

export class Checkbox extends React.Component<CheckboxProps> {
  public render() {
    const { name, selected, modified, readOnly, hoverText } = this.props;
    const modifiedClass = modified
      ? selected
        ? ' added'
        : ' removed'
      : '';
    return (
      <div className="selection-option-container" style={{ display: 'flex' }} title={hoverText ? hoverText : null}>
        { modified !== undefined &&
          <svg className={`selection-option-modified${modifiedClass}`}>
            <use href={`#${selected ? 'add' : 'remove'}-circle`} />
          </svg>
        }
        <label className={`selection-option-label${readOnly ? ' readonly' : ''}`}>
          {name ? name : null}
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
