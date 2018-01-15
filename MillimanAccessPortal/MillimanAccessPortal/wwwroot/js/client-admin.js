/* global domainValRegex, emailValRegex */

var nodeTemplate = $('script[data-template="node"]').html();
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
    $('#client-form #Name').focus();
  });
}

/**
 * Set the client form as read only
 * @return {undefined}
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
 * @return {undefined}
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
    $('#save-changes-button,#undo-changes-button').hide();
  } else {
    $('#form-buttons-new').show();
    $('#form-buttons-edit').hide();
  }
  $clientForm.find('.selectized').each(function enable() {
    this.selectize.enable();
  });
  $clientForm.find('#Name').focus();
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
 * @return {undefined}
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
 * @return {undefined}
 */
function resetValidation() {
  $('#client-form').validate().resetForm();
  $('.field-validation-error > span').remove();
}

/**
 * Find the set of client form input elements whose values have been modified
 * If an input element did not have an original value, then it is considered
 * to be modified only if the current value is not blank.
 * @return {jQuery} modifiedInputs
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
 * @return {undefined}
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
 * @return {undefined}
 */
function clearUserList() {
  $('#client-user-list > li').remove();
  $('#expand-user-icon,#collapse-user-icon,#add-user-icon').hide();
}

/**
 * Reset all client form input elements to their pre-modified values
 * @return {undefined}
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
  $('#save-changes-button,#undo-changes-button').hide();
}

function buildVexDialog(options) {
  if (typeof options !== 'object' || typeof options.callback !== 'function') {
    throw new Error('buildVexDialog(options) requires options.callback.');
  }
  vex.dialog.open({
    unsafeMessage: '<span class="vex-custom-message">' + options.message + '</span>',
    buttons: $.map(options.buttons, function buildButton(element) {
      return element.type(element.text, options.color);
    }),
    input: options.input || '',
    callback: options.callback
  });
  $('.vex-content')
    .prepend('<div class="vex-title-wrapper"><h3 class="vex-custom-title ' + options.color + '">' + options.title + '</h3></div>');
}

/**
 * Create a dialog box to confirm a discard action
 * @param {function} callback Executed if the user selects YES
 * @return {undefined}
 */
