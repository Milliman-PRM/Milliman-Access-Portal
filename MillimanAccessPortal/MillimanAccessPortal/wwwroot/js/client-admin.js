/* global domainValRegex, emailValRegex */

var clientNodeTemplate = $('script[data-template="clientNode"]').html();
var childNodePlaceholder = $('script[data-template="childNodePlaceholder"]').html();
var clientCard = $('script[data-template="createNewClientCard"]').html();
var userNodeTemplate = $('script[data-template="userNode"]').html();
var clientTree = {};
var SHOW_DURATION = 50;

/**
 * Remove all client insert elements.
 * While this function removes all client inserts, there should never be more
 * than one client insert present at a time.
 * @returns {undefined}
 */
function removeClientInserts() {
  $('#client-tree li.client-insert').remove();
}

/**
 * Clear 'selected' and 'editing' status from all card containers.
 * @returns {undefined}
 */
function clearClientSelection() {
  $('.card-container').removeAttr('editing selected');
}

/**
 * Hide the client info and client users panes
 * @returns {undefined}
 */
function hideClientDetails() {
  $('#client-info').hide(SHOW_DURATION);
  $('#client-users').hide(SHOW_DURATION);
}

/**
 * Hide the client users pane
 * @returns {undefined}
 */
function hideClientUsers() {
  $('#client-users').hide(SHOW_DURATION);
}

/**
 * Show client detail components and focus the first form element.
 * @returns {undefined}
 */
function showClientDetails() {
  var $clientPanes = $('#client-info');
  if ($('#client-tree [selected]').attr('data-client-id')) {
    $clientPanes = $clientPanes.add($('#client-users'));
  }
  $clientPanes.show(SHOW_DURATION, function onShown() {
    $('#client-form #Name').focus();
  });
}

/**
 * Set the client form as read only
 * @returns {undefined}
 */
function setClientFormReadOnly() {
  var $clientForm = $('#client-form');
  $('#edit-client-icon').show();
  $('#cancel-edit-client-icon').hide();
  $clientForm.find(':input').attr('readonly', '');
  $clientForm.find(':input,select').attr('disabled', '');
  $clientForm.find('#form-buttons-new').hide();
  $clientForm.find('#form-buttons-edit').hide();
  $clientForm.find('.selectized').each(function disable() {
    this.selectize.disable();
  });
}

/**
 * Set the client form as writeable
 * @returns {undefined}
 */
function setClientFormWriteable() {
  var $clientForm = $('#client-form');
  $('#edit-client-icon').hide();
  $('#cancel-edit-client-icon').show();
  $clientForm.find(':input').removeAttr('readonly');
  $clientForm.find(':input,select').removeAttr('disabled');
  if ($('#client-tree [selected]').attr('data-client-id')) {
    $('#form-buttons-new').hide();
    $('#form-buttons-edit').show();
    $('#undo-changes-button').hide();
  } else {
    $('#form-buttons-new').show();
    $('#form-buttons-edit').hide();
  }
  $clientForm.find('.selectized').each(function enable() {
    this.selectize.enable();
  });
}

/**
 * Populate client form
 * @param  {Object} clientEntity The client to be used to populate the client form
 * @return {undefined}
 */
