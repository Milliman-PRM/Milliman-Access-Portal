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

function renderSelectionGroup(selectionGroup) {
  var $card = new card.SelectionGroupCard(
    selectionGroup.SelectionGroupEntity,
    selectionGroup.MemberList,
    function () { console.log('Selection group clicked.'); },
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
  $('.admin-panel-searchbar').keyup(shared.filterTreeImp.listener);

  $('#selection-groups ul.admin-panel-content-action').append(new card.AddSelectionGroupActionCard(selectionGroupAddClickHandler).build());

  $('.tooltip').tooltipster();
});
