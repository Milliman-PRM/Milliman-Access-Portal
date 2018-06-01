import $ = require('jquery');
import toastr = require('toastr');
import card = require('./card');
import dialog = require('./dialog');
import shared = require('./shared');
import { globalSettings } from './lib-options';
import { Dictionary } from 'lodash';
import { FormBase } from './form/form-base';
import { SubmissionGroup } from './form/form-submission';
import { AccessMode } from './form/form-modes';
require('jquery-mask-plugin');
require('jquery-validation');
require('jquery-validation-unobtrusive');
require('selectize');
require('tooltipster');
require('vex-js');
require('./navbar');

require('bootstrap/scss/bootstrap-reboot.scss');
require('selectize/src/less/selectize.default.less');
require('toastr/toastr.scss');
require('tooltipster/src/css/tooltipster.css');
require('tooltipster/src/css/plugins/tooltipster/sideTip/tooltipster-sideTip.css');
require('../scss/map.scss');

var ajaxStatus: any = {};
var eligibleUsers;
var SHOW_DURATION = 50;
let formObject: FormBase;

function domainRegex() {
  return new RegExp(globalSettings.domainValidationRegex);
}
function emailRegex() {
  return new RegExp(globalSettings.emailValidationRegex);
}

function removeClientInserts() {
  $('#client-tree .insert-card').remove();
}

function clearClientSelection() {
  $('.card-body-container').removeAttr('editing selected');
}

function hideClientDetails() {
  $('#client-info').hide(SHOW_DURATION);
  $('#client-users').hide(SHOW_DURATION);
}

function hideClientUsers() {
  $('#client-users').hide(SHOW_DURATION);
}

function showClientDetails() {
  var $clientPanes = $('#client-info');
  if ($('#client-tree [selected]').attr('data-client-id')) {
    $clientPanes = $clientPanes.add($('#client-users'));
  }
  $clientPanes.show(SHOW_DURATION, function onShown() {
    $('#client-info form.admin-panel-content #Name').focus();
  });
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
    field.change();
  });
  bindForm();
}
function populateProfitCenterDropDown(profitCenterList) {
  $('#ProfitCenterId option:not(option[value = ""])').remove();
  $.each(profitCenterList, function appendProfitCenter() {
    $('#ProfitCenterId').append($('<option />').val(this.Id).text(this.Name + ' (' + this.Code + ')'));
  });
}

function bindForm() {
  const $clientForm = $('#client-info form.admin-panel-content');

  const createClientGroup = new SubmissionGroup<any>(
    undefined, // all sections
    'ClientAdmin/SaveNewClient',
    'POST',
    (response) => {
      renderClientTree(response.ClientTreeList, response.RelevantClientId);
      toastr.success('Created new client');
    },
  );
  const updateClientGroup = new SubmissionGroup<any>(
    undefined, // all sections
    'ClientAdmin/EditClient',
    'POST',
    (response) => {
      renderClientTree(response.ClientTreeList, response.RelevantClientId);
      toastr.success('Updated client');
    },
  );

  formObject = new FormBase();
  formObject.bindToDOM($clientForm[0]);
  formObject.configure([
    {
      groups: [ createClientGroup ],
      name: 'new',
      sparse: false,
    },
    {
      groups: [ updateClientGroup ],
      name: 'edit',
      sparse: false,
    },
  ]);
}

function displayActionPanelIcons(canManage: boolean) {
  $('#client-info .admin-panel-action-icons-container .action-icon').hide();
  if (canManage) {
    if (formObject.accessMode === AccessMode.Read) {
      $('#client-info .admin-panel-action-icons-container .action-icon-edit').show();
    } else if (formObject.accessMode === AccessMode.Write) {
      $('#client-info .admin-panel-action-icons-container .action-icon-cancel').show();
    }
  }
}

