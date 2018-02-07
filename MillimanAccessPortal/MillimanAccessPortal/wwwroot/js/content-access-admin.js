/* global Card */

var ajaxStatus = {
  getClientDetail: -1
};
var nodeTemplate = $('script[data-template="node"]').html();
var smallSpinner = '<div class="spinner-small""></div>';
var $createNewClientCard;
var $createNewChildClientCard;
var $addUserCard;
var eligibleUsers;
var SHOW_DURATION = 50;

/**
 * Remove all client insert elements.
 * While this function removes all client inserts, there should never be more
 * than one client insert present at a time.
 * @return {undefined}
 */
function removeClientInserts() {
  $('#client-tree li.client-insert').remove();
}

/**
 * Clear 'selected' and 'editing' status from all card containers.
 * @return {undefined}
 */
function clearClientSelection() {
  $('.card-container').removeAttr('editing selected');
}

/**
 * Send an AJAX request to get the client tree
 * @return {undefined}
 */
function getClientTree(clientId) {
  $('#client-tree .loading-wrapper').show();
  $.ajax({
    type: 'GET',
    url: 'ContentAccessAdmin/ClientFamilyList/'
  }).done(function onDone(response) {
    console.log(response);
    renderClientTree(response.ClientTreeList, clientId || response.RelevantClientId);
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

/**
 * Render user node by using string substitution on a clientNodeTemplate
 * @param  {Object} client Client object to render
 * @param  {Number} level  Client indentation level
 * @return {undefined}
 */
function renderClientNode(client, level) {
  var classes = ['card-100', 'card-90', 'card-80'];
  /* eslint-disable indent */
  var $template = Card
    .newCard()
    .container(
      [
        client.ClientDetailModel.ClientEntity.Name,
        client.ClientDetailModel.ClientEntity.ClientCode
      ],
      client.ClientDetailModel.ClientEntity.Id,
      null,
      client.ClientDetailModel.CanManage
    )
      .class(classes[level])
      .click(function onClick() {
        clientCardClickHandler($(this));
      })
    .primaryInfo(client.ClientDetailModel.ClientEntity.Name)
    .secondaryInfo(client.ClientDetailModel.ClientEntity.ClientCode || '')
    .cardStat('#action-icon-users', client.ClientDetailModel.EligibleUserCount)
      .tooltip('Content-eligible users')
    .cardStat('#action-icon-reports', client.ClientDetailModel.RootContentItemCount)
      .tooltip('Reports')
    .build();
  /* eslint-enable indent */

  $('#client-tree-list').append($template);

  // Render child nodes
  if (client.ChildClientModels.length) {
    client.ChildClientModels.forEach(function forEach(childNode) {
      renderClientNode(childNode, level + 1);
    });
  }
}

$(document).ready(function onReady() {
  getClientTree();

  $('.tooltip').tooltipster();
});
