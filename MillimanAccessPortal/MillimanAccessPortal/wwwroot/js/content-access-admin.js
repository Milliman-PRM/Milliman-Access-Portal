/* global Card */

var ajaxStatus = {
  getRootContentItemList: -1
};
var SHOW_DURATION = 50;

var clientCardClickHandler;
var rootContentItemCardClickHandler;

// Helper functions (TODO: consider moving to separate file)

function clearClientSelection() {
  $('#client-tree .card-container').removeAttr('editing selected');
}
function clearRootContentItemSelection() {
  $('#root-content .card-container').removeAttr('editing selected');
}

function showClientDetails() {
  // TODO: consider using nested divs for better hiding
  $('#root-content').show(SHOW_DURATION);
}
function showRootContentItemDetails() {
  $('#selection-groups').show(SHOW_DURATION);
}

function hideClientDetails() {
  // TODO: consider using nested divs for better hiding
  $('#root-content').hide(SHOW_DURATION);
  $('#selection-groups').hide(SHOW_DURATION);
  $('#selections').hide(SHOW_DURATION);
}
function hideRootContentItemDetails() {
  $('#selection-groups').hide(SHOW_DURATION);
  $('#selections').hide(SHOW_DURATION);
}

function openClientCard($clientCard) {
  clearClientSelection();
  $clientCard.attr('selected', '');
  getRootContentItemList($clientCard);
  showClientDetails();
}
function openRootContentItemCard($rootContentItemCard) {
  clearRootContentItemSelection();
  $rootContentItemCard.attr('selected', '');
  getSelectionGroupList($rootContentItemCard);
  showRootContentItemDetails();
}

function showRelevantActionIcons(panel) {
  $('#collapse-' + panel + '-icon').hide().filter(function anyMaximized() {
    return $('div.card-expansion-container[maximized]').length;
  }).show();
  $('#expand-' + panel + '-icon').hide().filter(function anyMinimized() {
    return $('div.card-expansion-container:not([maximized])').length;
  }).show();
}
function expandAll(panel) {
  $('#' + panel).find('div.card-expansion-container').attr('maximized', '');
  showRelevantActionIcons(panel);
}
function collapseAll(panel) {
  $('#' + panel).find('div.card-expansion-container[maximized]').removeAttr('maximized');
  showRelevantActionIcons(panel);
}

// Render functions

function renderClientNode(client, level) {
  var $card = new ClientCard(
    client.ClientDetailModel.ClientEntity.Name,
    client.ClientDetailModel.ClientEntity.ClientCode,
    client.ClientDetailModel.EligibleUserCount,
    client.ClientDetailModel.RootContentItemCount,
    level,
    client.ClientDetailModel.ClientEntity.Id,
    client.ClientDetailModel.CanManage,
    function onClick() {
      clientCardClickHandler($(this));
    }
  );

  $('#client-tree-list').append($card.build());

  // Render child nodes
  if (client.ChildClientModels.length) {
    client.ChildClientModels.forEach(function forEach(childNode) {
      renderClientNode(childNode, level + 1);
    });
  }
}

function renderRootContentItem(rootContentItem) {
  var $card = new RootContentItemCard(
    rootContentItem.RootContentItemEntity.ContentName,
    rootContentItem.RootContentItemEntity.ContentType.Name,
    rootContentItem.RootContentItemEntity.Id,
    rootContentItem.GroupCount,
    rootContentItem.EligibleUserCount,
    function onClick() {
      rootContentItemCardClickHandler($(this));
    }
  );

  $('#root-content-list').append($card.build());
}

function renderSelectionGroup(selectionGroup) {
  var $card = new SelectionGroupCard(
    selectionGroup.SelectionGroupEntity.GroupName,
    selectionGroup.SelectionGroupEntity.Id,
    selectionGroup.MemberList,
    function () { console.log('Selection group clicked.'); },
    function () { console.log('Add/remove user button clicked.'); },
    function () { console.log('Delete group button clicked.'); }
  );

  $('#selection-groups-list').append($card.build());
}

function renderClientTree(clientTreeList, clientId) {
  var $clientTreeList = $('#client-tree-list');
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
  var $rootContentItemList = $('#root-content-list');
  $rootContentItemList.empty();
  rootContentItemList.forEach(function render(rootContentItem) {
    renderRootContentItem(rootContentItem);
  });
  $rootContentItemList.find('.tooltip').tooltipster();

  if (rootContentItemId) {
    $('[data-root-content-item-id="' + rootContentItemId + '"]').click();
  }
}
function renderSelectionGroupList(selectionGroupList, selectionGroupId) {
  var $selectionGroupList = $('#selection-groups-list');
  $selectionGroupList.empty();
  selectionGroupList.forEach(function render(selectionGroup) {
    renderSelectionGroup(selectionGroup);
  });
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
function getRootContentItemList($clientCard) {
  var clientId = $clientCard.attr('data-client-id');
  $('#root-content .loading-wrapper').show();
  ajaxStatus.getRootContentItemList = clientId;
  $.ajax({
    type: 'GET',
    url: 'ContentAccessAdmin/RootContentItems',
    data: {
      ClientId: clientId
    }
  }).done(function onDone(response) {
    renderRootContentItemList(response.RootContentItemList);
    $('#root-content .loading-wrapper').hide();
  }).fail(function onFail(response) {
    $('#root-content .loading-wrapper').hide();
    if (response.getResponseHeader('Warning')) {
      toastr.warning(response.getResponseHeader('Warning'));
    } else {
      toastr.error('An error has occurred');
    }
  });
}
function getSelectionGroupList($rootContentItemCard) {
  var clientId = $('#client-tree [selected]').attr('data-client-id');
  var rootContentItemId = $rootContentItemCard.attr('data-root-content-item-id');
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
}


// Event handlers

clientCardClickHandler = function clientCardClickHandler_($clickedCard) {
  var $clientTree = $('#client-tree');
  var sameCard = ($clickedCard[0] === $clientTree.find('[selected]')[0]);
  if ($clientTree.has('[selected]').length) {
    // TODO: wrap if-else with confirmAndReset
    if (sameCard) {
      clearClientSelection();
      hideClientDetails();
    } else {
      openClientCard($clickedCard);
    }
  } else {
    openClientCard($clickedCard);
  }
};
rootContentItemCardClickHandler = function rootContentItemCardClickHandler_($clickedCard) {
  var $rootContent = $('#root-content');
  var sameCard = ($clickedCard[0] === $rootContent.find('[selected]')[0]);
  if ($rootContent.has('[selected]').length) {
    // TODO: wrap if-else with confirmAndReset
    if (sameCard) {
      clearRootContentItemSelection();
      hideRootContentItemDetails();
    } else {
      openRootContentItemCard($clickedCard);
    }
  } else {
    openRootContentItemCard($clickedCard);
  }
};


$(document).ready(function onReady() {
  getClientTree();

  $('#expand-selection-groups-icon').click(function () { expandAll('selection-groups'); });
  $('#collapse-selection-groups-icon').click(function () { collapseAll('selection-groups'); });

  $('.tooltip').tooltipster();
});
