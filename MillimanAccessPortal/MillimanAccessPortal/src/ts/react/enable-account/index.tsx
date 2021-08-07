declare function require(moduleName: string): any;

import '../../../images/map-logo.svg';
import '../../../scss/map.scss';

import * as React from 'react';
import * as ReactDOM from 'react-dom';
import { EnableAccount as Root } from './enable-account';

let EnableAccount: typeof Root = require('./enable-account').EnableAccount;

document.addEventListener('DOMContentLoaded', () => {
    ReactDOM.render(<EnableAccount />,
    document.getElementById('content-container'),
    );
});

if (module.hot) {
    module.hot.accept(['./enable-account'], () => {
      EnableAccount = require('./create-initial-user').EnableAccount;
    });
}
