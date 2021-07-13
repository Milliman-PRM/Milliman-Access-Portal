import * as React from 'react';
import { InitialUserForm } from './create-initial-user-form';

import '../../../images/map-logo.svg';
import '../../../scss/map.scss';

export class CreateInitialUser extends React.Component {
    public render() {
        return (
        <div className="form-content-container flex-item-for-tablet-up-10-12 flex-item-for-desktop-up-6-12">
          <InitialUserForm />
        </div>
        );
    }

}
