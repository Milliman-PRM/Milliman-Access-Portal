import 'bootstrap/scss/bootstrap-reboot.scss';
import 'toastr/toastr.scss';
import 'tooltipster';
import 'tooltipster/src/css/plugins/tooltipster/sideTip/tooltipster-sideTip.css';
import 'tooltipster/src/css/tooltipster.css';
import 'vex-js';
import '../lib-options';
import '../navbar';

import '../../scss/map.scss';

import * as $ from 'jquery';
import * as toastr from 'toastr';

import {
  AddSelectionGroupActionCard, ClientCard, RootContentItemCard, SelectionGroupCard,
} from '../card';
import { AddSelectionGroupDialog, DeleteSelectionGroupDialog } from '../dialog';
import {
  collapseAllListener, del, expandAllListener, filterFormListener, filterTreeListener, get,
  hideButtonSpinner, post, setExpanded, showButtonSpinner, toggleExpanded, updateCardStatus,
  wrapCardCallback,
} from '../shared';
import {
  SelectionDetails, SelectionGroupList, SelectionGroupSummary, SelectionsDetail,
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

function selectionGroupDeleteClickHandler(event: Event) {
  event.stopPropagation();
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
    IsMaster: $selectionInfo.find('#IsMaster').prop('checked'),
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
    url: 'ContentAccessAdmin/UpdateSelections',
  }).done(function onDone(response) {
    hideButtonSpinner($button);
    renderSelections(response);
    toastr.success(data.IsMaster
      ? 'Master content access granted.'
      : 'A reduction task has been queued.');
  }).fail(function onFail(response) {
    hideButtonSpinner($button);
    toastr.warning(response.getResponseHeader('Warning')
      || 'An unknown error has occurred.');
  });
}

function renderValue(
  value: ReductionFieldValueSelection,
  $fieldset: JQuery<HTMLElement>,
  liveSelections: SelectionDetails[],
  pendingSelections: SelectionDetails[],
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

  const live = liveSelections.filter((s) => s.Id === value.Id);
  const pending = pendingSelections && pendingSelections.filter((s) => s.Id === value.Id);
  $checkbox.find('input[type="checkbox"]')
    .prop('checked', pending ? pending.length > 0 : live.length > 0);
  if (pending && live.length && live[0].Marked) {
    $div.attr('style', 'color: red;');
  }
  if (pending && pending.length && pending[0].Marked) {
    $div.attr('style', 'color: green;');
  }
}

function renderField(
  field: ReductionField<ReductionFieldValueSelection>,
  $parent: JQuery<HTMLElement>,
  liveSelections: SelectionDetails[],
  pendingSelections: SelectionDetails[],
) {
  $parent.append('<fieldset></fieldset>');
  const $fieldset = $parent.find('fieldset').last();
  $fieldset.append(`<legend>${field.DisplayName}</legend>`);
  field.Values.forEach((value) => {
    renderValue(value, $fieldset, liveSelections, pendingSelections);
  });
}

function renderSelections(response: SelectionsDetail) {
  const $selectionInfo = $('#selection-info form.admin-panel-content');
  const $fieldsetDiv = $selectionInfo.find('.fieldset-container');
  const $relatedCard = $('#selection-groups [selected]').closest('.card-container');

  $selectionInfo.children('h2').html(response.SelectionGroupName);
  $selectionInfo.children('h3').html(response.RootContentItemName);

  // tslint:disable:object-literal-sort-keys
  const details = $.extend({
    User: {
      FirstName: '',
    },
    StatusEnum: 0,
    StatusName: '',
    SelectionGroupId: 0,
    RootContentItemId: 0,
  }, response.ReductionSummary);
  // tslint:enable:object-literal-sort-keys

  const comparison = response.SelectionComparison;
  const isMaster = comparison.PendingSelections === null && comparison.IsLiveMaster;
  $('#IsMaster').prop('checked', isMaster);
  $fieldsetDiv.hide().filter(() => !isMaster).show();
  $('#IsSuspended').prop('checked', response.IsSuspended);
  $selectionInfo.find('.selection-content').hide().filter(() => !response.IsSuspended).show();

  $fieldsetDiv.empty();
  comparison.Hierarchy.Fields.forEach((field) =>
    renderField(field, $fieldsetDiv, comparison.LiveSelections, comparison.PendingSelections));
  updateCardStatus($relatedCard, response.ReductionSummary);
  $selectionInfo
    .find('button').hide()
    .filter(`.button-status-${details.StatusEnum}`).show();
  // TODO: rely on some flag in the response to disable checkboxes
  const readonly = [10, 20, 30].indexOf(details.StatusEnum) !== -1;
  $fieldsetDiv
    .find('input[type="checkbox"]')
    .click(readonly
      ? (event) => {
        event.preventDefault();
      }
      : () => undefined);
  $('#IsMaster').attr('disabled', () => readonly ? '' : null);
}

