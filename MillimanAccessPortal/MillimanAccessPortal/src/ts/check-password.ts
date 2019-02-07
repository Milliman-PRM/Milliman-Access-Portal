import 'promise-polyfill/dist/polyfill';
import 'whatwg-fetch';

import * as $ from 'jquery';
import toastr = require('toastr');

import './lib-options';
import { postData } from './shared';

require('toastr/toastr.scss');

document.addEventListener('DOMContentLoaded', () => {
  const newPasswordInput = document.getElementById('NewPassword');
  const confirmPasswordInput = document.getElementById('ConfirmNewPassword');

  newPasswordInput.addEventListener('keyup', () => {
      newPasswordInput.removeAttribute('Warning');
      newPasswordInput.removeAttribute('Validated');
      confirmPasswordInput.removeAttribute('Validated');
  });

  newPasswordInput.addEventListener('blur', async () => {
    const proposedPassword = (newPasswordInput as HTMLInputElement).value;

    if (proposedPassword) {
      if (!newPasswordInput.hasAttribute('Validated')) {
        try {
          await postData('/Account/CheckPasswordValidity', { proposedPassword }, true);
          newPasswordInput.removeAttribute('Warning');
          newPasswordInput.setAttribute('Validated', 'Validated');
          $('#PasswordsAreValid').val('valid');
          $('[data-valmsg-for="PasswordsAreValid"]').remove();
        } catch (err) {
          toastr.warning(err.message, null, {
            timeOut: 0,
          });
          newPasswordInput.removeAttribute('Validated');
          newPasswordInput.setAttribute('Warning', 'Warning');
          $('#PasswordsAreValid').val('');
        }
      }
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
