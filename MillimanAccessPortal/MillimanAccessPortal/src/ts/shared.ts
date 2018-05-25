import $ = require('jquery');
import { Dialog, ResetConfirmationDialog, DiscardConfirmationDialog } from './dialog';
import toastr = require('toastr');
import { FormBase } from './form/form-base';
import { PublicationStatus } from './view-models/content-publishing';

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
export function wrapCardCallback(callback: ($card: JQuery<HTMLElement>) => void, form?: () => FormBase, panelCount: number = 1) {
  return function () {
    const $card = $(this);
    const $panel = $card.closest('.admin-panel-container');
    const $nextPanels = $panel.nextAll();
    const sameCard = ($card[0] === $panel.find('[selected]')[0]);

    const clearSelection = () => {
      $panel.find('.card-body-container').removeAttr('editing selected');
    };
    const openCard = () => {
      $panel.find('.insert-card').remove();
      clearSelection();
      $card.attr('selected', '');
      callback($card);
      $nextPanels.hide().slice(0, panelCount).show(SHOW_DURATION);
    };

    if ($panel.has('[selected]').length) {
      confirmAndContinue(DiscardConfirmationDialog, form && form(), () => {
        if (sameCard) {
          clearSelection();
          $nextPanels.hide(SHOW_DURATION);
        } else {
          openCard();
        }
      });
    } else {
      openCard();
    }
  };
};
export function wrapCardIconCallback(callback: ($card: JQuery<HTMLElement>, whenDone: () => void) => void, form?: () => FormBase, panelCount: number = 1, sameCard?: ($card: JQuery<HTMLElement>) => boolean, always?: () => void) {
  return function (event) {
    event.stopPropagation();

    const $icon = $(this);
    const $card = $icon.closest('.card-body-container');
    const $panel = $card.closest('.admin-panel-container');
    const $nextPanels = $panel.nextAll();
    const same = sameCard
      ? sameCard($card)
      : ($card[0] === $panel.find('[selected]')[0]);
    const openCard = (whenDone: () => void) => {
      $panel.find('.insert-card').remove();
      $panel.find('.card-body-container').removeAttr('editing selected');
      $card.attr({ selected: '', editing: '' });
      callback($card, whenDone);
      $nextPanels.hide().slice(0, panelCount).show(SHOW_DURATION);
    };

    if ($panel.has('[editing]').length) {
      confirmAndContinue(DiscardConfirmationDialog, form && form(), () => {
        if (!same) {
          openCard(always);
        } else {
          always();
        }
      });
    } else {
      openCard(always);
    }
  };
};

// AJAX
export function get<T>(url: string, callbacks: Array<(response: T) => void>) {
  return ($clickedCard?: JQuery<HTMLElement>) => {
    const $card = $clickedCard && $clickedCard.closest('.card-container');
    const $panel = $card
      ? $card.closest('.admin-panel-container').nextAll().slice(0, callbacks.length)
      : $('.admin-panel-container').first();
    const $loading = $panel.find('.loading-wrapper');
    const data = $card && $card.data();

    ajaxStatus[url] = data; // or some hash of the data
    $loading.show();

    $.ajax({
      type: 'GET',
      url: url,
      data: data,
    }).done((response: T) => {
      // if this was not the most recent AJAX call for its URL, don't process the return data
      if (ajaxStatus[url] !== data) {
        return;
      }
      callbacks.forEach((callback, index) => {
        callback(response);
        $loading.eq(index).hide();
      });
    }).fail((response) => {
      // if this was not the most recent AJAX call for its URL, don't process the return data
      if (ajaxStatus[url] !== data) {
        return;
      }
      const warning = response.getResponseHeader('Warning');
      toastr.warning(warning || 'An unknown error has occurred.');
      $loading.hide();
    });
  };
};

export function set<T>(method: string, url: string, successMessage: string, callbacks: Array<(response: T) => void>) {
  return (data: any, onResponse: () => void, buttonText: string) => {
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
      },
    }).done((response: T) => {
      ajaxStatus[url] = false;
      onResponse();
      callbacks.forEach((callback) => callback(response));
      toastr.success(successMessage);
    }).fail((response) => {
      ajaxStatus[url] = false;
      onResponse();
      const warning = response.getResponseHeader('Warning');
      toastr.warning(warning || 'An unknown error has occurred.');
    });
  };
};

export function post<T>(url: string, successMessage: string, callbacks: Array<(response: T) => void>) {
  set('POST', url, successMessage, callbacks);
}
export function del<T>(url: string, successMessage: string, callbacks: Array<(response: T) => void>) {
  set('DELETE', url, successMessage, callbacks);
}
export function put<T>(url: string, successMessage: string, callbacks: Array<(response: T) => void>) {
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
export function updateCardStatusButtons($card: JQuery<HTMLElement>, publishingStatusEnum: PublicationStatus) {
  $card.find('.card-button-dynamic').hide();
  if (publishingStatusEnum === PublicationStatus.Queued) {
    $card.find('.card-button-cancel').css('display', 'flex');
  } else if (publishingStatusEnum === PublicationStatus.Processing) {
  } else if (publishingStatusEnum === PublicationStatus.Complete) {
    $card.find('.card-button-add').css('display', 'flex');
  } else {
    $card.find('.card-button-file-upload').css('display', 'flex');
  }
}
export function updateFormStatusButtons() {
  var selectedData = $('#root-content-items [selected]').parent().data();
  var $statusFormContainer = $('#content-publishing-form').find('.form-status-container');
  $statusFormContainer.hide();

  if (selectedData.statusEnum === PublicationStatus.Unknown) {
    $statusFormContainer.filter('.form-status-edit-or-republish').show();
  } else {
    $statusFormContainer.filter('.form-status-edit').show();
  }
}

// Dialog helpers
// TODO: consider moving to dialog.js
export function confirmAndContinue(Dialog, form?: FormBase, onContinue?) {
  if (form && form.modified) {
    new Dialog(function () {
      // Assigning to access mode forces the form to reset
      // FIXME: this is really unintuitive - use function instead of getters
      //   and setters since there are side effects
      form.accessMode = form.accessMode;
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
