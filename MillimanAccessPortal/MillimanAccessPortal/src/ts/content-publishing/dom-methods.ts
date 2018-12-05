import 'jquery-validation';
import 'jquery-validation-unobtrusive';

import * as $ from 'jquery';
import * as toastr from 'toastr';

import { AddRootContentItemActionCard, ClientCard, RootContentItemCard } from '../card';
import { CancelContentPublicationRequestDialog, DeleteRootContentItemDialog } from '../dialog';
import { FormBase } from '../form/form-base';
import { isFileUploadInput } from '../form/form-input/file-upload';
import { AccessMode } from '../form/form-modes';
import { SubmissionGroup } from '../form/form-submission';
import { Guid } from '../react/shared-components/interfaces';
import {
  collapseAllListener, expandAllListener, filterFormListener, filterTreeListener, get,
  hideButtonSpinner, showButtonSpinner, updateCardStatus, updateCardStatusButtons,
  updateFormStatusButtons, wrapCardCallback, wrapCardIconCallback,
} from '../shared';
import { setUnloadAlert } from '../unload-alerts';
import { UploadComponent } from '../upload/upload';
import {
  BasicNode, ClientSummary, ClientTree, ContentType, PreLiveContentValidationSummary,
  PublishRequest, RootContentItemDetail, RootContentItemList, RootContentItemSummary,
  RootContentItemSummaryAndDetail,
} from '../view-models/content-publishing';
import { PublicationStatusMonitor } from './publication-status-monitor';

require('tooltipster');

let formObject: FormBase;
let statusMonitor: PublicationStatusMonitor;

let preLiveObject: PreLiveContentValidationSummary;

const goLiveDisabledTooltip = 'Complete checks to proceed';
const goLiveEnabledTooltip = 'Approve content and go live';

function deleteRootContentItem(
  rootContentItemId: Guid,
  rootContentItemName: string,
  password: string,
  callback: () => void,
) {
  $.ajax({
    data: {
      rootContentItemId,
      password,
    },
    headers: {
      RequestVerificationToken: $('input[name="__RequestVerificationToken"]').val().toString(),
    },
    type: 'DELETE',
    url: 'ContentPublishing/DeleteRootContentItem',
  }).done(function onDone(response: RootContentItemDetail) {
    $('#content-publishing-form').hide();
    $('#root-content-items .card-container')
      .filter((_, card) => $(card).data().rootContentItemId === response.id)
      .remove();
    addToDocumentCount(response.clientId, -1);
    callback();
    toastr.success(rootContentItemName + ' was successfully deleted.');
  }).fail(function onFail(response) {
    callback();
    toastr.warning(response.getResponseHeader('Warning')
      || 'An unknown error has occurred.');
  }).always(() => {
    statusMonitor.checkStatus();
  });
}
export function rootContentItemDeleteClickHandler(event) {
  const $clickedCard = $(this).closest('.card-container');
  const rootContentItemId = $clickedCard.data().rootContentItemId;
  const rootContentItemName = $clickedCard.find('.card-body-primary-text').first().text();
  event.stopPropagation();
  new DeleteRootContentItemDialog(
    rootContentItemName,
    rootContentItemId,
    (data, callback) => {
      if (data.password) {
        showButtonSpinner($('.vex-first'), 'Deleting');
        $('.vex-dialog-button').attr('disabled', '');
        deleteRootContentItem(rootContentItemId, rootContentItemName, data.password, callback);
      } else if (data.password === '') {
        toastr.warning('Please enter your password to proceed');
        return false;
      } else {
        toastr.info('Deletion was canceled');
      }
      return true;
    },
  ).open();
}
function cancelContentPublication(data, callback) {
  $.ajax({
    data: {
      RootContentItemId: data.RootContentItemId,
    },
    headers: {
      RequestVerificationToken: $('input[name="__RequestVerificationToken"]').val().toString(),
    },
    type: 'POST',
    url: 'ContentPublishing/CancelContentPublicationRequest',
  }).done(() => {
    if (typeof callback === 'function') { callback(); }
    toastr.success('Content publication request canceled');
  }).fail((response) => {
    if (typeof callback === 'function') { callback(); }
    toastr.warning(response.getResponseHeader('Warning')
      || 'An unknown error has occurred.');
  }).always(() => {
    statusMonitor.checkStatus();
  });
}

