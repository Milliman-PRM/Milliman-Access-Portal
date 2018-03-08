/* global card, dialog, shared */


function updateSelectionGroupCount() {
  $('#root-content-items [selected] [href="#action-icon-users"]').parent().next().html($('#selection-groups ul.admin-panel-content li').length);
}

function selectionGroupAddClickHandler() {
  new dialog.AddSelectionGroupDialog(shared.post(
    'ContentAccessAdmin/CreateSelectionGroup',
    'Selection group successfully created.',
    renderSelectionGroup,
    updateSelectionGroupCount
  )).open();
}

function selectionGroupDeleteClickHandler() {
  new dialog.DeleteSelectionGroupDialog($(this).closest('.card-container'), shared.delete(
    'ContentAccessAdmin/DeleteSelectionGroup',
    'Selection group successfully deleted.',
    function (response) {
      $('#selection-groups ul.admin-panel-content').empty();
      renderSelectionGroupList(response);
    },
    updateSelectionGroupCount
  )).open();
}

function cancelSelectionForm() {
  var $selectionInfo = $('#selection-info form.admin-panel-content');
  var $selectionGroups = $('#selection-groups ul.admin-panel-content');
  var $button = $selectionInfo.find('button');
  var data = {
    SelectionGroupId: $selectionGroups.find('[selected]').closest('.card-container').attr('data-selection-group-id')
  };

  shared.showButtonSpinner($button, 'Canceling');
  $.ajax({
    type: 'POST',
    url: 'ContentAccessAdmin/CancelReduction',
    data: data,
    headers: {
      RequestVerificationToken: $("input[name='__RequestVerificationToken']").val()
    }
  }).done(function onDone(response) {
    shared.hideButtonSpinner($button);
    renderSelections(response);
    toastr.success('Reduction tasks canceled.');
  }).fail(function onFail(response) {
    shared.hideButtonSpinner($button);
    toastr.warning(response.getResponseHeader('Warning'));
  });
}
function submitSelectionForm() {
  var $selectionInfo = $('#selection-info form.admin-panel-content');
  var $selectionGroups = $('#selection-groups ul.admin-panel-content');
  var $button = $selectionInfo.find('button');
  var data = {
    SelectionGroupId: $selectionGroups.find('[selected]').closest('.card-container').attr('data-selection-group-id'),
    Selections: $selectionInfo.serializeArray().reduce(function (acc, cur) {
      return (cur.value === 'on')
        ? acc.concat(cur.name)
        : undefined;
    }, [])
  };

  shared.showButtonSpinner($button);
  $.ajax({
    type: 'POST',
    url: 'ContentAccessAdmin/SingleReduction',
    data: data,
    headers: {
      RequestVerificationToken: $("input[name='__RequestVerificationToken']").val()
    }
  }).done(function onDone(response) {
    shared.hideButtonSpinner($button);
    renderSelections(response);
    toastr.success('A reduction task has been queued.');
  }).fail(function onFail(response) {
    shared.hideButtonSpinner($button);
    toastr.warning(response.getResponseHeader('Warning'));
  });
}

function renderValue(value, $fieldset, originalSelections) {
  var $div;
  var $checkbox = $('<input type="checkbox" id="selection-value-' + value.Id + '" name="' + value.Id + '">');
  $fieldset.append('<div></div>');
  $div = $fieldset.find('div').last();
  $div.append($checkbox);
  $div.append('<label for="selection-value-' + value.Id + '">' + value.Value + '</label>');
  $checkbox.prop('checked', value.SelectionStatus);
  if (originalSelections.includes(value.Id) !== value.SelectionStatus) {
    $div.attr('style', 'background: yellow;');
  }
}

function renderField(field, $parent, originalSelections) {
  var $fieldset;
  $parent.append('<fieldset></fieldset>');
  $fieldset = $parent.find('fieldset').last();
  $fieldset.append('<legend>' + field.DisplayName + '</legend>');
  field.Values.forEach(function (value) {
    renderValue(value, $fieldset, originalSelections);
  });
}

