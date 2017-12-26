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
 *
 * @returns {undefined}
 */
function removeClientInserts() {
  $('#client-tree li.client-insert').remove();
}

/**
 * Clear 'selected' and 'editing' status from all card containers.
 *
 * @returns {undefined}
 */
function clearClientSelection() {
  $('.card-container').removeAttr('editing selected');
}

/**
 * Hide the client info and client users panes
 *
 * @returns {undefined}
 */
function hideClientDetails() {
  $('#client-info').hide(SHOW_DURATION);
  $('#client-users').hide(SHOW_DURATION);
}

/**
 * Hide the client users pane
 *
 * @returns {undefined}
 */
function hideClientUsers() {
  $('#client-users').hide(SHOW_DURATION);
}

/**
 * Show client detail components and focus the first form element.
 *
 * @returns {undefined}
 */
function showClientDetails() {
  var $clientPanes = $('#client-info');
  if ($('#client-tree').find('[selected]').attr('data-client-id')) {
    $clientPanes = $clientPanes.add($('#client-users'));
  }
  $clientPanes.show(SHOW_DURATION, function onShown() {
    $('#client-form #Name').focus();
  });
}

/**
 * Set the client form as read only
 *
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
 *
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

function populateClientDetails(ClientEntity) {
  $('#client-form :input, #client-form select').removeAttr('data-original-value');
  $('#client-form #ProfitCenterId option[temporary-profitcenter]').remove();
  $.each(ClientEntity, function forEach(key, value) {
    var i;
    var ctrl = $('#' + key, '#client-info');
    if (ctrl.is('select')) {
      if ($('#client-form #' + key + ' option[value="' + value + '"]').length === 0) {
        $('#' + key).append($('<option temporary-profitcenter />').val(ClientEntity.ProfitCenterId).text(ClientEntity.ProfitCenter.Name + ' (' + ClientEntity.ProfitCenter.ProfitCenterCode + ')'));
      }
      ctrl.val(value).change();
    } else if (ctrl.hasClass('selectize-custom-input')) {
      ctrl[0].selectize.clear();
      ctrl[0].selectize.clearOptions();
      if (value) {
        for (i = 0; i < value.length; i += 1) {
          ctrl[0].selectize.addOption({ value: value[i], text: value[i] });
          ctrl[0].selectize.addItem(value[i]);
        }
      }
    } else {
      ctrl.val(value);
    }
    ctrl.attr('data-original-value', value);
  });
}

/**
 * Populate the Profit Center input
 *
 * @param {Array.<{Id: Number, Name: String, Code: String}>} profitCenterList
 * @returns {undefined}
 */
function populateProfitCenterDropDown(profitCenterList) {
  $('#ProfitCenterId option:not(option[value = ""])').remove();
  $.each(profitCenterList, function appendProfitCenter() {
    $('#ProfitCenterId').append($('<option />').val(this.Id).text(this.Name + ' (' + this.Code + ')'));
  });
}

function toggleExpandCollapse() {
  if ($('div.card-expansion-container:not([maximized])').length > 0) {
    $('#expand-user-icon').show();
  } else {
    $('#expand-user-icon').hide();
  }

  if ($('div.card-expansion-container[maximized]').length > 0) {
    $('#collapse-user-icon').show();
  } else {
    $('#collapse-user-icon').hide();
  }
}

function expandAllUsers() {
  $('#client-user-list div.card-expansion-container').attr('maximized', '');
  toggleExpandCollapse();
}

function collapseAllUsers() {
  $('#client-user-list div.card-expansion-container[maximized]').removeAttr('maximized');
  toggleExpandCollapse();
}

/**
 * Reset client form validation and remove validation messages
 *
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
 *
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
 *
 * @returns {undefined}
 */
function clearFormData() {
  var $clientForm = $('#client-form');
  $clientForm.find('.selectized').each(function clear() {
    this.selectize.clear();
    this.selectize.clearOptions();
  });
  $clientForm.find('input[name!="__RequestVerificationToken"][type!="hidden"],select')
    .not('div.selectize-input input')
    .attr('data-original-value', '').val('');
  resetValidation();
}

/**
 * Clear all user cards from the client user list
 *
 * @returns {undefined}
 */
function clearUserList() {
  $('#client-user-list > li').remove();
  $('#expand-user-icon,#collapse-user-icon').hide();
}

/**
 * Reset all client form input elements to their pre-modified values
 *
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
 *
 * @param {function} callback Executed iff the user selects YES
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
 *
 * @param {function} callback Executed iff the user selects YES
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
 *
 * @param {function} onContinue Executed if no inputs are modified or the user selects YES
 * @returns {undefined}
 */
function confirmDiscardAndReset(onContinue) {
  if (findModifiedInputs().length) {
    confirmDiscardDialog(function onConfirm() {
      resetFormData();
      if (typeof onContinue === 'function') onContinue();
    });
  } else {
    resetFormData();
    if (typeof onContinue === 'function') onContinue();
  }
}