function populateClientForm(clientEntity) {
  var $clientForm = $('#client-form');
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
 * @returns {undefined}
 */
function populateProfitCenterDropDown(profitCenterList) {
  $('#ProfitCenterId option:not(option[value = ""])').remove();
  $.each(profitCenterList, function appendProfitCenter() {
    $('#ProfitCenterId').append($('<option />').val(this.Id).text(this.Name + ' (' + this.Code + ')'));
  });
}

/**
 * Show or hide collapse/expand icons based on how many user cards are maximized
 * @return {undefined}
 */
function showRelevantUserActionIcons() {
  $('#collapse-user-icon').hide().filter(function anyMaximized() {
    return $('div.card-expansion-container[maximized]').length;
  }).show();
  $('#expand-user-icon').hide().filter(function anyMinimized() {
    return $('div.card-expansion-container:not([maximized])').length;
  }).show();
}

/**
 * Expand all user cards and adjust user action icons accordingly
 * @return {undefined}
 */
function expandAllUsers() {
  $('#client-user-list').find('div.card-expansion-container').attr('maximized', '');
  showRelevantUserActionIcons();
}

/**
 * Collapse all user cards and adjust user action icons accordingly
 * @return {undefined}
 */
function collapseAllUsers() {
  $('#client-user-list').find('div.card-expansion-container[maximized]').removeAttr('maximized');
  showRelevantUserActionIcons();
}

/**
 * Reset client form validation and remove validation messages
 * @returns {undefined}
 */
function resetValidation() {
  $('#client-form').validate().resetForm();
  $('.field-validation-error > span').remove();
}

/**
 * Find the set of client form input elements whose values have been modified
 * If an input element did not have an original value, then it is considered
 * to be modified only if the current value is not blank.
 * @returns {jQuery} modifiedInputs
 */
function findModifiedInputs() {
  return $('#client-form')
    .find('input[name!="__RequestVerificationToken"][type!="hidden"],select')
    .not('div.selectize-input input')
    .map(function compareValue() {
      return ($(this).val() === ($(this).attr('data-original-value') || '') ? null : this);
    });
}

/**
 * Clear all client form input elements, resulting in a blank form
 * @returns {undefined}
 */
function clearFormData() {
  var $clientForm = $('#client-form');
  $clientForm.find('.selectized').each(function clear() {
    this.selectize.clear();
    this.selectize.clearOptions();
  });
  $clientForm.find('input[name!="__RequestVerificationToken"],select')
    .not('div.selectize-input input')
    .attr('data-original-value', '').val('');
  resetValidation();
}

/**
 * Clear all user cards from the client user list
 * @returns {undefined}
 */
function clearUserList() {
  $('#client-user-list > li').remove();
  $('#expand-user-icon,#collapse-user-icon').hide();
}

/**
 * Reset all client form input elements to their pre-modified values
 * @returns {undefined}
 */
function resetFormData() {
  var $modifiedInputs = findModifiedInputs();
  $modifiedInputs.each(function resetValue() {
    if ($(this).is('.selectized')) {
      this.selectize.setValue($(this).attr('data-original-value').split(','));
    } else {
      $(this).val($(this).attr('data-original-value'));
    }
  });
  resetValidation();
}

/**
 * Create a dialog box to confirm a discard action
 * @param {function} callback Executed if the user selects YES
 * @returns {undefined}
 */
function confirmDiscardDialog(callback) {
  vex.dialog.confirm({
    message: 'Do you want to discard unsaved changes?',
    buttons: [
      $.extend({}, vex.dialog.buttons.YES, { text: 'Discard', className: 'green-button' }),
      $.extend({}, vex.dialog.buttons.NO, { text: 'Continue Editing', className: 'link-button' })
    ],
    callback: function onSelect(result) {
      if (result) {
        callback();
      }
    }
  });
}

/**
 * Create a dialog box to confirm a reset action
 * @param {function} callback Executed if the user selects YES
 * @returns {undefined}
 */
function confirmResetDialog(callback) {
  vex.dialog.confirm({
    message: 'Do you want to reset the new client form?',
    buttons: [
      $.extend({}, vex.dialog.buttons.YES, { text: 'Reset', className: 'green-button' }),
      $.extend({}, vex.dialog.buttons.NO, { text: 'Continue Editing', className: 'link-button' })
    ],
    callback: function onSelect(result) {
      if (result) {
        callback();
      }
    }
  });
}

/**
 * Create a dialog box if there are modified inputs
 * If there are modified inputs and the user selects YES, or if there are no
 * modified inputs, then the form is reset and onContinue is executed.
 * Otherwise, nothing happens.
 * @param {function} confirmDialog Confirmation dialog function
 * @param {function} onContinue Executed if no inputs are modified or the user selects YES
 * @returns {undefined}
 */
function confirmAndReset(confirmDialog, onContinue) {
  if (findModifiedInputs().length) {
    confirmDialog(function onConfirm() {
      resetFormData();
      if (typeof onContinue === 'function') onContinue();
    });
  } else {
    resetFormData();
    if (typeof onContinue === 'function') onContinue();
  }
}

/**
 * Render user node by using string substitution on a userNodeTemplate
 * @param  {Number} clientId ID of the client to which the user belongs
 * @param  {Object} user     User object to render
 * @return {undefined}
 */
function renderUserNode(clientId, user) {
  var $template = $(userNodeTemplate
    .replace(/{{clientId}}/g, clientId)
    .replace(/{{id}}/g, user.Id)
    .replace(/{{name}}/g, user.FirstName + ' ' + user.LastName)
    .replace(/{{username}}/g, user.UserName)
    .replace(/{{email}}/g, user.UserName !== user.Email ? user.Email : '{{email}}')
    .toString());

  $template.find('div.card-container[data-search-string]')
    .attr('data-search-string', (user.FirstName + ' ' + user.LastName + '|' + user.UserName + '|' + user.Email).toUpperCase());
  $template.find('.card-body-secondary-text:contains("{{email}}")')
    .remove();

  // if (!client.CanManage) {
  //     $('.icon-container', $template).remove();
  //     $('.card-container', $template).addClass('disabled');
  // }

  $('#client-user-list').append($template);
}

/**
 * Render user list for a client
 * @param  {object} client Client whose user list is to be rendered
 * @param  {Number} userId ID of a user to be expanded
 * @return {undefined}
 */
function renderUserList(client, userId) {
  $('#client-user-list').empty();
  client.AssignedUsers.forEach(function render(user) {
    renderUserNode(client.ClientEntity.Id, user);
  });
  $('div.card-button-remove-user').click(function onClick(event) {
    // TODO: Handle remove user click event
    event.stopPropagation();
  });
  $('div[data-client-id][data-user-id]').click(function toggleCard(event) {
    event.stopPropagation();
    $(this).find('div.card-expansion-container').attr('maximized', function toggle(index, attr) {
      return attr === '' ? null : '';
    });
    showRelevantUserActionIcons();
  });
  showRelevantUserActionIcons();

  if (userId) {
    $('[data-user-id="' + userId + '"]').click();
  }
}

/**
 * Perform necessary steps for configuring the new child client form
 * @param {Object} parentClientDiv the div of the parent client
 * @returns {undefined}
 */
function setupChildClientForm(parentClientDiv) {
  var parentClientId = parentClientDiv.attr('data-client-id').valueOf();
  var template = childNodePlaceholder.replace(/{{class}}/g, parentClientDiv.hasClass('card-100') ? 'card-90' : 'card-80');

  clearFormData();
  $('#client-form #ParentClientId').val(parentClientId);
  parentClientDiv.parent().after(template);
  parentClientDiv.parent().next().find('div.card-container')
    .click(function onClick() {
      // TODO: move this to a function
      confirmAndReset(confirmDiscardDialog, function onContinue() {
        clearClientSelection();
        removeClientInserts();
        hideClientDetails();
      });
    });

  $('#client-form #form-buttons-edit').hide();
  $('#client-form #form-buttons-new').show();
}

/**
 * Perform necessary steps for configuring the new client form
 * @returns {undefined}
 */
function setupClientForm() {
  var $clientForm = $('#client-form');
  clearFormData();
  $clientForm.find('#form-buttons-edit').hide();
  $clientForm.find('#form-buttons-new').show();
}

/**
 * Repopulate client form with details for the provided client
 * @param {Object} clientDiv the div for whom data will be retrieved
 * @returns {undefined}
 */
function getClientDetail(clientDiv) {
  var clientId = clientDiv.attr('data-client-id').valueOf();

  clearFormData();
  clearUserList();
  $.ajax({
    type: 'GET',
    url: 'ClientAdmin/ClientDetail/' + clientId,
    headers: {
      RequestVerificationToken: $("input[name='__RequestVerificationToken']").val()
    }
  }).done(function onDone(response) {
    populateClientForm(response.ClientEntity);
    renderUserList(response);
    if (clientDiv.is('[disabled]')) { // FIXME: should be elsewhere??
      $('#client-info #edit-client-icon').hide();
    }
  }).fail(function onFail(response) {
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
  setupChildClientForm($parentCard);
  $parentCard.parent().next('li').find('div.card-container')
    .attr({ selected: '', editing: '' });
  setClientFormWriteable();
  showClientDetails();
}

/**
 * Handle click events for all client cards and client inserts
 * @param {jQuery} $clickedCard the card that was clicked
 * @returns {undefined}
 */
function cardClickHandler($clickedCard) {
  var $clientTree = $('#client-tree');
  var sameCard = ($clickedCard[0] === $clientTree.find('[selected]')[0]);
  if ($clientTree.has('[selected]').length) {
    confirmAndReset(confirmDiscardDialog, function onContinue() {
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
function cardDeleteClickHandler($clickedCard) {
  var clientId = $clickedCard.attr('data-client-id').valueOf();
  var clientName = $clickedCard.find('.card-body-primary-text').first().text();
  vex.dialog.confirm({
    unsafeMessage: 'Do you want to delete <strong>' + clientName + '</strong>?<br /><br /> This action <strong><u>cannot</u></strong> be undone.',
    buttons: [
      $.extend({}, vex.dialog.buttons.YES, { text: 'Delete', className: 'red-button' }),
      $.extend({}, vex.dialog.buttons.NO, { text: 'Cancel', className: 'link-button' })
    ],
    callback: function onSelect(confirm) {
      if (confirm) {
        vex.dialog.prompt({
          unsafeMessage: 'Please provide your password to delete <strong>' + clientName + '</strong>.',
          input: [
            '<input name="password" type="password" placeholder="Password" required />'
          ].join(''),
          buttons: [
            $.extend({}, vex.dialog.buttons.YES, { text: 'Delete', className: 'red-button' }),
            $.extend({}, vex.dialog.buttons.NO, { text: 'Cancel', className: 'link-button' })
          ],
          callback: function onSelectWithPassword(password) {
            if (password) {
              deleteClient(clientId, clientName, password);
            } else if (password === '') {
              toastr.warning('Please enter your password to proceed');
              return false;
            } else {
              toastr.info('Deletion was canceled');
            }
            return true;
          }
        });
      } else {
        toastr.info('Deletion was canceled');
      }
    }
  });
}

/**
 * Handle click events for all client card edit buttons
 * @param {jQuery} $clickedCard the card that was clicked
 * @returns {undefined}
 */
function cardEditClickHandler($clickedCard) {
  var $clientTree = $('#client-tree');
  var sameCard = ($clickedCard[0] === $clientTree.find('[selected]')[0]);
  if ($clientTree.has('[editing]').length) {
    if (!sameCard) {
      confirmAndReset(confirmDiscardDialog, function onContinue() {
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
 * @returns {undefined}
 */
function cardCreateNewChildClickHandler($clickedCard) {
  var $clientTree = $('#client-tree');
  var sameCard = ($clickedCard[0] === $clientTree.find('[selected]').parent().prev().find('.card-container')[0]);
  if ($clientTree.has('[editing]').length) {
    if (!sameCard) {
      confirmAndReset(confirmDiscardDialog, function onContinue() {
        openNewChildClientForm($clickedCard);
      });
    }
  } else {
    openNewChildClientForm($clickedCard);
  }
}

/**
 * Handle click events for the create new client card
 * @returns {undefined}
 */
function createNewClientClickHandler() {
  var $clientTree = $('#client-tree');
  var sameCard = ($('#create-new-client-card')[0] === $clientTree.find('[selected]')[0]);
  if ($clientTree.has('[selected]').length) {
    confirmAndReset(confirmDiscardDialog, function onContinue() {
      if (sameCard) {
        clearClientSelection();
        hideClientDetails();
      } else {
        if ($('.client-insert').length) {
          removeClientInserts();
        }
        clearClientSelection();
        setupClientForm();
        $('#create-new-client-card').attr('selected', '');
        setClientFormWriteable();
        hideClientUsers();
        showClientDetails();
      }
    });
  } else {
    clearClientSelection();
    setupClientForm();
    $('#create-new-client-card').attr('selected', '');
    setClientFormWriteable();
    hideClientUsers();
    showClientDetails();
  }
}

/**
 * Handle click events for the client form edit icon
 * @returns {undefined}
 */
function editIconClickHandler() {
  setClientFormWriteable();
}

/**
 * Handle click events for the client form cancel icon
 * @returns {undefined}
 */
function cancelIconClickHandler() {
  confirmAndReset(confirmDiscardDialog, function onContinue() {
    if ($('#client-tree [selected]').attr('data-client-id')) {
      $('#client-tree [editing]').removeAttr('editing');
      setClientFormReadOnly();
    } else {
      clearClientSelection();
      hideClientDetails();
    }
  });
}

/**
 * Render user node by using string substitution on a clientNodeTemplate
 * @param  {Object} client Client object to render
 * @param  {Number} level  Client indentation level
 * @return {undefined}
 */
function renderClientNode(client, level) {
  var classes = ['card-100', 'card-90'];
  var $template = $(clientNodeTemplate
    .replace(/{{class}}/g, classes[level] || 'card-80')
    .replace(/{{header-level}}/g, (level + 1))
    .replace(/{{id}}/g, client.ClientModel.ClientEntity.Id)
    .replace(/{{name}}/g, client.ClientModel.ClientEntity.Name)
    .replace(/{{clientCode}}/g, client.ClientModel.ClientEntity.ClientCode || '')
    .replace(/{{users}}/g, client.ClientModel.AssignedUsers.length)
    .replace(/{{content}}/g, client.ClientModel.ContentItems.length)
    .toString());

  $template.find('div.card-container[data-search-string]')
    .attr('data-search-string', (client.ClientModel.ClientEntity.Name + '|' + client.ClientModel.ClientEntity.ClientCode).toUpperCase());

  if (!client.ClientModel.CanManage) {
    $template.find('.card-button-side-container').remove();
    $template.find('.card-container').attr('disabled', '');
  }

  // Only include the delete button on client nodes without children
  if (client.Children.length) {
    $template.find('.card-button-delete').remove();
  }

  // Don't include the add child client button on lowest level
  if (level === 2) {
    $template.find('.card-button-new-child').remove();
  }

  $('#client-tree-list').append($template);

  // Render child nodes
  if (client.Children.length) {
    client.Children.forEach(function forEach(childNode) {
      renderClientNode(childNode, level + 1);
    });
  }
}

/**
 * Render client tree recursively and attach event handlers
 * @param  {Number} clientId ID of the client card to click after render
 * @return {undefined}
 */
function renderClientTree(clientId) {
  var $clientTreeList = $('#client-tree-list');
  $clientTreeList.empty();
  clientTree.forEach(function render(rootClient) {
    renderClientNode(rootClient, 0);
    $clientTreeList.append('<li class="hr width-100pct"></li>');
  });
  $clientTreeList.find('div.card-container')
    .click(function onClick() {
      cardClickHandler($(this));
    });
  $clientTreeList.find('div.card-button-delete')
    .click(function onClick(event) {
      event.stopPropagation();
      cardDeleteClickHandler($(this).parents('div[data-client-id]'));
    });
  $clientTreeList.find('div.card-button-edit')
    .click(function onClick(event) {
      event.stopPropagation();
      cardEditClickHandler($(this).parents('div[data-client-id]'));
    });
  $clientTreeList.find('div.card-button-new-child')
    .click(function onClick(event) {
      event.stopPropagation();
      cardCreateNewChildClickHandler($(this).parents('div[data-client-id]'));
    });
  if (clientId) {
    $('[data-client-id="' + clientId + '"]').click();
  }
  if ($('#add-client-icon').length) {
    $clientTreeList.append(clientCard);
    $('#create-new-client-card')
      .click(function onClick() {
        createNewClientClickHandler($(this));
      });
  }
}

/**
 * Send an AJAX request to delete a client
 * @param  {Number} clientId   ID of the client to delete
 * @param  {String} clientName Name of the client to delete
 * @param  {String} password   User's password
 * @return {undefined}
 */
function deleteClient(clientId, clientName, password) {
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
    clientTree = response.ClientTreeList;
    renderClientTree(response.RelevantClientId);
    toastr.success(clientName + ' was successfully deleted.');
  }).fail(function onFail(response) {
    toastr.warning(response.getResponseHeader('Warning'));
  });
}

/**
 * Send an AJAX request to delete a client
 * @return {undefined}
 */
function getClientTree() {
  $.ajax({
    type: 'GET',
    url: 'ClientAdmin/ClientFamilyList/'
  }).done(function onDone(response) {
    clientTree = response.ClientTreeList;
    populateProfitCenterDropDown(response.AuthorizedProfitCenterList);
    renderClientTree(response.RelevantClientId);
  }).fail(function onFail(response) {
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
  var $clientForm = $('#client-form');
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
    } else {
      urlAction += 'SaveNewClient';
      successResponse = clientName + ' was successfully created';
    }

    $.ajax({
      type: 'POST',
      url: urlAction,
      data: $clientForm.serialize(),
      headers: {
        RequestVerificationToken: $("input[name='__RequestVerificationToken']").val()
      }
    }).done(function onDone(response) {
      clientTree = response.ClientTreeList;
      renderClientTree(response.RelevantClientId);
      toastr.success(successResponse);
    }).fail(function onFail(response) {
      toastr.warning(response.getResponseHeader('Warning'));
    });
  }
}

/**
 * Filter the client tree by a string
 * @param {String} searchString the string to filter by
 * @returns {undefined}
 */
function searchClientTree(searchString) {
  $('#client-tree-list').children('.hr').hide();
  $('#client-tree-list div[data-search-string]').each(function forEach(index, element) {
    if ($(element).attr('data-search-string').indexOf(searchString.toUpperCase()) > -1) {
      $(element).show();
      $(element).closest('li').nextAll('li.hr').first()
        .show();
    } else {
      $(element).hide();
    }
  });
}

/**
 * Filter the user list by a string
 * @param {String} searchString the string to filter by
 * @returns {undefined}
 */
function searchUser(searchString) {
  $('#client-user-list div[data-search-string]').each(function forEach(index, element) {
    if ($(element).attr('data-search-string').indexOf(searchString.toUpperCase()) > -1) {
      $(element).show();
    } else {
      $(element).hide();
    }
  });
}

$(document).ready(function onReady() {
  getClientTree();

  $('#add-client-icon').click(createNewClientClickHandler);
  $('#edit-client-icon').click(editIconClickHandler);
  $('#cancel-edit-client-icon').click(cancelIconClickHandler);
  $('#expand-user-icon').click(expandAllUsers);
  $('#collapse-user-icon').click(collapseAllUsers);
  $('#create-new-button').click(submitClientForm);
  $('#save-changes-button').click(submitClientForm);
  $('#reset-form-button').click(function confirmResetAndReset() {
    confirmAndReset(confirmResetDialog);
  });
  $('#undo-changes-button').click(function confirmDiscardAndReset() {
    confirmAndReset(confirmDiscardDialog);
  });

  $('#client-search-box').keyup(function onKeyup() {
    searchClientTree($(this).val());
  });

  $('#user-search-box').keyup(function onKeyup() {
    searchUser($(this).val());
  });

  // TODO: find a better place for this
  $('#client-form').find(':input,select')
    .change(function onChange() {
      if (findModifiedInputs().length) {
        $('#undo-changes-button').show();
      } else {
        $('#undo-changes-button').hide();
      }
    });

  $('#client-form #AcceptedEmailDomainList').selectize({
    plugins: ['remove_button'],
    persist: false,
    create: function onCreate(input) {
      if (input.match(domainValRegex)) {
        return {
          value: input,
          text: input
        };
      }
      vex.dialog.alert({
        unsafeMessage: 'The Approved Email Domain List only accepts the email domain (e.g. <i>username@@</i><strong><u>domain.com</u></strong>)',
        callback: function onAlert() {
          $('#AcceptedEmailDomainList-selectized').val(input);
          $('#client-form #AcceptedEmailDomainList')[0].selectize.unlock();
          $('#client-form #AcceptedEmailDomainList')[0].selectize.focus();
        }
      });
      return {};
    }
  });

  $('#client-form #AcceptedEmailAddressExceptionList').selectize({
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
      vex.dialog.alert({
        unsafeMessage: 'The Approved Email Address Exception List only accepts valid email addresses (e.g. <strong><u>username@domain.com</u></strong>)',
        callback: function onAlert() {
          $('#AcceptedEmailAddressExceptionList-selectized').val(input);
          $('#client-form #AcceptedEmailAddressExceptionList')[0].selectize.unlock();
          $('#client-form #AcceptedEmailAddressExceptionList')[0].selectize.focus();
        }
      });
      return {};
    }
  });
});
