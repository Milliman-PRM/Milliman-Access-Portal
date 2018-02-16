/* global
    domainValRegex, emailValRegex,
    card, dialog, shared
 */

var ajaxStatus = {
  getClientDetail: -1
};
var smallSpinner = '<div class="spinner-small"></div>';
var eligibleUsers;
var SHOW_DURATION = 50;

/**
 * Remove all client insert elements.
 * While this function removes all client inserts, there should never be more
 * than one client insert present at a time.
 * @return {undefined}
 */
function removeClientInserts() {
  $('#client-tree .insert').remove();
}

/**
 * Clear 'selected' and 'editing' status from all card containers.
 * @return {undefined}
 */
function clearClientSelection() {
  $('.card-container').removeAttr('editing selected');
}

/**
 * Hide the client info and client users panes
 * @return {undefined}
 */
function hideClientDetails() {
  $('#client-info').hide(SHOW_DURATION);
  $('#client-users').hide(SHOW_DURATION);
}

/**
 * Hide the client users pane
 * @return {undefined}
 */
function hideClientUsers() {
  $('#client-users').hide(SHOW_DURATION);
}

/**
 * Show client detail components and focus the first form element.
 * @return {undefined}
 */
function showClientDetails() {
  var $clientPanes = $('#client-info');
  if ($('#client-tree [selected]').attr('data-client-id')) {
    $clientPanes = $clientPanes.add($('#client-users'));
  }
  $clientPanes.show(SHOW_DURATION, function onShown() {
    $('#client-info form.admin-panel-content #Name').focus();
  });
}

/**
 * Set the client form as read only
 * @return {undefined}
 */
function setClientFormReadOnly() {
  var $clientForm = $('#client-info form.admin-panel-content');
  $('#client-info .action-icon-edit').show();
  $('#client-info .action-icon-cancel').hide();
  $clientForm.find(':input').attr('readonly', '');
  $clientForm.find(':input,select').attr('disabled', '');
  $clientForm.find('.form-button-container').add('button').hide();
  $clientForm.find('.selectized').each(function disable() {
    this.selectize.disable();
  });
}

/**
 * Set the client form as writeable
 * @return {undefined}
 */
function setClientFormWriteable() {
  var $clientForm = $('#client-info form.admin-panel-content');
  $('#client-info .action-icon-edit').hide();
  $('#client-info .action-icon-cancel').show();
  $clientForm.find(':input').removeAttr('readonly');
  $clientForm.find(':input,select').removeAttr('disabled');
  $clientForm.find('.form-button-container').add('button').hide();
  $clientForm.find('.edit-form-button-container').show();
  $clientForm.find('.selectized').each(function enable() {
    this.selectize.enable();
  });
  $clientForm.find('#Name').focus();
}

function setButtonSubmitting($button, text) {
  $button.attr('data-original-text', $button.html());
  $button.html(text || 'Submitting');
  $button.append(smallSpinner);
}

function unsetButtonSubmitting($button) {
  $button.html($button.attr('data-original-text'));
}

/**
 * Populate client form
 * @param  {Object} clientEntity The client to be used to populate the client form
 * @return {undefined}
 */
function populateClientForm(response) {
  var clientEntity = response.ClientEntity;
  var $clientForm = $('#client-info form.admin-panel-content');
  $clientForm.find(':input,select').removeAttr('data-original-value');
  $clientForm.find('#ProfitCenterId option[temporary-profitcenter]').remove();
  $.each(clientEntity, function populate(key, value) {
    var field = $clientForm.find('#' + key);
    if (field.is('#ProfitCenterId')) {
      if (!field.find('option[value="' + value + '"]').length) {
        field.append($('<option temporary-profitcenter />')
          .val(clientEntity.ProfitCenterId)
          .text(clientEntity.ProfitCenter.Name + ' (' + clientEntity.ProfitCenter.ProfitCenterCode + ')'));
      }
      field.val(value).change();
    } else if (field.hasClass('selectize-custom-input')) {
      field[0].selectize.clear();
      field[0].selectize.clearOptions();
      $.each(value, function addItem(index, item) {
        field[0].selectize.addOption({ value: item, text: item });
        field[0].selectize.addItem(item);
      });
    } else {
      field.val(value);
    }
    field.attr('data-original-value', value);
  });
}

