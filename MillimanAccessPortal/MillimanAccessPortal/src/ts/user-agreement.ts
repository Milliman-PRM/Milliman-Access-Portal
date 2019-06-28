import { enableButtonOnScrollBottom, postData } from './shared';

import '../../src/scss/content-disclaimer.scss';

document.addEventListener('DOMContentLoaded', () => {
  const scrollElement = document.getElementById('content-disclaimer-text');
  const acceptButton = document.getElementById('accept-button') as HTMLButtonElement;

  acceptButton.onclick = async () => {
    await postData('/Account/AcceptUserAgreement', {
      AgreementText: (document.getElementById('AgreementText') as HTMLInputElement).value,
      IsRenewal: (document.getElementById('IsRenewal') as HTMLInputElement).value,
      ReturnUrl: (document.getElementById('ReturnUrl') as HTMLInputElement).value,
    }, true)
      .then((response) => {
        const redirectUrl = response.headers.get('NavigateTo');
        window.location.replace(redirectUrl);
      })
      .catch(() => {
        alert('An error has occurred. Please try again.');
      });
  };

  enableButtonOnScrollBottom(scrollElement, acceptButton);
});
