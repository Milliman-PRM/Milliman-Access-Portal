import $ = require('jquery');
import toastr = require('toastr');
import card = require('./card');
import dialog = require('./dialog');
import shared = require('./shared');
require('tooltipster');
require('vex-js');
require('./navbar');
require('./lib-options');

require('bootstrap/scss/bootstrap-reboot.scss');
require('toastr/toastr.scss');
require('tooltipster/src/css/tooltipster.css');
require('tooltipster/src/css/plugins/tooltipster/sideTip/tooltipster-sideTip.css');
require('../scss/map.scss');

function updateSelectionGroupCount() {
  $('#root-content-items [selected] [href="#action-icon-users"]')
    .parent().next().html($('#selection-groups ul.admin-panel-content li').length.toString());
}

function selectionGroupAddClickHandler() {
  new dialog.AddSelectionGroupDialog(shared.post(
    'ContentAccessAdmin/CreateSelectionGroup',
    'Selection group successfully created.',
    [
      renderSelectionGroup,
      updateSelectionGroupCount,
    ],
  )).open();
}

function selectionGroupDeleteClickHandler() {
  new dialog.DeleteSelectionGroupDialog($(this).closest('.card-container'), shared.del(
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

  shared.showButtonSpinner($button, 'Canceling');
  $.ajax({
    data,
    headers: {
      RequestVerificationToken: $('input[name="__RequestVerificationToken"]').val().toString(),
    },
    type: 'POST',
    url: 'ContentAccessAdmin/CancelReduction',
  }).done(function onDone(response) {
    shared.hideButtonSpinner($button);
    renderSelections(response);
    toastr.success('Reduction tasks canceled.');
  }).fail(function onFail(response) {
    shared.hideButtonSpinner($button);
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

  shared.showButtonSpinner($button);
  $.ajax({
    data,
    headers: {
      RequestVerificationToken: $('input[name="__RequestVerificationToken"]').val().toString(),
    },
    type: 'POST',
    url: 'ContentAccessAdmin/SingleReduction',
  }).done(function onDone(response) {
    shared.hideButtonSpinner($button);
    renderSelections(response);
    toastr.success('A reduction task has been queued.');
  }).fail(function onFail(response) {
    shared.hideButtonSpinner($button);
    toastr.warning(response.getResponseHeader('Warning')
      || 'An unknown error has occurred.');
  });
}

function renderValue(value, $fieldset, originalSelections) {
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

function renderField(field, $parent, originalSelections) {
  $parent.append('<fieldset></fieldset>');
  const $fieldset = $parent.find('fieldset').last();
  $fieldset.append('<legend>' + field.DisplayName + '</legend>');
  field.Values.forEach((value) => {
    renderValue(value, $fieldset, originalSelections);
  });
}

function renderSelections(response) {
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
  response.Hierarchy.Fields.forEach((field) => {
    renderField(field, $fieldsetDiv, response.OriginalSelections);
  });
  shared.updateCardStatus($relatedCard, response.ReductionDetails);
  $selectionInfo
    .find('button').hide()
    .filter('.button-status-' + details.StatusEnum).show();
  // TODO: rely on some flag in the response to disable checkboxes
  $fieldsetDiv
    .find('input[type="checkbox"]')
    .click([10, 20, 30].includes(details.StatusEnum)
      ? (event) => {
        event.preventDefault();
      }
      : $.noop);
}

function renderSelectionGroup(selectionGroup) {
  const $card = new card.SelectionGroupCard(
    selectionGroup.SelectionGroupEntity,
    selectionGroup.MemberList,
    shared.wrapCardCallback(shared.get(
      'ContentAccessAdmin/Selections',
      [
        renderSelections,
      ],
    )),
    selectionGroupDeleteClickHandler,
    () => undefined,
  ).build();
  shared.updateCardStatus($card, selectionGroup.ReductionDetails);
  $('#selection-groups ul.admin-panel-content').append($card);
}
function renderSelectionGroupList(response, selectionGroupId?) {
  const $selectionGroupList = $('#selection-groups ul.admin-panel-content');
  $selectionGroupList.empty();
  response.SelectionGroupList.forEach(renderSelectionGroup);
  $selectionGroupList.find('.tooltip').tooltipster();

  $('#selection-groups .admin-panel-action-icons-container .action-icon-add')
    .click(selectionGroupAddClickHandler);

  if (selectionGroupId) {
    $('[data-selection-group-id="' + selectionGroupId + '"]').click();
  }
}

function renderRootContentItem(rootContentItem) {
  const $card = new card.RootContentItemCard(
    rootContentItem.RootContentItemEntity,
    rootContentItem.GroupCount,
    rootContentItem.EligibleUserCount,
    shared.wrapCardCallback(shared.get(
      'ContentAccessAdmin/SelectionGroups',
      [
        renderSelectionGroupList,
      ],
    )),
  ).build();
  shared.updateCardStatus($card, rootContentItem.PublicationDetails);
  $('#root-content-items ul.admin-panel-content').append($card);
}
function renderRootContentItemList(response, rootContentItemId?) {
  const $rootContentItemList = $('#root-content-items ul.admin-panel-content');
  $rootContentItemList.empty();
  response.RootContentItemList.forEach(renderRootContentItem);
  $rootContentItemList.find('.tooltip').tooltipster();

  if (rootContentItemId) {
    $('[data-root-content-item-id="' + rootContentItemId + '"]').click();
  }
}

function renderClientNode(client, level) {
  const $card = new card.ClientCard(
    client.ClientDetailModel.ClientEntity,
    client.ClientDetailModel.EligibleUserCount,
    client.ClientDetailModel.RootContentItemCount,
    level,
    shared.wrapCardCallback(shared.get(
      'ContentAccessAdmin/RootContentItems',
      [
        renderRootContentItemList,
      ],
    )),
  );
  $card.disabled = !client.ClientDetailModel.CanManage;
  $('#client-tree ul.admin-panel-content').append($card.build());

  // Render child nodes
  if (client.ChildClientModels.length) {
    client.ChildClientModels.forEach(function forEach(childNode) {
      renderClientNode(childNode, level + 1);
    });
  }
}
function renderClientTree(response, clientId?) {
  const $clientTreeList = $('#client-tree ul.admin-panel-content');
  $clientTreeList.empty();
  response.ClientTreeList.forEach(function render(rootClient) {
    renderClientNode(rootClient, 0);
    $clientTreeList.append('<li class="hr width-100pct"></li>');
  });
  $clientTreeList.find('.hr').last().remove();
  $clientTreeList.find('.tooltip').tooltipster();

  if (clientId) {
    $('[data-client-id="' + clientId + '"]').click();
  }
}

$(document).ready(() => {
  (shared.get(
    'ContentAccessAdmin/ClientFamilyList',
    [
      renderClientTree,
    ],
  )());

  $('.action-icon-expand').click(shared.expandAllListener);
  $('.action-icon-collapse').click(shared.collapseAllListener);
  $('.admin-panel-searchbar-tree').keyup(shared.filterTreeListener);
  $('.admin-panel-searchbar-form').keyup(shared.filterFormListener);

  $('#selection-groups ul.admin-panel-content-action')
    .append(new card.AddSelectionGroupActionCard(selectionGroupAddClickHandler).build());
  // TODO: select by ID or better classes
  $('#selection-info .blue-button').click(submitSelectionForm);
  $('#selection-info .red-button').click(cancelSelectionForm);

  $('.tooltip').tooltipster();
});