/**
 * Populate the Profit Center input
 * @param {Array.<{Id: Number, Name: String, Code: String}>} profitCenterList
 * @return {undefined}
 */
function populateProfitCenterDropDown(profitCenterList) {
  $('#ProfitCenterId option:not(option[value = ""])').remove();
  $.each(profitCenterList, function appendProfitCenter() {
    $('#ProfitCenterId').append($('<option />').val(this.Id).text(this.Name + ' (' + this.Code + ')'));
  });
}

/**
 * Clear all user cards from the client user list
 * @return {undefined}
 */
function clearUserList() {
  $('#client-users ul.admin-panel-content > li').remove();
  $('#client-users .action-icon').hide();
}

/**
 * Create a dialog box if there are modified inputs
 * If there are modified inputs and the user selects YES, or if there are no
 * modified inputs, then the form is reset and onContinue is executed.
 * Otherwise, nothing happens.
 * @param {function} confirmDialog Confirmation dialog function
 * @param {function} onContinue Executed if no inputs are modified or the user selects YES
 * @return {undefined}
 */
function confirmAndReset(Dialog, onContinue) {
  if (shared.modifiedInputs($('#client-info')).length) {
    new Dialog(function onConfirm() {
      shared.resetForm($('#client-info'));
      if (typeof onContinue === 'function') onContinue();
    }).open();
  } else {
    shared.resetForm($('#client-info'));
    if (typeof onContinue === 'function') onContinue();
  }
}

/*
shared.confirmAndContinue = function ($panel, onContinue) {
  if (shared.modifiedInputs($panel).length) {
    new dialog.ResetConfirmationDialog(onContinue).open();
  }
};
*/

/**
 * Determine whether the specified user role assignments are considered elevated
 * @param  {Object} userRoles The user roles to check, taken from AJAX response object
 * @return {Boolean}          Whether the user role assignments are elevated
 */
function elevatedRoles(userRoles) {
  return !!$.grep(userRoles, function isElevatedRole(role) {
    // FIXME: Definition of 'elevated role' should not live here
    return [1, 3, 4].some(function matchesRole(elevatedRole) {
      return role.RoleEnum === elevatedRole;
    });
  }).filter(function isAssigned(role) {
    return role.IsAssigned;
  }).length;
}

/**
 * Show the role indicator if the specified user role assignments are considered elevated
 * Otherwise, hide the role indicator.
 * @param  {Number} userId    Associated user ID of the role indicator to update.
 * @param  {Array} userRoles  Array of user roles to check against
 * @return {undefined}
 */
function updateUserRoleIndicator(userId, userRoles) {
  $('#client-users ul.admin-panel-content')
    .find('.card-container[data-user-id="' + userId + '"]')
    .find('.card-user-role-indicator')
    .hide()
    .filter(function anyElevated() { return elevatedRoles(userRoles); })
    .show();
}

/**
 * Send an AJAX request to set a user role
 * @param {Number}  userId     UserID of the user whose roll is to be updated
 * @param {Number}  roleEnum   The role to be updated
 * @param {Boolean} isAssigned The value to be assigned to the specified role
 * @return {undefined}
 */
function setUserRole(userId, roleEnum, isAssigned, onResponse) {
  var $cardContainer = $('#client-users ul.admin-panel-content .card-container[data-user-id="' + userId + '"]');
  var postData = {
    ClientId: $('#client-tree [selected]').attr('data-client-id'),
    UserId: userId,
    RoleEnum: roleEnum,
    IsAssigned: isAssigned
  };

  $.ajax({
    type: 'POST',
    url: 'ClientAdmin/SetUserRoleInClient',
    data: postData,
    headers: {
      RequestVerificationToken: $("input[name='__RequestVerificationToken']").val()
    }
  }).done(function onDone(response) {
    var modifiedRole;
    // Set checkbox states to match the response
    $.each(response, function setToggle(index, roleAssignment) {
      $cardContainer.find('input[data-role-enum=' + roleAssignment.RoleEnum + ']')
        .prop('checked', roleAssignment.IsAssigned);
    });
    updateUserRoleIndicator(postData.UserId, response);
    // Filter response to get the role that was set by the request
    modifiedRole = response.filter(function filter(responseRole) {
      return responseRole.RoleEnum.toString() === postData.RoleEnum;
    })[0];
    toastr.success($cardContainer.find('.card-body-primary-text').html() + ' was ' + (modifiedRole.IsAssigned ? 'set' : 'unset') + ' as ' + modifiedRole.RoleDisplayValue);
    onResponse();
  }).fail(function onFail(response) {
    toastr.warning(response.getResponseHeader('Warning'));
    onResponse();
  });
}