function renderUserNode(clientId, user) {
  var template = userNodeTemplate;
  var $template;

  template = template.replace(/{{clientId}}/g, clientId);
  template = template.replace(/{{id}}/g, user.Id);
  template = template.replace(/{{name}}/g, user.FirstName + ' ' + user.LastName);
  template = template.replace(/{{username}}/g, user.UserName);
  if (user.UserName !== user.Email) {
    template = template.replace(/{{email}}/g, user.Email);
  }

  // convert template to DOM element for jQuery manipulation
  $template = $(template.toString());

  $('div.card-container[data-search-string]', $template).attr(
    'data-search-string',
    (user.FirstName + ' ' + user.LastName + '|' + user.UserName + '|' + user.Email).toUpperCase()
  );

  $('.card-body-secondary-text:contains("{{email}}")', $template).remove();

  // if (!client.CanManage) {
  //     $('.icon-container', $template).remove();
  //     $('.card-container', $template).addClass('disabled');
  // }

  $('#client-user-list').append($template);
}

function renderUserList(client, userId) {
  $('#client-user-list').empty();
  client.AssignedUsers.forEach(function forEach(user) {
    renderUserNode(client.ClientEntity.Id, user);
  });

  $('div.card-button-remove-user').click(function onClick(event) {
    // removeUserFromClient($(this).parents('div[data-client-id][data-user-id]'));
    event.stopPropagation();
  });
  $('div[data-client-id][data-user-id]')
    .click(function toggleCard(event) {
      if ($(this).find('div.card-expansion-container').is('[maximized]')) {
        $(this).find('div.card-expansion-container').removeAttr('maximized');
      } else {
        $(this).find('div.card-expansion-container').attr('maximized', '');
      }
      toggleExpandCollapse();
      event.stopPropagation();
    });

  toggleExpandCollapse();

  if (userId) {
    $('[data-user-id="' + userId + '"]').click();
  }
}

/**
 * Perform necessary steps for configuring the new child client form
 *
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
      confirmDiscardAndReset(function onContinue() {
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
 *
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
 *
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
    populateClientDetails(response.ClientEntity);
    renderUserList(response);
    if (clientDiv.is('[disabled]')) { // FIXME: should be elsewhere??
      $('#client-info #edit-client-icon').hide();
    }
  }).fail(function onFail(response) {
    toastr.warning(response.getResponseHeader('Warning'));
  });
}

function renderClientNode(client, level) {
  var template = clientNodeTemplate;
  var $template;

  switch (level) {
    case 1:
      template = template.replace(/{{class}}/g, 'card-100');
      break;
    case 2:
      template = template.replace(/{{class}}/g, 'card-90');
      break;
    default:
      template = template.replace(/{{class}}/g, 'card-80');
      break;
  }

  template = template.replace(/{{header-level}}/g, (level + 1));
  template = template.replace(/{{id}}/g, client.ClientEntity.Id);
  template = template.replace(/{{name}}/g, client.ClientEntity.Name);
  if (client.ClientEntity.ClientCode) {
    template = template.replace(/{{clientCode}}/g, client.ClientEntity.ClientCode);
  } else {
    template = template.replace(/{{clientCode}}/g, '');
  }
  template = template.replace(/{{users}}/g, client.AssociatedUserCount);
  template = template.replace(/{{content}}/g, client.AssociatedContentCount);

  // convert template to DOM element for jQuery manipulation
  $template = $(template.toString());

  $('div.card-container[data-search-string]', $template).attr(
    'data-search-string',
    (client.ClientEntity.Name + '|' + client.ClientEntity.ClientCode).toUpperCase()
  );

  if (!client.CanManage) {
    $('.card-button-side-container', $template).remove();
    $('.card-container', $template).attr('disabled', '');
  }

  // Only include the delete button on client nodes without children
  if (client.Children.length !== 0) {
    $('.card-button-delete', $template).remove();
  }

  // Don't include the add child client button on lowest level
  if (level === 3) {
    $('.card-button-new-child', $template).remove();
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
 * Handle click events for all client cards and client inserts
 *
 * @param {jQuery} $clickedCard the card that was clicked
 * @returns {undefined}
 */
function cardClickHandler($clickedCard) {
  var $clientTree = $('#client-tree');
  var sameCard = ($clickedCard[0] === $clientTree.find('[selected]')[0]);
  if ($clientTree.has('[selected]').length) {
    confirmDiscardAndReset(function onContinue() {
      if (sameCard) {
        clearClientSelection();
        hideClientDetails();
      } else {
        if ($('.client-insert').length) {
          removeClientInserts();
        }
        clearClientSelection();
        $clickedCard.attr('selected', '');
        setClientFormReadOnly();
        getClientDetail($clickedCard);
        showClientDetails();
      }
    });
  } else {
    clearClientSelection();
    $clickedCard.attr('selected', '');
    setClientFormReadOnly();
    getClientDetail($clickedCard);
    showClientDetails();
  }
}

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
              removeClientNode(clientId, clientName, password);
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
 *
 * @param {jQuery} $clickedCard the card that was clicked
 * @returns {undefined}
 */
