import '../../../scss/react/shared-components/input.scss';

import * as React from 'react';

interface InputProps {
  name: string;
  label?: string;
  error?: string;
  inputIcon?: string;
}

const Input = ({ name, label, error, inputIcon, ...rest }: InputProps) => {
  return (
    <div className="form-input-container">
      {label && <label className="form-input-label" htmlFor={name}>{label}</label>}
      <div className="form-element-container">
        {inputIcon && <svg className="filter-icon"><use xlinkHref="#{inputIcon}" /></svg>}
        <input name={name} id={name} className="form-input" {...rest} />
        {error && <svg className="form-element-error-icon"><use xlinkHref="#warning" /></svg>}
      </div>
      {error && <div className="form-element-error-message">{error}</div>}
    </div>
  );
};

export default Input;
