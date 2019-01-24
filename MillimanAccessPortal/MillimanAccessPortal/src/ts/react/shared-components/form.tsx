import * as Joi from 'joi';
import 'promise-polyfill/dist/polyfill';
import * as React from 'react';

export interface BaseFormState {
  data: {
    [id: string]: string;
  };
  errors: {
    [id: string]: string;
  };
}

export class Form<TProps, TState extends BaseFormState> extends React.Component<TProps, TState> {

  protected schema: Joi.SchemaMap = {};

  protected doSubmit: () => void = () => null;

  protected validate = () => {
    const options = { abortEarly: false };
    const { error } = Joi.validate(this.state.data, this.schema, options);
    if (!error) {
      return null;
    }

    return error.details
      .map((item) => ({ path: item.path[0], message: item.message }))
      .reduce((prev, { path, message }) => ({ ...prev, [path]: message }), {});
  }

  protected validateProperty = ({ name, value }: Partial<EventTarget & HTMLInputElement>) => {
    const obj = { [name]: value };
    const schema = { [name]: this.schema[name] };
    const { error } = Joi.validate(obj, schema);
    return error ? error.details[0].message : null;
  }

  protected handleSubmit = (e: React.MouseEvent<HTMLButtonElement> | React.FormEvent<HTMLFormElement>) => {
    e.preventDefault();

    const errors = this.validate();
    this.setState({ errors: errors || {} });
    if (errors) { return; }

    this.doSubmit();
  }

  protected handleChange = ({ currentTarget: input }: React.FormEvent<HTMLInputElement>) => {
    const errorMessage = this.validateProperty(input);
    const { data, errors } = Object.assign({}, this.state);

    if (!errorMessage) {
      delete errors[input.name];
    }
    data[input.name] = input.value;
    this.setState({ data, errors });
  }

  protected handleBlur = ({ currentTarget: input }: React.FormEvent<HTMLInputElement>) => {
    const errorMessage = this.validateProperty(input);
    const { errors } = Object.assign({}, this.state);

    if (errorMessage && input.value) {
      errors[input.name] = errorMessage;
    } else {
      delete errors[input.name];
    }
    this.setState({ errors });
  }
}
