import '../../../scss/react/login/login-step-two.scss';

import * as React from 'react';

import { Input } from '../shared-components/form/input';

import '../../../images/map-logo.svg';
import '../../../scss/map.scss';

export class LoginStepTwo extends React.Component {
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
}
