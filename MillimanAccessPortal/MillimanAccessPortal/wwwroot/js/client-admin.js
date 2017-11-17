var clientNodeTemplate = $('script[data-template="clientNode"]').html();
var childNodePlaceholder = $('script[data-template="childNodePlaceholder"]').html();
var clientTree;

function getClientTree() {
    $.ajax({
        'type': 'GET',
        'url': 'ClientAdmin/ClientFamilyList/'
    }).done(function (response) {
        clientTree = response.ClientTree;
        populateProfitCenterDropDown(response.AuthorizedProfitCenterList);
        renderClientTree();
    }).fail(function (response) {
        if (response.status == 401) {
            toastr['error']('You are not authorized to view any clients');
        }
        else {
            toaster['error']('An error has occurred');
        }
    }).always(function () {
        applyClickEvents();
    });

}

function applyClickEvents() {
    $('div.client-admin-card').on('click', function () {
        GetClientDetail($(this));
    });
    $('div.card-button-background-edit').on('click', function (event) {
        EditClientDetail($(this).parents('div[data-client-id]'));
        event.stopPropagation();
    });
    $('div.card-button-background-add').on('click', function (event) {
        newChildClientFormSetup($(this).parents('div[data-client-id]'));
        event.stopPropagation();
    });
}

function populateProfitCenterDropDown(profitCenters) {
    $('#ProfitCenterId option:not(option[value = ""])').remove();
    $.each(profitCenters, function () {
        $('#ProfitCenterId').append($("<option />").val(this.Id).text(this.Name + ' (' + this.Code + ')'));
    })
}

function GetClientDetail(clientDiv) {

    removeClientInserts()

    var clientId = clientDiv.attr('data-client-id').valueOf();

    if (clientDiv.hasClass('selected') && !clientDiv.hasClass('editing')) {
        clearSelectedClient();
        hideClientForm();
        return false;
    }

    $.ajax({
        type: 'GET',
        url: 'ClientAdmin/ClientDetail/' + clientId,
        headers: {
            'RequestVerificationToken': $("input[name='__RequestVerificationToken']").val()
        },
    }).done(function (response) {
        populateClientDetails(response.ClientEntity);
        console.log(response.AssignedUsers);
        // Change the dom to reflect the selected client
        clearSelectedClient()
        clientDiv.addClass('selected');
        // Show the form in readonly mode
        makeFormReadOnly();
        showClientForm();

    }).fail(function (response) {
        toastr["warning"](response.getResponseHeader("Warning"));
        hideClientForm();
    })
};

function EditClientDetail(clientDiv) {

    removeClientInserts()

    var clientId = clientDiv.attr('data-client-id').valueOf();

    $.ajax({
        type: 'GET',
        url: 'ClientAdmin/ClientDetail/' + clientId,
        headers: {
            'RequestVerificationToken': $("input[name='__RequestVerificationToken']").val()
        },
    }).done(function (response) {
        populateClientDetails(response.ClientEntity);
        // Change the dom to reflect the selected client
        clearSelectedClient()
        clientDiv.addClass('selected');
        clientDiv.addClass('editing');
        // Show the form in read/write mode
        makeFormWriteable();
        $('#client-form #form-buttons-new').hide();
        $('#client-form #form-buttons-edit').show();
        $('#undo-changes-button').hide();
        showClientForm();
        $('#client-form :input, #client-form select').on('change', function () {
            if ($(this).value != $(this).attr('data-original-value')) {
                $('#undo-changes-button').show();
            }
        })
    }).fail(function (response) {
        toastr["warning"](response.getResponseHeader("Warning"));
        hideClientForm();
    })
};


function newClientFormSetup() {
    removeClientInserts()
    clearFormData();
    clearSelectedClient();
    makeFormWriteable();
    $('#client-form #form-buttons-edit').hide();
    $('#client-form #form-buttons-new').show();
    showClientForm();
}

