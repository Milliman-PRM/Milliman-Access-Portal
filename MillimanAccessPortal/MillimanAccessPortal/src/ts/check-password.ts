import toastr = require('toastr');

import './lib-options';

require('toastr/toastr.scss');

document.addEventListener('DOMContentLoaded', () => {
  const newPasswordInput = document.getElementById('NewPassword');
  const confirmPasswordInput = document.getElementById('ConfirmNewPassword');

  newPasswordInput.addEventListener('blur', () => {
    const proposedPassword = (newPasswordInput as HTMLInputElement).value;

    if (proposedPassword) {
      const antiforgeryToken = document.querySelector('input[name="__RequestVerificationToken"]').getAttribute('value');

      fetch('/Account/CheckPasswordValidity', {
        method: 'POST',
        headers: {
          'Accept': 'application/json',
          'Content-Type': 'application/json',
          'RequestVerificationToken': antiforgeryToken,
        },
        credentials: 'same-origin',
        body: JSON.stringify({ ProposedPassword: proposedPassword }),
      })
        .then((response) => {
          if (!response.ok) {
            toastr.warning(response.headers.get('Warning') || 'Unknown error');
            newPasswordInput.removeAttribute('Validated');
            newPasswordInput.setAttribute('Warning', 'Warning');
          } else {
            newPasswordInput.removeAttribute('Warning');
            newPasswordInput.setAttribute('Validated', 'Validated');
          }
        });
    } else {
      newPasswordInput.removeAttribute('Validated');
      newPasswordInput.removeAttribute('Warning');
    }
  });

  confirmPasswordInput.addEventListener('keyup', () => {
    const proposedPassword = (newPasswordInput as HTMLInputElement).value;
    const confirmPassword = (confirmPasswordInput as HTMLInputElement).value;

    if (confirmPassword === proposedPassword && newPasswordInput.hasAttribute('Validated')) {
      confirmPasswordInput.setAttribute('Validated', 'Validated');
    } else {
      confirmPasswordInput.removeAttribute('Validated');
    }
  });

});
