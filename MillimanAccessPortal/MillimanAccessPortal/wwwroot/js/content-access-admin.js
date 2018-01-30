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
    //renderClientTree(response.ClientTreeList, clientId || response.RelevantClientId);
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
  $clientTreeList.find('.tooltip').tooltipster();
  $clientTreeList.find('.card-container')
    .click(function onClick() {
      clientCardClickHandler($(this));
    });
  $clientTreeList.find('.card-button-delete')
    .click(function onClick(event) {
      event.stopPropagation();
      clientCardDeleteClickHandler($(this).parents('div[data-client-id]'));
    });
  $clientTreeList.find('.card-button-edit')
    .click(function onClick(event) {
      event.stopPropagation();
      clientCardEditClickHandler($(this).parents('div[data-client-id]'));
    });
  $clientTreeList.find('.card-button-new-child')
    .click(function onClick(event) {
      event.stopPropagation();
      clientCardCreateNewChildClickHandler($(this).parents('div[data-client-id]'));
    });

  // TODO: Consider applying this to other cards and buttons as well
  $clientTreeList.find('.card-container,.card-button-background')
    .mousedown(function onMousedown(event) {
      event.preventDefault();
    });

  if (clientId) {
    $('[data-client-id="' + clientId + '"]').click();
  }
  if ($('#add-client-icon').length) {
    $clientTreeList.append($createNewClientCard);
    $('#create-new-client-card')
      .click(function onClick() {
        createNewClientClickHandler($(this));
      });
  }
}



$(document).ready(function onReady() {
  getClientTree();

  //$('#expand-user-icon').click(expandAllUsers);
  //$('#collapse-user-icon').click(collapseAllUsers);

  //$('#client-search-box').keyup(function onKeyup() {
  //  searchClientTree($(this).val());
  //});

  //$('#user-search-box').keyup(function onKeyup() {
  //  searchUser($(this).val());
  //});

  //// Construct static cards
  //$createNewClientCard = $(nodeTemplate);
  //$createNewClientCard.find('.card-container')
  //  .addClass('card-100 action-card')
  //  .attr('id', 'create-new-client-card');
  //$createNewClientCard.find('.card-body-primary-text')
  //  .append('<svg class="action-card-icon"><use xlink:href="#action-icon-add"></use></svg>')
  //  .append('<span>New Client</span>');
  //$createNewClientCard.find('.card-expansion-container,.card-body-secondary-container,.card-stats-container,.card-button-side-container,.card-body-secondary-text')
  //  .remove();

  //$createNewChildClientCard = $(nodeTemplate);
  //$createNewChildClientCard
  //  .addClass('client-insert');
  //$createNewChildClientCard.find('.card-container')
  //  .addClass('flex-container flex-row-no-wrap items-align-center');
  //$createNewChildClientCard.find('.card-body-main-container')
  //  .addClass('content-item-flex-1');
  //$createNewChildClientCard.find('.card-body-primary-text')
  //  .append('<span>New Sub-Client</span>')
  //  .append('<svg class="new-child-icon"><use xlink:href="#action-icon-expand-card"></use></svg>');
  //$createNewChildClientCard.find('.card-expansion-container,.card-body-secondary-container,.card-stats-container,.card-button-side-container,.card-body-secondary-text')
  //  .remove();

  //$addUserCard = $(nodeTemplate);
  //$addUserCard.find('.card-container')
  //  .addClass('card-100 action-card')
  //  .attr('id', 'add-user-card');
  //$addUserCard.find('.card-body-primary-text')
  //  .append('<svg class="action-card-icon"><use xlink:href="#action-icon-add"></use></svg>')
  //  .append('<span>Add User</span>');
  //$addUserCard.find('.card-expansion-container,.card-body-secondary-container,.card-stats-container,.card-button-side-container,.card-body-secondary-text')
  //  .remove();


  $('.tooltip').tooltipster();

});