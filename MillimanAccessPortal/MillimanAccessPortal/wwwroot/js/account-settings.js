
function submitAccountSettings() {
  var $accountSettingsForm = $('#account-settings-form');
  var $button = $('#account-settings-form button.submit-button');
  var urlPartial = 'Account/';

  if ($accountSettingsForm.valid()) {
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
      $('.form-button-container').css({ 'visibility': 'hidden' });
      toastr.success("Your account has been updated");
    }).fail(function onFail(response) {
      shared.hideButtonSpinner($button);
      toastr.warning(response.getResponseHeader('Warning'));
    });

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
        //toastr.warning(response.getResponseHeader('Warning'));
        toastr.warning("Your password could not be updated");
      });
    }
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

});
