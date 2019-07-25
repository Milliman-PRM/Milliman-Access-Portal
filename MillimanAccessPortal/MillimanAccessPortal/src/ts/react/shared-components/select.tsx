import '../../../scss/react/shared-components/form-elements.scss';

import * as React from 'react';

interface SelectProps {
  name: string;
  label: string;
  value: string | number;
  values: Array<{ selectionValue: string | number, selectionLabel: string }>;
  onChange: (currentTarget: React.FormEvent<HTMLSelectElement>) => void;
  error: string;
  placeholderText?: string;
  autoFocus?: boolean;
  readOnly?: boolean;
  hidden?: boolean;
}

export const Select = React.forwardRef<HTMLSelectElement, SelectProps>((props, ref) => {
  const { name, label, error, placeholderText, readOnly, hidden, values, children, ...rest } = props;
  const options = values.map((option, index) => {
    return (<option key={index} value={option.selectionValue}>{option.selectionLabel}</option>);
  });
  return (
    <div className={'form-element-container' + (readOnly ? ' disabled' : '') + (hidden ? ' hidden' : '')}>
      <div className={'form-element-select' + (error ? ' error' : '')}>
        <div className="form-select-container">
          <select
            name={name}
            id={name}
            ref={ref}
            className={`form-input ${!props.value ? '' : 'item-selected'}`}
            disabled={readOnly}
            {...rest}
          >
            <option>{placeholderText || 'Select a ' + label}</option>
            {options}
          </select>
          <label className="form-input-label" htmlFor={name}>{label}</label>
        </div>
      </div>
      {error && <div className="error-message">{error}</div>}
    </div>
  );
});
