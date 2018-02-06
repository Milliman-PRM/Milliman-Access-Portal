/// <reference path="client-admin.js" />
/* global domainValRegex, emailValRegex */

var ajaxStatus = {
    getClientDetail: -1
};
var nodeTemplate = $('script[data-template="node"]').html();
var smallSpinner = '<div class="spinner-small""></div>';
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
 * Open a vex dialog with the specified attributes
 * @param  {Object} options Dialog attributes
 * @param  {String} options.title Dialog title
 * @param  {String} options.message Dialog message
 * @param  {Array.<{type: function, text: String}>} options.buttons Dialog buttons
 * @param  {String} options.color Dialog color
 * @param  {String} [options.input] Input element
 * @param  {function} options.callback Called when the dialog closes
 * @param  {function} options.submitHandler Called within onSubmit instead of vex.close()
 * @return {undefined}
 */
function buildVexDialog(opts) {
    var $dialog;
    var options = {
        unsafeMessage: '<span class="vex-custom-message">' + opts.message + '</span>',
        buttons: $.map(opts.buttons, function buildButton(element) {
            return element.type(element.text, opts.color);
        }),
        input: opts.input || '',
        callback: opts.callback ? opts.callback : $.noop
    };
    if (opts.submitHandler) {
        options = $.extend(options, {
            onSubmit: function onDialogSubmit(event) {
                event.preventDefault();
                if ($dialog.options.input) {
                    $dialog.value = $('.vex-dialog-input input').last().val();
                }
                return opts.submitHandler($dialog.value, function close() {
                    $dialog.close();
                });
            }
        });
    }
    $dialog = vex.dialog.open(options);
    $('.vex-content')
        .prepend('<div class="vex-title-wrapper"><h3 class="vex-custom-title ' + opts.color + '">' + opts.title + '</h3></div>');
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
function confirmRemoveDialog(name, submitHandler) {
    buildVexDialog({
        title: 'Remove User',
        message: 'Remove <strong>' + name + '</strong> from the selected client?',
        buttons: [
            { type: vex.dialog.buttons.yes, text: 'Remove' },
            { type: vex.dialog.buttons.no, text: 'Cancel' }
        ],
        color: 'red',
        submitHandler: submitHandler
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
    if (typeof onContinue === 'function') onContinue();
}


/**
 * Repopulate client form with details for the provided client
 * @param {Object} clientDiv the div for whom data will be retrieved
 * @return {undefined}
 */
function getClientDetail(clientDiv) {
    var clientId = clientDiv.attr('data-client-id').valueOf();

    ajaxStatus.getClientDetail = clientId;
    $.ajax({
        type: 'GET',
        url: 'ClientAdmin/ClientDetail/' + clientId,
        headers: {
            RequestVerificationToken: $("input[name='__RequestVerificationToken']").val()
        }
    })
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
    getClientDetail($clientCard);
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
        $clientTreeList.append($createNewClientCard.clone());
        $('#create-new-client-card')
            .click(function onClick() {
                createNewClientClickHandler($(this));
            });
    }
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


$(document).ready(function onReady() {
    getClientTree();

    $('#client-search-box').keyup(function onKeyup() {
        searchClientTree($(this).val());
    });
    
    $('.tooltip').tooltipster();

});