/**
 * Handle click events for user role toggles
 * @param  {Event} event The event to handle
 * @return {undefined}
 */
function userCardRoleToggleClickHandler(event) {
  var $clickedInput = $(event.target);
  event.preventDefault();

  setUserRole(
    $clickedInput.closest('.card-container').attr('data-user-id'),
    $clickedInput.attr('data-role-enum'),
    $clickedInput.prop('checked'),
    function onDone() {
      $('#client-users ul.admin-panel-content .toggle-switch-checkbox').removeAttr('disabled');
    }
  );
  $('#client-users ul.admin-panel-content .toggle-switch-checkbox').attr('disabled', '');
}

/**
 * Render user node by using string substitution on a userNodeTemplate
 * @param  {Number} client Client to which the user belongs
 * @param  {Object} user   User object to render
 * @return {undefined}
 */
function renderUserNode(client, user) {
  var $card = new card.UserCard(
    user,
    client.ClientEntity,
    userCardRoleToggleClickHandler,
    userCardRemoveClickHandler
  );
  $card.readonly = !client.CanManage;
  $('#client-users ul.admin-panel-content').append($card.build());
  updateUserRoleIndicator(user.Id, user.UserRoles);
}

/**
 * Render user list for a client
 * @param  {object} client Client whose user list is to be rendered
 * @param  {Number} userId ID of a user to be expanded
 * @return {undefined}
 */
function renderUserList(response) {
  var client = response;
  var $clientUserList = $('#client-users ul.admin-panel-content');
  $clientUserList.empty();
  client.AssignedUsers.forEach(function render(user) {
    renderUserNode(client, user);
  });
  $clientUserList.find('.tooltip').tooltipster();
  eligibleUsers = client.EligibleUsers;

  if (client.CanManage) {
    $('#add-user-icon').show();
    $('#client-users ul.admin-panel-content').append(new card.AddUserActionCard(addUserClickHandler).build());
  }
}

/**
 * Perform necessary steps for configuring the new child client form
 * @param {Object} parentClientDiv the div of the parent client
 * @return {undefined}
 */
function setupChildClientForm(parentClientDiv) {
  var parentClientId = parentClientDiv.attr('data-client-id').valueOf();
  var $template = new card.AddChildInsertCard(parentClientDiv.hasClass('card-100') ? 1 : 2).build();

  shared.clearForm($('#client-info'));
  $('#client-info form.admin-panel-content #ParentClientId').val(parentClientId);
  parentClientDiv.parent().after($template);
  parentClientDiv.parent().next().find('div.card-container')
    .click(function onClick() {
      // TODO: move this to a function
      confirmAndReset(dialog.DiscardConfirmationDialog, function () {
        clearClientSelection();
        removeClientInserts();
        hideClientDetails();
      });
    });

  $('#client-info .form-button-container').add('button').hide();
  $('#client-info .new-form-button-container').show();
}

/**
 * Perform necessary steps for configuring the new client form
 * @return {undefined}
 */
function setupClientForm() {
  var $clientForm = $('#client-info form.admin-panel-content');
  shared.clearForm($('#client-info'));
  $clientForm.find('.form-button-container').add('button').hide();
  $clientForm.find('.new-form-button-container').show();
}

/**
 * Repopulate client form with details for the provided client
 * @param {Object} clientDiv the div for whom data will be retrieved
 * @return {undefined}
 */
