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
    //TODO: check if necessary
    loginWarning: string;
}

export class InitialUserForm extends Form<{}, InitialUserFormState> {

    protected schema = Yup.object({
        username: Yup.string()
            .email()
            .required()
            .label('Username')
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


    }
    public render() {
        return (
            <form
                autoComplete="off"
                method="post"
                className="form-horizontal"
            >
                <div className="form-section-container">
                    <div className="form-section">
                        <h4 className="form-section-title">Create a new account</h4>
                        <div className="text-danger" />
                        <div className="form-input-container">
                            <div className="form-input form-input-text flex-item-12-12">
                                <label className="form-input-text-title" />
                                <div className="col-md-10">
                                    <input className="form-control" />
                                    <span className="text-danger" />
                                </div>
                            </div>
                        </div>
                    </div>
                    <div className="form-submission-section">
                        <div className="button-container button-container-update">
                            <button type="submit" className="button-submit blue-button">Create User</button>
                        </div>
                    </div>
                </div>
            </form>
        );
    }

}
