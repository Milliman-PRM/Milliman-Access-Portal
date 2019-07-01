import { convertMarkdownToHTML } from '../ts/convert-markdown';

import '../images/map-logo.svg';

import '../../src/scss/update-user-agreement.scss';

document.addEventListener('DOMContentLoaded', () => {
  $('#agreement-container .markdown-select-edit').click(setAgreementToEditMode);
  $('#agreement-container .markdown-select-preview').click(setAgreementToPreviewMode);
});

function setAgreementToEditMode() {
  // Toggle buttons
  $('#agreement-container .markdown-select-edit').addClass('selected');
  $('#agreement-container .markdown-select-preview').removeClass('selected');
  // Toggle preview -> textarea
  $('#newAgreementText').show();
  $('#AgreementPreview').hide();
  // Clear the preview
  $('AgreementPreview').empty();
}

function setAgreementToPreviewMode() {
  // Update markdown from textarea content
  const rawAgreementMarkdown = (document.getElementById('newAgreementText') as HTMLTextAreaElement).value.trimRight();
  const processedAgreementHTML = convertMarkdownToHTML(rawAgreementMarkdown);
  document.getElementById('AgreementPreview').innerHTML = processedAgreementHTML;
  // Toggle buttons
  $('#agreement-container .markdown-select-preview').addClass('selected');
  $('#agreement-container .markdown-select-edit').removeClass('selected');
  // Toggle textarea -> preview
  $('#AgreementPreview').show();
  $('#newAgreementText').hide();
}
