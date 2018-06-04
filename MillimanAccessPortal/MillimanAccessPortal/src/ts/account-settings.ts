import { FormBase } from './form/form-base';
import { AccessMode, SubmissionMode } from './form/form-modes';
import { SubmissionGroup } from './form/form-submission';

import $ = require('jquery');
import toastr = require('toastr');

require('./navbar');

require('bootstrap/scss/bootstrap-reboot.scss');
require('toastr/toastr.scss');
require('../scss/map.scss');

$(document).ready(() => {
  if ($('#UserName').val() !== $('#Email').val()) {
    $('#Email').show();
  }

  const accountGroup = new SubmissionGroup(
    [ 'account' ],
    'AccountSettings',
    'POST',
    (response) => {
      toastr.success('Your account has been updated');
    },
  );
  const passwordGroup = new SubmissionGroup(
    [ 'password' ],
    'UpdatePassword',
    'POST',
    (response) => {
      $('input[type="password"]').val('');
      toastr.success('Your password has been updated');
    },
  );
  const finalGroup = SubmissionGroup.FinalGroup(() => {
    formObject.bindToDOM($('#account-settings-form')[0]);
    $('#FirstName').change();
  });

  const formObject = new FormBase();
  formObject.bindToDOM($('#account-settings-form')[0]);
  formObject.configure([
    {
      groups: [ accountGroup, passwordGroup, finalGroup ],
      name: 'update',
      sparse: true,
    },
  ]);
  formObject.submissionMode = 'update';
});
