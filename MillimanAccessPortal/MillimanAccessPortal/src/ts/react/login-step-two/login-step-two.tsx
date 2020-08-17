import * as React from 'react';

import { Button, Form } from 'carbon-components-react';

import '../../../images/map-logo.svg';
import '../../../scss/map.scss';

export class LoginStepTwo extends React.Component {
  public render() {
    return (
      <>
        <h3>Login Step Two</h3>
        <Form>
          <Button>Log in</Button>
        </Form>
      </>
    );
  }
}
