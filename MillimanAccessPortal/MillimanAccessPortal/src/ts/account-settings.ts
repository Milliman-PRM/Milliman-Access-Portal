import $ = require('jquery');
import toastr = require('toastr');
import shared = require('./shared');
require( 'jquery-mask-plugin');
require( 'jquery-validation');
require( 'jquery-validation-unobtrusive');
require( './lib-options');
require('./navbar');

require('bootstrap/scss/bootstrap-reboot.scss');
require('toastr/toastr.scss');
require('../scss/map.scss');


var accountSettingsRunning = false;
var passwordChangeRunning = false;

function submitAccountSettings() {
  var $button = $('#account-settings-form button.submit-button');

  function settingsChanged() {
    var changedFields = 0;
    $('input[data-original-value]').each(function () {
      if ($(this).val() !== $(this).attr('data-original-value')) {
        changedFields += 1;
      }
    });

    if (changedFields > 0) {
      return true;
    }
    return false;
  }

  if ($('#account-settings-form').valid()) {
    if (settingsChanged()) {
      accountSettingsRunning = true;
      shared.showButtonSpinner($button);
      $.ajax({
        type: 'POST',
        url: '/Account/AccountSettings',
        data: {
          FirstName: $('#FirstName').val(),
          LastName: $('#LastName').val(),
          PhoneNumber: $('#PhoneNumber').val(),
          Employer: $('#Employer').val()
        },
        headers: {
          RequestVerificationToken: $("input[name='__RequestVerificationToken']").val().toString()
        }
      }).done(function onDone() {
        $('input[data-original-value]').each(function () {
          $(this).attr('data-original-value', $(this).val().toString());
        });
        toastr.success('Your account has been updated');
      }).fail(function onFail(response) {
        toastr.warning(response.getResponseHeader('Warning'));
      }).always(function onFinish() {
        accountSettingsRunning = false;
        if (!passwordChangeRunning) {
          shared.hideButtonSpinner($button);
          $('.form-button-container').css({ visibility: 'hidden' });
        }
      });
    }

    if ($('#CurrentPassword').val() && $('#NewPassword').val()) {
      passwordChangeRunning = true;
      shared.showButtonSpinner($button);
      $.ajax({
        type: 'POST',
        url: '/Account/UpdatePassword',
        data: {
          CurrentPassword: $('#CurrentPassword').val(),
          NewPassword: $('#NewPassword').val(),
          ConfirmNewPassword: $('#ConfirmNewPassword').val()
        },
        headers: {
          RequestVerificationToken: $("input[name='__RequestVerificationToken']").val().toString()
        }
      }).done(function onDone() {
        $('input[type="password"]').val('');
        toastr.success('Your password has been updated');
      }).fail(function onFail(response) {
        toastr.warning(response.getResponseHeader('Warning'));
      }).always(function onFinish() {
        passwordChangeRunning = false;
        if (!accountSettingsRunning) {
          shared.hideButtonSpinner($button);
          $('.form-button-container').css({ visibility: 'hidden' });
        }
      });
    }
  }
}

function resetForm() {
  var $elementsToReset = $('input[data-original-value]');
  var $elementsToClear = $('input:not([data-original-value])');

  $elementsToReset.each(function () {
    $(this).val($(this).attr('data-original-value'));
  });

  $elementsToClear.val('');

  $('.form-button-container').css({ visibility: 'hidden' });

  shared.resetValidation($('#account-settings'));
  if (document.activeElement instanceof HTMLElement) {
    (<HTMLElement>document.activeElement).blur();
  }
}

$(document).ready(function onReady() {
  $('#PhoneNumber').mask('(999) 999-9999');

  if ($('#UserName').val() !== $('#Email').val()) {
    $('#Email').show();
  }

  $('input').on('keyup', function () {
    $('.form-button-container').css({ visibility: 'visible' });
  });

  $('input:not([type="password"])').attr('readonly', 'readonly');

  $('input:not([disabled])').on('focus', function () {
    $(this).removeAttr('readonly');
  });

  $('#account-settings-form button.submit-button').on('click', function (event) {
    event.preventDefault();
    submitAccountSettings();
  });

  $('#account-settings-form button.reset-button').on('click', function () {
    resetForm();
  });
});
