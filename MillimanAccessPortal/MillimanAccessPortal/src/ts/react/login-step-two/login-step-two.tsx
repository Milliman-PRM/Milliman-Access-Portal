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
        provider: params.get('DefaultEmailProvider'),
        code: '',
        returnUrl: params.get('returnUrl'),
        rememberBrowser: 'false',
        rememberMe: 'false',
      },
      errors: {},
      formIsValid: false,
    };

    this.usernameInput = React.createRef<HTMLInputElement>();
    this.codeInput = React.createRef<HTMLInputElement>();

    this.focusCodeInput = this.focusCodeInput.bind(this);
  }

  public render() {
    return (
      <>
        <div className="form-content-container">
          <div id="login-logo-container">
            <svg id="login-logo">
              <use xlinkHref={'#map-logo'} />
            </svg>
          </div>
          <form>
            <h3>Enter your authentication code</h3>
            <p>Check your email to view your authentication code.</p>
            <label htmlFor="username">Username</label>
            <Input
              name="username"
              label={null}
              type="text"
              ref={this.usernameInput}
              value={this.state.username}
              onChange={() => false}
              error={''}
              readOnly={true}
            />
            <label htmlFor="code">Authentication Code</label>
            <Input
              name="code"
              label="Authentication code"
              type="text"
              ref={this.codeInput}
              value={this.state.data.code}
              onChange={this.handleChange}
              error={''}
            />
            <div className="button-container">
              <button
                type="button"
                className="white-button"
                onClick={() => false}
              >
                Forgot password
              </button>
              <button
                type="submit"
                disabled={false}
                className="blue-button"
                onClick={this.handleSubmit}
              >
                Log in
              </button>
            </div>
          </form>
        </div>
      </>
    );
  }

  protected handleSubmit = async (e: React.MouseEvent<HTMLButtonElement> | React.FormEvent<HTMLFormElement>) => {
    e.preventDefault();

    postData('/Account/VerifyCode', this.state.data, true);
  }

  private focusCodeInput() {
    this.codeInput.current.focus();
  }
}