function getClientDetail(clientDiv) {
  var clientId = clientDiv.data('client-id');

  shared.clearForm($('#client-info'));
  $('#client-info .loading-wrapper').show();

  clearUserList();
  $('#client-users .loading-wrapper').show();

  ajaxStatus.getClientDetail = clientId;
  $.ajax({
    type: 'GET',
    url: 'ClientAdmin/ClientDetail',
    data: clientDiv.data(),
    headers: {
      RequestVerificationToken: $("input[name='__RequestVerificationToken']").val()
    }
  }).done(function onDone(response) {
    if (ajaxStatus.getClientDetail !== clientId) return;
    populateClientForm(response);
    $('#client-info .loading-wrapper').hide();
    renderUserList(response);
    $('#client-users .loading-wrapper').hide();
  }).fail(function onFail(response) {
    if (ajaxStatus.getClientDetail !== clientId) return;
    $('#client-info .loading-wrapper').hide();
    $('#client-users .loading-wrapper').hide();
    toastr.warning(response.getResponseHeader('Warning'));
  });
}

/**
 * Display client card details
 * @param  {jQuery} $clientCard The .card-container element to open
 * @return {undefined}
 */
function openClientCardReadOnly($clientCard) {
  removeClientInserts();
  clearClientSelection();
  $clientCard.attr('selected', '');
  setClientFormReadOnly();
  getClientDetail($clientCard);
  showClientDetails();
}

/**
 * Allow editing of client card details
 * @param  {jQuery} $clientCard The .card-container element to editing
 * @return {undefined}
 */
function openClientCardWriteable($clientCard) {
  removeClientInserts();
  clearClientSelection();
  $clientCard.attr({ selected: '', editing: '' });
  getClientDetail($clientCard);
  setClientFormWriteable();
  showClientDetails();
}

/**
 * Display the new child client form
 * @param  {jQuery} $parentCard The .card-container element that corresponds to the parent client
 *                              of the new child client
 * @return {undefined}
 */
function openNewChildClientForm($parentCard) {
  removeClientInserts();
  clearClientSelection();
  setClientFormWriteable();
  setupChildClientForm($parentCard);
  $parentCard.parent().next('li').find('div.card-container')
    .attr({ selected: '', editing: '' });
  hideClientUsers();
  showClientDetails();
}

/**
 * Display the new client form
 * @return {undefined}
 */
function openNewClientForm() {
  clearClientSelection();
  setClientFormWriteable();
  setupClientForm();
  $('#new-client-card').attr('selected', '');
  hideClientUsers();
  showClientDetails();
}

/**
 * Handle click events for all client cards and client inserts
 * @param {jQuery} $clickedCard the card that was clicked
 * @return {undefined}
 */
function clientCardClickHandler() {
  var $clickedCard = $(this);
  var $clientTree = $('#client-tree ul.admin-panel-content');
  var sameCard = ($clickedCard[0] === $clientTree.find('[selected]')[0]);
  if ($clientTree.has('[selected]').length) {
    confirmAndReset(dialog.DiscardConfirmationDialog, function () {
      if (sameCard) {
        clearClientSelection();
        hideClientDetails();
      } else {
        openClientCardReadOnly($clickedCard);
      }
    });
  } else {
    openClientCardReadOnly($clickedCard);
  }
}

/**
 * Handle click events for all client card delete buttons
 * @param  {jQuery} $clickedCard the card that was clickedCard
 * @return {undefined}
 */
function clientCardDeleteClickHandler(event) {
  var $clickedCard = $(this).closest('.card-container');
  var clientId = $clickedCard.attr('data-client-id');
  var clientName = $clickedCard.find('.card-body-primary-text').first().text();
  event.stopPropagation();
  new dialog.DeleteClientDialog(
    clientName,
    clientId,
    function (password, callback) {
      if (password) {
        setButtonSubmitting($('.vex-first'), 'Deleting');
        $('.vex-dialog-button').attr('disabled', '');
        deleteClient(clientId, clientName, password, callback);
      } else if (password === '') {
        toastr.warning('Please enter your password to proceed');
        return false;
      } else {
        toastr.info('Deletion was canceled');
      }
      return true;
    }
  ).open();
}

/**
 * Handle click events for all client card edit buttons
 * @param {jQuery} $clickedCard the card that was clicked
 * @return {undefined}
 */
