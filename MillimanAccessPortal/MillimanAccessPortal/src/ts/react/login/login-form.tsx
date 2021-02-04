import '../../../scss/react/shared-components/form-elements.scss';

import '../../../images/icons/login.svg';
import '../../../images/icons/password.svg';
import '../../../images/icons/user.svg';

import * as React from 'react';
import * as Yup from 'yup';

import { getParameterByName, postData, postJsonDataNoSession } from '../../shared';
import { ButtonSpinner } from '../shared-components/button-spinner';
import { BaseFormState, Form } from '../shared-components/form/form';
import { Input } from '../shared-components/form/input';

interface LoginFormState extends BaseFormState {
  userConfirmed: boolean;
  awaitingConfirmation: boolean;
  awaitingLogin: boolean;
  loginWarning: string;
}

export class LoginForm extends Form<{}, LoginFormState> {

  protected schema = Yup.object({
    username: Yup.string()
      .email()
      .required()
      .label('Username'),
    password: Yup.string()
      .required()
      .label('Password'),
  });

  private usernameInput: string | React.RefObject<{}> | any;
  private passwordInput: string | React.RefObject<{}> | any;

  public constructor(props: {}) {
    super(props);

    this.state = {
      userConfirmed: false,
      awaitingConfirmation: false,
      awaitingLogin: false,
      loginWarning: null,
      data: { username: '', password: '', returnUrl: getParameterByName('returnUrl') },
      errors: {},
      formIsValid: false,
    };

    this.usernameInput = React.createRef<HTMLInputElement>();
    this.passwordInput = React.createRef<HTMLInputElement>();
    this.focusUsernameInput = this.focusUsernameInput.bind(this);
    this.focusPasswordInput = this.focusPasswordInput.bind(this);
  }

  public render() {
    const { data, errors, formIsValid, userConfirmed, awaitingConfirmation, awaitingLogin, loginWarning } = this.state;
    let actionButton;
    if (!userConfirmed && !awaitingConfirmation) {
      actionButton = (
        <button type="submit" className="action-icon-label" onClick={this.checkUser}>
          <svg className="action-icon"><use xlinkHref="#login" /></svg>
        </button>
      );
    } else if (awaitingConfirmation) {
      actionButton = (
        <div className="action-icon-label">
          <ButtonSpinner version="bars" />
        </div>
      );
    } else {
      actionButton = null;
    }
    return (
      <form onSubmit={!userConfirmed ? this.checkUser : this.handleSubmit}>
        <Input
          name="username"
          label="Username"
          ref={this.usernameInput}
          type="text"
          value={data.username}
          onChange={this.handleChange && this.handleWhiteSpace}
          onBlur={this.handleBlur}
          onClick={userConfirmed ? this.handleUsernameClick : undefined}
          error={errors.username}
          autoFocus={!userConfirmed}
          inputIcon="user"
          readOnly={userConfirmed}
        >
          {actionButton}
        </Input>
        <Input
          name="password"
          label="Password"
          ref={this.passwordInput}
          type="password"
          value={data.password}
          onChange={this.handleChange}
          onBlur={this.handleBlur}
          error={errors.password}
          autoFocus={userConfirmed}
          inputIcon="password"
          hidden={!userConfirmed}
        />
        <p className="terms-reminder" style={{ marginTop: !userConfirmed ? '-5em' : null }}>
          By continuing to log in, you are agreeing to Milliman Access Portal's
          <a href="us.milliman.com/legal/terms"> Terms of Use</a>.
        </p>
        {loginWarning && <div className="error-message">{loginWarning}</div>}
        <div className={'button-container' + (userConfirmed ? ' visible' : ' hidden')}>
          <a href="/Account/ForgotPassword" className="link-button">Forgot Password</a>
          <button
            type={userConfirmed ? 'submit' : 'button'}
            disabled={awaitingLogin || userConfirmed && !formIsValid}
            className="blue-button"
            onClick={userConfirmed ? this.handleSubmit : undefined}
          >
            Login
            {awaitingLogin && <ButtonSpinner version="bars" />}
          </button>
        </div>
      </form>
    );
  }

  protected handleSubmit = async (e: React.MouseEvent<HTMLButtonElement> | React.FormEvent<HTMLFormElement>) => {
    e.preventDefault();

    this.setState({ awaitingLogin: true });

    const errors = await this.validate();
    this.setState({ errors: errors || {} });
    if (errors) {
      this.setState({ awaitingLogin: false });
      return;
    }

    await postJsonDataNoSession(window.location.href, this.state.data)
      .then((response) => {
        const loginWarning = response.headers.get('Warning');
        const redirectUrl = response.headers.get('NavigateTo');
        if (loginWarning) {
          const data = { ...this.state.data };
          data.password = '';
          this.focusPasswordInput();
          this.setState({ data, loginWarning, awaitingLogin: false });
          return;
        } else if (redirectUrl) {
          window.location.replace(redirectUrl);
        } else {
          const unknownError = 'An unknown error occurred.  Please try again.';
          this.setState({ loginWarning: unknownError, awaitingLogin: false });
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
      postData('/Account/IsLocalAccount', { username })
        .then((response) => {
          if (response.localAccount) {
            this.setState({
              userConfirmed: true,
              awaitingConfirmation: false,
            }, () => {
              this.focusPasswordInput();
            });
          } else {
            const returnTo = escape(getParameterByName('returnUrl') || '/');
            window.location.replace(`/Account/RemoteAuthenticate?username=${username}&returnURL=${returnTo}`);
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

  private focusPasswordInput() {
    this.passwordInput.current.focus();
  }
}
