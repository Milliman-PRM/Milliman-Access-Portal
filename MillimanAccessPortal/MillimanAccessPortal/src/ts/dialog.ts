import $ = require('jquery');
import toastr = require('toastr');
import shared = require('./shared');
const vex = require('vex-js');
require('typeahead.js');

require('vex-js/sass/vex.sass');
require('vex-js/sass/vex-theme-default.sass');

// TODO: move to types file
interface User {
  UserName: string,
  Email: string,
  FirstName: string,
  LastName: string,
};


// This is a duplicate of the function in shared
// Better separation of functionality would allow this to exist in one place
// This is a temporary solution only.
var userSubstringMatcher = function (users) {
  console.warn('You are using a duplicate of shared.userSubstringMatcher. Refactor shared.js to require this function in only one place.')
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
};


export function Dialog(
  title, message, buttons, color, input,
  callback, submitHandler
) {
  var self = this;
  this.title = title;
  this.color = color;
  this.options = {
    unsafeMessage: '<span class="vex-custom-message">' + message + '</span>',
    buttons: $.map(buttons, function (element) {
      return element.type(element.text, color);
    }),
    input: input || '',
    callback: callback || $.noop
  };
  if (submitHandler) {
    this.options = $.extend(this.options, {
      onSubmit: function (event) {
        var vexObject = this;
        var data;
        event.preventDefault();
        if (self.options.input) {
          data = {};
          $.each($('.vex-dialog-input input').serializeArray(), function (i, obj) {
            data[obj.name] = obj.value;
          });
        }
        return submitHandler(data, function () {
          vexObject.close();
        }, self.buttonText);
      }
    });
  }
};

Dialog.prototype.open = function () {
  vex.dialog.open(this.options);
  $('.vex-content')
    .prepend([
      '<div class="vex-title-wrapper">',
      '  <h3 class="vex-custom-title ' + this.color + '">',
      '' + this.title,
      '  </h3>',
      '</div>'
    ].join(''));
  if (this.afterOpen) this.afterOpen();
};

export function ConfirmationDialog(title, message, buttonText, callback) {
  Dialog.call(
    this,
    title,
    message,
    [
      { type: vex.dialog.buttons.yes, text: buttonText },
      { type: vex.dialog.buttons.no, text: 'Continue Editing' }
    ],
    'blue',
    null,
    function (result) {
      if (result) {
        callback();
      }
    }
  );
};
ConfirmationDialog.prototype = Object.create(Dialog.prototype);
ConfirmationDialog.prototype.constructor = ConfirmationDialog;

export function DiscardConfirmationDialog(callback) {
  ConfirmationDialog.call(
    this,
    'Discard Changes',
    'Would you like to discard unsaved changes?',
    'Discard',
    callback
  );
};
DiscardConfirmationDialog.prototype = Object.create(ConfirmationDialog.prototype);
DiscardConfirmationDialog.prototype.constructor = DiscardConfirmationDialog;

export function ResetConfirmationDialog(callback) {
  ConfirmationDialog.call(
    this,
    'Reset Form',
    'Would you like to reset the form?',
    'Reset',
    callback
  );
};
ResetConfirmationDialog.prototype = Object.create(ConfirmationDialog.prototype);
ResetConfirmationDialog.prototype.constructor = ResetConfirmationDialog;

export function RemoveUserDialog(username, submitHandler) {
  this.buttonText = 'Removing';
  Dialog.call(
    this,
    'Remove User',
    'Remove <strong>' + username + '</strong> from the selected client?',
    [
      { type: vex.dialog.buttons.yes, text: 'Remove' },
      { type: vex.dialog.buttons.no, text: 'Cancel' }
    ],
    'red',
    null,
    null,
    submitHandler
  );
};
RemoveUserDialog.prototype = Object.create(Dialog.prototype);
RemoveUserDialog.prototype.constructor = RemoveUserDialog;

export function DeleteSelectionGroupDialog($selectionGroup, submitHandler) {
  Dialog.call(
    this,
    'Delete Selection Group',
    'Delete <strong>' + $selectionGroup.find('.card-body-primary-text').html() + '</strong>?',
    [
      { type: vex.dialog.buttons.yes, text: 'Delete' },
      { type: vex.dialog.buttons.no, text: 'Cancel' }
    ],
    'red',
    '<input name="SelectionGroupId" type="hidden" value="' + $selectionGroup.closest('.card-container').data('selection-group-id') + '">',
    null,
    submitHandler
  );
  this.buttonText = 'Deleting';
};
DeleteSelectionGroupDialog.prototype = Object.create(Dialog.prototype);
DeleteSelectionGroupDialog.prototype.constructor = DeleteSelectionGroupDialog;

export function PasswordDialog(title, message, buttons, color, submitHandler) {
  Dialog.call(
    this,
    title,
    message,
    buttons,
    color,
    '<input name="password" type="password" placeholder="Password" required />',
    null,
    submitHandler
  );
};
PasswordDialog.prototype = Object.create(Dialog.prototype);
PasswordDialog.prototype.constructor = PasswordDialog;