function clientCardEditClickHandler(event) {
  var $clickedCard = $(this).closest('.card-container');
  var $clientTree = $('#client-tree');
  var sameCard = ($clickedCard[0] === $clientTree.find('[selected]')[0]);
  event.stopPropagation();
  if ($clientTree.has('[editing]').length) {
    if (!sameCard) {
      confirmAndReset(dialog.DiscardConfirmationDialog, function () {
        openClientCardWriteable($clickedCard);
      });
    }
  } else {
    openClientCardWriteable($clickedCard);
  }
}

/**
 * Handle click events for all client card new child buttons
 * @param {jQuery} $clickedCard the card that was clicked
 * @return {undefined}
 */
function clientCardCreateNewChildClickHandler(event) {
  var $clickedCard = $(this).closest('.card-container');
  var $clientTree = $('#client-tree');
  /**
   * The clicked card is the same as the selected card if and only if the currently selected card
   * is a client insert ("New Child Client" card) AND the currently selected card is immediately
   * preceded by the clicked card in the client list.
   */
  var sameCard = ($clientTree.find('[selected]').is('.insert') &&
    $clickedCard[0] === $clientTree.find('[selected]').parent().prev().find('.card-container')[0]);
  event.stopPropagation();
  if ($clientTree.has('[editing]').length) {
    if (!sameCard) {
      confirmAndReset(dialog.DiscardConfirmationDialog, function () {
        openNewChildClientForm($clickedCard);
      });
    }
  } else {
    openNewChildClientForm($clickedCard);
  }
}

function userCardRemoveClickHandler(event) {
  var $clickedCard = $(this).closest('.card-container');
  var userName = $clickedCard.find('.card-body-primary-text').html();
  event.stopPropagation();
  new dialog.RemoveUserDialog(userName, function removeUser(value, callback) {
    var clientId = $('#client-tree [selected]').attr('data-client-id');
    var userId = $clickedCard.attr('data-user-id');
    removeUserFromClient(clientId, userId, callback);
  }).open();
}

/**
 * Handle click events for the create new client card
 * @return {undefined}
 */
function newClientClickHandler() {
  var $clientTree = $('#client-tree');
  var sameCard = ($('#new-client-card')[0] === $clientTree.find('[selected]')[0]);
  if ($clientTree.has('[selected]').length) {
    confirmAndReset(dialog.DiscardConfirmationDialog, function () {
      if (sameCard) {
        clearClientSelection();
        hideClientDetails();
      } else {
        if ($('.insert').length) {
          removeClientInserts();
        }
        openNewClientForm();
      }
    });
  } else {
    openNewClientForm();
  }
}

/**
 * Send an AJAX request to save a new user
 * @param  {String} email Email address of the user
 * @return {undefined}
 */
function saveNewUser(username, email, callback) {
  var clientId = $('#client-tree [selected]').attr('data-client-id');
  $.ajax({
    type: 'POST',
    url: 'ClientAdmin/SaveNewUser',
    data: {
      UserName: username || email,
      Email: email,
      MemberOfClientIdArray: [clientId]
    },
    headers: {
      RequestVerificationToken: $("input[name='__RequestVerificationToken']").val()
    }
  }).done(function onDone() {
    openClientCardReadOnly($('#client-tree [data-client-id="' + clientId + '"]'));
    if (typeof callback === 'function') callback();
    toastr.success('User successfully added');
  }).fail(function onFail(response) {
    if (typeof callback === 'function') callback();
    toastr.warning(response.getResponseHeader('Warning'));
  });
}

/**
 * Handle click events for add new user inputs
 * @return {undefined}
 */
function addUserClickHandler() {
  new dialog.AddUserDialog(
    eligibleUsers,
    function (user, callback) {
      var singleMatch = 0;
      shared.userSubstringMatcher(eligibleUsers)(user, function (matches) {
        singleMatch = matches.length;
      });
      if (singleMatch) {
        setButtonSubmitting($('.vex-first'), 'Adding');
        $('.vex-dialog-button').attr('disabled', '');
        saveNewUser(user, null, callback);
      } else if (emailValRegex.test(user)) {
        setButtonSubmitting($('.vex-first'), 'Adding');
        $('.vex-dialog-button').attr('disabled', '');
        saveNewUser(null, user, callback);
      } else if (user) {
        toastr.warning('Please provide a valid email address');
        return false;
      }
      return true;
    }
  ).open();
}

