
﻿import * as React from 'react';
import '../../../images/map-logo.svg';
import '../../../scss/map.scss';
import { InitialUserForm } from './create-initial-user-form';

export class CreateInitialUser extends React.Component {

    public constructor(props: {}) {
        super(props);

    }
    public render() {
        return (
            <div id="create-initial-user-container">
                <div
                    id="create-initial-user"
                    className={'admin-panel-container flex-item-for-phone-only-12-12 ' +
                        'flex-item-for-tablet-up-10-12 flex-item-for-desktop-up-4-12'}
                >
                    <div className="form-content-container">
                        <InitialUserForm />
                    </div>
                </div>
            </div>
        );
    }

}