function newChildClientFormSetup(parentClientDiv) {

    clearFormData();

    var parentClientId = parentClientDiv.attr('data-client-id').valueOf();

    $('#client-form #ParentClientId').val(parentClientId);

    removeClientInserts()
    clearSelectedClient();

    var template = childNodePlaceholder;
    if (parentClientDiv.hasClass('col-xs-12')) {
        template = template.replace(/{{class}}/g, "col-xs-offset-1 col-xs-11");
    }
    else {
        template = template.replace(/{{class}}/g, "col-xs-offset-2 col-xs-10");
    }

    parentClientDiv.parent().after(template);

    makeFormWriteable();
    $('#client-form #form-buttons-edit').hide();
    $('#client-form #form-buttons-new').show();
    showClientForm();
}


function removeClientInserts() {
    $('#client-tree li.client-insert').remove();
}

function clearFormData() {
    $('#client-form #AcceptedEmailDomainList')[0].selectize.clear();
    $('#client-form #AcceptedEmailDomainList')[0].selectize.clearOptions();
    $('#client-form #AcceptedEmailAddressExceptionList')[0].selectize.clear();
    $('#client-form #AcceptedEmailAddressExceptionList')[0].selectize.clearOptions();
    $('#client-form :input:not(input[name="__RequestVerificationToken"]), #client-form select').attr('data-original-value', '');
    $('#client-form :input:not(input[name="__RequestVerificationToken"]), #client-form select').val("");
}

