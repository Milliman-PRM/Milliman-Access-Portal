declare function require(moduleName: string): any;

import '../../../../images/map-logo.svg';
import '../../../../scss/map.scss';

import * as React from 'react';
import * as ReactDOM from 'react-dom';
import { UpdateUserAgreement as Root} from './update-user-agreement';

let UpdateUserAgreement: typeof Root = require('./update-user-agreement').UpdateUserAgreement;

document.addEventListener('DOMContentLoaded', () => {
    ReactDOM.render(<UpdateUserAgreement />, document.getElementById('content-container'));
});

if (module.hot) {
    module.hot.accept(['./update-user-agreement'], () => {
        UpdateUserAgreement = require('./update-user-agreement').UpdateUserAgreement;
    });
}
