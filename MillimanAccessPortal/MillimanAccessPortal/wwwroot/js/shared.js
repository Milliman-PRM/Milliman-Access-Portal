/* global dialog */

var shared = {};

(function () {
  var SHOW_DURATION = 50;
  var ajaxStatus = [];

  var updateToolbarIcons;
  var set;

  // Functions with associated event listeners

  // Filtering
  shared.filterTree = function ($panel, $this) {
    var $content = $panel.find('ul.admin-panel-content');
    $content.children('.hr').hide();
    $content.find('[data-filter-string]').each(function (index, element) {
      var $element = $(element);
      if ($element.data('filter-string').indexOf($this.val().toUpperCase()) > -1) {
        $element.show();
        $element.closest('li').nextAll('li.hr').first()
          .show();
      } else {
        $element.hide();
      }
    });
  };

  // Card expansion
  updateToolbarIcons = function ($panel) {
    $panel.find('.action-icon-collapse').hide().filter(function anyMaximized() {
      return $panel.find('.card-expansion-container[maximized]').length;
    }).show();
    $panel.find('.action-icon-expand').hide().filter(function anyMinimized() {
      return $panel.find('.card-expansion-container:not([maximized])').length;
    }).show();
  };
  shared.toggleExpanded = function ($panel, $this) {
    $this.closest('.card-container')
      .find('.card-expansion-container')
      .attr('maximized', function (index, attr) {
        var data = (attr === '')
          ? { text: 'Expand card', rv: null }
          : { text: 'Collapse card', rv: '' };
        $this.find('.tooltip').tooltipster('content', data.text);
        return data.rv;
      });
    updateToolbarIcons($panel);
  };
  shared.expandAll = function ($panel) {
    $panel.find('.card-expansion-container').attr('maximized', '');
    updateToolbarIcons($panel);
  };
  shared.collapseAll = function ($panel) {
    $panel.find('.card-expansion-container[maximized]').removeAttr('maximized');
    updateToolbarIcons($panel);
  };

  // Form control
  shared.modifiedInputs = function ($panel) {
    return $panel.find('form.admin-panel-content')
      .find('input[name!="__RequestVerificationToken"][type!="hidden"],select')
      .not('.selectize-input input')
      .filter(function () {
        var $element = $(this);
        return ($element.val() !== ($element.attr('data-original-value') || ''));
      });
  };
  shared.resetValidation = function ($panel) {
    $panel.find('form.admin-panel-content').validate().resetForm();
    $panel.find('.field-validation-error > span').remove();
  };
  shared.resetForm = function ($panel) {
    shared.modifiedInputs($panel).each(function () {
      var $input = $(this);
      if ($input.is('.selectized')) {
        this.selectize.setValue($input.attr('data-original-value').split(','));
      } else {
        $input.val($input.attr('data-original-value'));
      }
    });
    shared.resetValidation($panel);
    $panel.find('.form-button-container button').hide();
  };
  shared.clearForm = function ($panel) {
    $panel.find('.selectized').each(function () {
      this.selectize.clear();
      this.selectize.clearOptions();
    });
    $panel.find('input[name!="__RequestVerificationToken"],select')
      .not('.selectize-input input')
      .attr('data-original-value', '').val('');
    shared.resetValidation($panel);
  };

  // Add listener property to each function
  Object.keys(shared).forEach(function (fn) {
    shared[fn].listener = function (event) {
      var $this = $(this);
      var $panel = $this.closest('.admin-panel-container');
      event.stopPropagation();
      shared[fn]($panel, $this);
    };
  });

  // Functions without associated event listeners

  // Wrappers
  shared.wrapCardCallback = function (callback, panels) {
    return function () {
      var $card = $(this);
      var $panel = $card.closest('.admin-panel-container');
      var $nextPanels = $panel.nextAll();
      var $formPanels = $nextPanels.slice(0, panels || 1).filter('form');
      var sameCard = ($card[0] === $panel.find('[selected]')[0]);

      var removeInserts = function () {
        $panel.find('.insert-card').remove();
      };
      var clearSelection = function () {
        $panel.find('.card-body-container').removeAttr('editing selected');
      };
      var showDetails = function () {
        $nextPanels.hide().slice(0, panels || 1).show(SHOW_DURATION);
      };
      var hideDetails = function () {
        $panel.nextAll().hide(SHOW_DURATION);
      };
      var openCard = function () {
        removeInserts();
        clearSelection();
        $card.attr('selected', '');
        callback($card);
        showDetails();
      };

      if ($panel.has('[selected]').length) {
        shared.confirmAndContinue($formPanels, dialog.DiscardConfirmationDialog, function () {
          if (sameCard) {
            clearSelection();
            hideDetails();
          } else {
            openCard();
          }
        });
      } else {
        openCard();
      }
    };
  };

  // AJAX
  shared.get = function (url) {
    var callbacks = Array.prototype.slice.call(arguments, 1);
    return function ($clickedCard) {
      var $card = $clickedCard && $clickedCard.closest('.card-container');
      var $panel = $card
        ? $card.closest('.admin-panel-container').nextAll().slice(0, callbacks.length)
        : $('.admin-panel-container').first();
      var $loading = $panel.find('.loading-wrapper');
      var data = $card && $card.data();

      ajaxStatus[url] = data; // or some hash of the data
      $loading.show();

      $.ajax({
        type: 'GET',
        url: url,
        data: data
      }).done(function (response) {
        if (ajaxStatus[url] !== data) return;
        callbacks.forEach(function (callback, index) {
          callback(response);
          $loading.eq(index).hide();
        });
      }).fail(function (response) {
        var warning = response.getResponseHeader('Warning');
        if (ajaxStatus[url] !== data) return;
        toastr.warning(warning || 'An unknown error has occurred.');
        $loading.hide();
      });
    };
  };

  set = function (method, url, successMessage) {
    var callbacks = Array.prototype.slice.call(arguments, 3);
    return function (data, onResponse, buttonText) {
      if (ajaxStatus[url]) {
        return; // TODO: do something when a request has already been sent
      }
      shared.showButtonSpinner($('.vex-first').attr('disabled', ''), buttonText);
      ajaxStatus[url] = true;
      $.ajax({
        type: method,
        url: url,
        data: data,
        headers: {
          RequestVerificationToken: $("input[name='__RequestVerificationToken']").val()
        }
      }).done(function (response) {
        ajaxStatus[url] = false;
        onResponse();
        callbacks.forEach(function (callback) {
          callback(response);
        });
        toastr.success(successMessage);
      }).fail(function (response) {
        var warning = response.getResponseHeader('Warning');
        ajaxStatus[url] = false;
        onResponse();
        toastr.warning(warning || 'An unknown error has occurred.');
      });
    };
  };

  shared.put = set.bind(this, 'PUT');
  shared.post = set.bind(this, 'POST');
  shared.delete = set.bind(this, 'DELETE');

  shared.showButtonSpinner = function ($buttons, text) {
    $buttons.each(function (i) {
      var $button = $buttons.eq(i);
      $button.data('originalText', $button.html());
      $button.html(text || 'Submitting');
      $button.append('<div class="spinner-small"></div>');
    });
  };

  shared.hideButtonSpinner = function ($buttons) {
    $buttons.each(function (i) {
      var $button = $buttons.eq(i);
      $button.html($button.data().originalText);
    });
  };

  shared.xhrWithProgress = function (onProgress) {
    return function () {
      var xhr = new window.XMLHttpRequest();
      xhr.upload.addEventListener('progress', function (event) {
        if (event.lengthComputable) {
          onProgress(event.loaded / event.total);
        }
      }, false);
      return xhr;
    };
  };

  // Typeahead
  shared.userSubstringMatcher = function (users) {
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

  // Card helpers
  // TODO: decide whether to put these in card.js
  shared.updateCardStatus = function ($card, reductionDetails) {
    var $statusContainer = $card.find('.card-status-container');
    var $statusName = $statusContainer.find('strong');
    var $statusUser = $statusContainer.find('em');
    var details = $.extend({
      User: {
        FirstName: ''
      },
      StatusEnum: 0,
      StatusName: '',
      SelectionGroupId: 0,
      RootContentItemId: 0
    }, reductionDetails);

    $statusContainer
      .removeClass(function (i, classString) {
        var classNames = classString.split(' ');
        return classNames
          .filter(function (className) {
            return className.startsWith('status-');
          })
          .join(' ');
      })
      .addClass('status-' + details.StatusEnum);
    $statusName.html(details.StatusName);
    $statusUser.html(details.User.FirstName);
  };

  // Dialog helpers
  // TODO: consider moving to dialog.js
  shared.confirmAndContinue = function ($panel, Dialog, onContinue) {
    if ($panel.length && shared.modifiedInputs($panel).length) {
      new Dialog(function () {
        shared.resetForm($panel);
        if (onContinue) {
          onContinue();
        }
      }).open();
    } else if (onContinue) {
      onContinue();
    }
  };
}());
