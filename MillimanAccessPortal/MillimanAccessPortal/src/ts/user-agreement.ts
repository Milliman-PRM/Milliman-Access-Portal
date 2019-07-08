import { convertMarkdownToHTML } from './convert-markdown';
import { enableButtonOnScrollBottom, postData } from './shared';

import '../../src/scss/disclaimer.scss';

document.addEventListener('DOMContentLoaded', () => {
  const rawMarkdown = document.getElementById('raw-markdown').innerText;
  const contentDisclaimer = document.getElementById('disclaimer-text');
  contentDisclaimer.innerHTML = convertMarkdownToHTML(rawMarkdown);

  const declineButton = document.getElementById('decline-button') as HTMLButtonElement;
  declineButton.onclick = async () => {
    await postData('/Account/DeclineUserAgreement', {
      ValidationId: (document.getElementById('ValidationId') as HTMLInputElement).value,
    }, true)
      .then((response) => {
        const redirectUrl = response.headers.get('NavigateTo');
        window.location.replace(redirectUrl);
      })
      .catch(() => {
        alert('An error has occurred. Please try again.');
      });
  };

  const acceptButton = document.getElementById('accept-button') as HTMLButtonElement;
  acceptButton.onclick = async () => {
    await postData('/Account/AcceptUserAgreement', {
      AgreementText: (document.getElementById('AgreementText') as HTMLInputElement).value,
      ValidationId: (document.getElementById('ValidationId') as HTMLInputElement).value,
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

  enableButtonOnScrollBottom(contentDisclaimer, acceptButton);
});