function renderSelections(response) {
  var $selectionInfo = $('#selection-info form.admin-panel-content');
  var $fieldsetDiv = $selectionInfo.find('.fieldset-container');
  var $statusContainer = $('#selection-groups [selected]').closest('.card-container').find('.card-status-container');
  $fieldsetDiv.empty();
  response.Hierarchy.Fields.forEach(function (field) {
    renderField(field, $fieldsetDiv, response.OriginalSelections);
  });
  $selectionInfo.find('button').hide();
  if (response.ReductionDetails) {
    $statusContainer.find('em').html(response.ReductionDetails.User.FirstName);
    if (response.ReductionDetails.StatusEnum === 10) {
      $selectionInfo.find('.red-button').show();
      $fieldsetDiv.find('input[type="checkbox"]').click(function (event) { event.preventDefault(); });
      $statusContainer.attr('class', 'card-status-container status-queued');
    } else if (response.ReductionDetails.StatusEnum === 20) {
      $fieldsetDiv.find('input[type="checkbox"]').click(function (event) { event.preventDefault(); });
      $statusContainer.attr('class', 'card-status-container status-reducing');
    } else if (response.ReductionDetails.StatusEnum === 30) {
      $fieldsetDiv.find('input[type="checkbox"]').click(function (event) { event.preventDefault(); });
      $statusContainer.attr('class', 'card-status-container status-reduced');
    } else {
      $selectionInfo.find('.blue-button').show();
      $fieldsetDiv.find('input[type="checkbox"]').removeAttr('disabled');
      $statusContainer.attr('class', 'card-status-container status-default');
    }
  } else {
    $selectionInfo.find('.blue-button').show();
    $fieldsetDiv.find('input[type="checkbox"]').removeAttr('disabled');
    $statusContainer.attr('class', 'card-status-container status-default');
  }
}

function renderSelectionGroup(selectionGroup) {
  var $card = new card.SelectionGroupCard(
    selectionGroup.SelectionGroupEntity,
    selectionGroup.MemberList,
    shared.wrapCardCallback(shared.get(
      'ContentAccessAdmin/Selections',
      renderSelections
    )),
    selectionGroupDeleteClickHandler,
    function () { console.log('Add/remove user button clicked.'); }
  ).build();
  var $statusContainer = $card.find('.card-status-container');
  if (selectionGroup.ReductionDetails) {
    $statusContainer.find('em').html(selectionGroup.ReductionDetails.User.FirstName);
    if (selectionGroup.ReductionDetails.StatusEnum === 10) {
      $statusContainer.removeClass('status-default').addClass('status-queued');
    } else if (selectionGroup.ReductionDetails.StatusEnum === 20) {
      $statusContainer.removeClass('status-default').addClass('status-reducing');
    } else if (selectionGroup.ReductionDetails.StatusEnum === 30) {
      $statusContainer.removeClass('status-default').addClass('status-reduced');
    }
  }
  $('#selection-groups ul.admin-panel-content').append($card);
}
function renderSelectionGroupList(response, selectionGroupId) {
  var $selectionGroupList = $('#selection-groups ul.admin-panel-content');
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
  var $card = new card.RootContentItemCard(
    rootContentItem.RootContentItemEntity,
    rootContentItem.GroupCount,
    rootContentItem.EligibleUserCount,
    shared.wrapCardCallback(shared.get(
      'ContentAccessAdmin/SelectionGroups',
      renderSelectionGroupList
    ))
  );

  $('#root-content-items ul.admin-panel-content').append($card.build());
}
function renderRootContentItemList(response, rootContentItemId) {
  var $rootContentItemList = $('#root-content-items ul.admin-panel-content');
  $rootContentItemList.empty();
  response.RootContentItemList.forEach(renderRootContentItem);
  $rootContentItemList.find('.tooltip').tooltipster();

  if (rootContentItemId) {
    $('[data-root-content-item-id="' + rootContentItemId + '"]').click();
  }
}


function renderClientNode(client, level) {
  var $card = new card.ClientCard(
    client.ClientDetailModel.ClientEntity,
    client.ClientDetailModel.EligibleUserCount,
    client.ClientDetailModel.RootContentItemCount,
    level,
    shared.wrapCardCallback(shared.get(
      'ContentAccessAdmin/RootContentItems',
      renderRootContentItemList
    ))
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
function renderClientTree(response, clientId) {
  var $clientTreeList = $('#client-tree ul.admin-panel-content');
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


$(document).ready(function () {
  (shared.get(
    'ContentAccessAdmin/ClientFamilyList',
    renderClientTree
  )());

  $('.action-icon-expand').click(shared.expandAll.listener);
  $('.action-icon-collapse').click(shared.collapseAll.listener);
  $('.admin-panel-searchbar').keyup(shared.filterTree.listener);

  $('#selection-groups ul.admin-panel-content-action').append(new card.AddSelectionGroupActionCard(selectionGroupAddClickHandler).build());
  // TODO: select by ID or better classes
  $('#selection-info .blue-button').click(submitSelectionForm);
  $('#selection-info .red-button').click(cancelSelectionForm);

  $('.tooltip').tooltipster();

  setTimeout(function () {
    var poll = function () {
      var $rootContentItem = $('#root-content-items [selected]').closest('.card-container');
      if ($rootContentItem.length) {
        shared.get(
          'ContentAccessAdmin/SelectionGroups',
          renderSelectionGroupList
        )($rootContentItem);
      }
      setTimeout(poll, 10000);
    };
    poll();
  }, 10000000);
});