function clearSelectedClient() {
    $('#client-tree-list div.selected').removeClass('selected');
    $('#client-tree-list div.editing').removeClass('editing');
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

function makeFormWriteable() {
    $('#edit-client-icon').hide();
    $('#cancel-edit-client-icon').show();
    $('#client-form :input').removeAttr('readonly');
    $('#client-form :input, #client-form select').removeAttr('disabled');
    $('#client-form #AcceptedEmailDomainList')[0].selectize.enable();
    $('#client-form #AcceptedEmailAddressExceptionList')[0].selectize.enable();
}

function showClientForm() {
    var showTime = 50;
    $('#client-info').show(showTime, function () {
        if ($('#client-form #Id').val()) {
            $('#client-users').show(showTime);
        }
        else {
            $('#client-users').hide();
        }
    });
}

function hideClientForm() {
    $('#client-info').hide();
    $('#client-users').hide();
}

function populateClientDetails(ClientEntity) {
    $('#client-form :input, #client-form select').removeAttr('data-original-value');
    $.each(ClientEntity, function (key, value) {
        var ctrl = $('#' + key, '#client-info');
        if (ctrl.is('select')) {
            ctrl.val(value).change();
        }
        else if (ctrl.hasClass('selectize-custom-input')) {
            ctrl[0].selectize.clear();
            ctrl[0].selectize.clearOptions();
            if (value) {
                for (i = 0; i < value.length; i++) {
                    ctrl[0].selectize.addOption({ value: value[i], text: value[i] });
                    ctrl[0].selectize.addItem(value[i]);
                }
            }
        }
        else {
            ctrl.val(value);
        }
        ctrl.attr('data-original-value', value);
    });
};

function renderClientTree() {
    $('#client-tree-list').empty();
    clientTree.forEach(function (rootClient) {
        renderClientNode(rootClient, 1);
        $('#client-tree-list').append('<li class="hr col-xs-12"></li>');
    });
};

function renderClientNode(client, level) {
    var template = clientNodeTemplate;

    switch (level) {
        case 1:
            template = template.replace(/{{class}}/g, "col-xs-12");
            break;
        case 2:
            template = template.replace(/{{class}}/g, "col-xs-offset-1 col-xs-11");
            break;
        default:
            template = template.replace(/{{class}}/g, "col-xs-offset-2 col-xs-10");
            break;
    }

    template = template.replace(/{{header-level}}/g, (level + 1))
    template = template.replace(/{{id}}/g, client.ClientEntity.Id);
    template = template.replace(/{{name}}/g, client.ClientEntity.Name);
    if (client.ClientEntity.ClientCode) {
        template = template.replace(/{{clientCode}}/g, client.ClientEntity.ClientCode);
    }
    else {
        template = template.replace(/{{clientCode}}/g, '');
    }
    template = template.replace(/{{users}}/g, client.AssociatedUserCount);
    template = template.replace(/{{content}}/g, client.AssociatedContentCount);


    // convert template to DOM element for jQuery manipulation
    var $template = $(template.toString());

    if (!client.CanManage) {
        $('.icon-container', $template).remove();
        $('.client-admin-card', $template).addClass('disabled');
    }

    if (client.Children.length != 0) {  // Only include the delete button on client nodes without children
        $('.card-button-background-delete', $template).remove();
    }

    if (level == 3) {  // Don't include the add child client button on lowest level
        $('.card-button-background-add', $template).remove();
    }

    $('#client-tree-list').append($template);

    // Render child nodes
    if (client.Children.length) {
        client.Children.forEach(function (childNode) {
            renderClientNode(childNode, level + 1);
        })
    }

};

function deleteClient(event, id, name) {

    event.stopPropagation();

    bootbox.confirm({
        title: "Delete " + name + "?",
        message: "This action can not be undone.  Do you wish to proceed?",
        className: 'screen-center',
        backdrop: true,
        onEscape: true,
        buttons: {
            confirm: {
                label: '<i class="fa fa-check"></i> Confirm',
                className: 'primary-button btn-danger'
            },
            cancel: {
                label: 'Cancel',
                className: 'btn-link'
            }
        },
        callback: function (result) {
            if (result) {
                bootbox.prompt({
                    title: "Please provide your password to proceed with deletion",
                    className: 'screen-center',
                    inputType: 'password',
                    backdrop: true,
                    onEscape: true,
                    buttons: {
                        confirm: {
                            label: '<i class="fa fa-trash"></i> DELETE',
                            className: 'primary-button btn-danger'
                        },
                        cancel: {
                            label: 'Cancel',
                            className: 'btn-link'
                        }
                    },
                    callback: function (result) {
                        if (result) {
                            removeClientNode(id, name, result);
                        }
                        else if (result == "") {
                            toastr['warning']("Please enter a password");
                            return false;
                        }
                        else {
                            toastr['warning']("Deletion was canceled");

                        }
                    }
                })

                $('.bootbox-input-password').on('keyup', function (e) {
                    if (e.which === 13) {
                        $('button[data-bb-handler="confirm"]').trigger('click');
                    }
                });
            }
        }
    });

};

function removeClientNode(clientId, clientName, password) {
    $.ajax({
        type: 'DELETE',
        url: 'ClientAdmin/DeleteClient',
        data: {
            Id: clientId,
            Password: password
        },
        headers: {
            'RequestVerificationToken': $("input[name='__RequestVerificationToken']").val()
        },
    }).done(function (response) {
        recursiveClientNodeSearch(clientTree, clientId);
        renderClientTree();
        toastr['success'](clientName + " was successfully deleted.");
    }).fail(function (response) {
        toastr["warning"](response.getResponseHeader("Warning"));
    })
};

function recursiveClientNodeSearch(array, clientId) {
    for (var i = 0; i < array.length; i++) {
        if (array[i].ClientEntity.Id == clientId) {
            array.splice(i, 1);
            return;
        }
        if (array[i].children.length > 0) {
            recursiveClientNodeSearch(array[i].children, clientId);
        }
    }
}

function searchClientTree(searchString) {
    var searchString = searchString.toUpperCase();
    var nodes = document.getElementById('client-tree-list').getElementsByTagName('li');
    var hrSwitch = 0;

    for (i = 0; i < nodes.length; i++) {
        var title, clientCode;
        if (nodes[i].getElementsByClassName('client-admin-card-title').length > 0) {
            title = nodes[i].getElementsByClassName('client-admin-card-title')[0];
            clientCode = nodes[i].getElementsByClassName('client-admin-card-clientcode')[0];
            if (title || clientCode) {
                if (title.innerHTML.toUpperCase().indexOf(searchString) > -1 ||
                    clientCode.innerHTML.toUpperCase().indexOf(searchString) > -1) {
                    nodes[i].style.display = "";
                    hrSwitch = 1;
                }
                else {
                    nodes[i].style.display = "none";
                }
            }
        }
        else {
            if (hrSwitch == 0) {
                nodes[i].style.display = "none";
            }
            else {
                nodes[i].style.display = "";

            }
            hrSwitch = 0;
        }
    }
}

function submitClientForm(event) {

    event.preventDefault();

    var form = $('#client-form');
    var clientId = $('#client-form #Id').val();
    var clientName = $('#client-form #Name').val();
    var urlAction = 'ClientAdmin/';
    var successResponse, failResponse;

    if (clientId) {
        urlAction += 'EditClient';
        successResponse = clientName + ' was successfully updated';
        failResponse = 'Could not update client information';
    }
    else {
        urlAction += 'SaveNewClient';
        successResponse = clientName + ' was successfully created';
        failResponse = 'Could not create client';
    }

    $.ajax({
        type: 'POST',
        url: urlAction,
        data: form.serialize(),
        headers: {
            'RequestVerificationToken': $("input[name='__RequestVerificationToken']").val()
        },
    }).done(function (response) {
        hideClientForm();
        clearFormData();
        clientTree = response.ClientTree;
        renderClientTree();
        applyClickEvents();
        toastr['success'](successResponse);
        $('div.client-admin-card[data-client-id="' + clientId + '"]').click();
    }).fail(function (response) {
        toastr["warning"](failResponse);
    })

}

function resetNewClientForm() {

    event.preventDefault();

    $('#client-form :input:not(input[name="__RequestVerificationToken"], input[type="hidden"]), #client-form select').each(function () {
        if ($(this).val() != "") {
            bootbox.confirm({
                title: "Discard changes?",
                message: "Would you like to discard the unsaved changes?",
                className: 'screen-center',
                backdrop: true,
                onEscape: true,
                buttons: {
                    confirm: {
                        label: '<i class="fa fa-check"></i> Confirm',
                        className: 'primary-button'
                    },
                    cancel: {
                        label: 'Cancel',
                        className: 'btn-link'
                    }
                },
                callback: function (result) {
                    if (result) {
                        $('#client-form .input-validation-error').removeClass('input-validation-error');
                        $('#client-form span.field-validation-error > span').remove();
                        clearFormData();
                    }
                    else {
                        return false;
                    }
                }
            })
            return false;
        }
    })
}

function undoChangesEditClientForm(event) {

    event.preventDefault();

    var clientId = $('#client-form #Id').val();

    bootbox.confirm({
        title: "Discard changes?",
        message: "Would you like to discard the unsaved changes?",
        className: 'screen-center',
        backdrop: true,
        onEscape: true,
        buttons: {
            confirm: {
                label: '<i class="fa fa-check"></i> Confirm',
                className: 'primary-button'
            },
            cancel: {
                label: 'Cancel',
                className: 'btn-link'
            }
        },
        callback: function (result) {
            if (result) {
                EditClientDetail($('#client-tree div[data-client-id="' + clientId + '"]'));
            }
        }
    })

}

function toggleEditExistingClient() {
    EditClientDetail($('div.selected'));
}

function cancelClientEdit() {

    var clientId = $('#client-form #Id').val();

    if (pendingChanges()) {
        bootbox.confirm({
            title: "Discard changes?",
            message: "Would you like to discard the unsaved changes?",
            className: 'screen-center',
            backdrop: true,
            onEscape: true,
            buttons: {
                confirm: {
                    label: '<i class="fa fa-check"></i> Confirm',
                    className: 'primary-button'
                },
                cancel: {
                    label: 'Cancel',
                    className: 'btn-link'
                }
            },
            callback: function (result) {
                if (result) {
                    cancelEditTasks(clientId);
                }
            }
        })
    }
    else {
        cancelEditTasks(clientId);
    }
}

function pendingChanges() {
    var inputsList = $('#client-form input:not(input[name="__RequestVerificationToken"], input[type="hidden"]), #client-form select');

    for (i = 0; i < inputsList.length; i++) {
        if ($(inputsList[i]).val() != $(inputsList[i]).attr('data-original-value')) {
            return true;
        }
    }

    return false;
}

function resetFormValues() {
    var inputsList = $('#client-form input:not(input[name="__RequestVerificationToken"], input[type="hidden"]), #client-form select');

    for (i = 0; i < inputsList.length; i++) {
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
    }
}