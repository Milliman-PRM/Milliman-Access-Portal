import 'bootstrap/scss/bootstrap-reboot.scss';
import 'toastr/toastr.scss';
import 'tooltipster';
import 'tooltipster/src/css/plugins/tooltipster/sideTip/tooltipster-sideTip.css';
import 'tooltipster/src/css/tooltipster.css';
import 'vex-js';
import '../../scss/map.scss';
import '../lib-options';
import '../navbar';

import * as $ from 'jquery';
import * as toastr from 'toastr';

import {
  AddSelectionGroupActionCard, ClientCard, RootContentItemCard, SelectionGroupCard,
} from '../card';
import { AddSelectionGroupDialog, DeleteSelectionGroupDialog } from '../dialog';
import {
  collapseAllListener, del, expandAllListener, filterFormListener, filterTreeListener, get,
  hideButtonSpinner, post, showButtonSpinner, updateCardStatus, wrapCardCallback,
} from '../shared';
import {
  SelectionGroupList, SelectionGroupSummary, SelectionsDetail,
} from '../view-models/content-access-admin';
import {
  BasicNode, ClientSummary, ClientTree, ReductionField, ReductionFieldValueSelection,
  RootContentItemDetail, RootContentItemList, RootContentItemSummary, UserInfo,
} from '../view-models/content-publishing';

function updateSelectionGroupCount() {
  $('#root-content-items [selected] [href="#action-icon-users"]')
    .parent().next().html($('#selection-groups ul.admin-panel-content li').length.toString());
}

function selectionGroupAddClickHandler() {
  new AddSelectionGroupDialog(post(
    'ContentAccessAdmin/CreateSelectionGroup',
    'Selection group successfully created.',
    [
      renderSelectionGroup,
      updateSelectionGroupCount,
    ],
  )).open();
}

function selectionGroupDeleteClickHandler() {
  new DeleteSelectionGroupDialog($(this).closest('.card-container'), del<SelectionGroupList>(
    'ContentAccessAdmin/DeleteSelectionGroup',
    'Selection group successfully deleted.',
    [
      (response) => {
        $('#selection-groups ul.admin-panel-content').empty();
        renderSelectionGroupList(response);
      },
      updateSelectionGroupCount,
    ],
  )).open();
}

function cancelSelectionForm() {
  const $selectionInfo = $('#selection-info form.admin-panel-content');
  const $selectionGroups = $('#selection-groups ul.admin-panel-content');
  const $button = $selectionInfo.find('button');
  const data = {
    SelectionGroupId: $selectionGroups.find('[selected]').closest('.card-container').attr('data-selection-group-id'),
  };

  showButtonSpinner($button, 'Canceling');
  $.ajax({
    data,
    headers: {
      RequestVerificationToken: $('input[name="__RequestVerificationToken"]').val().toString(),
    },
    type: 'POST',
    url: 'ContentAccessAdmin/CancelReduction',
  }).done(function onDone(response) {
    hideButtonSpinner($button);
    renderSelections(response);
    toastr.success('Reduction tasks canceled.');
  }).fail(function onFail(response) {
    hideButtonSpinner($button);
    toastr.warning(response.getResponseHeader('Warning')
      || 'An unknown error has occurred.');
  });
}
function submitSelectionForm() {
  const $selectionInfo = $('#selection-info form.admin-panel-content');
  const $selectionGroups = $('#selection-groups ul.admin-panel-content');
  const $button = $selectionInfo.find('button');
  const data = {
    SelectionGroupId: $selectionGroups.find('[selected]').closest('.card-container').attr('data-selection-group-id'),
    Selections: $selectionInfo.serializeArray().reduce((acc, cur) => {
      return (cur.value === 'on')
        ? acc.concat(cur.name)
        : undefined;
    }, []),
  };

  showButtonSpinner($button);
  $.ajax({
    data,
    headers: {
      RequestVerificationToken: $('input[name="__RequestVerificationToken"]').val().toString(),
    },
    type: 'POST',
    url: 'ContentAccessAdmin/SingleReduction',
  }).done(function onDone(response) {
    hideButtonSpinner($button);
    renderSelections(response);
    toastr.success('A reduction task has been queued.');
  }).fail(function onFail(response) {
    hideButtonSpinner($button);
    toastr.warning(response.getResponseHeader('Warning')
      || 'An unknown error has occurred.');
  });
}

function renderValue(
  value: ReductionFieldValueSelection,
  $fieldset: JQuery<HTMLElement>,
  originalSelections: number[],
) {
  const $checkbox = $(`<label class="selection-option-label">
    ${value.Value}
      <input type="checkbox" id="selection-value-${value.Id}" name="${value.Id}" class="selection-option-value">
        <span class="selection-option-checkmark"></span>
    </label>`);
  $fieldset.append(
    `<div class="selection-option-container" data-selection-value="${value.Value.toUpperCase()}"></div>`);
  const $div = $fieldset.find('div.selection-option-container').last();
  $div.append($checkbox);
  $checkbox.find('input[type="checkbox"]').prop('checked', value.SelectionStatus);
  if (originalSelections.includes(value.Id) !== value.SelectionStatus) {
    $div.attr('style', 'background: yellow;');
  }
}

function renderField(
  field: ReductionField<ReductionFieldValueSelection>,
  $parent: JQuery<HTMLElement>,
  originalSelections: number[],
) {
  $parent.append('<fieldset></fieldset>');
  const $fieldset = $parent.find('fieldset').last();
  $fieldset.append(`<legend>${field.DisplayName}</legend>`);
  field.Values.forEach((value) => {
    renderValue(value, $fieldset, originalSelections);
  });
}