function elevatedRoles(userRoles) {
  return !!$.grep(userRoles, function isElevatedRole(role: {RoleEnum: number, IsAssigned: boolean}) {
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

function setUserRole(clientId, userId, roleEnum, isAssigned, onResponse) {
  var $cardContainer = $('#client-users ul.admin-panel-content .card-container[data-user-id="' + userId + '"]');
  var postData = {
    ClientId: clientId,
    UserId: userId,
    RoleEnum: roleEnum,
    IsAssigned: isAssigned
  };

  $.ajax({
    type: 'POST',
    url: 'ClientAdmin/SetUserRoleInClient',
    data: postData,
    headers: {
      RequestVerificationToken: $("input[name='__RequestVerificationToken']").val().toString()
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

function userCardRoleToggleClickHandler(event) {
  var $clickedInput = $(event.target);
  event.preventDefault();

  setUserRole(
    $clickedInput.closest('.card-container').attr('data-client-id'),
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

function setupChildClientForm($parentClientDiv: JQuery<HTMLElement>) {
  const parentClientId = $parentClientDiv.parent().data().clientId;
  const $template = new card.AddChildInsertCard($parentClientDiv.parent().hasClass('card-100') ? 1 : 2).build();

  shared.clearForm($('#client-info'));
  $('#client-info form.admin-panel-content #ParentClientId').val(parentClientId);
  bindForm();
  $parentClientDiv.parent().parent().after($template);
  $parentClientDiv.parent().parent().next().find('div.card-body-container')
    .click(() => {
      shared.confirmAndContinue(dialog.DiscardConfirmationDialog, formObject, () => {
        clearClientSelection();
        removeClientInserts();
        hideClientDetails();
      });
    });
}

// TODO
function setupClientForm() {
  var $clientForm = $('#client-info form.admin-panel-content');
  shared.clearForm($('#client-info'));
  bindForm();
  formObject.submissionMode = 'new';
  formObject.accessMode = AccessMode.Write;
}

function clearUserList() {
  $('#client-users ul.admin-panel-content > li').remove();
  $('#client-users .action-icon').hide();
}

function getClientDetail($clientDiv, accessMode?: AccessMode) {
  const data = $clientDiv.data();
  const clientId = data.clientId;

  $('#client-info .loading-wrapper').show();

  clearUserList();
  $('#client-users .loading-wrapper').show();

  ajaxStatus.getClientDetail = clientId;
  return $.ajax({
    type: 'GET',
    url: 'ClientAdmin/ClientDetail',
    data: data,
    headers: {
      RequestVerificationToken: $("input[name='__RequestVerificationToken']").val().toString()
    },
  }).done((response) => {
    if (ajaxStatus.getClientDetail !== clientId) return;
    populateClientForm(response);
    formObject.submissionMode = 'edit';
    if (accessMode) formObject.accessMode = accessMode;
    displayActionPanelIcons(response.CanManage);
    $('#client-info .loading-wrapper').hide();
    renderUserList(response);
    $('#client-users .loading-wrapper').hide();
  }).fail((response) => {
    if (ajaxStatus.getClientDetail !== clientId) return;
    $('#client-info .loading-wrapper').hide();
    $('#client-users .loading-wrapper').hide();
    toastr.warning(response.getResponseHeader('Warning'));
  });
}

function openClientCardReadOnly($clientCard) {
  removeClientInserts();
  clearClientSelection();
  $clientCard.attr('selected', '');
  getClientDetail($clientCard, AccessMode.Read);
  showClientDetails();
}

function openNewClientForm() {
  clearClientSelection();
  setupClientForm();
  $('#new-client-card').find('div.card-body-container').attr('selected', '');
  hideClientUsers();
  showClientDetails();
}

function clientCardDeleteClickHandler(event) {
  var $clickedCard = $(this).closest('.card-container');
  var clientId = $clickedCard.data().clientId;
  var clientName = $clickedCard.find('.card-body-primary-text').first().text();
  event.stopPropagation();
  new dialog.DeleteClientDialog(
    clientName,
    clientId,
    function (data, callback) {
      if (data.password) {
        shared.showButtonSpinner($('.vex-first'), 'Deleting');
        $('.vex-dialog-button').attr('disabled', '');
        deleteClient(clientId, clientName, data.password, callback);
      } else if (data.password === '') {
        toastr.warning('Please enter your password to proceed');
        return false;
      } else {
        toastr.info('Deletion was canceled');
      }
      return true;
    }
  ).open();
}

function userCardRemoveClickHandler(event) {
  var $clickedCard = $(this).closest('.card-container');
  var userName = $clickedCard.find('.card-body-primary-text').html();
  event.stopPropagation();
  new dialog.RemoveUserDialog(userName, function removeUser(value, callback) {
    var clientId = $clickedCard.attr('data-client-id');
    var userId = $clickedCard.attr('data-user-id');
    removeUserFromClient(clientId, userId, callback);
  }).open();
}

const newClientClickHandler = shared.wrapCardCallback(() => openNewClientForm(), () => formObject);

function saveNewUser(username, email, callback) {
  var clientId = $('#client-tree [selected]').closest('[data-client-id]').attr('data-client-id');
  $.ajax({
    type: 'POST',
    url: 'ClientAdmin/SaveNewUser',
    data: {
      UserName: username || email,
      Email: email,
      MemberOfClientIdArray: [clientId]
    },
    headers: {
      RequestVerificationToken: $("input[name='__RequestVerificationToken']").val().toString()
    }
  }).done(function onDone() {
    openClientCardReadOnly($('#client-tree [data-client-id="' + clientId + '"] .card-body-container'));
    if (typeof callback === 'function') callback();
    toastr.success('User successfully added');
  }).fail(function onFail(response) {
    if (typeof callback === 'function') callback();
    toastr.warning(response.getResponseHeader('Warning'));
  });
}

function addUserClickHandler() {
  new dialog.AddUserDialog(
    eligibleUsers,
    function (data, callback) {
      var singleMatch = 0;
      shared.userSubstringMatcher(eligibleUsers)(data.username, function (matches) {
        singleMatch = matches.length;
      });
      if (singleMatch) {
        shared.showButtonSpinner($('.vex-first'), 'Adding');
        $('.vex-dialog-button').attr('disabled', '');
        saveNewUser(data.username, null, callback);
      } else if (emailRegex().test(data.username)) {
        shared.showButtonSpinner($('.vex-first'), 'Adding');
        $('.vex-dialog-button').attr('disabled', '');
        saveNewUser(null, data.username, callback);
      } else if (data.username) {
        toastr.warning('Please provide a valid email address');
        return false;
      }
      return true;
    }
  ).open();
}

function removeUserFromClient(clientId, userId, callback) {
  var userName = $('#client-users ul.admin-panel-content [data-user-id="' + userId + '"] .card-body-primary-text').html();
  var clientName = $('#client-tree [data-client-id="' + clientId + '"] .card-body-primary-text').html();
  shared.showButtonSpinner($('.vex-first'), 'Removing');
  $.ajax({
    type: 'POST',
    url: 'ClientAdmin/RemoveUserFromClient',
    data: {
      ClientId: clientId,
      userId: userId
    },
    headers: {
      RequestVerificationToken: $("input[name='__RequestVerificationToken']").val().toString()
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

function cancelIconClickHandler() {
  shared.confirmAndContinue(dialog.DiscardConfirmationDialog, formObject, function () {
    if ($('#client-tree [selected]').parent().attr('data-client-id')) {
      $('#client-tree [editing]').removeAttr('editing');
      formObject.accessMode = AccessMode.Read;
      displayActionPanelIcons(true);
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
      [
        populateClientForm,
        () => formObject.submissionMode = 'edit',
        () => formObject.accessMode = AccessMode.Read,
        (response: any) => displayActionPanelIcons(response.CanManage),
        renderUserList,
      ],
    ), () => formObject, 2),
    !client.Children.length && clientCardDeleteClickHandler,
    shared.wrapCardIconCallback(($card) => getClientDetail($card.parent(), AccessMode.Write), () => formObject),
    level < 2 && shared.wrapCardIconCallback(($card) => {
      setupChildClientForm($card);
      formObject.accessMode = AccessMode.Write;
      $card.removeAttr('editing selected');
      $card.parent().parent().next('li').find('div.card-body-container')
        .attr({ selected: '', editing: '' });
      hideClientUsers();
    }, () => formObject, {count: 1, offset: 0}, ($card) => {
      const $selected = $('#client-tree [selected]');
      const $expected = $card.parent().parent().next('li').find('.card-body-container');
      return $selected.length
        && $selected.parent().is('.insert-card')
        && $expected.length
        && $selected[0] === $expected[0];
    }),
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
    $('#client-tree [data-client-id="' + clientId + '"] .card-body-container').click();
  }
  if ($('#client-tree .action-icon-add').length) {
    $clientTreeList.append(new card.AddClientActionCard(newClientClickHandler).build());
  }
}

function deleteClient(clientId, clientName, password, callback) {
  $.ajax({
    type: 'DELETE',
    url: 'ClientAdmin/DeleteClient',
    data: {
      Id: clientId,
      Password: password
    },
    headers: {
      RequestVerificationToken: $("input[name='__RequestVerificationToken']").val().toString()
    }
  }).done(function onDone(response) {
    shared.clearForm($('#client-info'));
    $('#client-users .admin-panel-content').empty();
    hideClientDetails();
    renderClientTree(response.ClientTreeList, response.RelevantClientId);
    callback();
    toastr.success(clientName + ' was successfully deleted.');
  }).fail(function onFail(response) {
    callback();
    toastr.warning(response.getResponseHeader('Warning'));
  });
}

function getClientTree(clientId?) {
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

$(document).ready(function onReady() {
  getClientTree();

  $('#client-tree .action-icon-add').click(newClientClickHandler);
  $('#client-info .action-icon-edit').click(() => {
    formObject.accessMode = AccessMode.Write;
    displayActionPanelIcons(true);
  });
  $('#client-info .action-icon-cancel').click(cancelIconClickHandler);
  $('.action-icon-expand').click(shared.expandAllListener);
  $('.action-icon-collapse').click(shared.collapseAllListener);
  $('#client-users .action-icon-add').click(addUserClickHandler);

  $('.admin-panel-searchbar-tree').keyup(shared.filterTreeListener);
  $('.tooltip').tooltipster();

  // TODO
  $('#client-info form.admin-panel-content #AcceptedEmailDomainList').selectize({
    plugins: ['remove_button'],
    persist: false,
    create: function onCreate(input) {
      if (input.match(domainRegex())) {
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

  // TODO
  $('#client-info form.admin-panel-content #AcceptedEmailAddressExceptionList').selectize({
    plugins: ['remove_button'],
    delimiter: ',',
    persist: false,
    create: function onCreate(input) {
      if (input.match(emailRegex())) {
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
