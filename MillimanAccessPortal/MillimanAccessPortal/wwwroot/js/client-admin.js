/* global shared, dialog, card */

var ajaxStatus = {};
var smallSpinner = '<div class="spinner-small"></div>';
var eligibleUsers;
var SHOW_DURATION = 50;

// TODO: move to shared
function removeClientInserts() {
  $('#client-tree .insert').remove();
}

// TODO: move to shared
function clearClientSelection() {
  $('.card-container').removeAttr('editing selected');
}

// TODO: move to shared
function hideClientDetails() {
  $('#client-info').hide(SHOW_DURATION);
  $('#client-users').hide(SHOW_DURATION);
}

// TODO: move to shared
function hideClientUsers() {
  $('#client-users').hide(SHOW_DURATION);
}

// TODO: move to shared
function showClientDetails() {
  var $clientPanes = $('#client-info');
  if ($('#client-tree [selected]').attr('data-client-id')) {
    $clientPanes = $clientPanes.add($('#client-users'));
  }
  $clientPanes.show(SHOW_DURATION, function onShown() {
    $('#client-info form.admin-panel-content #Name').focus();
  });
}

// TODO: move to shared
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

// TODO: move to shared
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

// TODO: move to shared
function setButtonSubmitting($button, text) {
  $button.attr('data-original-text', $button.html());
  $button.html(text || 'Submitting');
  $button.append(smallSpinner);
}

// TODO: move to shared
function unsetButtonSubmitting($button) {
  $button.html($button.attr('data-original-text'));
}


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
function populateProfitCenterDropDown(profitCenterList) {
  $('#ProfitCenterId option:not(option[value = ""])').remove();
  $.each(profitCenterList, function appendProfitCenter() {
    $('#ProfitCenterId').append($('<option />').val(this.Id).text(this.Name + ' (' + this.Code + ')'));
  });
}


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
function updateUserRoleIndicator(userId, userRoles) {
  $('#client-users ul.admin-panel-content')
    .find('.card-container[data-user-id="' + userId + '"]')
    .find('.card-user-role-indicator')
    .hide()
    .filter(function anyElevated() { return elevatedRoles(userRoles); })
    .show();
}

// TODO: move to shared
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

// TODO: move to shared
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


function setupChildClientForm(parentClientDiv) {
  var parentClientId = parentClientDiv.attr('data-client-id').valueOf();
  var $template = new card.AddChildInsertCard(parentClientDiv.hasClass('card-100') ? 1 : 2).build();

  shared.clearForm($('#client-info'));
  $('#client-info form.admin-panel-content #ParentClientId').val(parentClientId);
  parentClientDiv.parent().after($template);
  parentClientDiv.parent().next().find('div.card-container')
    .click(function onClick() {
      // TODO: move this to a function
      shared.confirmAndContinue($('#client-info'), dialog.DiscardConfirmationDialog, function () {
        clearClientSelection();
        removeClientInserts();
        hideClientDetails();
      });
    });

  $('#client-info .form-button-container').add('button').hide();
  $('#client-info .new-form-button-container').show();
}


function setupClientForm() {
  var $clientForm = $('#client-info form.admin-panel-content');
  shared.clearForm($('#client-info'));
  $clientForm.find('.form-button-container').add('button').hide();
  $clientForm.find('.new-form-button-container').show();
}

// TODO: move to shared
function clearUserList() {
  $('#client-users ul.admin-panel-content > li').remove();
  $('#client-users .action-icon').hide();
}
// TODO: move to shared
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

// TODO: move to shared
function openClientCardReadOnly($clientCard) {
  removeClientInserts();
  clearClientSelection();
  $clientCard.attr('selected', '');
  setClientFormReadOnly();
  getClientDetail($clientCard);
  showClientDetails();
}

// TODO: move to shared
function openClientCardWriteable($clientCard) {
  removeClientInserts();
  clearClientSelection();
  $clientCard.attr({ selected: '', editing: '' });
  getClientDetail($clientCard);
  setClientFormWriteable();
  showClientDetails();
}

