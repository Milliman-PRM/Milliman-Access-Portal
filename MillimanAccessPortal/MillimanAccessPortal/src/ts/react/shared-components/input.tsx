import '../../../images/icons/show-password.svg';
import '../../../images/icons/hide-password.svg';

import '../../../scss/react/shared-components/input.scss';

import * as React from 'react';

interface InputProps {
  name: string;
  label: string;
  type: string;
  value: string | number | string[];
  onChange: (currentTarget: React.FormEvent<HTMLInputElement>) => void;
  onBlur: (currentTarget: React.FormEvent<HTMLInputElement>) => void;
  error: string;
  placeholderText?: string;
  autoFocus?: boolean;
  inputIcon?: string;
}

export class Input extends React.Component<InputProps, {}> {

  public render() {
    const { name, label, error, inputIcon, placeholderText, children, ...rest } = this.props;
    return (
      <div className="form-element-container">
        <div className={"form-element" + (error ? " error" : "")}>
          {inputIcon && <div className="input-icon-label"><svg className="input-icon"><use xlinkHref={`#${inputIcon}`} /></svg></div>}
          <div className="form-input-container">
            <input name={name} id={name} className="form-input" placeholder={placeholderText || label} {...rest} />
            <label className="form-input-label" htmlFor={name}>{label}</label>
          </div>
          {children}
        </div>
        {error && <div className="error-message">{error}</div>}
      </div>
    );
  }
}
