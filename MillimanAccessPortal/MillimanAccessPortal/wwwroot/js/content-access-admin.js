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

function submitSelectionForm() {
  var $selectionInfo = $('#selection-info form.admin-panel-content');
  var $selectionGroups = $('#selection-groups ul.admin-panel-content');
  var $button = $selectionInfo.find('.submit-button');
  var data = {
    SelectionGroupId: $selectionGroups.find('[selected]').attr('data-selection-group-id'),
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
    toastr.success('A reduction task has been queued.');
  }).fail(function onFail(response) {
    shared.hideButtonSpinner($button);
    toastr.warning(response.getResponseHeader('Warning'));
  });
}

function renderValue(value, $fieldset, originalSelections) {
  var $div;
  $fieldset.append('<div></div>');
  $div = $fieldset.find('div').last();
  $div.append('<input type="checkbox" id="selection-value-' + value.Id + '" name="' + value.Id + '">');
  $div.append('<label for="selection-value-' + value.Id + '">' + value.Value + '</label>');
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
  var $selectionsInfo = $('#selection-info form.admin-panel-content > .fieldset-container');
  $selectionsInfo.empty();
  response.Hierarchy.Fields.forEach(function (field) {
    renderField(field, $selectionsInfo, response.OriginalSelections);
  });
}

function populateSelections(response) {
  $('#selection-info input[type="checkbox"]').each(function (i, box) {
    var $box = $(box);
    $box.prop('checked', response.SelectedHierarchyFieldValueList.includes($box.attr('name')));
  });
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
  );
  $('#selection-groups ul.admin-panel-content').append($card.build());
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
  $('#selection-info .submit-button').click(submitSelectionForm);

  $('.tooltip').tooltipster();
});
