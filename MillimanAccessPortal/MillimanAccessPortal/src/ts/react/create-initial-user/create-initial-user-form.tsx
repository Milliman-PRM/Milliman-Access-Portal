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
    loginWarning: string;
}

export class InitialUserForm extends Form<{}, InitialUserFormState> {

    protected schema = Yup.object({
        email: Yup.string()
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
            data: { email: '', returnUrl: getParameterByName('returnUrl') },
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
               // onSubmit={!userConfirmed ? this.checkUser : this.handleSubmit}
                autoComplete="off"
                action="/Account/CreateInitialUser"
                method="post"
            >
                <div className="form-section-container">
                    <div className="form-section">
                        <h4 className="form-section-title">Create a new account</h4>
                        <div className="text-danger" />
                        <div className="form-input-container">
                            <div className="form-input form-input-text flex-item-12-12">
                                <label className="form-input-text-title" />
                                <div className="col-md-10">
                                    <Input
                                        name="Email"
                                        label="Email"
                                        ref={this.usernameInput}
                                        type="text"
                                        value={data.email}
                                        onChange={this.handleChange && this.handleWhiteSpace}
                                        onClick={userConfirmed ? this.handleUsernameClick : undefined}
                                        error={errors.email}
                                        autoFocus={!userConfirmed}
                                        readOnly={userConfirmed}
                                    />
                                    <span className="text-danger" />
                                </div>
                            </div>
                        </div>
                    </div>
                    <div className="form-submission-section">
                        <div className="button-container button-container-update">
                            <button
                                disabled={awaitingRedirect || userConfirmed && !formIsValid}
                                type="submit"
                                className="button-submit blue-button"
                                // onClick={userConfirmed ? this.handleSubmit : undefined}
                            >
                             Create User
                            </button>
                        </div>
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
        const errorMessage = await this.validateProperty({ name: 'email', value: this.state.data.email });
        if (errorMessage) {
          errors.email = errorMessage.email;
          this.setState({ errors });
          return;
        } else {
          delete errors.username;
        }

        // hold for
        this.setState({ awaitingConfirmation: true }, () => {
          const { email } = this.state.data;
          postData('/Account/CreateInitialUser', { email })
              .then((response) => {
                if (response.ok) {
                  this.setState({
                    userConfirmed: true,
                    awaitingConfirmation: false,
                  }, () => {
                    this.focusUsernameInput();
                  });
                  window.location.replace('/Account/LogIn');
                }
              });
             /* .catch(() => {
                errors.username = 'An error occurred.';
                this.setState({
                  errors,
                  userConfirmed: false,
                  awaitingConfirmation: false,
                }, () => {
                  this.focusUsernameInput();
                });
              }); */
        });
    }

    private focusUsernameInput() {
        this.usernameInput.current.focus();
    }

}
