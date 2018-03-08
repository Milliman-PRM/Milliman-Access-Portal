
function submitAccountSettings() {
  var $accountSettingsForm = $('#account-settings-form');
  var $button = $('#account-settings-form button.submit-button');
  var urlPartial = 'Account/';

  if ($accountSettingsForm.valid()) {

    if (settingsChanged()) {
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
          RequestVerificationToken: $("input[name='__RequestVerificationToken']").val()
        }
      }).done(function onDone() {
        shared.hideButtonSpinner($button);
        $('input[data-original-value]').each(function () {
          $(this).attr('data-original-value', $(this).val());
        });
        $('.form-button-container').css({ 'visibility': 'hidden' });
        toastr.success("Your account has been updated");
      }).fail(function onFail(response) {
        shared.hideButtonSpinner($button);
        toastr.warning(response.getResponseHeader('Warning'));
      });
    }

    if ($('#CurrentPassword').val() && $('#NewPassword').val()) {
      $.ajax({
        type: 'POST',
        url: '/Account/UpdatePassword',
        data: {
          CurrentPassword: $('#CurrentPassword').val(),
          NewPassword: $('#NewPassword').val(),
          ConfirmNewPassword: $('#ConfirmNewPassword').val()
        },
        headers: {
          RequestVerificationToken: $("input[name='__RequestVerificationToken']").val()
        }
      }).done(function onDone() {
        //shared.hideButtonSpinner($button);
        $('.form-button-container').css({ 'visibility': 'hidden' });
        $('input[type="password"]').val('');
        toastr.success("Your password has been updated");
      }).fail(function onFail(response) {
        //shared.hideButtonSpinner($button);
        toastr.warning(response.getResponseHeader('Warning'));
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

  shared.resetValidation($('#account-settings'));
}

function settingsChanged() {
  var changedFields = 0;
  $('input[data-original-value]').each(function () {
    if ($(this).val() != $(this).attr('data-original-value')) {
      changedFields++;
    }
  });

  if (changedFields > 0) {
    return true;
  } else {
    return false;
  }
}

$(document).ready(function onReady() {

  if ($('#UserName').val() != $('#Email').val()) {
    $('#Email').show();
  }

  $('input').on('keyup', function () {
    $('.form-button-container').css({ 'visibility': 'visible' });
  });

  $('input:not([type="password"])').attr('readonly', 'readonly');

  $('input:not([disabled])').on('focus', function () {
    $(this).removeAttr('readonly');
  });

  $('#account-settings-form button.submit-button').on('click', function(event) {
    event.preventDefault();
    submitAccountSettings();
  });

  $('#account-settings-form button.reset-button').on('click', function () {
    resetForm();
  });

});
