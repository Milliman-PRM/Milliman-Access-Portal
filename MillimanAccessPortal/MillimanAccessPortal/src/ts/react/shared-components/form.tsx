import * as React from 'react';
const Joi = require('joi-browser');
import 'promise-polyfill/dist/polyfill';

export interface BaseFormState {
  data: {
    [id: string]: string | boolean;
  };
  errors: {
    [id: string]: string;
  };
}

export class Form<TProps, TState extends BaseFormState> extends React.Component<TProps, TState> {

  schema = {};

  componentDidMount() {
    const requestVerificationToken = document.querySelector('input[name="__RequestVerificationToken"]').getAttribute('value');
    const data = { ...(this.state.data as object) };
    data["__RequestVerificationToken"] = requestVerificationToken;
    this.schema["__RequestVerificationToken"] = Joi.string().required();
    this.setState({ data })
  }

  doSubmit = () => { }

  validate = () => {
    const options = { abortEarly: false };
    const { error } = Joi.validate(this.state.data, this.schema, options);
    if (!error) {
      return null;
    }

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
    const errors = { ...(this.state.errors as object) };
    const errorMessage = this.validateProperty(input);
    if (!errorMessage) delete errors[input.name];

    const data = { ...(this.state.data as object) };
    data[input.name] = input.value;

    this.setState({ data, errors });
  }

  handleBlur = ({ currentTarget: input }) => {
    const errors = { ...(this.state.errors as object) };
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
}
