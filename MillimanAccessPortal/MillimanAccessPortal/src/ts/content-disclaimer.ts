import { convertMarkdownToHTML } from './convert-markdown';
import { postData } from './shared';

import '../scss/disclaimer.scss';

document.addEventListener('DOMContentLoaded', () => {
  const rawMarkdown = document.getElementById('raw-markdown').firstChild.nodeValue;
  const contentDisclaimer = document.getElementById('disclaimer-text');
  contentDisclaimer.innerHTML = convertMarkdownToHTML(rawMarkdown);

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
