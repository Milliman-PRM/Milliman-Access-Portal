const clientNodeTemplate = $('script[data-template="clientNode"]').html();
const childNodePlaceholder = $('script[data-template="childNodePlaceholder"]').html();
const clientCard = $('script[data-template="createNewClientCard"]').html();
const userNodeTemplate = $('script[data-template="userNode"]').html();
let clientTree = {};

function populateProfitCenterDropDown(profitCenters) {
  $('#ProfitCenterId option:not(option[value = ""])').remove();
  $.each(profitCenters, () => {
    $('#ProfitCenterId').append($('<option />').val(this.Id).text(`${this.Name} (${this.Code})`));
  });
}

function renderClientNode(client, level) {
  let template = clientNodeTemplate;

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
  const $template = $(template.toString());

  if (!client.CanManage) {
    $('.icon-container', $template).remove();
    $('.client-admin-card', $template).attr('disabled', '');
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
    client.Children.forEach((childNode) => {
      renderClientNode(childNode, level + 1);
    });
  }
}

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
  $.each(ClientEntity, (key, value) => {
    const ctrl = $(`#${key}`, '#client-info');
    if (ctrl.is('select')) {
      if ($(`#client-form #${key} option[value="${value}"]`).length === 0) {
        $(`#${key}`).append($('<option temporary-profitcenter />').val(ClientEntity.ProfitCenterId).text(`${ClientEntity.ProfitCenter.Name} (${ClientEntity.ProfitCenter.ProfitCenterCode})`));
      }
      ctrl.val(value).change();
    } else if (ctrl.hasClass('selectize-custom-input')) {
      ctrl[0].selectize.clear();
      ctrl[0].selectize.clearOptions();
      if (value) {
        for (let i = 0; i < value.length; i += 1) {
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

function renderUserNode(clientId, user) {
  let template = userNodeTemplate;

  template = template.replace(/{{clientId}}/g, clientId);
  template = template.replace(/{{id}}/g, user.Id);
  template = template.replace(/{{name}}/g, `${user.FirstName} ${user.LastName}`);
  template = template.replace(/{{username}}/g, user.UserName);
  if (user.UserName !== user.Email) {
    template = template.replace(/{{email}}/g, user.Email);
  }

  // convert template to DOM element for jQuery manipulation
  const $template = $(template.toString());

  $('div.card-container[data-search-string]', $template).attr(
    'data-search-string',
    `${user.FirstName} ${user.LastName.toUpperCase()}|${user.UserName.toUpperCase()}|${user.Email.toUpperCase()}`.toUpperCase(),
  );

  $('.card-body-secondary-text:contains("{{email}}")', $template).remove();

  // if (!client.CanManage) {
  //     $('.icon-container', $template).remove();
  //     $('.card-container', $template).addClass('disabled');
  // }

  $('#client-user-list').append($template);
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

function renderUserList(client, userId) {
  $('#client-user-list').empty();
  client.AssignedUsers.forEach((user) => {
    renderUserNode(client.ClientEntity.Id, user);
  });

  $('div.card-button-remove-user').on('click', (event) => {
    // removeUserFromClient($(this).parents('div[data-client-id][data-user-id]'));
    event.stopPropagation();
  });
  $('div[data-client-id][data-user-id]').on('click', (event) => {
    if ($(this).find('div.card-expansion-container').is('[maximized]')) {
      $(this).find('div.card-expansion-container').removeAttr('maximized');
    } else {
      $(this).find('div.card-expansion-container').attr('maximized', '');
    }
    toggleExpandCollapse();
    event.stopPropagation();
  });

  toggleExpandCollapse();
  // $('#client-user-list').append(userCard);

  // if (client.EligibleUsers) {
  //   console.log(client.EligibleUsers);
  // }

  if (userId) {
    $(`[data-user-id="${userId}"]`).click();
  }
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
  const showTime = 50;
  $('#client-info').show(showTime, () => {
    $('#client-form #Name').focus();
    if ($('#client-form #Id').val()) {
      $('#client-users').show(showTime);
    } else {
      $('#client-users').hide();
    }
  });
}

function GetClientDetail(clientDiv) {
  removeClientInserts();

  const clientId = clientDiv.attr('data-client-id').valueOf();

  if (clientDiv.is('[selected]') && !clientDiv.is('[editing]')) {
    clearSelectedClient();
    hideClientForm();
    return false;
  }

  $.ajax({
    type: 'GET',
    url: `ClientAdmin/ClientDetail/${clientId}`,
    headers: {
      RequestVerificationToken: $("input[name='__RequestVerificationToken']").val(),
    },
  }).done((response) => {
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
  }).fail((response) => {
    toastr.warning(response.getResponseHeader('Warning'));
    hideClientForm();
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

function EditClientDetail(clientDiv) {
  removeClientInserts();

  clearValidationErrors();

  const clientId = clientDiv.attr('data-client-id').valueOf();

  $.ajax({
    type: 'GET',
    url: `ClientAdmin/ClientDetail/${clientId}`,
    headers: {
      RequestVerificationToken: $("input[name='__RequestVerificationToken']").val(),
    },
  }).done((response) => {
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
    $('#client-form :input, #client-form select').on('change', () => {
      if ($(this).value !== $(this).attr('data-original-value')) {
        $('#undo-changes-button').show();
      }
    });
  }).fail((response) => {
    toastr.warning(response.getResponseHeader('Warning'));
    hideClientForm();
  });
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

function newChildClientFormSetup(parentClientDiv) {
  clearFormData();

  const parentClientId = parentClientDiv.attr('data-client-id').valueOf();

  $('#client-form #ParentClientId').val(parentClientId);

  removeClientInserts();
  clearSelectedClient();

  let template = childNodePlaceholder;
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

function renderClientTree(clientId) {
  $('#client-tree-list').empty();
  clientTree.forEach((rootClient) => {
    renderClientNode(rootClient, 1);
    $('#client-tree-list').append('<li class="hr width-100pct"></li>');
  });
  $('#client-tree-list div.card-container').on('click', () => {
    GetClientDetail($(this));
  });
  $('div.card-button-edit').on('click', (event) => {
    EditClientDetail($(this).parents('div[data-client-id]'));
    event.stopPropagation();
  });
  $('div.card-button-new-child').on('click', (event) => {
    newChildClientFormSetup($(this).parents('div[data-client-id]'));
    event.stopPropagation();
  });
  if (clientId) {
    $(`[data-client-id="${clientId}"]`).click();
  }
  if ($('#add-client-icon').length) {
    $('#client-tree-list').append(clientCard);
  }
}

function removeClientNode(clientId, clientName, password) {
  $.ajax({
    type: 'DELETE',
    url: 'ClientAdmin/DeleteClient',
    data: {
      Id: clientId,
      Password: password,
    },
    headers: {
      RequestVerificationToken: $("input[name='__RequestVerificationToken']").val(),
    },
  }).done((response) => {
    renderClientTree(response.RelevantClientId);
    clearFormData();
    hideClientForm();
    toastr.success(`${clientName} was successfully deleted.`);
  }).fail((response) => {
    toastr.warning(response.getResponseHeader('Warning'));
  });
}

function resetFormValues() {
  const inputsList = $('#client-form input:not(input[name="__RequestVerificationToken"], input[type="hidden"]), #client-form select');

  for (let i = 0; i < inputsList.length; i += 1) {
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

function pendingChanges() {
  const inputsList = $('#client-form input:not(input[name="__RequestVerificationToken"], input[type="hidden"]), #client-form select');

  for (let i = 0; i < inputsList.length; i += 1) {
    if ($(inputsList[i]).val() !== $(inputsList[i]).attr('data-original-value')) {
      return true;
    }
  }

  return false;
}


function getClientTree() {
  $.ajax({
    type: 'GET',
    url: 'ClientAdmin/ClientFamilyList/',
  }).done((response) => {
    clientTree = response.ClientTree;
    populateProfitCenterDropDown(response.AuthorizedProfitCenterList);
    renderClientTree(response.RelevantClientId);
  }).fail((response) => {
    if (response.getResponseHeader('Warning')) {
      toastr.warning(response.getResponseHeader('Warning'));
    } else {
      toastr.error('An error has occurred');
    }
  });
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

function deleteClient(event, id, name) {
  event.stopPropagation();

  vex.dialog.confirm({
    unsafeMessage: `<h3>Delete ${name}?</h3><p>This action can not be undone.  Do you wish to proceed?</p>`,
    buttons: [
      $.extend({}, vex.dialog.buttons.YES, { text: 'Confirm', className: 'red-button' }),
      $.extend({}, vex.dialog.buttons.NO, { text: 'Cancel', className: 'link-button' }),
    ],
    callback(result) {
      if (result) {
        vex.dialog.prompt({
          message: 'Please provide your password to proceed with deletion',
          input: [
            '<input name="password" type="password" placeholder="Password" required />',
          ].join(''),
          buttons: [
            $.extend({}, vex.dialog.buttons.YES, { text: 'DELETE', className: 'red-button' }),
            $.extend({}, vex.dialog.buttons.NO, { text: 'Cancel', className: 'link-button' }),
          ],
          callback(innerResult) {
            if (innerResult) {
              removeClientNode(id, name, innerResult);
            } else if (result === '') {
              toastr.warning('Please enter your password to proceed');
              return false;
            } else {
              toastr.info('Deletion was canceled');
            }
            return true;
          },
        });
      } else {
        toastr.info('Deletion was canceled');
      }
    },
  });
}

function searchClientTree(searchString) {
  const searchStringUpper = searchString.toUpperCase();
  const nodes = $('#client-tree-list > li');
  let hrSwitch = 0;

  for (let i = 0; i < nodes.length; i += 1) {
    if (nodes[i].getElementsByClassName('card-body-primary-text').length > 0) {
      const title = nodes[i].getElementsByClassName('card-body-primary-text')[0];
      const clientCode = nodes[i].getElementsByClassName('card-body-secondary-text')[0];
      if (title || clientCode) {
        if (title.innerHTML.toUpperCase().indexOf(searchStringUpper) > -1 ||
          clientCode.innerHTML.toUpperCase().indexOf(searchStringUpper) > -1) {
          nodes[i].style.display = '';
          hrSwitch = 1;
        } else {
          nodes[i].style.display = 'none';
        }
      }
    } else {
      if (hrSwitch === 0) {
        nodes[i].style.display = 'none';
      } else {
        nodes[i].style.display = '';
      }
      hrSwitch = 0;
    }
  }
}

function submitClientForm(event) {
  if ($('#client-form').valid()) {
    event.preventDefault();

    const form = $('#client-form');
    const clientId = $('#client-form #Id').val();
    const clientName = $('#client-form #Name').val();
    let urlAction = 'ClientAdmin/';
    let successResponse;

    if (clientId) {
      urlAction += 'EditClient';
      successResponse = `${clientName} was successfully updated`;
    } else {
      urlAction += 'SaveNewClient';
      successResponse = `${clientName} was successfully created`;
    }

    $.ajax({
      type: 'POST',
      url: urlAction,
      data: form.serialize(),
      headers: {
        RequestVerificationToken: $("input[name='__RequestVerificationToken']").val(),
      },
    }).done((response) => {
      hideClientForm();
      clearFormData();
      clientTree = response.ClientTree;
      renderClientTree(response.RelevantClientId);
      toastr.success(successResponse);
      $(`div.client-admin-card[data-client-id="${clientId}"]`).click();
    }).fail((response) => {
      toastr.warning(response.getResponseHeader('Warning'));
    });
  }
}

function resetNewClientForm(event) {
  event.preventDefault();

  clearValidationErrors();

  $('#client-form :input:not(input[name="__RequestVerificationToken"], input[type="hidden"]), #client-form select').each(() => {
    if ($(this).val() !== '') {
      vex.dialog.confirm({
        message: 'Would you like to discard the unsaved changes?',
        buttons: [
          $.extend({}, vex.dialog.buttons.YES, { text: 'Confirm', className: 'green-button' }),
          $.extend({}, vex.dialog.buttons.NO, { text: 'Cancel', className: 'link-button' }),
        ],
        callback(result) {
          if (result) {
            $('#client-form .input-validation-error').removeClass('input-validation-error');
            $('#client-form span.field-validation-error > span').remove();
            clearFormData();
          } else {
            return false;
          }
          return true;
        },
      });
      return false;
    }
    return true;
  });
}

function undoChangesEditClientForm(event) {
  event.preventDefault();

  clearValidationErrors();

  const clientId = $('#client-form #Id').val();


  vex.dialog.confirm({
    message: 'Would you like to discard the unsaved changes?',
    buttons: [
      $.extend({}, vex.dialog.buttons.YES, { text: 'Confirm', className: 'green-button' }),
      $.extend({}, vex.dialog.buttons.NO, { text: 'Cancel', className: 'link-button' }),
    ],
    callback(result) {
      if (result) {
        EditClientDetail($(`#client-tree div[data-client-id="${clientId}"]`));
      }
    },
  });
}

function toggleEditExistingClient() {
  EditClientDetail($('div[selected]'));
}

function cancelClientEdit() {
  const clientId = $('#client-form #Id').val();

  if (pendingChanges()) {
    vex.dialog.confirm({
      message: 'Would you like to discard the unsaved changes?',
      buttons: [
        $.extend({}, vex.dialog.buttons.YES, { text: 'Confirm', className: 'green-button' }),
        $.extend({}, vex.dialog.buttons.NO, { text: 'Cancel', className: 'link-button' }),
      ],
      callback(result) {
        if (result) {
          cancelEditTasks(clientId);
        }
      },
    });
  } else {
    cancelEditTasks(clientId);
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

function searchUser(searchString) {
  const searchStringUpper = searchString.toUpperCase();
  const nodes = $('#client-user-list div[data-search-string]');

  for (let i = 0; i < nodes.length; i += 1) {
    if ($(nodes[i]).attr('data-search-string').indexOf(searchStringUpper) > -1) {
      nodes[i].style.display = '';
    } else {
      nodes[i].style.display = 'none';
    }
  }
}
