import '../../images/icons/expand-frame.svg';

import 'jquery-validation';
import 'jquery-validation-unobtrusive';

import * as $ from 'jquery';
import { unionWith } from 'lodash';
import * as moment from 'moment';
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
  BasicNode, ClientSummary, ClientTree, ContentReductionHierarchy, ContentType, ContentTypeEnum, isSelection,
    PreLiveContentValidationSummary, PublishRequest, ReductionFieldValue, RootContentItemDetail,
    RootContentItemList, RootContentItemSummary, RootContentItemSummaryAndDetail,
} from '../view-models/content-publishing';
import { PublicationStatusMonitor } from './publication-status-monitor';

require('tooltipster');

let formObject: FormBase;
let statusMonitor: PublicationStatusMonitor;

let preLiveObject: PreLiveContentValidationSummary;

// whether unchanged values in the prelive panel should be displayed
let hideUnchangedValues: boolean = false;

const goLiveDisabledTooltip = 'Complete checks to proceed';
const goLiveEnabledTooltip = 'Approve content and go live';

function deleteRootContentItem(
  rootContentItemId: Guid,
  rootContentItemName: string,
  callback: () => void,
) {
  $.ajax({
    data: {
      rootContentItemId,
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
export function rootContentItemDeleteClickHandler(event: Event) {
  const $clickedCard = $(this).closest('.card-container');
  const rootContentItemId = $clickedCard.data().rootContentItemId;
  const rootContentItemName = $clickedCard.find('.card-body-primary-text').first().text();
  event.stopPropagation();
  new (DeleteRootContentItemDialog as any)(
    rootContentItemName,
    rootContentItemId,
    (data: { confirmDeletion: string }, callback: () => void) => {
      if (data.confirmDeletion.toUpperCase() === 'DELETE') {
        showButtonSpinner($('.vex-first'), 'Deleting');
        $('.vex-dialog-button').attr('disabled', '');
        deleteRootContentItem(rootContentItemId, rootContentItemName, callback);
      } else {
        toastr.warning('Please type <strong>DELETE</strong> to proceed with deletion');
        return false;
      }
      return true;
    },
  ).open();
}
function cancelContentPublication(data: { RootContentItemId: string }, callback: () => void) {
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

export function rootContentItemCancelClickHandler(event: Event) {
  const $clickedCard = $(this).closest('.card-container');
  const rootContentItemId = $clickedCard.data().rootContentItemId;
  const rootContentItemName = $clickedCard.find('.card-body-primary-text').first().text();
  event.stopPropagation();
  new (CancelContentPublicationRequestDialog as any)(
    rootContentItemId, rootContentItemName, cancelContentPublication).open();
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
    typeSpecificDetailObject: {
      filterPaneEnabled: false,
      navigationPaneEnabled: false,
    },
    id: '0',
    notes: '',
    contentDisclaimer: '',
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
  formMap.set('DoesReduce', item.doesReduce);
  if (item.typeSpecificDetailObject) {
    formMap.set('FilterPaneEnabled',
      (item.typeSpecificDetailObject.hasOwnProperty('filterPaneEnabled')
        ? item.typeSpecificDetailObject.filterPaneEnabled
        : false));
    formMap.set('NavigationPaneEnabled',
      (item.typeSpecificDetailObject.hasOwnProperty('navigationPaneEnabled')
        ? item.typeSpecificDetailObject.navigationPaneEnabled
        : false));
  }
  formMap.set('Description', item.description);
  formMap.set('Notes', item.notes);
  formMap.set('ContentDisclaimer', item.contentDisclaimer);

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
    $(`#confirmation-section-${pair.sectionName} .content-preview-container`)
      .find(pair.node || '.content-preview')
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
      .show()
      .siblings()
      .hide()
      .filter(() => pair.link === null)
      .filter('.content-preview-none')
      .show()
      .siblings()
      .hide()
      .closest('.confirmation-section').find('label')
      .hide()
      .find('input')
      .attr('disabled', '');
    // hide/show new tab links
    $(`#confirmation-section-${pair.sectionName} .new-tab-icon`)
      .show()
      .attr('href', pair.link)
      .filter(() => pair.link === null || response.contentTypeName === 'FileDownload')
      .hide();
  });

  // render hierarchy diff and selection group changes
  if (!response.doesReduce || !response.selectionGroups) {
    $('#confirmation-section-hierarchy-diff')
      .hide()
      .find('input[type="checkbox"].requires-confirmation')
      .attr('disabled', '');
    $('#confirmation-section-hierarchy-stats')
      .hide()
      .find('input[type="checkbox"].requires-confirmation')
      .attr('disabled', '');
  } else {
    $('#confirmation-section-hierarchy-diff')
      .show();
    $('#confirmation-section-hierarchy-stats')
      .show();

    // render master hierarchy diff
    $('#confirmation-section-hierarchy-diff .hierarchy > ul').children().remove();

    // common function for diffing two hierarchies
    const renderHierarchyDiff = <TValue extends ReductionFieldValue>(
      live: ContentReductionHierarchy<TValue>,
      pending: ContentReductionHierarchy<TValue>,
      selectedOnly: boolean,  // whether to only display values that are selected in live
    ) => {
      // assume fields match
      const $diff = $('<div></div>');
      if (live.fields.length === 0 && pending.fields.length === 0) {
        return $diff;
      }
      // if one of the hierarchies has no fields, set them from the other
      if (live.fields.length === 0) {
        live = {
          ...pending,
          fields: pending.fields.map((f) => ({
            ...f,
            values: [],
          })),
        };
      }
      if (pending.fields.length === 0) {
        pending = {
          ...live,
          fields: live.fields.map((f) => ({
            ...f,
            values: [],
          })),
        };
      }
      live.fields.forEach(({ values: liveValues, fieldName, displayName }) => {
        $diff.append(`<h3 class="hierarchy-diff-field">${displayName}</h3>`);
        const pendingValues = pending.fields.filter((f) => f.fieldName === fieldName)[0].values;
        const allValues = unionWith(liveValues, pendingValues,
          (v1: TValue, v2: TValue) => v1.value === v2.value);

        // exclude values that aren't in the live hierarchy if using selectedOnly
        const filteredValues = allValues.filter((value) => {
          const liveData = liveValues.filter((v) => v.value === value.value)[0];
          if (selectedOnly) {
            if (!liveData || (liveData && isSelection(liveData) && !liveData.selectionStatus)) {
              return false;
            }
          }
          return true;
        }).sort((a, b) => {
          const aUpper = a.value.toUpperCase();
          const bUpper = b.value.toUpperCase();
          if (aUpper < bUpper) {
            return -1;
          } else if (aUpper > bUpper) {
            return 1;
          } else {
            return 0;
          }
        });
        // display special message if no hierarchy changes
        if (!filteredValues.length) {
          if (!selectedOnly) {
            $diff.append('<div class="hierarchy-diff-values">No values have changed for this field.</div>');
          }
          return;
        }
        // build a table to hold the diff
        const $table = $(`<div class="hierarchy-diff-container">
          <table class="hierarchy-diff-values">
            <colgroup>
              <col width="50">
              <col>
            </colgroup>
            <tbody>
            </tbody>
          </table>
        </div>`);
        filteredValues.forEach((value) => {
          const liveData = liveValues.filter((v) => v.value === value.value)[0];
          const pendingData = pendingValues.filter((v) => v.value === value.value)[0];
          const $row = $('<tr></tr>');
          if (selectedOnly) {
            // accounts for removal of selection as well as removal of value from new hierarchy
            const $diffSymbol = $(`
              <td class="hierarchy-diff-symbol"><div>${!pendingData ? '-' : ''}</div></td>`);
            if (!pendingData) {
              $diffSymbol.addClass('minus');
            }
            $row.append($diffSymbol);
            $row.append($(`<td><div>${liveData.value}</div></td>`));
          } else {
            const $diffSymbol = $(`
              <td class="hierarchy-diff-symbol"><div>
                ${!pendingData ? 'Removed' : !liveData ? 'Added' : ''}
              </div></td>`);
            if (!pendingData) {
              $diffSymbol.addClass('minus');
            } else if (!liveData) {
              $diffSymbol.addClass('plus');
            } else {
              $row.addClass('no-change');
            }

            $row.append($diffSymbol);
            $row.append($(`<td><div>${liveData ? liveData.value : pendingData.value}</div></td>`));
          }
          $table.find('tbody').append($row);
        });
        $diff.append($table);
      });
      $diff.find('.no-change').show().filter(() => hideUnchangedValues).hide();
      return $diff;
    };

    // call the diff function for the master hierarchies
    $('#confirmation-section-hierarchy-diff .hierarchy-container')
      .children('div').remove();
    $('#confirmation-section-hierarchy-diff .hierarchy-container')
      .append(renderHierarchyDiff(response.liveHierarchy, response.newHierarchy, false));
    // handlers for toggling visible unchanged selections
    $('#hide-unchanged').prop('checked', hideUnchangedValues);
    $('#hide-unchanged').change(() => {
      hideUnchangedValues = $('#hide-unchanged').prop('checked');
      $('#confirmation-section-hierarchy-diff .hierarchy-container')
        .find('.no-change').show().filter(() => hideUnchangedValues).hide();
    });

    // populate hierarchy stats
    $('#confirmation-section-hierarchy-stats > div > ul').children().remove();
    const $statsList = $('#confirmation-section-hierarchy-stats > div > ul');
    response.selectionGroups.forEach((selectionGroup) => {
      const duration = moment.duration(selectionGroup.duration);
      const timeDisplay = ((hours, minutes, seconds) => (`
        ${hours ? hours + ' hour' + (hours === 1 ? ' ' : 's ') : ''}
        ${(hours || minutes) ? minutes + ' minute' + (minutes === 1 ? ' ' : 's ') : ''}
        ${seconds + ' second' + (seconds === 1 ? ' ' : 's ')}
      `))(duration.hours(), duration.minutes(), duration.seconds());
      const liveSelectionCount = selectionGroup.isMaster
        ? 0
        : selectionGroup.liveSelections.fields
          .map((f) => f.values.reduce((prev, cur) => prev + (cur.selectionStatus ? 1 : 0), 0))
          .reduce((prev, cur) => prev + cur, 0);
      const pendingSelectionCount = selectionGroup.isMaster
        ? 0
        : selectionGroup.pendingSelections && selectionGroup.pendingSelections.fields
          .map((f) => f.values.reduce((prev, cur) => prev + (cur.selectionStatus ? 1 : 0), 0))
          .reduce((prev, cur) => prev + cur, 0);
      const $selectionGroupStats = $(`<li data-id="${selectionGroup.id}"><div class="selection-group-summary">
          <h5>
            ${selectionGroup.isMaster ? '<strong>[Master]</strong>&nbsp' : ''}
            ${selectionGroup.name}&nbsp
          </h5>
          <div class="selection-group-stat">
            <span class="selection-group-stat-label">Duration:</span>
            <span class="selection-group-stat-value">${timeDisplay}</span>
          </div>
          <div class="selection-group-stat pre-live-users-stat">
            <span class="selection-group-stat-label">Users:</span>
            <span class="selection-group-stat-value">${selectionGroup.users.length}</span>
          </div>
          <div class="pre-live-user-list" style="display: none;">
            <ul></ul>
          </div>
          <div class="selection-group-stat pre-live-selection-summary">
            <span class="selection-group-stat-label">Selections:</span>
            <span class="selection-group-stat-value">
              ${selectionGroup.isMaster
                ? 'N/A'
                : liveSelectionCount === pendingSelectionCount
                  ? `${pendingSelectionCount}`
                  : `${liveSelectionCount} → ${pendingSelectionCount}`}
            </span>
          </div>
          <div class="pre-live-selections-table hierarchy-container" style="display: none;">
          </div>
        </div></li>`);
      function activeIndicator(isInactive: boolean) {
        return isInactive
          ? '<span class="pre-live-group-inactive">Inactive</span>'
          : '<span class="pre-live-group-active">Active</span>';
      }
      const $activeStatus = selectionGroup.wasInactive === selectionGroup.isInactive
        ? $(`<span>[ ${activeIndicator(selectionGroup.isInactive)} ]</span>`)
        : $(`<span>[ ${activeIndicator(selectionGroup.wasInactive)} → `
            + `${activeIndicator(selectionGroup.isInactive)} ]</span>`);
      $selectionGroupStats.find('h5').append($activeStatus);
      if (selectionGroup.inactiveReason !== null) {
        $selectionGroupStats.find('.selection-group-summary').append(`
          <div class="selection-group-stat">
            <span class="selection-group-stat-label">Inactive reason:</span>
            <span class="selection-group-stat-value">${selectionGroup.inactiveReason}</span>
          </div>
        `);
      }
      // fill in the list of users for this selection group
      const $userExpansion = $('<span class="pre-live-list-toggle">(<a href="">expand</a>)</span>');
      $userExpansion.find('a').click((event) => {
        event.preventDefault();
        const $statsListNew  = $('#confirmation-section-hierarchy-stats > div > ul');
        const $stat = $statsListNew
          .children('li')
          .filter((_, element) => $(element).data().id === selectionGroup.id);
        const $userExpansionNew = $stat.find('.pre-live-list-toggle > a');
        const $userList = $stat.find('.pre-live-user-list');
        if ($userExpansionNew[0].innerHTML === 'expand') {
          $userExpansionNew[0].innerHTML = 'collapse';
          $userList.show(100);
        } else {
          $userExpansionNew[0].innerHTML = 'expand';
          $userList.hide(100);
        }
      });
      // fill in the selection diff for this selection group
      const $selectionsExpansion = $('<span class="pre-live-selections-toggle">(<a href="">expand</a>)</span>');
      $selectionsExpansion.find('a').click((event) => {
        event.preventDefault();
        const $statsListNew  = $('#confirmation-section-hierarchy-stats > div > ul');
        const $stat = $statsListNew
          .children('li')
          .filter((_, element) => $(element).data().id === selectionGroup.id);
        const $selectionsExpansionNew = $stat.find('.pre-live-selections-toggle > a');
        const $selectionsList = $stat.find('.pre-live-selections-table');
        if ($selectionsExpansionNew[0].innerHTML === 'expand') {
          $selectionsExpansionNew[0].innerHTML = 'collapse';
          $selectionsList.show(100);
        } else {
          $selectionsExpansionNew[0].innerHTML = 'expand';
          $selectionsList.hide(100);
        }
      });
      if (selectionGroup.users.length > 0) {
        $selectionGroupStats.find('.pre-live-users-stat').append($userExpansion);
        selectionGroup.users.forEach((user) => {
          $selectionGroupStats.find('.pre-live-user-list > ul')
            .append(`<li>${user.firstName} ${user.lastName} (${user.userName})</li>`);
        });
      }
      if (!selectionGroup.isMaster && (!selectionGroup.isInactive || !selectionGroup.wasInactive)) {
        $selectionGroupStats.find('.pre-live-selection-summary').append($selectionsExpansion);
        $selectionGroupStats.find('.pre-live-selections-table')
          .append(renderHierarchyDiff(selectionGroup.liveSelections, selectionGroup.pendingSelections, true));
      }
      $statsList.append($selectionGroupStats);
    });
    // add a message explaining what inactive means
    if (response.selectionGroups.filter((sg) => sg.isInactive)[0]) {
      $statsList.prepend(`<div>
        Some selection groups will be marked <strong>inactive</strong> if this publication goes live
        due to reduction failures.
        Members of inactive selection groups will be unable to access this content
        until the reduction failure is resolved.
      </div>`);
    }
  }
  // populate attestation
  $('#confirmation-section-attestation .attestation-language').html(response.attestationLanguage);

  const anyEnabled = $('#report-confirmation input[type="checkbox"].requires-confirmation')
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
      if (key !== 'DoesReduce'
        && key !== 'FilterPaneEnabled'
        && key !== 'NavigationPaneEnabled') {  // because these are checkboxes
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

    if (item.typeSpecificDetailObject) {
      const $filterPaneToggle = $rootContentItemForm.find('#FilterPaneEnabled');
      $filterPaneToggle.prop('checked',
        (item.typeSpecificDetailObject.hasOwnProperty('filterPaneEnabled')
          ? item.typeSpecificDetailObject.filterPaneEnabled
          : false));
      const $navigationPaneToggle = $rootContentItemForm.find('#NavigationPaneEnabled');
      $navigationPaneToggle.prop('checked',
        (item.typeSpecificDetailObject.hasOwnProperty('navigationPaneEnabled')
          ? item.typeSpecificDetailObject.navigationPaneEnabled
          : false));
    }
  }

  $('textarea').change();

  const createContentGroup = new SubmissionGroup<RootContentItemSummaryAndDetail>(
    [
      'common',
      'root-content-item-info',
      'root-content-item-content-type',
      'root-content-item-display-settings',
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
      'root-content-item-content-type',
      'root-content-item-display-settings',
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
  if (contentType && !contentType.canReduce) {
    $('#DoesReduce').closest('.form-input-toggle').hide();
  } else {
    $('#DoesReduce').closest('.form-input-toggle').show();
  }
  const $contentDisplaySettings = $('.form-section[data-section="root-content-item-display-settings"]');
  if (contentType.typeEnum === ContentTypeEnum.PowerBi) {
    $contentDisplaySettings.show();
  } else {
    $contentDisplaySettings.hide();
  }
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
  const $rootContentItemCard = new (RootContentItemCard as any)(
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
      $('.confirmation-section iframe,object')
        .attr('src', 'about:blank')
        .attr('data', 'about:blank');
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
  const $card = new (ClientCard as any)(
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
      $doesReduceToggle.closest('.form-input-toggle').hide();
      $doesReduceToggle.prop('checked', false);
    } else {
      $doesReduceToggle.removeAttr('disabled');
      $doesReduceToggle.closest('.form-input-toggle').show();
    }
    const $contentDisplaySettings = $('.form-section[data-section="root-content-item-display-settings"]');
    if (contentType && contentType.typeEnum === ContentTypeEnum.PowerBi) {
      $contentDisplaySettings.show();
      $('#FilterPaneEnabled').removeAttr('disabled');
      $('#NavigationPaneEnabled').removeAttr('disabled');
    } else {
      $contentDisplaySettings.hide();
      $('#FilterPaneEnabled')
        .prop('checked', false)
        .attr('disabled', '');
      $('#NavigationPaneEnabled')
        .prop('checked', false)
        .attr('disabled', '');
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

  $('textarea').on('change keydown paste cut', function() {
    $(this).height(0).height(Math.min(this.scrollHeight + 2, 300));
    if ($(this).height() >= 300) {
      $(this).css('overflow', 'auto');
    } else {
      $(this).css('overflow', 'hidden');
    }
  });

  $('#root-content-items .admin-panel-toolbar .action-icon-add').click(() => {
    openNewRootContentItemForm();
    $('#root-content-items .card-body-container').removeAttr('selected');
    $('#root-content-items .card-body-container.action-card').attr('selected', '');
    $('#content-publishing-form').show();
  });
  $('#root-content-items ul.admin-panel-content-action')
    .append(new (AddRootContentItemActionCard as any)(
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
  $('#report-confirmation input[type="checkbox"].requires-confirmation').change(() =>
    $('#confirmation-section-attestation .button-approve')
      .addClass('disabled')
      .tooltipster('content', goLiveDisabledTooltip)
      .filter(() =>
        $('#report-confirmation input[type="checkbox"].requires-confirmation').not('[disabled]').toArray()
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