export function rootContentItemCancelClickHandler(event) {
  const $clickedCard = $(this).closest('.card-container');
  const rootContentItemId = $clickedCard.data().rootContentItemId;
  const rootContentItemName = $clickedCard.find('.card-body-primary-text').first().text();
  event.stopPropagation();
  new CancelContentPublicationRequestDialog(rootContentItemId, rootContentItemName, cancelContentPublication).open();
}
export function openNewRootContentItemForm() {
  if (formObject && formObject.submissionMode === 'new') {
    return;
  }
  const clientId = $('#client-tree [selected]').parent().data().clientId;
  renderRootContentItemForm({
    clientId,
    contentName: '',
    contentTypeId: '0',
    description: '',
    doesReduce: false,
    id: '0',
    notes: '',
    relatedFiles: [],
    isSuspended: false,
  });
  setFormNew();
}

function setFormReadOnly() {
  formObject.submissionMode = 'hidden';
  formObject.accessMode = AccessMode.Read;
  $('#root-content-items [selected]').removeAttr('editing');
  $('#content-publishing-form .admin-panel-toolbar .action-icon').show();
}
function setFormNew() {
  formObject.submissionMode = 'new';
  formObject.accessMode = AccessMode.Write;
  $('#root-content-items [selected]').attr('editing', '');
  $('#content-publishing-form .admin-panel-toolbar .action-icon').hide();
  $('#content-publishing-form .admin-panel-toolbar .action-icon-cancel').show();
}
function setFormEdit() {
  formObject.submissionMode = 'edit';
  formObject.accessMode = AccessMode.Defer;
  $('#root-content-items [selected]').attr('editing', '');
  $('#content-publishing-form .admin-panel-toolbar .action-icon').hide();
  $('#content-publishing-form .admin-panel-toolbar .action-icon-cancel').show();
}
function setFormEditOrRepublish() {
  formObject.submissionMode = 'edit-or-republish';
  formObject.accessMode = AccessMode.Defer;
  $('#root-content-items [selected]').attr('editing', '');
  $('#content-publishing-form .admin-panel-toolbar .action-icon').hide();
  $('#content-publishing-form .admin-panel-toolbar .action-icon-cancel').show();
}

function mapRootContentItemDetail(item: RootContentItemDetail) {
  const formMap = new Map<string, string | number | boolean>();

  formMap.set('Id', item.id);
  formMap.set('ClientId', item.clientId);
  formMap.set('ContentName', item.contentName);
  formMap.set('ContentTypeId', item.contentTypeId);
  formMap.set('DoesReduce',  item.doesReduce);
  formMap.set('Description', item.description);
  formMap.set('Notes', item.notes);

  return formMap;
}

function addToDocumentCount(clientId: Guid, offset: number) {
  const itemCount = $('#client-tree .card-container')
    .filter((_, card) => $(card).data().clientId === clientId)
    .find('use[href="#reports"]').closest('div').find('h4');
  itemCount.html(`${parseInt(itemCount.html(), 10) + offset}`);
}

