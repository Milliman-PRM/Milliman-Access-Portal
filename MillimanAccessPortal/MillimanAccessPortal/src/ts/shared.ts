import $ = require('jquery');
import { Dialog, ResetConfirmationDialog, DiscardConfirmationDialog } from './dialog';
import toastr = require('toastr');

var SHOW_DURATION = 50;
var ajaxStatus = [];

var updateToolbarIcons;

// Functions with associated event listeners

// Filtering
export function filterTree($panel, $this) {
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
export function filterTreeListener(event) {
  buildListener(filterTree).bind(this)(event);
};
export function filterForm($panel, $this) {
  var $content = $panel.find('form.admin-panel-content');
  $content.find('[data-selection-value]').each(function (index, element) {
    var $element = $(element);
    if ($element.data('selection-value').indexOf($this.val().toUpperCase()) > -1) {
      $element.show();
    } else {
      $element.hide();
    }
  });
};
export function filterFormListener(event) {
  buildListener(filterForm).bind(this)(event);
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
export function toggleExpanded($panel, $this) {
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
export function toggleExpandedListener(event) {
  buildListener(toggleExpanded).bind(this)(event);
};
export function expandAll($panel) {
  $panel.find('.card-expansion-container').attr('maximized', '');
  updateToolbarIcons($panel);
};
export function expandAllListener(event) {
  buildListener(expandAll).bind(this)(event);
};
export function collapseAll($panel) {
  $panel.find('.card-expansion-container[maximized]').removeAttr('maximized');
  updateToolbarIcons($panel);
};
export function collapseAllListener(event) {
  buildListener(collapseAll).bind(this)(event);
};

// Form control
export function modifiedInputs($panel) {
  return $panel.find('form.admin-panel-content')
    .find('input[name!="__RequestVerificationToken"][type!="hidden"],select')
    .not('.selectize-input input')
    .filter(function () {
      var $element = $(this);
      return ($element.val() !== ($element.attr('data-original-value') || ''));
    });
};
export function modifiedInputsListener(event) {
  buildListener(modifiedInputs).bind(this)(event);
};
export function resetValidation($panel) {
  $panel.find('form.admin-panel-content').validate().resetForm();
  $panel.find('.field-validation-error > span').remove();
};
export function resetValidationListener(event) {
  buildListener(resetValidation).bind(this)(event);
};
export function resetForm($panel) {
  modifiedInputs($panel).each(function () {
    var $input = $(this);
    if ($input.is('.selectized')) {
      this.selectize.setValue($input.attr('data-original-value').split(','));
    } else {
      $input.val($input.attr('data-original-value'));
    }
  });
  resetValidation($panel);
  $panel.find('.form-button-container button').hide();
};
export function resetFormListener(event) {
  buildListener(resetForm).bind(this)(event);
};
export function clearForm($panel) {
  $panel.find('.selectized').each(function () {
    this.selectize.clear();
    this.selectize.clearOptions();
  });
  $panel.find('input[name!="__RequestVerificationToken"],select')
    .not('.selectize-input input')
    .attr('data-original-value', '').val('');
  resetValidation($panel);
};
export function clearFormListener(event) {
  buildListener(clearForm).bind(this)(event);
};

function buildListener(fn) {
  return (function (event) {
    const $this = $(this);
    const $panel = $this.closest('.admin-panel-container');
    event.stopPropagation();
    fn($panel, $this);
  });
}

// Functions without associated event listeners

// Wrappers
export function wrapCardCallback(callback, panels?) {
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
      confirmAndContinue($formPanels, DiscardConfirmationDialog, function () {
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
export function get(url, callbacks) {
  return function ($clickedCard?) {
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

function set(method, url, successMessage, callbacks) {
  return function (data, onResponse, buttonText) {
    if (ajaxStatus[url]) {
      return; // TODO: do something when a request has already been sent
    }
    showButtonSpinner($('.vex-first').attr('disabled', ''), buttonText);
    ajaxStatus[url] = true;
    $.ajax({
      type: method,
      url: url,
      data: data,
      headers: {
        RequestVerificationToken: $("input[name='__RequestVerificationToken']").val().toString()
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

export function post(url, successMessage, callbacks) {
  set('POST', url, successMessage, callbacks);
}
export function del(url, successMessage, callbacks) {
  set('DELETE', url, successMessage, callbacks);
}
export function put(url, successMessage, callbacks) {
  set('PUT', url, successMessage, callbacks);
}

export function showButtonSpinner($buttons, text?) {
  $buttons.each(function (i) {
    var $button = $buttons.eq(i);
    if ($buttons.find('.spinner-small').length) return;
    $button.data('originalText', $button.html());
    $button.html(text || 'Submitting');
    $button.append('<div class="spinner-small"></div>');
  });
};

export function hideButtonSpinner($buttons) {
  $buttons.each(function (i) {
    var $button = $buttons.eq(i);
    $button.html($button.data().originalText);
  });
};

export function xhrWithProgress(onProgress: Function) {
  return function () {
    var xhr = new XMLHttpRequest();
    xhr.upload.addEventListener('progress', function (event: ProgressEvent) {
      if (event.lengthComputable) {
        onProgress(event.loaded / event.total);
      }
    }, false);
    return xhr;
  };
};

// Typeahead
export function userSubstringMatcher(users: any) {
  return function findMatches(query: string, callback: Function) {
    var matches: Array<any> = [];
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
// TODO: consider moving to card.js
export function updateCardStatus($card, reductionDetails) {
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
export function confirmAndContinue($panel, Dialog, onContinue?) {
  if ($panel.length && modifiedInputs($panel).length) {
    new Dialog(function () {
      resetForm($panel);
      if (onContinue) {
        onContinue();
      }
    }).open();
  } else if (onContinue) {
    onContinue();
  }
};

export function confirmAndContinueForm(onContinue, condition = true) {
  if (condition) {
    new ResetConfirmationDialog(onContinue).open();
  } else {
    onContinue();
  }
}
