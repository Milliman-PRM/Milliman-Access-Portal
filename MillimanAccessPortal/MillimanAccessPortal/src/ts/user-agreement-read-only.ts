import '../images/map-logo.svg';
import { convertMarkdownToHTML } from './convert-markdown';

require('../scss/map.scss');

import '../../src/scss/disclaimer.scss';

document.addEventListener('DOMContentLoaded', () => {
  const rawMarkdown = document.getElementById('raw-markdown').textContent;
  const contentDisclaimer = document.getElementById('disclaimer-text');
  contentDisclaimer.innerHTML = convertMarkdownToHTML(rawMarkdown);
});
