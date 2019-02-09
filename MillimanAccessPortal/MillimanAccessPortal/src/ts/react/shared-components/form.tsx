import * as React from 'react';
import * as Yup from 'yup';

export interface BaseFormState {
  data: {
    [id: string]: string;
  };
  errors: {
    [id: string]: string;
  };
  formIsValid: boolean;
}

export class Form<TProps, TState extends BaseFormState> extends React.Component<TProps, TState> {

  protected schema: Yup.MixedSchema = Yup.object();

  protected doSubmit: () => void = () => null;

  protected validate = async () => {
    const options = { abortEarly: false };
    let error: Yup.ValidationError = null;
    await this.schema.validate(this.state.data, options)
      .catch((err) => {
        this.setState({ formIsValid: false });
        error = err;
      });

    if (!error) {
      this.setState({ formIsValid: true });
      return null;
    }

    return error.inner
      .map((item) => ({ path: item.path, message: item.errors }))
      .reduce((prev, { path, message }) => ({ ...prev, [path]: message }), {});
  }

  protected validateProperty = async ({ name, value }: Partial<EventTarget & HTMLInputElement>) => {
    const obj = { [name]: value };
    let error: Yup.ValidationError = null;
    await this.schema.validateAt(name, this.state.data)
      .catch((err) => {
        error = err;
      });

    if (error) {
      return { [name]: error.message };
    }
  }

  protected handleSubmit = async (e: React.MouseEvent<HTMLButtonElement> | React.FormEvent<HTMLFormElement>) => {
    e.preventDefault();

    const errors = await this.validate();
    this.setState({ errors: errors || {} });
    if (errors) { return; }

    this.doSubmit();
  }

  protected handleChange = async ({ currentTarget: input }: React.FormEvent<HTMLInputElement>) => {
    const errorMessage = this.validateProperty(input);
    const { data, errors } = Object.assign({}, this.state);
    this.validate();

    if (!errorMessage) {
      delete errors[input.name];
    }

    data[input.name] = input.value;

    this.setState({ data, errors });
  }

  protected handleBlur = async ({ currentTarget: input }: React.FormEvent<HTMLInputElement>) => {
    const errorMessage = await this.validateProperty(input);
    const { errors } = Object.assign({}, this.state);

    if (errorMessage && input.value) {
      errors[input.name] = errorMessage[input.name];
    } else {
      delete errors[input.name];
    }

    this.setState({ errors });
  }
}