function renderSelectionGroup(selectionGroup: SelectionGroupSummary) {
  $('#root-content-items [selected]').parent().data('eligibleMembers', selectionGroup.MemberList);
  const $card = new SelectionGroupCard(
    selectionGroup,
    $('#root-content-items [selected]').parent().data().eligibleList,
    wrapCardCallback(get(
      'ContentAccessAdmin/Selections',
      [
        renderSelections,
      ],
    )),
    selectionGroupDeleteClickHandler,
    (event: Event) => {
      event.stopPropagation();
      const $target = $(event.target).closest('.card-body-container');
      $target.find('.card-button-side-container .card-button-green').show();
      $target.find('.detail-item-user-icon').hide();
      $target.find('.detail-item-user-create').show();
      $target.find('.detail-item-user-remove').show();
      $target.find('.card-body-primary-text-box h2').hide();
      $target.find('.card-body-primary-text-box input').show();
      $target
        .find('.card-button-dynamic').hide()
        .filter('.card-button-green').show();
      setExpanded($('#selection-groups'), $target);
      $target.find('.tt-input').focus();
    },
    (event: Event) => {
      event.stopPropagation();
      const $target = $(event.target).closest('.card-body-container');
      $target.find('.card-button-side-container .card-button-green').hide();
      $target.find('.detail-item-user-icon').show();
      $target.find('.detail-item-user-create').hide();
      $target.find('.detail-item-user-remove').hide();
      const $h2 = $target.find('.card-body-primary-text-box h2');
      const $input = $target.find('.card-body-primary-text-box input');
      $h2.html($input.val().toString());
      $h2.show();
      $input.hide();
      $.post({
        data: {
          name: $target.find('.card-body-primary-text-box input').val() || 'Untitled',
          selectionGroupId: $target.parent().data().selectionGroupId,
        },
        headers: {
          RequestVerificationToken: $('input[name="__RequestVerificationToken"]').val().toString(),
        },
        url: 'ContentAccessAdmin/RenameSelectionGroup/',
      }).done((response) => {
        $target
          .find('.card-button-dynamic').hide()
          .filter('.card-button-blue').show();
      }).fail((response) => {
        toastr.warning(response.getResponseHeader('Warning')
          || 'An unknown error has occurred.');
      });
    },
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

  $('#IsMaster').click(() => {
    $('#selection-info form.admin-panel-content .fieldset-container')
      .hide().filter(() => !$('#IsMaster').prop('checked')).show();
  });
  $('#IsSuspended').click((event) => {
    event.preventDefault();
    $('#IsSuspended').attr('disabled', '');

    const data = {
      isSuspended: $('#IsSuspended').prop('checked'),
      selectionGroupId: $('#selection-groups [selected]').parent().data().selectionGroupId,
    };

    function onResponse() {
      $('#IsSuspended').removeAttr('disabled');
      $('#selection-info form.admin-panel-content .selection-content')
        .hide().filter(() => !$('#IsSuspended').prop('checked')).show();
    }

    $.post({
      data,
      headers: {
        RequestVerificationToken: $('input[name="__RequestVerificationToken"]').val().toString(),
      },
      url: 'ContentAccessAdmin/SetSuspendedSelectionGroup',
    }).done((response) => {
      // Set checkbox states to match the response
      $('#IsSuspended').prop('checked', response.IsSuspended);

      const setUnset = response.IsSuspended ? '' : 'un';
      toastr.success(`${response.GroupName} was ${setUnset}suspended.`);
      onResponse();
    }).fail((response) => {
      toastr.warning(response.getResponseHeader('Warning')
        || 'An unknown error has occurred.');
      onResponse();
    });
  });

  $('.tooltip').tooltipster();
}
