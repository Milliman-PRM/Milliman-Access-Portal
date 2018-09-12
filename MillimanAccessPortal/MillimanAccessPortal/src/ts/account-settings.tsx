import '../images/map-logo.svg';

import * as React from 'react';
import * as ReactDOM from 'react-dom';

import { FormBase } from './form/form-base';
import { SubmissionGroup } from './form/form-submission';
import { NavBar } from './react/shared-components/navbar';

import './check-password';

import $ = require('jquery');
import toastr = require('toastr');
import { AccessMode } from './form/form-modes';

require('toastr/toastr.scss');
require('../scss/map.scss');

document.addEventListener('DOMContentLoaded', () => {
  const view = document.getElementsByTagName('body')[0].getAttribute('data-nav-location');
  ReactDOM.render(<NavBar currentView={view} />, document.getElementById('navbar'));
});

$(document).ready(() => {
  if ($('#UserName').val() !== $('#Email').val()) {
    $('#Email').show();
  }

  const accountGroup = new SubmissionGroup(
    [ 'username', 'account' ],
    'AccountSettings',
    'POST',
    () => {
      toastr.success('Your account has been updated');
    },
  );
  const passwordGroup = new SubmissionGroup(
    [ 'username', 'password' ],
    'UpdatePassword',
    'POST',
    () => {
      $('input[type="password"]').val('');
      toastr.success('Your password has been updated');
    },
  );
  const finalGroup = SubmissionGroup.FinalGroup(() => {
    formObject = new FormBase();
    formObject.bindToDOM($('#account-settings-form')[0]);
    formObject.configure([
      {
        groups: [ accountGroup, passwordGroup, finalGroup ],
        name: 'update',
        sparse: true,
      },
    ]);
    formObject.submissionMode = 'update';
    $('#FirstName').change();
  });

  let formObject = new FormBase();
  formObject.bindToDOM($('#account-settings-form')[0]);
  formObject.configure([
    {
      groups: [ accountGroup, passwordGroup, finalGroup ],
      name: 'update',
      sparse: true,
    },
  ]);
  formObject.submissionMode = 'update';
  formObject.accessMode = AccessMode.Write;
});