function cardEditClickHandler($clickedCard) {
  var $clientTree = $('#client-tree');
  var sameCard = ($clickedCard[0] === $clientTree.find('[selected]')[0]);
  if ($clientTree.has('[editing]').length) {
    if (!sameCard) {
      confirmDiscardAndReset(function onContinue() {
        removeClientInserts();
        clearClientSelection();
        $clickedCard.attr({ selected: '', editing: '' });
        getClientDetail($clickedCard);
        showClientDetails();
      });
    }
  } else {
    clearClientSelection();
    $clickedCard.attr({ selected: '', editing: '' });
    getClientDetail($clickedCard);
    setClientFormWriteable();
    showClientDetails();
  }
}

/**
 * Handle click events for all client card new child buttons
 *
 * @param {jQuery} $clickedCard the card that was clicked
 * @returns {undefined}
 */
function cardCreateNewChildClickHandler($clickedCard) {
  var $clientTree = $('#client-tree');
  var sameCard = ($clickedCard[0] === $clientTree.find('[selected]').parent().prev().find('.card-container')[0]);
  if ($clientTree.has('[editing]').length) {
    if (!sameCard) {
      confirmDiscardAndReset(function onContinue() {
        removeClientInserts();
        clearClientSelection();
        setupChildClientForm($clickedCard);
        $clickedCard.parent().next('li').find('div.card-container')
          .attr({ selected: '', editing: '' });
        showClientDetails();
      });
    }
  } else {
    clearClientSelection();
    setupChildClientForm($clickedCard);
    $clickedCard.parent().next('li').find('div.card-container')
      .attr({ selected: '', editing: '' });
    setClientFormWriteable();
    showClientDetails();
  }
}

/**
 * Handle click events for the create new client card
 *
 * @returns {undefined}
 */
function createNewClientClickHandler() {
  var $clientTree = $('#client-tree');
  var sameCard = ($('#create-new-client-card')[0] === $clientTree.find('[selected]')[0]);
  if ($clientTree.has('[selected]').length) {
    confirmDiscardAndReset(function onContinue() {
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
*
* @returns {undefined}
*/
function editIconClickHandler() {
  setClientFormWriteable();
}

/**
* Handle click events for the client form cancel icon
*
* @returns {undefined}
*/
function cancelIconClickHandler() {
  confirmDiscardAndReset(function onContinue() {
    if ($('#client-tree [selected]').attr('data-client-id')) {
      $('#client-tree [editing]').removeAttr('editing');
      setClientFormReadOnly();
    } else {
      clearClientSelection();
      hideClientDetails();
    }
  });
}

function renderClientTree(clientId) {
  var $clientTreeList = $('#client-tree-list');
  $clientTreeList.empty();
  clientTree.forEach(function do_(rootClient) {
    renderClientNode(rootClient, 1);
    $clientTreeList.append('<li class="hr width-100pct"></li>');
  });
  $clientTreeList.find('div.card-container')
    .click(function onClick() {
      cardClickHandler($(this));
    });
  $('div.card-button-delete')
    .click(function onClick(event) {
      event.stopPropagation();
      cardDeleteClickHandler($(this).parents('div[data-client-id]'));
    });
  $('div.card-button-edit')
    .click(function onClick(event) {
      event.stopPropagation();
      cardEditClickHandler($(this).parents('div[data-client-id]'));
    });
  $('div.card-button-new-child')
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

function removeClientNode(clientId, clientName, password) {
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
    clientTree = response.ClientTree;
    renderClientTree(response.RelevantClientId);
    toastr.success(clientName + ' was successfully deleted.');
  }).fail(function onFail(response) {
    toastr.warning(response.getResponseHeader('Warning'));
  });
}

function getClientTree() {
  $.ajax({
    type: 'GET',
    url: 'ClientAdmin/ClientFamilyList/'
  }).done(function onDone(response) {
    clientTree = response.ClientTree;
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

function submitClientForm(event) {
  var form;
  var clientId;
  var clientName;
  var urlAction;
  var successResponse;
  if ($('#client-form').valid()) {
    event.preventDefault();

    form = $('#client-form');
    clientId = $('#client-form #Id').val();
    clientName = $('#client-form #Name').val();
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
      data: form.serialize(),
      headers: {
        RequestVerificationToken: $("input[name='__RequestVerificationToken']").val()
      }
    }).done(function onDone(response) {
      clientTree = response.ClientTree;
      renderClientTree(response.RelevantClientId);
      toastr.success(successResponse);
      $('#client-tree div.card-container[data-client-id="' + clientId + '"]').click();
    }).fail(function onFail(response) {
      toastr.warning(response.getResponseHeader('Warning'));
    });
  }
}

/**
* Filter the client tree by a string
*
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
*
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
  $('#reset-form-button').click(confirmDiscardAndReset);
  $('#undo-changes-button').click(confirmDiscardAndReset);

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