function renderSelections(response: SelectionsDetail) {
  const $selectionInfo = $('#selection-info form.admin-panel-content');
  const $fieldsetDiv = $selectionInfo.find('.fieldset-container');
  const $relatedCard = $('#selection-groups [selected]').closest('.card-container');
  // tslint:disable:object-literal-sort-keys
  const details = $.extend({
    User: {
      FirstName: '',
    },
    StatusEnum: 0,
    StatusName: '',
    SelectionGroupId: 0,
    RootContentItemId: 0,
  }, response.ReductionDetails);
  // tslint:enable:object-literal-sort-keys

  $fieldsetDiv.empty();
  response.Hierarchy.Fields.forEach((field) =>
    renderField(field, $fieldsetDiv, response.OriginalSelections));
  updateCardStatus($relatedCard, response.ReductionDetails);
  $selectionInfo
    .find('button').hide()
    .filter(`.button-status-${details.StatusEnum}`).show();
  // TODO: rely on some flag in the response to disable checkboxes
  $fieldsetDiv
    .find('input[type="checkbox"]')
    .click([10, 20, 30].includes(details.StatusEnum)
      ? (event) => {
        event.preventDefault();
      }
      : () => undefined);
}

function renderSelectionGroup(selectionGroup: SelectionGroupSummary) {
  $('#root-content-items [selected]').parent().data('eligibleMembers', selectionGroup.MemberList);
  const $card = new SelectionGroupCard(
    selectionGroup,
    wrapCardCallback(get(
      'ContentAccessAdmin/Selections',
      [
        renderSelections,
      ],
    )),
    selectionGroupDeleteClickHandler,
    () => undefined,
  ).build();
  updateCardStatus($card, selectionGroup.ReductionDetails);
  $('#selection-groups ul.admin-panel-content').append($card);
}
function renderSelectionGroupList(response: SelectionGroupList, selectionGroupId?) {
  const $selectionGroupList = $('#selection-groups ul.admin-panel-content');
  $selectionGroupList.empty();
  response.SelectionGroups.forEach((selectionGroup) =>
    renderSelectionGroup(selectionGroup));
  $selectionGroupList.find('.tooltip').tooltipster();

  $('#selection-groups .admin-panel-action-icons-container .action-icon-add')
    .click(selectionGroupAddClickHandler);

  if (selectionGroupId) {
    $(`[data-selection-group-id="${selectionGroupId}"]`).click();
  }
}

function renderRootContentItem(item: RootContentItemSummary) {
  const $rootContentItemCard = new RootContentItemCard(
    item,
    wrapCardCallback(get(
      'ContentAccessAdmin/SelectionGroups',
      [
        renderSelectionGroupList,
      ],
    )),
  ).build();
  updateCardStatus($rootContentItemCard, item.PublicationDetails);
  $('#root-content-items ul.admin-panel-content').append($rootContentItemCard);
}
function renderRootContentItemList(response: RootContentItemList, rootContentItemId?: number) {
  const $rootContentItemList = $('#root-content-items ul.admin-panel-content');
  $rootContentItemList.empty();
  response.SummaryList.forEach(renderRootContentItem);
  $rootContentItemList.find('.tooltip').tooltipster();

  if (!isNaN(rootContentItemId)) {
    $(`[data-root-content-item-id=${rootContentItemId}]`).click();
  }
}

function renderClientNode(client: BasicNode<ClientSummary>, level: number = 0) {
  const $card = new ClientCard(
    client.Value,
    client.Value.EligibleUserCount,
    client.Value.RootContentItemCount,
    level,
    wrapCardCallback(get(
      'ContentAccessAdmin/RootContentItems',
      [ renderRootContentItemList ],
    )),
  );
  $card.disabled = !client.Value.CanManage;
  $('#client-tree ul.admin-panel-content').append($card.build());

  // Render child nodes
  client.Children.forEach((childNode) => {
    renderClientNode(childNode, level + 1);
  });
}
function renderClientTree(response: ClientTree, clientId?: number) {
  const $clientTreeList = $('#client-tree ul.admin-panel-content');
  $clientTreeList.empty();
  response.Root.Children.forEach((rootClient) => {
    renderClientNode(rootClient);
    $clientTreeList.append('<li class="hr width-100pct"></li>');
  });
  $clientTreeList.find('.hr').last().remove();
  $clientTreeList.find('.tooltip').tooltipster();

  if (!isNaN(clientId)) {
    $(`[data-client-id=${clientId}]`).click();
  }
}

export function setup() {
  (get(
    'ContentAccessAdmin/ClientFamilyList',
    [
      renderClientTree,
    ],
  )());

  $('.action-icon-expand').click(expandAllListener);
  $('.action-icon-collapse').click(collapseAllListener);
  $('.admin-panel-searchbar-tree').keyup(filterTreeListener);
  $('.admin-panel-searchbar-form').keyup(filterFormListener);

  $('#selection-groups ul.admin-panel-content-action')
    .append(new AddSelectionGroupActionCard(selectionGroupAddClickHandler).build());
  // TODO: select by ID or better classes
  $('#selection-info .blue-button').click(submitSelectionForm);
  $('#selection-info .red-button').click(cancelSelectionForm);

  $('.tooltip').tooltipster();
}
