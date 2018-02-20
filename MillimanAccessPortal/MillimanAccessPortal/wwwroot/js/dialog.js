/* global shared */

var dialog = {};

(function () {
  var Dialog;
  var ConfirmationDialog;
  var DiscardConfirmationDialog;
  var ResetConfirmationDialog;
  var RemoveUserDialog;
  var DeleteSelectionGroupDialog;
  var PasswordDialog;
  var DeleteClientDialog;
  var AddUserDialog;
  var AddSelectionGroupDialog;

  Dialog = function (
    title, message, buttons, color, input,
    callback, submitHandler
  ) {
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
          var self = this;
          var data;
          event.preventDefault();
          if (this.options.input) {
            data = {};
            $.each($('.vex-dialog-input input').serializeArray(), function (i, obj) {
              data[obj.name] = obj.value;
            });
          }
          return submitHandler(data, function () {
            self.close();
          });
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

  ConfirmationDialog = function (title, message, buttonText, callback) {
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

  DiscardConfirmationDialog = function (callback) {
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

  ResetConfirmationDialog = function (callback) {
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

  RemoveUserDialog = function (username, submitHandler) {
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

  DeleteSelectionGroupDialog = function ($selectionGroup, submitHandler) {
    Dialog.call(
      this,
      'Delete Selection Group',
      'Delete <strong>' + $selectionGroup.find('.card-body-primary-text').html() + '</strong>?',
      [
        { type: vex.dialog.buttons.yes, text: 'Delete' },
        { type: vex.dialog.buttons.no, text: 'Cancel' }
      ],
      'red',
      '<input name="SelectionGroupId" type="hidden" value="' + $selectionGroup.data('selection-group-id') + '">',
      null,
      submitHandler
    );
  };
  DeleteSelectionGroupDialog.prototype = Object.create(Dialog.prototype);
  DeleteSelectionGroupDialog.prototype.constructor = DeleteSelectionGroupDialog;

  PasswordDialog = function (title, message, buttons, color, submitHandler) {
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

  DeleteClientDialog = function (clientName, clientId, submitHandler) {
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

  AddUserDialog = function (eligibleUsers, submitHandler) {
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
          source: shared.userSubstringMatcher(eligibleUsers),
          display: function (data) {
            return data.UserName;
          },
          templates: {
            suggestion: function (data) {
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

  AddSelectionGroupDialog = function (submitHandler) {
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
        '<input name="RootContentItemId" type="hidden" value="' + $('#root-content-items [selected]').data('root-content-item-id') + '">',
        '<input name="SelectionGroupName" required />'
      ].join(''),
      null,
      submitHandler
    );
  };
  AddSelectionGroupDialog.prototype = Object.create(Dialog.prototype);
  AddSelectionGroupDialog.prototype.constructor = AddSelectionGroupDialog;


  dialog.DiscardConfirmationDialog = DiscardConfirmationDialog;
  dialog.ResetConfirmationDialog = ResetConfirmationDialog;
  dialog.RemoveUserDialog = RemoveUserDialog;
  dialog.DeleteSelectionGroupDialog = DeleteSelectionGroupDialog;
  dialog.DeleteClientDialog = DeleteClientDialog;
  dialog.AddUserDialog = AddUserDialog;
  dialog.AddSelectionGroupDialog = AddSelectionGroupDialog;
}());