/**
 * Remove the specified user from the specified client
 * @param  {Number} clientId Client ID
 * @param  {Number} userId   User ID
 * @return {undefined}
 */
function removeUserFromClient(clientId, userId, callback) {
  var userName = $('#client-users ul.admin-panel-content [data-user-id="' + userId + '"] .card-body-primary-text').html();
  var clientName = $('#client-tree [data-client-id="' + clientId + '"] .card-body-primary-text').html();
  setButtonSubmitting($('.vex-first'), 'Removing');
  $.ajax({
    type: 'POST',
    url: 'ClientAdmin/RemoveUserFromClient',
    data: {
      ClientId: clientId,
      userId: userId
    },
    headers: {
      RequestVerificationToken: $("input[name='__RequestVerificationToken']").val()
    }
  }).done(function onDone(response) {
    renderUserList(response);
    callback();
    toastr.success('Successfully removed ' + userName + ' from ' + clientName);
  }).fail(function onFail(response) {
    callback();
    toastr.warning(response.getResponseHeader('Warning'));
  });
}

/**
 * Handle click events for the client form edit icon
 * @return {undefined}
 */
function editIconClickHandler() {
  setClientFormWriteable();
}

/**
 * Handle click events for the client form cancel icon
 * @return {undefined}
 */
function cancelIconClickHandler() {
  confirmAndReset(dialog.DiscardConfirmationDialog, function () {
    if ($('#client-tree [selected]').attr('data-client-id')) {
      $('#client-tree [editing]').removeAttr('editing');
      setClientFormReadOnly();
    } else {
      clearClientSelection();
      removeClientInserts();
      hideClientDetails();
    }
  });
}

function renderClientNode(client, level) {
  var $card = new card.ClientCard(
    client.ClientModel.ClientEntity,
    client.ClientModel.AssignedUsers.length,
    client.ClientModel.ContentItems.length,
    level,
    clientCardClickHandler,
    !client.Children.length && clientCardDeleteClickHandler,
    clientCardEditClickHandler,
    level < 2 && clientCardCreateNewChildClickHandler
  );
  $card.readonly = !client.ClientModel.CanManage;
  $('#client-tree ul.admin-panel-content').append($card.build());

  // Render child nodes
  client.Children.forEach(function (child) {
    renderClientNode(child, level + 1);
  });
}

/**
 * Render client tree recursively and attach event handlers
 * @param  {Number} clientId ID of the client card to click after render
 * @return {undefined}
 */
function renderClientTree(clientTreeList, clientId) {
  var $clientTreeList = $('#client-tree ul.admin-panel-content');
  $clientTreeList.empty();
  clientTreeList.forEach(function render(rootClient) {
    renderClientNode(rootClient, 0);
    $clientTreeList.append('<li class="hr width-100pct"></li>');
  });
  $clientTreeList.find('.tooltip').tooltipster();

  // TODO: Consider applying this to other cards and buttons as well
  $clientTreeList.find('.card-container,.card-button-background')
    .mousedown(function onMousedown(event) {
      event.preventDefault();
    });

  if (clientId) {
    $('[data-client-id="' + clientId + '"]').click();
  }
  if ($('#client-tree .action-icon-add').length) {
    $clientTreeList.append(new card.AddClientActionCard(newClientClickHandler).build());
  }
}

/**
 * Send an AJAX request to delete a client
 * @param  {Number} clientId   ID of the client to delete
 * @param  {String} clientName Name of the client to delete
 * @param  {String} password   User's password
 * @return {undefined}
 */
function deleteClient(clientId, clientName, password, callback) {
  $.ajax({
    type: 'DELETE',
    url: 'ClientAdmin/DeleteClient',
    data: {
      Id: clientId,
      Password: password
    },
    headers: {
      RequestVerificationToken: $("input[name='__RequestVerificationToken']").val()
    }
  }).done(function onDone(response) {
    shared.clearForm($('#client-info'));
    renderClientTree(response.ClientTreeList, response.RelevantClientId);
    callback();
    toastr.success(clientName + ' was successfully deleted.');
  }).fail(function onFail(response) {
    callback();
    toastr.warning(response.getResponseHeader('Warning'));
  });
}

