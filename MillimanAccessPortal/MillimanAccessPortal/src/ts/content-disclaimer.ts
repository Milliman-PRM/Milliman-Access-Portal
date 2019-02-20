import { postData } from './shared';

document.addEventListener('DOMContentLoaded', () => {
  const acceptButton = document.getElementById('accept-button');
  acceptButton.onclick = async () => {
    try {
      await postData('/AuthorizedContent/AcceptDisclaimer', {
        SelectionGroupId: (document.getElementById('SelectionGroupId') as HTMLInputElement).value,
        ValidationId: (document.getElementById('ValidationId') as HTMLInputElement).value,
      }, true);
    } catch (e) {
      alert('An error has occurred. Please try again.');
    }
    window.location.replace(window.location.href);
  };
});
