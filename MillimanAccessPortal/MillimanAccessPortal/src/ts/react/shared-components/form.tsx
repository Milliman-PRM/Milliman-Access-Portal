import * as React from 'react';
import * as Joi from 'joi-browser';
import 'promise-polyfill/dist/polyfill';

export interface BaseFormState {
  data: {
    [id: string]: string | number | string[];
  };
  errors: {
    [id: string]: string;
  };
}

export class Form<TProps, TState extends BaseFormState> extends React.Component<TProps, TState> {

  protected schema = {};

  protected doSubmit = () => { }

  protected validate = () => {
    const options = { abortEarly: false };
    const { error } = Joi.validate(this.state.data, this.schema, options);
    if (!error) {
      return null;
    }

    const errors = {};
    for (let item of error.details) errors[item.path[0]] = item.message;
    return errors;
  }

  protected validateProperty = ({ name, value }) => {
    const obj = { [name]: value };
    const schema = { [name]: this.schema[name] };
    const { error } = Joi.validate(obj, schema);
    return error ? error.details[0].message : null;
  }

  protected handleSubmit = e => {
    e.preventDefault();

    const errors = this.validate();
    this.setState({ errors: errors || {} });
    if (errors) return;

    this.doSubmit();
  }

  protected handleChange = ({ currentTarget: input }) => {
    const errors = { ...(this.state.errors as object) };
    const errorMessage = this.validateProperty(input);
    if (!errorMessage) delete errors[input.name];

    const data = { ...(this.state.data as object) };
    data[input.name] = input.value;

    this.setState({ data, errors });
  }

  protected handleBlur = ({ currentTarget: input }) => {
    const errors = { ...(this.state.errors as object) };
    const errorMessage = this.validateProperty(input);
    if (errorMessage && input.value !== "") errors[input.name] = errorMessage;
    else delete errors[input.name];

    this.setState({ errors });
  }
}
