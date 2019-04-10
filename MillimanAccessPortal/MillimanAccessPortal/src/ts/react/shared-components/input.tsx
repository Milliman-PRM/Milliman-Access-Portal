import '../../../images/icons/hide-password.svg';
import '../../../images/icons/show-password.svg';

import '../../../scss/react/shared-components/form-elements.scss';

import * as React from 'react';

interface InputProps {
  name: string;
  label: string;
  type: string;
  value: string | number | string[];
  onChange: (currentTarget: React.FormEvent<HTMLInputElement>) => void;
  onBlur: (currentTarget: React.FormEvent<HTMLInputElement>) => void;
  onClick?: (currentTarget: React.FormEvent<HTMLInputElement> | null) => void;
  error: string;
  placeholderText?: string;
  autoFocus?: boolean;
  inputIcon?: string;
  readOnly?: boolean;
  hidden?: boolean;
}

export const Input = React.forwardRef<HTMLInputElement, InputProps>((props, ref) => {
  const { name, label, error, inputIcon, placeholderText, children, readOnly, hidden, ...rest } = props;
  return (
    <div className={'form-element-container' + (readOnly ? ' disabled' : '') + (hidden ? ' hidden' : '')}>
      <div className={'form-element' + (error ? ' error' : '')}>
        {inputIcon && (
          <div className="input-icon-label">
            <svg className="input-icon">
              <use xlinkHref={`#${inputIcon}`} />
            </svg>
          </div>
        )}
        <div className="form-input-container">
          <input
            name={name}
            id={name}
            ref={ref}
            className="form-input"
            placeholder={placeholderText || label}
            readOnly={readOnly}
            {...rest}
          />
          <label className="form-input-label" htmlFor={name}>{label}</label>
        </div>
        {children}
      </div>
      {error && <div className="error-message">{error}</div>}
    </div>
  );
});