export function DeleteClientDialog(clientName, clientId, submitHandler) {
  var title = 'Delete Client';
  var buttons = [
    { type: vex.dialog.buttons.yes, text: 'Delete' },
    { type: vex.dialog.buttons.no, text: 'Cancel' }
  ];
  var color = 'red';
  Dialog.call(
    this,
    title,
    'Delete <strong>' + clientName + '</strong>?<br /><br /> This action <strong><u>cannot</u></strong> be undone.',
    buttons,
    color,
    null,
    function (confirm) {
      if (confirm) {
        new PasswordDialog(
          title,
          'Please provide your password to delete <strong>' + clientName + '</strong>.',
          buttons,
          color,
          submitHandler
        ).open();
      } else {
        toastr.info('Deletion was canceled');
      }
    }
  );
};
DeleteClientDialog.prototype = Object.create(Dialog.prototype);
DeleteClientDialog.prototype.constructor = DeleteClientDialog;

export function DeleteRootContentItemDialog(rootContentItemName, rootContentItemId, submitHandler) {
  var title = 'Delete RootContentItem';
  var buttons = [
    { type: vex.dialog.buttons.yes, text: 'Delete' },
    { type: vex.dialog.buttons.no, text: 'Cancel' }
  ];
  var color = 'red';
  Dialog.call(
    this,
    title,
    'Delete <strong>' + rootContentItemName + '</strong>?<br /><br /> This action <strong><u>cannot</u></strong> be undone.',
    buttons,
    color,
    null,
    function (confirm) {
      if (confirm) {
        new PasswordDialog(
          title,
          'Please provide your password to delete <strong>' + rootContentItemName + '</strong>.',
          buttons,
          color,
          submitHandler
        ).open();
      } else {
        toastr.info('Deletion was canceled');
      }
    }
  );
};
DeleteRootContentItemDialog.prototype = Object.create(Dialog.prototype);
DeleteRootContentItemDialog.prototype.constructor = DeleteRootContentItemDialog;

export function AddUserDialog(eligibleUsers, submitHandler) {
  Dialog.call(
    this,
    'Add User',
    'Please provide a valid email address',
    [
      { type: vex.dialog.buttons.yes, text: 'Add User' },
      { type: vex.dialog.buttons.no, text: 'Cancel' }
    ],
    'blue',
    '<input class="typeahead" name="username" placeholder="Email" required />',
    null,
    submitHandler
  );
  this.afterOpen = function () {
    $('.vex-dialog-input .typeahead').typeahead(
      {
        hint: true,
        highlight: true,
        minLength: 1
      },
      {
        name: 'eligibleUsers',
        source: userSubstringMatcher(eligibleUsers),
        display: function (data: User) {
          return data.UserName;
        },
        templates: {
          suggestion: function (data: User) {
            return [
              '<div>',
              data.UserName + '',
              (data.UserName !== data.Email)
                ? '<br /> ' + data.Email
                : '',
              (data.FirstName && data.LastName)
                ? '<br /><span class="secondary-text">' + data.FirstName + ' ' + data.LastName + '</span>'
                : '',
              '</div>'
            ].join('');
          }
        }
      }
    ).focus();
  };
};
AddUserDialog.prototype = Object.create(Dialog.prototype);
AddUserDialog.prototype.constructor = AddUserDialog;

export function AddSelectionGroupDialog(submitHandler) {
  Dialog.call(
    this,
    'Add Selection Group',
    'Please enter the selection group name',
    [
      { type: vex.dialog.buttons.yes, text: 'Add Group' },
      { type: vex.dialog.buttons.no, text: 'Cancel' }
    ],
    'blue',
    [
      '<input name="RootContentItemId" type="hidden" value="' + $('#root-content-items [selected]').closest('.card-container').data('root-content-item-id') + '">',
      '<input name="SelectionGroupName" required />'
    ].join(''),
    null,
    submitHandler
  );
};
AddSelectionGroupDialog.prototype = Object.create(Dialog.prototype);
AddSelectionGroupDialog.prototype.constructor = AddSelectionGroupDialog;

export function CancelContentPublicationRequestDialog(rootContentItemId, rootContentItemName, submitHandler) {
  Dialog.call(
    this,
    'Cancel content publication request',
    `Cancel publication for <strong>${rootContentItemName}</strong>?`,
    [
      { type: vex.dialog.buttons.yes, text: 'Yes' },
      { type: vex.dialog.buttons.no, text: 'No' }
    ],
    'red',
    '<input name="RootContentItemId" type="hidden" value="' + rootContentItemId + '">',
    null,
    submitHandler,
  );
  this.buttonText = 'Canceling';
};
CancelContentPublicationRequestDialog.prototype = Object.create(Dialog.prototype);
CancelContentPublicationRequestDialog.prototype.constructor = CancelContentPublicationRequestDialog;