function confirmDiscardDialog(callback) {
  buildVexDialog({
    title: 'Discard Changes',
    message: 'Would you like to discard unsaved changes?',
    buttons: [
      { type: vex.dialog.buttons.yes, text: 'Discard' },
      { type: vex.dialog.buttons.no, text: 'Continue Editing' }
    ],
    color: 'blue',
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
 * @return {undefined}
 */
function confirmResetDialog(callback) {
  buildVexDialog({
    title: 'Reset Form',
    message: 'Would you like to discard unsaved changes?',
    buttons: [
      { type: vex.dialog.buttons.yes, text: 'Discard' },
      { type: vex.dialog.buttons.no, text: 'Continue Editing' }
    ],
    color: 'blue',
    callback: function onSelect(result) {
      if (result) {
        callback();
      }
    }
  });
}

/**
 * Create a dialog box to confirm user removal
 * @param {function} callback Executed if the user selects YES
 * @return {undefined}
 */
function confirmRemoveDialog(name, callback) {
  buildVexDialog({
    title: 'Reset Form',
    message: 'Remove <strong>' + name + '</strong> from the selected client?',
    buttons: [
      { type: vex.dialog.buttons.yes, text: 'Remove' },
      { type: vex.dialog.buttons.no, text: 'Cancel' }
    ],
    color: 'red',
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
 * @return {undefined}
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
  $('#user-list')
    .find('.card-container[data-user-id="' + userId + '"]')
    .find('.card-user-role-indicator')
    .hide()
    .filter(function anyElevated() { return elevatedRoles(userRoles); })
    .show();
}

/**
 * Render user node by using string substitution on a userNodeTemplate
 * @param  {Number} client Client to which the user belongs
 * @param  {Object} user   User object to render
 * @return {undefined}
 */
function renderUserNode(client, user) {
  var $template = $(nodeTemplate.toString());

  $template.find('.card-container')
    .attr('data-search-string', (user.FirstName + ' ' + user.LastName + '|' + user.UserName + '|' + user.Email).toUpperCase())
    .attr('data-client-id', client.ClientEntity.Id)
    .attr('data-user-id', user.Id);
  $template.find('.card-body-primary-container .card-body-primary-text')
    .html((user.FirstName && user.LastName) ?
      user.FirstName + ' ' + user.LastName :
      user.UserName);
  $template.find('.card-body-primary-container .card-body-secondary-text').first()
    .html(user.UserName || '');
  $template.find('.card-body-primary-container .card-body-secondary-text').last()
    .html(user.Email + ' (email)').filter(function sameAsUsername() {
      return user.UserName === user.Email;
    })
    .remove();
  $template.find('.card-stats-container,.card-button-delete,.card-button-edit,.card-button-new-child')
    .remove();

  // generate id's for toggles
  $template.find('.switch-container')
    .map(function applyName(i, element) {
      var $element = $(element);
      var name = (
        'user-role-' +
        $element.closest('.card-container')
          .attr('data-user-id') +
        '-' +
        $element.find('.toggle-switch-checkbox')
          .attr('data-role-enum')
      );
      $element.find('.toggle-switch-checkbox')
        .attr({
          name: name,
          id: name
        });
      $element.find('label.toggle-switch-label')
        .attr('for', name);
      return $element;
    });

  $.each(user.UserRoles, function displayRoles(index, roleAssignment) {
    $template.find('input[data-role-enum=' + roleAssignment.RoleEnum + ']')
      .prop('checked', roleAssignment.IsAssigned)
      .click(userCardRoleToggleClickHandler);
  });

  if (!client.CanManage) {
    $template.find('.icon-container,.card-button-remove-user').remove();
    $template.find('.card-container,.toggle-switch-checkbox').attr('disabled', '');
  }

  $('#client-user-list').append($template);
  updateUserRoleIndicator(user.Id, user.UserRoles);
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
    renderUserNode(client, user);
  });
  eligibleUsers = client.EligibleUsers;
  $('div.card-button-remove-user').click(function onClick(event) {
    event.stopPropagation();
    userCardRemoveClickHandler($(this).closest('.card-container'));
  });
  $('#user-list .card-body-main-container,.card-button-expansion')
    .click(function toggleCard(event) {
      event.stopPropagation();
      $(this).closest('.card-container')
        .find('div.card-expansion-container')
        .attr('maximized', function toggle(index, attr) {
          return attr === '' ? null : '';
        });
      showRelevantUserActionIcons();
    });
  showRelevantUserActionIcons();

  if (userId) {
    $('[data-user-id="' + userId + '"]').click();
  }

  if (client.CanManage) {
    $('#add-user-icon').show();
    $('#client-user-list').append($addUserCard);
    $('#add-user-card')
      .click(function onClick() {
        addUserClickHandler();
      });
  }
}

/**
 * Perform necessary steps for configuring the new child client form
 * @param {Object} parentClientDiv the div of the parent client
 * @return {undefined}
 */
function setupChildClientForm(parentClientDiv) {
  var parentClientId = parentClientDiv.attr('data-client-id').valueOf();
  var $template = $createNewChildClientCard.clone();
  $template
    .addClass(parentClientDiv.hasClass('card-100') ? 'card-90' : 'card-80')
    .find('.card-body-primary-text')
    .addClass(parentClientDiv.hasClass('card-100') ? 'indent-level-1' : 'indent-level-2');

  clearFormData();
  $('#client-form #ParentClientId').val(parentClientId);
  parentClientDiv.parent().after($template);
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
 * @return {undefined}
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
 * @return {undefined}
 */
function getClientDetail(clientDiv) {
  var clientId = clientDiv.attr('data-client-id').valueOf();

  clearFormData();
  clearUserList();

  if (clientDiv.is('[disabled]')) {
    $('#client-info #edit-client-icon').css('visibility', 'hidden');
  }
  else {
    $('#client-info #edit-client-icon').css('visibility', 'visible');
  }

  $.ajax({
    type: 'GET',
    url: 'ClientAdmin/ClientDetail/' + clientId,
    headers: {
      RequestVerificationToken: $("input[name='__RequestVerificationToken']").val()
    }
  }).done(function onDone(response) {
    populateClientForm(response.ClientEntity);
    renderUserList(response);
  }).fail(function onFail(response) {
    toastr.warning(response.getResponseHeader('Warning'));
  });
}

/**
 * Send an AJAX request to set a user role
 * @param {Number}  userId     UserID of the user whose roll is to be updated
 * @param {Number}  roleEnum   The role to be updated
 * @param {Boolean} isAssigned The value to be assigned to the specified role
 * @return {undefined}
 */
function setUserRole(userId, roleEnum, isAssigned, onResponse) {
  var $cardContainer = $('#user-list .card-container[data-user-id="' + userId + '"]');
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
  hideClientUsers();
  showClientDetails();
}

/**
 * Display the new client form
 * @return {undefined}
 */
function openNewClientForm() {
  clearClientSelection();
  setupClientForm();
  $('#create-new-client-card').attr('selected', '');
  setClientFormWriteable();
  hideClientUsers();
  showClientDetails();
}

/**
 * Handle click events for all client cards and client inserts
 * @param {jQuery} $clickedCard the card that was clicked
 * @return {undefined}
 */
function clientCardClickHandler($clickedCard) {
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
function clientCardDeleteClickHandler($clickedCard) {
  var clientId = $clickedCard.attr('data-client-id').valueOf();
  var clientName = $clickedCard.find('.card-body-primary-text').first().text();
  buildVexDialog({
    title: 'Delete Client',
    message: 'Delete <strong>' + clientName + '</strong>?<br /><br /> This action <strong><u>cannot</u></strong> be undone.',
    buttons: [
      { type: vex.dialog.buttons.yes, text: 'Delete' },
      { type: vex.dialog.buttons.no, text: 'Cancel' }
    ],
    color: 'red',
    callback: function onSelect(confirm) {
      if (confirm) {
        buildVexDialog({
          title: 'Delete Client',
          message: 'Please provide your password to delete <strong>' + clientName + '</strong>.',
          buttons: [
            { type: vex.dialog.buttons.yes, text: 'Delete' },
            { type: vex.dialog.buttons.no, text: 'Cancel' }
          ],
          color: 'red',
          input: [
            '<input name="password" type="password" placeholder="Password" required />'
          ].join(''),
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
 * @return {undefined}
 */
function clientCardEditClickHandler($clickedCard) {
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
 * @return {undefined}
 */
function clientCardCreateNewChildClickHandler($clickedCard) {
  var $clientTree = $('#client-tree');
  /**
   * The clicked card is the same as the selected card if and only if the currently selected card
   * is a client insert ("New Child Client" card) AND the currently selected card is immediately
   * preceded by the clicked card in the client list.
   */
  var sameCard = ($clientTree.find('[selected]').closest('.client-insert').length &&
    $clickedCard[0] === $clientTree.find('[selected]').parent().prev().find('.card-container')[0]);
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
 * @return {undefined}
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
        openNewClientForm();
      }
    });
  } else {
    openNewClientForm();
  }
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
      $('#user-list .toggle-switch-checkbox').removeAttr('disabled');
    }
  );
  $('#user-list .toggle-switch-checkbox').attr('disabled', '');
}

// FIXME: send more appropriate data
function saveNewUser(email) {
  var clientId = $('#client-tree [selected]').attr('data-client-id');
  $.ajax({
    type: 'POST',
    url: 'UserAdmin/SaveNewUser',
    data: {
      UserName: email,
      Email: email,
      MemberOfClientIdArray: [clientId]
    },
    headers: {
      RequestVerificationToken: $("input[name='__RequestVerificationToken']").val()
    }
  }).done(function onDone() {
    openClientCardReadOnly($('#client-tree [data-client-id="' + clientId + '"]'));
    toastr.success('User successfully added');
  }).fail(function onFail(response) {
    toastr.warning(response.getResponseHeader('Warning'));
  });
}

function substringMatcher(users) {
  return function findMatches(query, callback) {
    var matches = [];
    var regex = new RegExp(query, 'i');

    $.each(users, function check(i, user) {
      if (regex.test(user.Email) ||
          regex.test(user.UserName) ||
          regex.test(user.FirstName + ' ' + user.LastName)) {
        matches.push(user);
      }
    });

    callback(matches);
  };
}

function initializeAddUserForm() {
  buildVexDialog({
    title: 'Add User',
    message: 'Please provide a valid email address',
    buttons: [
      { type: vex.dialog.buttons.yes, text: 'Add User' },
      { type: vex.dialog.buttons.no, text: 'Cancel' }
    ],
    color: 'blue',
    input: [
      '<input class="typeahead" name="email" placeholder="Email" required />'
    ].join(''),
    callback: function onSubmit(user) {
      if (emailValRegex.test(user.email)) {
        saveNewUser(user.email);
      } else if (user.email) {
        toastr.warning('Please provide a valid email address');
        return false;
      }
      return true;
    }
  });
  $('.vex-dialog-input .typeahead').typeahead(
    {
      hint: true,
      highlight: true,
      minLength: 1
    },
    {
      name: 'eligibleUsers',
      source: substringMatcher(eligibleUsers),
      display: function display(data) {
        return data.Email;
      },
      templates: {
        suggestion: function suggestion(data) {
          return [
            '<div>',
            data.Email + '',
            (data.UserName !== data.Email) ?
              ' - ' + data.UserName :
              '',
            (data.FirstName && data.LastName) ?
              ' (' + data.FirstName + ' ' + data.LastName + ')' :
              '',
            '</div>'
          ].join('');
        }
      }
    }
  );
}

/**
 * Handle click events for add new user inputs
 * @return {undefined}
 */
function addUserClickHandler() {
  initializeAddUserForm();
}

/**
 * Remove the specified user from the specified client
 * @param  {Number} clientId Client ID
 * @param  {Number} userId   User ID
 * @return {undefined}
 */
function removeUserFromClient(clientId, userId) {
  var userName = $('#user-list [data-user-id="' + userId + '"] .card-body-primary-text').html();
  var clientName = $('#client-tree [data-client-id="' + clientId + '"] .card-body-primary-text').html();
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
    toastr.success('Successfully removed ' + userName + ' from ' + clientName);
  }).fail(function onFail(response) {
    toastr.warning(response.getResponseHeader('Warning'));
  });
}

/**
 * Handle click events for remove user buttons
 * @return {undefined}
 */
function userCardRemoveClickHandler($clickedCard) {
  var userName = $clickedCard.find('.card-body-primary-text').html();
  confirmRemoveDialog(userName, function removeUser() {
    var clientId = $('#client-tree [selected]').attr('data-client-id');
    var userId = $clickedCard.attr('data-user-id');
    removeUserFromClient(clientId, userId);
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
  confirmAndReset(confirmDiscardDialog, function onContinue() {
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

/**
 * Render user node by using string substitution on a clientNodeTemplate
 * @param  {Object} client Client object to render
 * @param  {Number} level  Client indentation level
 * @return {undefined}
 */
function renderClientNode(client, level) {
  var classes = ['card-100', 'card-90', 'card-80'];
  var $template = $(nodeTemplate.toString());

  $template.find('.card-container')
    .addClass(classes[level])
    .attr('data-search-string', (client.ClientModel.ClientEntity.Name + '|' + client.ClientModel.ClientEntity.ClientCode).toUpperCase())
    .attr('data-client-id', client.ClientModel.ClientEntity.Id)
    .removeAttr('data-user-id');
  $template.find('.card-body-secondary-container')
    .remove();
  $template.find('.card-body-primary-container .card-body-primary-text')
    .addClass('indent-level-' + level)
    .html(client.ClientModel.ClientEntity.Name);
  $template.find('.card-body-primary-container .card-body-secondary-text')
    .html(client.ClientModel.ClientEntity.ClientCode || '')
    .first().remove();
  $template.find('.card-stat-user-count')
    .html(client.ClientModel.AssignedUsers.length);
  $template.find('.card-stat-content-count')
    .html(client.ClientModel.ContentItems.length);
  $template.find('.card-button-remove-user,.card-expansion-container,.card-button-bottom-container')
    .remove();

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
function renderClientTree(clientTreeList, clientId) {
  var $clientTreeList = $('#client-tree-list');
  $clientTreeList.empty();
  clientTreeList.forEach(function render(rootClient) {
    renderClientNode(rootClient, 0);
    $clientTreeList.append('<li class="hr width-100pct"></li>');
  });
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
    clearFormData();
    renderClientTree(response.ClientTreeList, response.RelevantClientId);
    toastr.success(clientName + ' was successfully deleted.');
  }).fail(function onFail(response) {
    toastr.warning(response.getResponseHeader('Warning'));
  });
}

/**
 * Send an AJAX request to delete a client
 * @return {undefined}
 */
function getClientTree(clientId) {
  $.ajax({
    type: 'GET',
    url: 'ClientAdmin/ClientFamilyList/'
  }).done(function onDone(response) {
    populateProfitCenterDropDown(response.AuthorizedProfitCenterList);
    renderClientTree(response.ClientTreeList, clientId || response.RelevantClientId);
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
      renderClientTree(response.ClientTreeList, response.RelevantClientId);
      toastr.success(successResponse);
    }).fail(function onFail(response) {
      toastr.warning(response.getResponseHeader('Warning'));
    });
  }
}

/**
 * Filter the client tree by a string
 * @param {String} searchString the string to filter by
 * @return {undefined}
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
 * @return {undefined}
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
  $('#add-user-icon').click(addUserClickHandler);
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

  // Construct static cards
  $createNewClientCard = $(nodeTemplate);
  $createNewClientCard.find('.card-container')
    .addClass('card-100 action-card')
    .attr('id', 'create-new-client-card');
  $createNewClientCard.find('.card-body-primary-text')
    .append('<i class="fa fa-plus"></i>')
    .append('<span>New Client</span>');
  $createNewClientCard.find('.card-expansion-container,.card-body-secondary-container,.card-stats-container,.card-button-side-container,.card-body-secondary-text')
    .remove();

  $createNewChildClientCard = $(nodeTemplate);
  $createNewChildClientCard
    .addClass('client-insert');
  $createNewChildClientCard.find('.card-container')
    .addClass('flex-container flex-row-no-wrap items-align-center');
  $createNewChildClientCard.find('.card-body-main-container')
    .addClass('content-item-flex-1');
  $createNewChildClientCard.find('.card-body-primary-text')
    .html('New Sub-Client');
  $createNewChildClientCard.find('.card-container')
    .append('<i class="fa fa-fw fa-2x fa-chevron-right"></i>');
  $createNewChildClientCard.find('.card-expansion-container,.card-body-secondary-container,.card-stats-container,.card-button-side-container,.card-body-secondary-text')
    .remove();

  $addUserCard = $(nodeTemplate);
  $addUserCard.find('.card-container')
    .addClass('card-100 action-card')
    .attr('id', 'add-user-card');
  $addUserCard.find('.card-body-primary-text')
    .append('<i class="fa fa-plus"></i>')
    .append('<span>Add User</span>');
  $addUserCard.find('.card-expansion-container,.card-body-secondary-container,.card-stats-container,.card-button-side-container,.card-body-secondary-text')
    .remove();

  // TODO: find a better place for this
  $('#client-form').find(':input,select')
    .change(function onChange() {
      if (findModifiedInputs().length) {
        $('#save-changes-button,#undo-changes-button').show();
      } else {
        $('#save-changes-button,#undo-changes-button').hide();
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
      buildVexDialog({
        title: 'Invalid Input',
        message: 'The Approved Email Domain List only accepts the email domain (e.g. <i>username@</i><strong><u>domain.com</u></strong>)',
        buttons: [
          {
            type: vex.dialog.buttons.yes,
            text: 'OK'
          }
        ],
        color: 'blue',
        callback: function onAlert() {
          $('#AcceptedEmailDomainList-selectized').val(input);
          $('#client-form #AcceptedEmailDomainList')[0].selectize.unlock();
          $('#client-form #AcceptedEmailDomainList')[0].selectize.focus();
        }
      });
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
      buildVexDialog({
        title: 'Invalid Input',
        message: 'The Approved Email Address Exception List only accepts valid email addresses (e.g. <strong><u>username@domain.com</u></strong>)',
        buttons: [
          { type: vex.dialog.buttons.yes, text: 'OK' }
        ],
        color: 'blue',
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