// TODO: move to shared
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

// TODO: move to shared
function openNewClientForm() {
  clearClientSelection();
  setClientFormWriteable();
  setupClientForm();
  $('#new-client-card').attr('selected', '');
  hideClientUsers();
  showClientDetails();
}

// TODO: move to shared
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

// TODO: move to shared
function clientCardEditClickHandler(event) {
  var $clickedCard = $(this).closest('.card-container');
  var $clientTree = $('#client-tree');
  var sameCard = ($clickedCard[0] === $clientTree.find('[selected]')[0]);
  event.stopPropagation();
  if ($clientTree.has('[editing]').length) {
    if (!sameCard) {
      shared.confirmAndContinue($('#client-info'), dialog.DiscardConfirmationDialog, function () {
        openClientCardWriteable($clickedCard);
      });
    }
  } else {
    openClientCardWriteable($clickedCard);
  }
}

// TODO: move to shared
function clientCardCreateNewChildClickHandler(event) {
  var $clickedCard = $(this).closest('.card-container');
  var $clientTree = $('#client-tree');

  var sameCard = ($clientTree.find('[selected]').is('.insert') &&
    $clickedCard[0] === $clientTree.find('[selected]').parent().prev().find('.card-container')[0]);
  event.stopPropagation();
  if ($clientTree.has('[editing]').length) {
    if (!sameCard) {
      shared.confirmAndContinue($('#client-info'), dialog.DiscardConfirmationDialog, function () {
        openNewChildClientForm($clickedCard);
      });
    }
  } else {
    openNewChildClientForm($clickedCard);
  }
}

// TODO: move to shared
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

// TODO: move to shared
function newClientClickHandler() {
  var $clientTree = $('#client-tree');
  var sameCard = ($('#new-client-card')[0] === $clientTree.find('[selected]')[0]);
  if ($clientTree.has('[selected]').length) {
    shared.confirmAndContinue($('#client-info'), dialog.DiscardConfirmationDialog, function () {
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

// TODO: move to shared
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

// TODO: move to shared
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

// TODO: move to shared
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

// TODO: move to shared
function cancelIconClickHandler() {
  shared.confirmAndContinue($('#client-info'), dialog.DiscardConfirmationDialog, function () {
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
    shared.wrapCardCallback(shared.get(
      'ClientAdmin/ClientDetail',
      setClientFormReadOnly,
      populateClientForm,
      renderUserList
    ), 2),
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


function renderClientTree(clientTreeList, clientId) {
  var $clientTreeList = $('#client-tree ul.admin-panel-content');
  $clientTreeList.empty();
  clientTreeList.forEach(function render(rootClient) {
    renderClientNode(rootClient, 0);
    $clientTreeList.append('<li class="hr width-100pct"></li>');
  });
  $clientTreeList.find('.tooltip').tooltipster();

  if (clientId) {
    $('[data-client-id="' + clientId + '"]').click();
  }
  if ($('#client-tree .action-icon-add').length) {
    $clientTreeList.append(new card.AddClientActionCard(newClientClickHandler).build());
  }
}

// TODO: move to shared
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

// TODO: move to shared
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

// TODO: move to shared
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
  $('#client-info .action-icon-edit').click(setClientFormWriteable);
  $('#client-info .action-icon-cancel').click(cancelIconClickHandler);
  $('.action-icon-expand').click(shared.expandAll);
  $('.action-icon-collapse').click(shared.collapseAll);
  $('#client-users .action-icon-add').click(addUserClickHandler);
  $('.submit-button').click(submitClientForm);
  $('.new-form-button-container .reset-button').click(function () {
    shared.confirmAndContinue($('#client-info'), dialog.ResetConfirmationDialog);
  });
  $('.edit-form-button-container .reset-button').click(function () {
    shared.confirmAndContinue($('#client-info'), dialog.DiscardConfirmationDialog);
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
