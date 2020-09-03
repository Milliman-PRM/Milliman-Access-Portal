import '../../../scss/react/login/login-step-two.scss';

import * as React from 'react';
import * as Yup from 'yup';

import { BaseFormState, Form } from '../shared-components/form/form';
import { Input } from '../shared-components/form/input';

import { postData } from '../../shared';

import '../../../images/map-logo.svg';
import '../../../scss/map.scss';

interface LoginStepTwoFormState extends BaseFormState {
  awaitingLogin: boolean;
  loginWarning: string;
  username: string;
}

export class LoginStepTwo extends Form<{}, LoginStepTwoFormState> {

  protected schema = Yup.object({
    code: Yup.string()
      .required()
      .label('Code'),
  });

  private usernameInput: string | React.RefObject<{}> | any;
  private codeInput: string | React.RefObject<{}> | any;

  public constructor(props: {}) {
    super(props);

    const params = new URLSearchParams(window.location.search);

    this.state = {
      awaitingLogin: false,
      loginWarning: null,
      username: params.get('Username'),
      data: {
        code: '',
        returnUrl: params.get('returnUrl'),
        rememberBrowser: 'false',
        rememberMe: params.get('RememberMe'),
      },
      errors: {},
      formIsValid: false,
    };

    this.usernameInput = React.createRef<HTMLInputElement>();
    this.codeInput = React.createRef<HTMLInputElement>();

    this.focusCodeInput = this.focusCodeInput.bind(this);
  }

  public render() {
    const { formIsValid, errors } = this.state;
    return (
      <>
        <div className="form-content-container">
          <div id="login-logo-container">
            <svg id="login-logo">
              <use xlinkHref={'#map-logo'} />
            </svg>
          </div>
          <form className="login-step-two-form" onSubmit={this.handleSubmit}>
            <div className="form-contents">
              <h3>Enter your authentication code</h3>
              <p>Check your email to view your authentication code.</p>
              <Input
                name="username"
                label="Username"
                type="text"
                ref={this.usernameInput}
                value={this.state.username}
                onChange={() => false}
                error={''}
                readOnly={true}
              />
              <Input
                name="code"
                label="Authentication code"
                type="text"
                ref={this.codeInput}
                value={this.state.data.code}
                onChange={this.handleChange}
                error={errors.code}
              />
              <div className="button-container">
                <a href={'/Account/Login?ReturnUrl=' + this.state.data.returnUrl} className="link-button">
                  <button
                    type="button"
                    className="link-button"
                    onClick={() => false}
                  >
                      Cancel
                  </button>
                </a>
                <button
                  type="submit"
                  disabled={!formIsValid}
                  className="blue-button"
                >
                  Log in
                </button>
              </div>
            </div>
          </form>
        </div>
      </>
    );
  }

  protected handleSubmit = async (e: React.MouseEvent<HTMLButtonElement> | React.FormEvent<HTMLFormElement>) => {
    e.preventDefault();

    const errors = { ...this.state.errors };
    const errorMessage = await this.validateProperty({ name: 'code', value: this.state.data.code });
    if (errorMessage) {
      errors.code = errorMessage.code;
      this.setState({ errors });
      return;
    } else {
      this.setState({ errors: {} });
    }

    postData(window.location.href, this.state.data, true)
      .then((response) => {
        if (response) {
          const redirectUrl = response.headers.get('NavigateTo');
          window.location.replace(redirectUrl || this.state.data.returnUrl);
        }
      }).catch((error) => {
        errors.code = error.message;
        this.setState({ errors }, () => {
          this.focusCodeInput();
        });
      });
  }

  private focusCodeInput() {
    this.codeInput.current.focus();
  }
}