function renderConfirmationPane(response: PreLiveContentValidationSummary) {
  // Show and clear all confirmation checkboxes
  $('#report-confirmation .loading-wrapper').hide();
  $('#report-confirmation .admin-panel-content-container')
    .show()[0].scrollTop = 0;
  $('#report-confirmation label')
    .show()
    .find('input[type="checkbox"]')
    .removeAttr('disabled')
    .prop('checked', false);
  $('#confirmation-section-attestation .button-approve')
    .addClass('disabled')
    .tooltipster('content', goLiveDisabledTooltip);
  // set src for iframes, conditionally marking iframes as unchanged
  const linkPairs: Array<{sectionName: string, link: string, node?: string}> = [
    {
      sectionName: 'master-content',
      link: response.masterContentLink,
      node: response.contentTypeName === 'FileDownload'
        ? '.content-preview-download'
        : response.contentTypeName === 'Html'
          ? '.content-preview-sandbox'
          : '.content-preview',
    },
    { sectionName: 'user-guide', link: response.userGuideLink },
    { sectionName: 'release-notes', link: response.releaseNotesLink },
  ];
  linkPairs.forEach((pair) => {
    $(`#confirmation-section-${pair.sectionName} div`)
      .filter(pair.node || '.content-preview')
      .find('a,iframe,object')
      .attr('src', function() {
        return $(this).is('iframe')
          ? pair.link
          : null;
      })
      .attr('data', function() {
        return $(this).is('object')
          ? pair.link
          : null;
      })
      .attr('href', function() {
        return $(this).is('a')
          ? pair.link
          : null;
      })
      .parent()
      .show()
      .siblings('div')
      .hide()
      .filter(() => pair.link === null)
      .filter('.content-preview-none')
      .show()
      .siblings('div')
      .hide()
      .find('iframe,object')
      .closest('.confirmation-section').find('label')
      .hide()
      .find('input')
      .attr('disabled', '');
  });

  if (!response.doesReduce || !response.selectionGroups) {
    $('#confirmation-section-hierarchy-diff')
      .hide()
      .find('input[type="checkbox"]')
      .attr('disabled', '');
    $('#confirmation-section-hierarchy-stats')
      .hide()
      .find('input[type="checkbox"]')
      .attr('disabled', '');
  } else {
    $('#confirmation-section-hierarchy-diff')
      .show();
    $('#confirmation-section-hierarchy-stats')
      .show();
    // populate (after calculating, if need be) hierarchy diff
    $('#confirmation-section-hierarchy-diff .hierarchy > ul').children().remove();
    if (!response.liveHierarchy) {
      $('#confirmation-section-hierarchy-diff .hierarchy-left > ul').append('<div>None</div>');
    } else {
      response.liveHierarchy.fields.forEach((field) => {
        const subList = $(`<li><h6>${field.displayName}</h6><ul class="hierarchy-list"></ul></li>`);
        field.values.forEach((value) =>
            subList.find('ul').append(`<li>${value.value}</li>`));
        $('#confirmation-section-hierarchy-diff .hierarchy-left > ul')
          .append(subList);
      });
    }
    if (!response.newHierarchy) {
      $('#confirmation-section-hierarchy-diff .hierarchy-right > ul').append('<div>None</div>');
    } else {
      response.newHierarchy.fields.forEach((field) => {
        const subList = $(`<li><h6>${field.displayName}</h6><ul class="hierarchy-list"></ul></li>`);
        field.values.forEach((value) =>
            subList.find('ul').append(`<li>${value.value}</li>`));
        $('#confirmation-section-hierarchy-diff .hierarchy-right > ul')
          .append(subList);
      });
    }
    // populate hierarchy stats
    $('#confirmation-section-hierarchy-stats > div > ul').children().remove();
    response.selectionGroups.forEach((selectionGroup) => {
      $('#confirmation-section-hierarchy-stats > div > ul')
        .append(`<li><div class="selection-group-summary">
          <h5>${selectionGroup.name}${selectionGroup.isMaster ? ' (Master)' : ''}</h5>
          <ul>
            <li><div class="selection-group-stat">
              <span class="selection-group-stat-label">Users:</span>
              <span class="selection-group-stat-value">${selectionGroup.userCount}</span>
            </div></li>
          </ul>
        </div></li>`);
    });
  }
  // populate attestation
  $('#confirmation-section-attestation .attestation-language').html(response.attestationLanguage);

  const anyEnabled = $('#report-confirmation input[type="checkbox"]')
    .filter((_, element) => $(element).attr('disabled') === undefined).length;
  if (!anyEnabled) {
    $('#confirmation-section-attestation .button-approve')
      .removeClass('disabled')
      .tooltipster('content', goLiveEnabledTooltip);
  }

  preLiveObject = response;
}

