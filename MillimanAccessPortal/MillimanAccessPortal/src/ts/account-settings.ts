import $ = require('jquery');
import toastr = require('toastr');
import { FormBase } from './form/form-base';
import { SubmissionMode, AccessMode } from './form/form-modes';
import { SubmissionGroup } from './form/form-submission';
require('./navbar');

require('bootstrap/scss/bootstrap-reboot.scss');
require('toastr/toastr.scss');
require('../scss/map.scss');


let formObject: FormBase;

var accountSettingsRunning = false;
var passwordChangeRunning = false;

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
    formObject.bindToDOM();
    $('#FirstName').change();
  });

  const formObject = new FormBase();
  formObject.bindToDOM($('#account-settings-form')[0]);
  formObject.configure([
    {
      group: accountGroup.chain(passwordGroup, true).chain(finalGroup, true),
      name: 'update',
    },
  ]);
  formObject.submissionMode = 'update';
});
