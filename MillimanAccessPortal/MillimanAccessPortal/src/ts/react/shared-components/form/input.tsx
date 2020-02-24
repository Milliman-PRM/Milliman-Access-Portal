import '../../../../images/icons/hide-password.svg';
import '../../../../images/icons/show-password.svg';

import '../../../../scss/react/shared-components/form-elements.scss';

import * as React from 'react';
import TextareaAutosize from 'react-autosize-textarea';

interface BaseInputProps {
  name: string;
  label: string;
  value: string | number | string[];
  onChange: (currentTarget: React.FormEvent<HTMLInputElement> | React.FormEvent<HTMLTextAreaElement>) => void;
  onBlur?: (currentTarget: React.FormEvent<HTMLInputElement> | React.FormEvent<HTMLTextAreaElement>) => void;
  onClick?: (currentTarget: React.FormEvent<HTMLInputElement> | React.FormEvent<HTMLTextAreaElement> | null) => void;
  error: string;
  placeholderText?: string;
  autoFocus?: boolean;
  inputIcon?: string;
  readOnly?: boolean;
  hidden?: boolean;
}

interface InputProps extends BaseInputProps {
  type: string;
}

export const Input = React.forwardRef<HTMLInputElement, InputProps>((props, ref) => {
  const { name, label, error, inputIcon, placeholderText, children, readOnly, hidden, ...rest } = props;
  return (
    <div className={'form-element-container' + (readOnly ? ' disabled' : '') + (hidden ? ' hidden' : '')}>
      <div className={'form-element-input' + (error ? ' error' : '')}>
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

interface TextareaProps extends BaseInputProps {
  rows?: number;
  maxRows?: number;
}

export const TextAreaInput = React.forwardRef<HTMLTextAreaElement, TextareaProps>((props, ref) => {
  const { name, label, error, placeholderText, children, readOnly, hidden, value, rows, maxRows, ...rest } = props;
  return (
    <div className={'form-element-container' + (readOnly ? ' disabled' : '') + (hidden ? ' hidden' : '')}>
      <div className={'form-element-textarea' + (error ? ' error' : '')}>
        <div className="form-input-container">
          <TextareaAutosize
            name={name}
            id={name}
            ref={ref}
            className="form-input"
            placeholder={placeholderText || label}
            readOnly={readOnly}
            data-input-value={value}
            value={value || ''}
            {...rest}
            rows={rows ? rows : 5}
            maxRows={maxRows ? maxRows : 10}
          />
          <label className="form-input-label" htmlFor={name}>{label}</label>
        </div>
        {children}
      </div>
      {error && <div className="error-message">{error}</div>}
    </div>
  );
});