function renderRootContentItemForm(item?: RootContentItemDetail, ignoreFiles: boolean = false) {
  const $panel = $('#content-publishing-form');
  const $rootContentItemForm = $panel.find('form.admin-panel-content');

  if (item) {
    const formMap = mapRootContentItemDetail(item);
    formMap.forEach((value, key) => {
      if (key !== 'DoesReduce') {  // because DoesReduce is a checkbox
        $rootContentItemForm.find(`#${key}`).val(value ? value.toString() : '');
      }
    });
    $rootContentItemForm.find('.file-upload').data('originalName', '');
    if (item.relatedFiles && !ignoreFiles) {
      item.relatedFiles.forEach((relatedFile) => {
        $rootContentItemForm.find(`#${relatedFile.filePurpose}`)
          .val('')
          .siblings('label').find('.file-upload')
          .data('originalName', relatedFile.fileOriginalName);
      });
    }

    const $doesReduceToggle = $rootContentItemForm.find('#DoesReduce');
    $doesReduceToggle.prop('checked', item.doesReduce);
  }

  const createContentGroup = new SubmissionGroup<RootContentItemSummaryAndDetail>(
    [
      'common',
      'root-content-item-info',
      'root-content-item-content-type',
      'root-content-item-description',
    ],
    'ContentPublishing/CreateRootContentItem',
    'POST',
    (response) => {
      // Rerender the form to set Id and reset original values
      renderRootContentItemForm(response.detail);
      // Add the new content item as a card and select it
      renderRootContentItem(response.summary);
      $('#root-content-items .card-container')
        .filter((_, card) => $(card).data().rootContentItemId === response.detail.id)
        .children().click();
      // Update the root content item count stat on the client card
      addToDocumentCount(response.detail.clientId, 1);

      toastr.success('content item created');
    },
    (data) => data.indexOf('DoesReduce=') === -1
      ? data + '&DoesReduce=False'
      : data,
  );
  const updateContentGroup = new SubmissionGroup<RootContentItemSummaryAndDetail>(
    [
      'common',
      'root-content-item-info',
      'root-content-item-description',
    ],
    'ContentPublishing/UpdateRootContentItem',
    'POST',
    (response) => {
      renderRootContentItemForm(response.detail, true);
      // Update related root content item card
      const $card = $('#root-content-items .card-container')
        .filter((_, card) => $(card).data().rootContentItemId === response.detail.id);
      $card.find('.card-body-primary-text').html(response.summary.contentName);
      $card.find('.card-body-secondary-text').html(response.summary.contentTypeName);
      toastr.success('content item updated');
    },
    (data) => data.indexOf('DoesReduce=') === -1
      ? data + '&DoesReduce=False'
      : data,
  );
  const submitPublication = new SubmissionGroup<any>(
    [
      'common',
      'publication-files',
    ],
    'ContentPublishing/Publish',
    'POST',
    (itemDetail) => {
      renderRootContentItemForm(itemDetail);
      toastr.success('Publication request submitted');
    },
    (data) => {
      const dataArray: { [key: string]: string } = {};
      data.split('&')
        .map((kvp) => kvp.split('='))
        .forEach((kvp) => dataArray[decodeURIComponent(kvp[0])] = decodeURIComponent(kvp[1]));
      const fileChanges = ['MasterContent', 'UserGuide', 'Thumbnail', 'ReleaseNotes']
        .map((file) => {
          const fileData = dataArray[file].split('~');
          return {
            fileOriginalName: fileData[0],
            filePurpose: file,
            fileUploadId: fileData[1],
          };
        });
      const publishRequest: PublishRequest = {
        newRelatedFiles: fileChanges
          .filter((file) => file.fileUploadId && file.fileUploadId !== 'delete'),
        deleteFilePurposes: fileChanges
          .filter((file) => file.fileUploadId && file.fileUploadId === 'delete')
          .map((file) => file.filePurpose),
        rootContentItemId: dataArray.Id,
      };
      return publishRequest;
    },
  );
  const readOnlyGroup = SubmissionGroup.FinalGroup(() => {
    setFormReadOnly();
    // Update root content item card status immediately
    statusMonitor.checkStatus();
  });

  // Create/retrieve and bind the new form
  formObject = new FormBase();
  formObject.bindToDOM($rootContentItemForm[0]);
  formObject.configure(
    [
      {
        groups: [],
        name: 'hidden',
        sparse: false,
      },
      {
        groups: [ createContentGroup, submitPublication, readOnlyGroup ],
        name: 'new',
        sparse: false,
      },
      {
        groups: [ updateContentGroup, submitPublication, readOnlyGroup ],
        name: 'edit-or-republish',
        sparse: true,
      },
      {
        groups: [ updateContentGroup, readOnlyGroup ],
        name: 'edit',
        sparse: false,
      },
    ],
  );
  const $contentTypeDropdown = $('#ContentTypeId');
  const contentType = $contentTypeDropdown
    .find(`option[value="${$contentTypeDropdown.val()}"]`)
    .data() as ContentType;
  formObject.inputSections.forEach((section) =>
    section.inputs.forEach((input) => {
      if (contentType && isFileUploadInput(input)) {
        input.fileTypes.set(UploadComponent.Content, contentType.fileExtensions);
        input.configure();
      }
    }));

  $rootContentItemForm
    .removeData('validator')
    .removeData('unobtrusiveValidation');
  $.validator.unobtrusive.parse($rootContentItemForm[0]);
}

