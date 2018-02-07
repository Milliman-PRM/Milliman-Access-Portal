/* global Card */

var ajaxStatus = {
  getRootContentItemList: -1
};
var smallSpinner = '<div class="spinner-small""></div>';
var eligibleUsers;
var SHOW_DURATION = 50;

var clientCardClickHandler;

// Helper functions (TODO: consider moving to separate file)

function clearClientSelection() {
  $('.card-container').removeAttr('editing selected');
}

function showClientDetails() {
  // TODO: consider using nested divs for better hiding
  $('#root-content').show(SHOW_DURATION);
}

function hideClientDetails() {
  // TODO: consider using nested divs for better hiding
  $('#root-content').hide(SHOW_DURATION);
  $('#selection-groups').hide(SHOW_DURATION);
  $('#selections').hide(SHOW_DURATION);
}

function openClientCard($clientCard) {
  clearClientSelection();
  $clientCard.attr('selected', '');
  getRootContentItemList($clientCard);
  showClientDetails();
}

/**
 * Render user node by using string substitution on a clientNodeTemplate
 * @param  {Object} client Client object to render
 * @param  {Number} level  Client indentation level
 * @return {undefined}
 */
function renderClientNode(client, level) {
  var classes = ['card-100', 'card-90', 'card-80'];
  /* eslint-disable indent */
  var $card = Card
    .newCard()
    .container(client.ClientDetailModel.CanManage)
      .searchString([
        client.ClientDetailModel.ClientEntity.Name,
        client.ClientDetailModel.ClientEntity.ClientCode
      ])
      .attr({ 'data-client-id': client.ClientDetailModel.ClientEntity.Id })
      .class(classes[level])
      .click(client.ClientDetailModel.CanManage
        ? function onClick() {
          clientCardClickHandler($(this));
        }
        : undefined)
    .primaryInfo(client.ClientDetailModel.ClientEntity.Name)
    .secondaryInfo(client.ClientDetailModel.ClientEntity.ClientCode || '')
    .cardStat('#action-icon-users', client.ClientDetailModel.EligibleUserCount)
      .tooltip('Content-eligible users')
    .cardStat('#action-icon-reports', client.ClientDetailModel.RootContentItemCount)
      .tooltip('Reports')
    .build();
  /* eslint-enable indent */

  $('#client-tree-list').append($card);

  // Render child nodes
  if (client.ChildClientModels.length) {
    client.ChildClientModels.forEach(function forEach(childNode) {
      renderClientNode(childNode, level + 1);
    });
  }
}

function renderRootContentItem(rootContentItem) {
  /* eslint-disable indent */
  var $card = Card
    .newCard()
    .container()
      .searchString([
        rootContentItem.RootContentItemEntity.ContentName,
        rootContentItem.RootContentItemEntity.ContentType.Name
      ])
      .attr({ 'data-root-content-item-id': rootContentItem.RootContentItemEntity.Id })
      .click(function onClick() {
        console.log('Root content item ' + rootContentItem.RootContentItemEntity.ContentName + ' clicked.');
      })
    .primaryInfo(rootContentItem.RootContentItemEntity.ContentName)
    .secondaryInfo(rootContentItem.RootContentItemEntity.ContentType.Name)
    .cardStat('#action-icon-users', rootContentItem.GroupCount)
      .tooltip('Selection groups')
    .cardStat('#action-icon-reports', rootContentItem.EligibleUserCount)
      .tooltip('Eligible users')
    .build();
  /* eslint-enable indent */

  $('#root-content-list').append($card);
}

/**
 * Render client tree recursively and attach event handlers
 * @param  {Number} clientId ID of the client card to click after render
 * @return {undefined}
 */
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

/**
 * Send an AJAX request to get the client tree
 * @return {undefined}
 */
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

// Event handlers

/**
 * Handle click events for all client cards and client inserts
 * @param {jQuery} $clickedCard the card that was clicked
 * @return {undefined}
 */
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


$(document).ready(function onReady() {
  getClientTree();

  $('.tooltip').tooltipster();
});
