/* global domainValRegex, emailValRegex */

var clientNodeTemplate = $('script[data-template="clientNode"]').html();
var childNodePlaceholder = $('script[data-template="childNodePlaceholder"]').html();
var clientCard = $('script[data-template="createNewClientCard"]').html();
var userNodeTemplate = $('script[data-template="userNode"]').html();
var clientTree = {};

function removeClientInserts() {
  $('#client-tree li.client-insert').remove();
}

function clearSelectedClient() {
  $('#client-tree-list div[selected]').removeAttr('selected');
  $('#client-tree-list div[editing]').removeAttr('editing');
}

function hideClientForm() {
  $('#client-info').hide();
  $('#client-users').hide();
}

function clearValidationErrors() {
  $('#client-form .input-validation-error').removeClass('input-validation-error');
  $('#client-form span.field-validation-error > span').remove();
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

function populateProfitCenterDropDown(profitCenters) {
  $('#ProfitCenterId option:not(option[value = ""])').remove();
  $.each(profitCenters, function populateProfitCenter() {
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

function makeFormReadOnly() {
  $('#edit-client-icon').show();
  $('#cancel-edit-client-icon').hide();
  $('#client-form :input').attr('readonly', 'readonly');
  $('#client-form :input, #client-form select').attr('disabled', 'disabled');
  $('#client-form #form-buttons-new').hide();
  $('#client-form #form-buttons-edit').hide();
  $('#client-form #AcceptedEmailDomainList')[0].selectize.disable();
  $('#client-form #AcceptedEmailAddressExceptionList')[0].selectize.disable();
}

function showClientForm() {
  var showTime = 50;
  $('#client-info').show(showTime, function onShow() {
    $('#client-form #Name').focus();
    if ($('#client-form #Id').val()) {
      $('#client-users').show(showTime);
    } else {
      $('#client-users').hide();
    }
  });
}

function makeFormWriteable() {
  $('#edit-client-icon').hide();
  $('#cancel-edit-client-icon').show();
  $('#client-form :input').removeAttr('readonly');
  $('#client-form :input, #client-form select').removeAttr('disabled');
  $('#client-form #AcceptedEmailDomainList')[0].selectize.enable();
  $('#client-form #AcceptedEmailAddressExceptionList')[0].selectize.enable();
}

function clearFormData() {
  $('#client-form #AcceptedEmailDomainList')[0].selectize.clear();
  $('#client-form #AcceptedEmailDomainList')[0].selectize.clearOptions();
  $('#client-form #AcceptedEmailAddressExceptionList')[0].selectize.clear();
  $('#client-form #AcceptedEmailAddressExceptionList')[0].selectize.clearOptions();
  $('#client-form :input:not(input[name="__RequestVerificationToken"]), #client-form select').attr('data-original-value', '');
  $('#client-form :input:not(input[name="__RequestVerificationToken"]), #client-form select').val('');
  clearValidationErrors();
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

  $('div.card-button-remove-user').on('click', function onClick(event) {
    // removeUserFromClient($(this).parents('div[data-client-id][data-user-id]'));
    event.stopPropagation();
  });
  $('div[data-client-id][data-user-id]')
    .on('click', function toggleCard(event) {
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

function newClientFormSetup() {
  removeClientInserts();
  clearFormData();
  clearSelectedClient();
  makeFormWriteable();
  $('#client-tree #create-new-client-card').attr('selected', '');
  $('#client-form #form-buttons-edit').hide();
  $('#client-form #form-buttons-new').show();
  showClientForm();
}

function newChildClientFormSetup(parentClientDiv) {
  var parentClientId;
  var template;

  clearFormData();
  parentClientId = parentClientDiv.attr('data-client-id').valueOf();

  $('#client-form #ParentClientId').val(parentClientId);

  removeClientInserts();
  clearSelectedClient();

  template = childNodePlaceholder;
  if (parentClientDiv.hasClass('card-100')) {
    template = template.replace(/{{class}}/g, 'card-90');
  } else {
    template = template.replace(/{{class}}/g, 'card-80');
  }

  parentClientDiv.parent().after(template);

  makeFormWriteable();
  $('#client-form #form-buttons-edit').hide();
  $('#client-form #form-buttons-new').show();
  showClientForm();
}

function GetClientDetail(clientDiv) {
  var clientId;

  removeClientInserts();
  clientId = clientDiv.attr('data-client-id').valueOf();

  if (clientDiv.is('[selected]') && !clientDiv.is('[editing]')) {
    clearSelectedClient();
    hideClientForm();
    return false;
  }

  $.ajax({
    type: 'GET',
    url: 'ClientAdmin/ClientDetail/' + clientId,
    headers: {
      RequestVerificationToken: $("input[name='__RequestVerificationToken']").val()
    }
  }).done(function onDone(response) {
    clearValidationErrors();
    populateClientDetails(response.ClientEntity);
    renderUserList(response);
    // Change the dom to reflect the selected client
    clearSelectedClient();
    clientDiv.attr('selected', '');
    // Show the form in readonly mode
    makeFormReadOnly();
    if (clientDiv.is('[disabled]')) {
      $('#client-info #edit-client-icon').hide();
    }
    showClientForm();
  }).fail(function onFail(response) {
    toastr.warning(response.getResponseHeader('Warning'));
    hideClientForm();
  });
}

function EditClientDetail(clientDiv) {
  var clientId;

  removeClientInserts();
  clearValidationErrors();
  clientId = clientDiv.attr('data-client-id').valueOf();

  $.ajax({
    type: 'GET',
    url: 'ClientAdmin/ClientDetail/' + clientId,
    headers: {
      RequestVerificationToken: $("input[name='__RequestVerificationToken']").val()
    }
  }).done(function onDone(response) {
    populateClientDetails(response.ClientEntity);
    // Change the dom to reflect the selected client
    clearSelectedClient();
    clientDiv.attr('selected', '');
    clientDiv.attr('editing', '');
    // Show the form in read/write mode
    makeFormWriteable();
    $('#client-form #form-buttons-new').hide();
    $('#client-form #form-buttons-edit').show();
    $('#undo-changes-button').hide();
    showClientForm();
    $('#client-form :input, #client-form select')
      .on('change', function checkFormChanges() {
        if ($(this).value !== $(this).attr('data-original-value')) {
          $('#undo-changes-button').show();
        }
      });
  }).fail(function onFail(response) {
    toastr.warning(response.getResponseHeader('Warning'));
    hideClientForm();
  });
}

function toggleEditExistingClient() {
  EditClientDetail($('div[selected]'));
}

function deleteClient(clientDiv) {
  var clientId = clientDiv.attr('data-client-id').valueOf();
  var clientName = clientDiv.find('.card-body-primary-text').first().text();

  vex.dialog.confirm({
    unsafeMessage: 'Do you want to delete <strong>' + clientName + '</strong>?<br /><br /> This action <strong><u>cannot</u></strong> be undone.',
    buttons: [
      $.extend({}, vex.dialog.buttons.YES, { text: 'Delete', className: 'red-button' }),
      $.extend({}, vex.dialog.buttons.NO, { text: 'Cancel', className: 'link-button' })
    ],
    callback: function onSelect(result) {
      if (result) {
        vex.dialog.prompt({
          unsafeMessage: 'Please provide your password to delete <strong>' + clientName + '</strong>.',
          input: [
            '<input name="password" type="password" placeholder="Password" required />'
          ].join(''),
          buttons: [
            $.extend({}, vex.dialog.buttons.YES, { text: 'Delete', className: 'red-button' }),
            $.extend({}, vex.dialog.buttons.NO, { text: 'Cancel', className: 'link-button' })
          ],
          callback: function onSelectInner(resultInner) {
            if (resultInner) {
              removeClientNode(clientId, clientName, resultInner);
            } else if (resultInner === '') {
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

function renderClientTree(clientId) {
  $('#client-tree-list').empty();
  clientTree.forEach(function forEach(rootClient) {
    renderClientNode(rootClient, 1);
    $('#client-tree-list').append('<li class="hr width-100pct"></li>');
  });
  $('#client-tree-list div.card-container')
    .on('click', function getClientDetailClickHandler() {
      GetClientDetail($(this));
    });
  $('div.card-button-delete')
    .on('click', function deleteClientClickHandler(event) {
      deleteClient($(this).parents('div[data-client-id]'));
      event.stopPropagation();
    });
  $('div.card-button-edit')
    .on('click', function editClientDetailClickHandler(event) {
      EditClientDetail($(this).parents('div[data-client-id]'));
      event.stopPropagation();
    });
  $('div.card-button-new-child')
    .on('click', function newClientFormSetupClickHandler(event) {
      newChildClientFormSetup($(this).parents('div[data-client-id]'));
      event.stopPropagation();
    });
  if (clientId) {
    $('[data-client-id="' + clientId + '"]').click();
  }
  if ($('#add-client-icon').length) {
    $('#client-tree-list').append(clientCard);
    $('#create-new-client-card').click(newClientFormSetup);
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
    clearFormData();
    hideClientForm();
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
      hideClientForm();
      clearFormData();
      clientTree = response.ClientTree;
      renderClientTree(response.RelevantClientId);
      toastr.success(successResponse);
      $('#client-tree div.card-container[data-client-id="' + clientId + '"]').click();
    }).fail(function onFail(response) {
      toastr.warning(response.getResponseHeader('Warning'));
    });
  }
}

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

function resetNewClientForm() {
  $('#client-form :input:not(input[name="__RequestVerificationToken"], input[type="hidden"]), #client-form select')
    .each(function resetClientForm() {
      if ($(this).val() !== '') {
        confirmResetDialog(function confirm() {
          $('#client-form .input-validation-error').removeClass('input-validation-error');
          $('#client-form span.field-validation-error > span').remove();
          clearFormData();
        });
      }
    });
}

function undoChangesEditClientForm() {
  confirmDiscardDialog(function confirm() {
    var clientId = $('#client-form #Id').val();
    EditClientDetail($('#client-tree div[data-client-id="' + clientId + '"]'));
  });
}

function pendingChanges() {
  return $('#client-form')
    .find('input[name!="__RequestVerificationToken"][type!="hidden"],select')
    .not('div.selectize-input input')
    .map(function compareValue() {
      return ($(this).val() === $(this).attr('data-original-value') ? null : this);
    })
    .length;
}

function resetFormValues() {
  var inputsList = $('#client-form input:not(input[name="__RequestVerificationToken"], input[type="hidden"]), #client-form select');
  var i;

  for (i = 0; i < inputsList.length; i += 1) {
    $(inputsList[i]).val($(inputsList[i]).attr('data-original-value'));
  }
}

function cancelEditTasks(clientId) {
  resetFormValues();
  makeFormReadOnly();
  if (!clientId) {
    removeClientInserts();
    clearFormData();
    hideClientForm();
    clearSelectedClient();
  }
}

function cancelEditClientForm() {
  var clientId = $('#client-form #Id').val();
  if (pendingChanges()) {
    confirmDiscardDialog(function confirm() {
      cancelEditTasks(clientId);
    });
  } else {
    cancelEditTasks(clientId);
  }
}

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

  $('#expand-user-icon').click(expandAllUsers);
  $('#collapse-user-icon').click(collapseAllUsers);
  $('#add-client-icon').click(newClientFormSetup);
  $('#edit-client-icon').click(toggleEditExistingClient);
  $('#create-new-button').click(submitClientForm);
  $('#save-changes-button').click(submitClientForm);
  $('#reset-form-button').click(resetNewClientForm);
  $('#undo-changes-button').click(undoChangesEditClientForm);
  $('#cancel-edit-client-icon').click(cancelEditClientForm);

  $('#client-search-box').keyup(function clientSearchBoxKeyup() {
    searchClientTree($(this).val());
  });

  $('#user-search-box').keyup(function userSearchBoxKeyup() {
    searchUser($(this).val());
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