function renderRootContentItem(item: RootContentItemSummary) {
  const $rootContentItemCard = new RootContentItemCard(
    item,
    wrapCardCallback(get(
      'ContentPublishing/RootContentItemDetail',
      [
        updateFormStatusButtons,
        renderRootContentItemForm,
        setFormReadOnly,
      ],
      (data) => ({
        rootContentItemId: data && data.rootContentItemId,
      }),
    ), () => formObject),
    wrapCardIconCallback(($card, always) => get(
        'ContentPublishing/RootContentItemDetail',
        [
          renderRootContentItemForm,
          always,
        ],
        (data) => ({
          rootContentItemId: data && data.rootContentItemId,
        }),
      )($card), () => formObject, {count: 1, offset: 0}, undefined, () => {
      setFormEditOrRepublish();
    }),
    rootContentItemDeleteClickHandler,
    rootContentItemCancelClickHandler,
    wrapCardIconCallback((card) => {
      $('#report-confirmation .admin-panel-content-container').hide();
      $('#report-confirmation .loading-wrapper').show();
      get(
        'ContentPublishing/PreLiveSummary',
        [
          renderConfirmationPane,
        ],
        (data) => ({
          rootContentItemId: data && data.rootContentItemId,
        }),
      )(card);
    }, () => formObject, {count: 1, offset: 1}, () => false),
  ).build();
  updateCardStatus($rootContentItemCard, item.publicationDetails);
  updateCardStatusButtons($rootContentItemCard, item.publicationDetails && item.publicationDetails.statusEnum);
  $rootContentItemCard.data('statusEnum', item.publicationDetails && item.publicationDetails.statusEnum);
  $('#root-content-items ul.admin-panel-content').append($rootContentItemCard);
}
function renderRootContentItemList(response: RootContentItemList, rootContentItemId?: string) {
  const $rootContentItemList = $('#root-content-items ul.admin-panel-content');
  $rootContentItemList.empty();
  response.summaryList.forEach(renderRootContentItem);
  $rootContentItemList.find('.tooltip').tooltipster();

  if (rootContentItemId !== null) {
    $(`[data-root-content-item-id=${rootContentItemId}]`).click();
  }
}

