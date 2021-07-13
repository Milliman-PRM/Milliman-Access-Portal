import '../../../images/map-logo.svg';
import '../../../scss/map.scss';

import * as React from 'react';
import * as Yup from 'yup';

import { getParameterByName, postData, postJsonDataNoSession } from '../../shared';
import { BaseFormState, Form } from '../shared-components/form/form';
import { Input } from '../shared-components/form/input';

interface InitialUserFormState extends BaseFormState {
    userConfirmed: boolean;
    awaitingConfirmation: boolean;
    awaitingRedirect: boolean;
    // TODO: check if necessary
    loginWarning: string;
}

export class InitialUserForm extends Form<{}, InitialUserFormState> {

    protected schema = Yup.object({
        username: Yup.string()
            .email()
            .required()
            .label('Email'),
    });

    private usernameInput: string | React.RefObject<{}> | any;

    public constructor(props: {}) {
        super(props);

        this.state = {
            userConfirmed: false,
            awaitingConfirmation: false,
            awaitingRedirect: false,
            loginWarning: null,
            data: { username: '', returnUrl: getParameterByName('returnUrl') },
            errors: {},
            formIsValid: false,
        };

        this.usernameInput = React.createRef<HTMLInputElement>();
        this.focusUsernameInput = this.focusUsernameInput.bind(this);

    }
    public render() {
        const {data, errors, formIsValid, userConfirmed, awaitingConfirmation, awaitingRedirect} = this.state;
        return (
          <form
            onSubmit={!userConfirmed ? this.checkUser : this.handleSubmit}
            autoComplete="off"
            className="form-horizontal"
          >
            <div className="form-section">
              <h3 className="form-section-title">Create a new account</h3>
              <div className="form-input-container">
                <Input
                  name="username"
                  label="Email"
                  ref={this.usernameInput}
                  type="text"
                  value={data.username}
                  onChange={this.handleChange && this.handleWhiteSpace}
                  onClick={userConfirmed ? this.handleUsernameClick : undefined}
                  error={errors.username}
                  autoFocus={!userConfirmed}
                  readOnly={userConfirmed}
                />
              </div>
            </div>
              <div className="form-submission-section">
                <div className="button-container button-container-update">
                 <button
                   disabled={awaitingRedirect || userConfirmed && !formIsValid}
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

    protected handleSubmit = async (e: React.MouseEvent<HTMLButtonElement> | React.FormEvent<HTMLFormElement>) => {
        e.preventDefault();

        this.setState({ awaitingRedirect: true });

        const errors = await this.validate();
        this.setState({ errors: errors || {} });
        if (errors) {
            this.setState({ awaitingRedirect: false });
            return;
        }

        await postJsonDataNoSession(window.location.href, this.state.data)
            .then((response) => {
                const loginWarning = response.headers.get('Warning');
                if (loginWarning) {
                    const unknownError = 'An unknown error occurred.  Please try again.';
                    this.setState({ loginWarning: unknownError, awaitingRedirect: false });
                }
            });
    }

    protected handleUsernameClick = () => {
        this.setState({ userConfirmed: false, loginWarning: null }, () => {
            this.focusUsernameInput();
        });
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
        this.setState({ awaitingConfirmation: true }, () => {
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
                        userConfirmed: false,
                        awaitingConfirmation: false,
                    }, () => {
                        this.focusUsernameInput();
                    });
                });
        });
    }

    private focusUsernameInput() {
        this.usernameInput.current.focus();
    }

}
