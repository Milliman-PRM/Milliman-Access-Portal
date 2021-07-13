declare function require(moduleName: string): any;

import '../../../images/map-logo.svg';
import '../../../scss/map.scss';

import * as React from 'react';
import * as ReactDOM from 'react-dom';
import { CreateInitialUser as Root } from './create-initial-user';

let CreateInitialUser: typeof Root = require('./create-initial-user').CreateInitialUser;

document.addEventListener('DOMContentLoaded', () => {
    ReactDOM.render(<CreateInitialUser />,
    document.getElementById('content-container'),
    );
});

if (module.hot) {
    module.hot.accept(['./create-initial-user'], () => {
        CreateInitialUser = require('./create-initial-user').CreateInitialUser;
    });
}
