import '../../../scss/react/login/login-step-two.scss';

import * as React from 'react';

import { BaseFormState, Form } from '../shared-components/form/form';
import { Input } from '../shared-components/form/input';

import '../../../images/map-logo.svg';
import '../../../scss/map.scss';

interface LoginStepTwoFormState extends BaseFormState {
  awaitingLogin: boolean;
  loginWarning: string;
}

export class LoginStepTwo extends Form<{}, LoginStepTwoFormState> {

  private usernameInput: string | React.RefObject<{}> | any;
  private codeInput: string | React.RefObject<{}> | any;

  public constructor(props: {}) {
    super(props);

    this.state = {
      awaitingLogin: false,
      loginWarning: null,
      data: { username: '', code: '' },
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
              value={'eklein217@gmail.com'}
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
              value={''}
              onChange={() => false}
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
                onClick={() => false}
              >
                Log in
              </button>
            </div>
          </form>
        </div>
      </>
    );
  }

  private focusCodeInput() {
    this.codeInput.current.focus();
  }
}
