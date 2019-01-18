import '../../../images/icons/error.svg';
import '../../../images/icons/show-password.svg';
import '../../../images/icons/hide-password.svg';

import '../../../scss/react/shared-components/input.scss';

import * as React from 'react';
import { InputProps } from './interfaces';

const Input = ({ name, label, placeholderText = null, error = null, inputIcon = null, actionIcon = null, actionIconEvent = null, type = "text", ...rest }: InputProps) => {
  return (
    <div className="form-element-container">
      <div className={"form-element" + (error ? " error" : "")}>
        {inputIcon && <div className="input-icon-label"><svg className="input-icon"><use xlinkHref={`#${inputIcon}`} /></svg></div>}
        <div className="form-input-container">
          <input name={name} id={name} type={type} className="form-input" placeholder={ placeholderText || label } {...rest} />
          <label className="form-input-label" htmlFor={name}>{label}</label>
        </div>
        {actionIcon && <div className="action-icon-label" onClick={actionIconEvent}><svg className="action-icon"><use xlinkHref={`#${actionIcon}`} /></svg></div>}
      </div>
      {error && <div className="error-message">{error}</div>}
    </div>
  );
};

export default Input;

//        {error && <svg className="error-icon"><use xlinkHref="#error" /></svg>}
