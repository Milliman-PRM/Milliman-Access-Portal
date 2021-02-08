import { convertMarkdownToHTML } from './convert-markdown';

import '../../src/scss/disclaimer.scss';

document.addEventListener('DOMContentLoaded', () => {
  const rawMarkdown = document.getElementById('raw-markdown').textContent;
  const contentDisclaimer = document.getElementById('disclaimer-text');
  contentDisclaimer.innerHTML = convertMarkdownToHTML(rawMarkdown);
});
