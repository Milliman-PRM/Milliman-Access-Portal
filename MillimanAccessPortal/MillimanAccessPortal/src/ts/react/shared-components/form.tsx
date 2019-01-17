import * as React from 'react';
const Joi = require('joi-browser');
import 'promise-polyfill/dist/polyfill';
import Input from "./input";

import { OptionalInputProps, InputProps } from '../shared-components/interfaces';

class Form extends React.Component {
  state = {
    data: {},
    errors: {}
  };

  schema = {};

  componentDidMount() {
    const requestVerificationToken = document.querySelector('input[name="__RequestVerificationToken"]').getAttribute('value');
    const data = { ...this.state.data };
    data["__RequestVerificationToken"] = requestVerificationToken;
    this.schema["__RequestVerificationToken"] = Joi.string().required();
    this.setState({ data })
  }

  doSubmit = () => { };

  validate = () => {
    const options = { abortEarly: false };
    const { error } = Joi.validate(this.state.data, this.schema, options);
    if (!error) return null;

    const errors = {};
    for (let item of error.details) errors[item.path[0]] = item.message;
    return errors;
  };

  validateProperty = ({ name, value }) => {
    const obj = { [name]: value };
    const schema = { [name]: this.schema[name] };
    const { error } = Joi.validate(obj, schema);
    return error ? error.details[0].message : null;
  };

  handleSubmit = e => {
    e.preventDefault();

    const errors = this.validate();
    this.setState({ errors: errors || {} });
    if (errors) return;

    this.doSubmit();
  };

  handleChange = ({ currentTarget: input }) => {
    const errors = { ...this.state.errors };
    const errorMessage = this.validateProperty(input);
    if (!errorMessage) delete errors[input.name];

    const data = { ...this.state.data };
    data[input.name] = input.value;

    this.setState({ data, errors });
  }

  handleBlur = ({ currentTarget: input }) => {
    const errors = { ...this.state.errors };
    const errorMessage = this.validateProperty(input);
    if (errorMessage && input.value !== "") errors[input.name] = errorMessage;
    else delete errors[input.name];

    this.setState({ errors });
  };

  renderButton(label: string, primary: boolean = true, action: () => void = null) {
    const buttonDisabled = (primary && this.validate()) ? true : false;
    return (
      <button type={primary ? 'submit' : 'button'} disabled={buttonDisabled} className={(primary) ? 'blue-button' : 'link-button'} onClick={action}>
        {label}
      </button>
    );
  }

  renderInput(name: string, label: string, type: string, attr: OptionalInputProps) {
    const { data, errors } = this.state;

    return (
      <Input
        name={name}
        label={label}
        type={type}
        error={errors[name]}
        value={data[name]}
        onChange={this.handleChange}
        onBlur={this.handleBlur}
        {...attr}
      />
    );
  }

  renderRequestVerificationToken() {
    const requestVerificationToken = this.state.data["__RequestVerificationToken"];
    if (requestVerificationToken) {
      return (
        <input name="__RequestVerificationToken" type="hidden" value={this.state.data["__RequestVerificationToken"]} />
      )
    }
  }
}

export default Form;