function renderClientNode(client: BasicNode<ClientSummary>, level: number = 0) {
  const $card = new ClientCard(
    client.value,
    client.value.eligibleUserCount,
    client.value.rootContentItemCount,
    level,
    wrapCardCallback(get(
      'ContentPublishing/RootContentItems',
      [ renderRootContentItemList ],
      (data) => ({
        clientId: data && data.clientId,
      }),
    ), () => formObject),
  );
  $card.disabled = !client.value.canManage;
  $('#client-tree ul.admin-panel-content').append($card.build());

  // Render child nodes
  client.children.forEach((childNode) => {
    renderClientNode(childNode, level + 1);
  });
}
function renderClientTree(response: ClientTree, clientId?: string) {
  const $clientTreeList = $('#client-tree ul.admin-panel-content');
  $clientTreeList.empty();
  response.root.children.forEach((rootClient) => {
    renderClientNode(rootClient);
    $clientTreeList.append('<li class="hr width-100pct"></li>');
  });
  $clientTreeList.find('.hr').last().remove();
  $clientTreeList.find('.tooltip').tooltipster();

  if (clientId !== null) {
    $(`[data-client-id=${clientId}]`).click();
  }
}

function populateAvailableContentTypes(contentTypes: ContentType[]) {
  const $panel = $('#content-publishing-form');
  const $rootContentItemForm = $panel.find('form.admin-panel-content');

  const $contentTypeDropdown = $rootContentItemForm.find('#ContentTypeId');
  $contentTypeDropdown.children(':not(option[value = "0"])').remove();

  contentTypes.forEach((contentType) => {
    const option = new Option(contentType.name, contentType.id.toString());
    $(option).data(contentType);
    $contentTypeDropdown.append(option);
  });

  $contentTypeDropdown.val(0);
}

