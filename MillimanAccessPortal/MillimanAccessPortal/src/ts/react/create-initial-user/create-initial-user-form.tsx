import * as React from 'react';
import * as Yup from 'yup';

import { postData } from '../../shared';
import { BaseFormState, Form } from '../shared-components/form/form';
import { Input } from '../shared-components/form/input';

export class InitialUserForm extends Form<{}, BaseFormState> {

    protected schema = Yup.object({
        username: Yup.string()
            .email()
            .required()
            .label('Email'),
    });

    public constructor(props: {}) {
        super(props);

        this.state = {
            data: { username: ''},
            errors: {},
            formIsValid: false,
        };
    }

    public render() {
        const {data, errors} = this.state;
        return (
          <form
            onSubmit={this.checkUser}
            autoComplete="off"
            className="form-horizontal"
          >
            <div className="form-section">
              <h3 className="form-section-title">Create a new account</h3>
              <div className="form-input-container">
                <Input
                  name="username"
                  label="Email"
                  type="text"
                  value={data.username}
                  onChange={this.handleChange && this.handleWhiteSpace}
                  error={errors.username}
                  autoFocus={true}
                />
              </div>
            </div>
              <div className="form-submission-section">
                <div className="button-container button-container-update">
                  <button
                    type="submit"
                    className="button-submit blue-button"
                  >
                    Create User
                  </button>
                </div>
              </div>
          </form>
        );
    }

    private checkUser = async (e: React.MouseEvent<HTMLButtonElement> | React.FormEvent<HTMLFormElement>) => {
        e.preventDefault();

        const errors = { ...this.state.errors };
        const errorMessage = await this.validateProperty({ name: 'username', value: this.state.data.username });
        if (errorMessage) {
            errors.username = errorMessage.username;
            this.setState({ errors });
            return;
        } else {
            delete errors.username;
        }

        // hold for
        const { username } = this.state.data;
        postData('/Account/CreateInitialUser', { email: username }, true)
            .then((response) => {
                if (response.ok) {
                    window.location.replace('/');
                }
            })
            .catch(() => {
                errors.username = 'An error occurred.';
                this.setState({
                    errors,
                });
            });
    }
}
