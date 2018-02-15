/* global ClientCard, RootContentItemCard, SelectionGroupCard */

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
  $('#root-content-items .card-container').removeAttr('editing selected');
}

function showClientDetails() {
  // TODO: consider using nested divs for better hiding
  $('#root-content-items').show(SHOW_DURATION);
}
function showRootContentItemDetails() {
  $('#selection-groups').show(SHOW_DURATION);
}

function hideClientDetails() {
  // TODO: consider using nested divs for better hiding
  $('#root-content-items').hide(SHOW_DURATION);
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
  $(panel + ' .action-icon-collapse').hide().filter(function anyMaximized() {
    return $('div.card-expansion-container[maximized]').length;
  }).show();
  $(panel + ' .action-icon-expand').hide().filter(function anyMinimized() {
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
    client.ClientDetailModel.ClientEntity,
    client.ClientDetailModel.EligibleUserCount,
    client.ClientDetailModel.RootContentItemCount,
    level,
    clientCardClickHandler
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
  var $card = new RootContentItemCard(
    rootContentItem.RootContentItemEntity,
    rootContentItem.GroupCount,
    rootContentItem.EligibleUserCount,
    rootContentItemCardClickHandler
  );

  $('#root-content-items ul.admin-panel-content').append($card.build());
}

function renderSelectionGroup(selectionGroup) {
  var $card = new SelectionGroupCard(
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
function getRootContentItemList($clientCard) {
  var clientId = $clientCard.attr('data-client-id');
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

clientCardClickHandler = function () {
  var $clickedCard = $(this);
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
rootContentItemCardClickHandler = function () {
  var $clickedCard = $(this);
  var $rootContent = $('#root-content-items');
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


$(document).ready(function () {
  getClientTree();

  $('#expand-selection-groups-icon').click(function () { expandAll('selection-groups'); });
  $('#collapse-selection-groups-icon').click(function () { collapseAll('selection-groups'); });

  $('.tooltip').tooltipster();
});