export function setup() {
  const $contentTypeDropdown = $('#ContentTypeId');
  $contentTypeDropdown.change(() => {
    const $doesReduceToggle = $('#DoesReduce');
    const contentType = $contentTypeDropdown
      .find(`option[value="${$contentTypeDropdown.val()}"]`)
      .data() as ContentType;
    if (contentType && !contentType.canReduce) {
      $doesReduceToggle.attr('disabled', '');
      $doesReduceToggle.prop('checked', false);
    } else {
      $doesReduceToggle.removeAttr('disabled');
    }
    formObject.inputSections.forEach((section) =>
      section.inputs.forEach((input) => {
        if (contentType && isFileUploadInput(input)) {
          input.fileTypes.set(UploadComponent.Content, contentType.fileExtensions);
          input.configure();
        }
      }));
  });

  $('.action-icon-expand').click(expandAllListener);
  $('.action-icon-collapse').click(collapseAllListener);
  $('.admin-panel-searchbar-tree').keyup(filterTreeListener);
  $('.admin-panel-searchbar-form').keyup(filterFormListener);

  $('#root-content-items .admin-panel-toolbar .action-icon-add').click(() => {
    openNewRootContentItemForm();
    $('#root-content-items .card-body-container').removeAttr('selected');
    $('#root-content-items .card-body-container.action-card').attr('selected', '');
    $('#content-publishing-form').show();
  });
  $('#root-content-items ul.admin-panel-content-action')
    .append(new AddRootContentItemActionCard(
      wrapCardCallback(openNewRootContentItemForm, () => formObject),
    ).build());

  $('#content-publishing-form .admin-panel-toolbar .action-icon-cancel').click(() => {
    if (formObject.accessMode === AccessMode.Read || formObject.submissionMode === 'new') {
      $('#root-content-items [selected]').click();
    } else {
      setFormReadOnly();
    }
    statusMonitor.checkStatus();
  });

  $('#report-confirmation .admin-panel-toolbar .action-icon-cancel').click(() => {
    $('#root-content-items [selected]').click();
    statusMonitor.checkStatus();
  });
  $('#report-confirmation input[type="checkbox"]').change(() =>
    $('#confirmation-section-attestation .button-approve')
      .addClass('disabled')
      .tooltipster('content', goLiveDisabledTooltip)
      .filter(() =>
        $('#report-confirmation input[type="checkbox"]').not('[disabled]').toArray()
          .map((checkbox: HTMLInputElement) => checkbox.checked)
          .reduce((cum, cur) => cum && cur, true))
      .removeClass('disabled')
      .tooltipster('content', goLiveEnabledTooltip));
  $('#confirmation-section-attestation .button-reject').click((event) => {
    const $target = $(event.target);
    if ($target.hasClass('disabled')) {
      return;
    }
    $target.addClass('disabled');
    const rootContentItemId = $('#root-content-items [selected]').closest('.card-container').data().rootContentItemId;
    showButtonSpinner($target, 'Rejecting');
    $.post({
      data: {
        publicationRequestId: preLiveObject && preLiveObject.publicationRequestId,
        rootContentItemId,
      },
      headers: {
        RequestVerificationToken: $('input[name="__RequestVerificationToken"]').val().toString(),
      },
      url: 'ContentPublishing/Reject/',
    }).done(() => {
      toastr.success('Publication rejected.');
    }).fail((response) => {
      toastr.warning(response.getResponseHeader('Warning')
        || 'An unknown error has occurred.');
    }).always(() => {
      hideButtonSpinner($target);
      $('#report-confirmation').hide();
      $('#root-content-items [selected]').removeAttr('selected');
      $target.removeClass('disabled');
      statusMonitor.checkStatus();
    });
  });
  $('#confirmation-section-attestation .button-approve').click((event) => {
    const $target = $(event.target);
    if ($target.hasClass('disabled')) {
      return;
    }
    $target.addClass('disabled');
    const rootContentItemId = $('#root-content-items [selected]').closest('.card-container').data().rootContentItemId;
    showButtonSpinner($target, 'Approving');
    $.post({
      data: {
        publicationRequestId: preLiveObject && preLiveObject.publicationRequestId,
        rootContentItemId,
        validationSummaryId: preLiveObject && preLiveObject.validationSummaryId,
      },
      headers: {
        RequestVerificationToken: $('input[name="__RequestVerificationToken"]').val().toString(),
      },
      url: 'ContentPublishing/GoLive/',
    }).done(() => {
      toastr.success('Publication queued to go live.');
    }).fail((response) => {
      toastr.warning(response.getResponseHeader('Warning')
        || 'An unknown error has occurred.');
    }).always(() => {
      hideButtonSpinner($target);
      $('#report-confirmation').hide();
      $('#root-content-items [selected]').removeAttr('selected');
      $target.removeClass('disabled');
      statusMonitor.checkStatus();
    });
  });

  $('.admin-panel-toolbar .action-icon-edit').click(() => {
    setFormEdit();
  });
  $('.admin-panel-toolbar .action-icon-file-upload').click(() => {
    setFormEditOrRepublish();
  });

  setUnloadAlert(() => formObject && formObject.modified);

  $('.tooltip').tooltipster();

  get(
    'ContentPublishing/Clients',
    [ renderClientTree ],
  )();
  get(
    'ContentPublishing/AvailableContentTypes',
    [ populateAvailableContentTypes ],
  )();

  statusMonitor = new PublicationStatusMonitor();
  statusMonitor.start();
}
