/* global card, shared */

var ajaxStatus = {
  getRootContentItemList: -1
};

var getRootContentItemList;
var getSelectionGroupList;

// Render functions
function renderClientNode(client, level) {
  var $card = new card.ClientCard(
    client.ClientDetailModel.ClientEntity,
    client.ClientDetailModel.EligibleUserCount,
    client.ClientDetailModel.RootContentItemCount,
    level,
    shared.wrapCardCallback(getRootContentItemList)
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

function renderRootContentItem(rootContentItem) {
  var $card = new card.RootContentItemCard(
    rootContentItem.RootContentItemEntity,
    rootContentItem.GroupCount,
    rootContentItem.EligibleUserCount,
    shared.wrapCardCallback(getSelectionGroupList)
  );

  $('#root-content-items ul.admin-panel-content').append($card.build());
}

function renderSelectionGroup(selectionGroup) {
  var $card = new card.SelectionGroupCard(
    selectionGroup.SelectionGroupEntity,
    selectionGroup.MemberList,
    function () { console.log('Selection group clicked.'); },
    function () { console.log('Delete group button clicked.'); },
    function () { console.log('Add/remove user button clicked.'); }
  );

  $('#selection-groups ul.admin-panel-content').append($card.build());
}

function renderClientTree(clientTreeList, clientId) {
  var $clientTreeList = $('#client-tree ul.admin-panel-content');
  $clientTreeList.empty();
  clientTreeList.forEach(function render(rootClient) {
    renderClientNode(rootClient, 0);
    $clientTreeList.append('<li class="hr width-100pct"></li>');
  });
  $clientTreeList.find('.hr').last().remove();
  $clientTreeList.find('.tooltip').tooltipster();

  if (clientId) {
    $('[data-client-id="' + clientId + '"]').click();
  }
}
function renderRootContentItemList(rootContentItemList, rootContentItemId) {
  var $rootContentItemList = $('#root-content-items ul.admin-panel-content');
  $rootContentItemList.empty();
  rootContentItemList.forEach(renderRootContentItem);
  $rootContentItemList.find('.tooltip').tooltipster();

  if (rootContentItemId) {
    $('[data-root-content-item-id="' + rootContentItemId + '"]').click();
  }
}
function renderSelectionGroupList(selectionGroupList, selectionGroupId) {
  var $selectionGroupList = $('#selection-groups ul.admin-panel-content');
  $selectionGroupList.empty();
  selectionGroupList.forEach(renderSelectionGroup);
  $selectionGroupList.find('.tooltip').tooltipster();

  if (selectionGroupId) {
    $('[data-selection-group-id="' + selectionGroupId + '"]').click();
  }
}


// AJAX functions

function getClientTree() {
  $('#client-tree .loading-wrapper').show();
  $.ajax({
    type: 'GET',
    url: 'ContentAccessAdmin/ClientFamilyList'
  }).done(function onDone(response) {
    renderClientTree(response.ClientTreeList);
    $('#client-tree .loading-wrapper').hide();
  }).fail(function onFail(response) {
    $('#client-tree .loading-wrapper').hide();
    if (response.getResponseHeader('Warning')) {
      toastr.warning(response.getResponseHeader('Warning'));
    } else {
      toastr.error('An error has occurred');
    }
  });
}
getRootContentItemList = function ($card) {
  var clientId = $card.data('client-id');
  $('#root-content-items .loading-wrapper').show();
  ajaxStatus.getRootContentItemList = clientId;
  $.ajax({
    type: 'GET',
    url: 'ContentAccessAdmin/RootContentItems',
    data: {
      ClientId: clientId
    }
  }).done(function onDone(response) {
    renderRootContentItemList(response.RootContentItemList);
    $('#root-content-items .loading-wrapper').hide();
  }).fail(function onFail(response) {
    $('#root-content-items .loading-wrapper').hide();
    if (response.getResponseHeader('Warning')) {
      toastr.warning(response.getResponseHeader('Warning'));
    } else {
      toastr.error('An error has occurred');
    }
  });
};
getSelectionGroupList = function ($card) {
  var clientId = $('#client-tree [selected]').data('client-id');
  var rootContentItemId = $card.data('root-content-item-id');
  $('#selection-groups .loading-wrapper').show();
  ajaxStatus.getRootContentItemList = [clientId, rootContentItemId];
  $.ajax({
    type: 'GET',
    url: 'ContentAccessAdmin/SelectionGroups',
    data: {
      ClientId: clientId,
      RootContentItemId: rootContentItemId
    }
  }).done(function onDone(response) {
    renderSelectionGroupList(response.SelectionGroupList);
    $('#selection-groups .loading-wrapper').hide();
  }).fail(function onFail(response) {
    $('#selection-groups .loading-wrapper').hide();
    if (response.getResponseHeader('Warning')) {
      toastr.warning(response.getResponseHeader('Warning'));
    } else {
      toastr.error('An error has occurred');
    }
  });
};

$(document).ready(function () {
  getClientTree();

  $('.action-icon-expand').click(shared.expandAll);
  $('.action-icon-collapse').click(shared.collapseAll);
  $('.admin-panel-searchbar').keyup(shared.filterTree);

  $('.tooltip').tooltipster();
});