/**
 * Send an AJAX request to get the client tree
 * @return {undefined}
 */
function getClientTree(clientId) {
  $('#client-tree .loading-wrapper').show();
  $.ajax({
    type: 'GET',
    url: 'ClientAdmin/ClientFamilyList/'
  }).done(function onDone(response) {
    populateProfitCenterDropDown(response.AuthorizedProfitCenterList);
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
 * Send an AJAX request to create or edit a client
 * @return {undefined}
 */
function submitClientForm() {
  var $clientForm = $('#client-info form.admin-panel-content');
  var $button;
  var clientId;
  var clientName;
  var urlAction;
  var successResponse;
  if ($clientForm.valid()) {
    clientId = $clientForm.find('#Id').val();
    clientName = $clientForm.find('#Name').val();
    urlAction = 'ClientAdmin/';

    if (clientId) {
      urlAction += 'EditClient';
      successResponse = clientName + ' was successfully updated';
      $button = $('.edit-form-button-container .submit-button');
    } else {
      urlAction += 'SaveNewClient';
      successResponse = clientName + ' was successfully created';
      $button = $('.new-form-button-container .submit-button');
    }

    setButtonSubmitting($button);
    $.ajax({
      type: 'POST',
      url: urlAction,
      data: $clientForm.serialize(),
      headers: {
        RequestVerificationToken: $("input[name='__RequestVerificationToken']").val()
      }
    }).done(function onDone(response) {
      unsetButtonSubmitting($button);
      renderClientTree(response.ClientTreeList, response.RelevantClientId);
      toastr.success(successResponse);
    }).fail(function onFail(response) {
      unsetButtonSubmitting($button);
      toastr.warning(response.getResponseHeader('Warning'));
    });
  }
}

$(document).ready(function onReady() {
  getClientTree();

  $('#client-tree .action-icon-add').click(newClientClickHandler);
  $('#client-info .action-icon-edit').click(editIconClickHandler);
  $('#client-info .action-icon-cancel').click(cancelIconClickHandler);
  $('.action-icon-expand').click(shared.expandAll);
  $('.action-icon-collapse').click(shared.collapseAll);
  $('#client-users .action-icon-add').click(addUserClickHandler);
  $('.submit-button').click(submitClientForm);
  $('.new-form-button-container .reset-button').click(function () {
    confirmAndReset(dialog.ResetConfirmationDialog);
  });
  $('.edit-form-button-container .reset-button').click(function () {
    confirmAndReset(dialog.DiscardConfirmationDialog);
  });

  $('.admin-panel-searchbar').keyup(shared.filterTree);
  $('.tooltip').tooltipster();

  // TODO: find a better place for this
  $('#client-info form.admin-panel-content').find(':input,select')
    .change(function onChange() {
      if (shared.modifiedInputs($('#client-info')).length) {
        $('.form-button-container button').show();
      } else {
        $('.form-button-container button').hide();
      }
    });

  $('#client-info form.admin-panel-content #AcceptedEmailDomainList').selectize({
    plugins: ['remove_button'],
    persist: false,
    create: function onCreate(input) {
      if (input.match(domainValRegex)) {
        return {
          value: input,
          text: input
        };
      }

      toastr.warning('Please enter a valid domain name (e.g. domain.com)');

      $('#AcceptedEmailDomainList-selectized').val(input);
      $('#client-info form.admin-panel-content #AcceptedEmailDomainList')[0].selectize.unlock();
      $('#client-info form.admin-panel-content #AcceptedEmailDomainList')[0].selectize.focus();

      return {};
    }
  });

  $('#client-info form.admin-panel-content #AcceptedEmailAddressExceptionList').selectize({
    plugins: ['remove_button'],
    delimiter: ',',
    persist: false,
    create: function onCreate(input) {
      if (input.match(emailValRegex)) {
        return {
          value: input,
          text: input
        };
      }
      toastr.warning('Please enter a valid email address (e.g. username@domain.com)');
      return {};
    }
  });
});
