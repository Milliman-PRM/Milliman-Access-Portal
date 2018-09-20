import 'promise-polyfill/dist/polyfill';
import 'whatwg-fetch';

import * as $ from 'jquery';
import * as toastr from 'toastr';

import { DiscardConfirmationDialog, ResetConfirmationDialog } from './dialog';
import { FormBase } from './form/form-base';
import { SelectionGroupSummary } from './view-models/content-access-admin';
import { PublicationStatus, UserInfo } from './view-models/content-publishing';

const SHOW_DURATION = 50;
const ajaxStatus = [];

// Functions with associated event listeners

// Filtering
export function filterTree($panel, $this) {
  const $content = $panel.find('ul.admin-panel-content');
  $content.children('.hr').hide();
  $content.find('[data-filter-string]').each((_, element) => {
    const $element = $(element);
    if ($element.data('filter-string').indexOf($this.val().toUpperCase()) > -1) {
      $element.show();
      $element.closest('li').nextAll('li.hr').first()
        .show();
    } else {
      $element.hide();
    }
  });
}
export function filterTreeListener(event) {
  buildListener.call(this, filterTree).bind(this)(event);
}
export function filterForm($panel, $this) {
  const $content = $panel.find('form.admin-panel-content');
  $content.find('[data-selection-value]').each((_, element) => {
    const $element = $(element);
    if ($element.data('selection-value').indexOf($this.val().toUpperCase()) > -1) {
      $element.show();
    } else {
      $element.hide();
    }
  });
}
export function filterFormListener(event) {
  buildListener.call(this, filterForm).bind(this)(event);
}

// Card expansion
export function updateToolbarIcons($panel: JQuery<HTMLElement>) {
  $panel.find('.action-icon-collapse').hide().filter(() =>
    $panel.find('.card-expansion-container[maximized]').length > 0).show();
  $panel.find('.action-icon-expand').hide().filter(() =>
    $panel.find('.card-expansion-container:not([maximized])').length > 0).show();
}
export function setExpanded($panel: JQuery<HTMLElement>, $this: JQuery<HTMLElement>) {
  const $cardContainer = $this.closest('.card-container');
  $cardContainer
    .find('.card-expansion-container')
      .attr('maximized', '')
    .find('.card-button-expansion .tooltip')
      .tooltipster('content', 'Collapse card');
  updateToolbarIcons($panel);
}
export function toggleExpanded($panel, $this) {
  const $cardContainer = $this.closest('.card-container');
  $cardContainer
    .find('.card-expansion-container')
    .attr('maximized', (_, attr) => {
      const data = (attr === '')
        ? { text: 'Expand card', rv: null }
        : { text: 'Collapse card', rv: '' };
      $this.filter('.tooltipstered').tooltipster('content', data.text);
      return data.rv;
    });
  updateToolbarIcons($panel);
}
export function toggleExpandedListener(event) {
  buildListener.call(this, toggleExpanded).bind(this)(event);
}
export function expandAll($panel) {
  $panel.find('.card-expansion-container').attr('maximized', '');
  updateToolbarIcons($panel);
}
export function expandAllListener(event) {
  buildListener.call(this, expandAll).bind(this)(event);
}
export function collapseAll($panel) {
  $panel.find('.card-expansion-container[maximized]').removeAttr('maximized');
  updateToolbarIcons($panel);
}
export function collapseAllListener(event) {
  buildListener.call(this, collapseAll).bind(this)(event);
}

// Form control
export function resetValidation($panel) {
  $panel.find('form.admin-panel-content').validate().resetForm();
  $panel.find('.field-validation-error > span').remove();
}
export function clearForm($panel) {
  $panel.find('.selectized').each(function() {
    this.selectize.clear();
    this.selectize.clearOptions();
  });
  $panel.find('input[name!="__RequestVerificationToken"],select')
    .not('.selectize-input input')
    .attr('data-original-value', '').val('');
  resetValidation($panel);
}

function buildListener(fn) {
  return ((event) => {
    const $this = $(this);
    const $panel = $this.closest('.admin-panel-container');
    event.stopPropagation();
    fn($panel, $this);
  });
}

// Functions without associated event listeners

// Wrappers
export function wrapCardCallback(
  callback: ($card: JQuery<HTMLElement>) => void,
  form?: () => FormBase,
  panelCount: number = 1,
) {
  return function() {
    const $card = $(this);
    const $panel = $card.closest('.admin-panel-container');
    const $nextPanels = $panel.nextAll();
    const sameCard = ($card[0] === $panel.find('[selected]')[0]);

    const clearSelection = () => {
      $panel.find('.card-body-container').removeAttr('editing selected');
      $nextPanels.find('.card-body-container').removeAttr('editing selected');
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
}
export function wrapCardIconCallback(
  callback: ($card: JQuery<HTMLElement>, whenDone: () => void) => void,
  form?: () => FormBase,
  panelCount: {count: number, offset: number} = {count: 1, offset: 0},
  sameCard?: ($card: JQuery<HTMLElement>) => boolean,
  always?: () => void,
) {
  return function(event) {
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
      $nextPanels.hide().slice(panelCount.offset, panelCount.offset + panelCount.count).show(SHOW_DURATION);
    };

    if ($panel.has('[editing]').length) {
      confirmAndContinue(DiscardConfirmationDialog, form && form(), () => {
        if (!same) {
          openCard(always);
        } else if (always) {
          always();
        }
      });
    } else {
      openCard(always);
    }
  };
}

// AJAX
export function get<T>(url: string, callbacks: Array<(response: T) => void>, dataFn: (data: any) => any = null) {
  return ($clickedCard?: JQuery<HTMLElement>) => {
    const $card = $clickedCard && $clickedCard.closest('.card-container');
    const $panel = $card
      ? $card.closest('.admin-panel-container').nextAll().slice(0, callbacks.length)
      : $('.admin-panel-container').first();
    const $loading = $panel.find('.loading-wrapper');
    const data = dataFn
      ? dataFn($card && $card.data())
      : $card && $card.data();

    ajaxStatus[url] = data; // or some hash of the data
    $loading.show();

    $.ajax({
      data,
      type: 'GET',
      url,
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
      toastr.warning(response.getResponseHeader('Warning')
        || 'An unknown error has occurred.');
      $loading.hide();
    });
  };
}

export function set<T>(method: string, url: string, successMessage: string, callbacks: Array<(response: T) => void>) {
  return (data: any, onResponse: () => void, buttonText: string) => {
    if (ajaxStatus[url]) {
      return;
    }
    showButtonSpinner($('.vex-first').attr('disabled', ''), buttonText);
    ajaxStatus[url] = true;
    $.ajax({
      data,
      headers: {
        RequestVerificationToken: $('input[name="__RequestVerificationToken"]').val().toString(),
      },
      type: method,
      url,
    }).done((response: T) => {
      ajaxStatus[url] = false;
      onResponse();
      callbacks.forEach((callback) => callback(response));
      toastr.success(successMessage);
    }).fail((response) => {
      ajaxStatus[url] = false;
      onResponse();
      toastr.warning(response.getResponseHeader('Warning')
        || 'An unknown error has occurred.');
    });
  };
}

export function post<T>(url: string, successMessage: string, callbacks: Array<(response: T) => void>) {
  return set('POST', url, successMessage, callbacks);
}
export function del<T>(url: string, successMessage: string, callbacks: Array<(response: T) => void>) {
  return set('DELETE', url, successMessage, callbacks);
}
export function put<T>(url: string, successMessage: string, callbacks: Array<(response: T) => void>) {
  return set('PUT', url, successMessage, callbacks);
}

export function showButtonSpinner($buttons, text?) {
  $buttons.each((i) => {
    const $button = $buttons.eq(i);
    if ($buttons.find('.spinner-small').length) { return; }
    $button.data('originalText', $button.html());
    $button.html(text || 'Submitting');
    $button.append('<div class="spinner-small"></div>');
  });
}

export function hideButtonSpinner($buttons) {
  $buttons.each((i) => {
    const $button = $buttons.eq(i);
    $button.html($button.data().originalText);
  });
}

export function updateMemberList(
  $memberCard: JQuery<HTMLElement>,
  $eligibleCard: JQuery<HTMLElement>,
  selectionGroup: SelectionGroupSummary,
) {
  const $memberList = $memberCard.find('.detail-item-user-list');

  $memberList.empty();
  const memberList = $memberCard.data().memberList as UserInfo[];
  const eligibleList = $eligibleCard.data().eligibleList as UserInfo[];

  eligibleList.filter((eligible) =>
      memberList.filter((member) => eligible.Id === member.Id).length === 0);
  memberList
    .forEach((user) => {
      const firstLast = user.FirstName || user.LastName
        ? `${user.FirstName || ''} ${user.LastName || ''}`
        : user.UserName;
      const userName = firstLast === user.UserName
        ? ''
        : user.UserName;
      const $li = $([
        // If you make any changes to this component, also change the user component in card.ts
        '<li>',
        `  <span class="detail-item-user" data-user-id="${user.Id}">`,
        '    <div class="detail-item-user-icon">',
        '      <svg class="card-user-icon">',
        '        <use href="#user"></use>',
        '      </svg>',
        '    </div>',
        '    <div class="detail-item-user-remove">',
        '      <div class="card-button-background card-button-red">',
        '        <svg class="card-button-icon">',
        '          <use href="#remove-circle"></use>',
        '        </svg>',
        '      </div>',
        '    </div>',
        '    <div class="detail-item-user-name">',
        `      <h4 class="first-last">${firstLast}</h4>`,
        `      <span class="user-name">${userName}</span>`,
        '    </div>',
        '  </span>',
        '</li>',
      ].join(''));
      $li.find('.detail-item-user-remove').click((event) =>
        removeUserFromSelectionGroup(event, user, selectionGroup));
      $li.find('.detail-item-user-icon').hide();
      $memberList.append($li);
    });
  $memberCard.find('.card-stat-value').html(memberList.length.toString());
}

export function removeUserFromSelectionGroup(event, member: UserInfo, selectionGroup: SelectionGroupSummary) {
  event.stopPropagation();
  const assignment = {};
  assignment[member.Id] = false;
  const $selectionGroup = $(`#selection-groups [data-selection-group-id="${selectionGroup.Id}"]`);
  put<SelectionGroupSummary>(
    'ContentAccessAdmin/UpdateSelectionGroupUserAssignments/',
    `Removed ${member.Email} from selection group ${selectionGroup.Name}.`,
    [
      (response) => {
        $selectionGroup.data('memberList', response.MemberList);
        updateMemberList(
          $selectionGroup,
          $('#root-content-items [selected]').parent(),
          selectionGroup,
        );
      },
    ],
  )(
    {
      SelectionGroupId: selectionGroup.Id,
      UserAssignments: assignment,
    },
    () => undefined,
    'Removing',
  );
}
export function addUserToSelectionGroup(selectionGroup: SelectionGroupSummary) {
  const $selectionGroup = $(`#selection-groups [data-selection-group-id="${selectionGroup.Id}"]`);
  $selectionGroup.data('memberList', selectionGroup.MemberList);
  updateMemberList(
    $selectionGroup,
    $('#root-content-items [selected]').parent(),
    selectionGroup,
  );
}

// Typeahead
export function userSubstringMatcher(users: any) {
  return function findMatches(query: string, callback: (matches: any) => void) {
    const matches: any[] = [];
    const regex = new RegExp(query, 'i');

    $.each(users, function check(_, user) {
      if (regex.test(user.Email) ||
          regex.test(user.UserName) ||
          regex.test(user.FirstName + ' ' + user.LastName)) {
        matches.push(user);
      }
    });

    callback(matches);
  };
}

export function eligibleUserMatcher(query: string, callback: (matches: any) => void) {
  const allEligibleUsers = $('#root-content-items [selected]').parent().data().eligibleList as UserInfo[];
  const assignedUsers = $('#selection-groups .admin-panel-content .card-container').toArray()
    .map((card) => $(card).data().memberList)
    .reduce((cum: UserInfo[], cur: UserInfo[]) => cum.concat(cur), []) as UserInfo[];
  const eligibleUsers = allEligibleUsers.filter((eligibleUser) =>
    assignedUsers.filter((assignedUser) => eligibleUser.Id === assignedUser.Id).length === 0);

  const regex = new RegExp(query, 'i');
  callback(eligibleUsers.filter((user) =>
    [user.Email, user.UserName, `${user.FirstName} ${user.LastName}`].filter((text) =>
      regex.test(text)).length > 0));
}

// Card helpers
export function updateCardStatus($card, reductionDetails) {
  const $statusContainer = $card.find('.card-status-container');
  const $statusName = $statusContainer.find('strong');
  const $statusUser = $statusContainer.find('em');
  const details = $.extend({
    User: {
      FirstName: '',
    },
    StatusEnum: 0,
    StatusName: '',
    SelectionGroupId: 0,
    RootContentItemId: 0,
  }, reductionDetails);

  $statusContainer
    .removeClass((_, classString) => {
      const classNames = classString.split(' ');
      return classNames
        .filter((className) => {
          return className.indexOf('status-') === 0;
        })
        .join(' ');
    })
    .addClass('status-' + details.StatusEnum);
  $statusName.html(details.StatusName);
  $statusUser.html(`${details.User.FirstName[0]}. ${details.User.LastName}`);
}
export function updateCardStatusButtons($card: JQuery<HTMLElement>, publishingStatusEnum: PublicationStatus) {
  $card.find('.card-button-dynamic').hide();
  if (publishingStatusEnum === PublicationStatus.Validating ||
      publishingStatusEnum === PublicationStatus.Queued) {
    $card.find('.card-button-cancel').css('display', 'flex');
  } else if (publishingStatusEnum === PublicationStatus.Processed) {
    $card.find('.card-button-checkmark').css('display', 'flex');
  } else if (publishingStatusEnum !== PublicationStatus.Processing) {
    $card.find('.card-button-upload').css('display', 'flex');
    $card.find('.card-button-delete').css('display', 'flex');
  }
}
export function updateFormStatusButtons() {
  // get the selected card's status by parsing its status container class.
  const selectedCard = $('#root-content-items [selected]').parent().find('.card-status-container')[0];
  const statusClass = selectedCard
    && selectedCard.className.split(' ').filter((className) => className.indexOf('status-') === 0)[0];
  const statusEnum = statusClass && parseInt(statusClass.split('-')[1], 10);
  const $statusFormContainer = $('#content-publishing-form').find('.form-status-container');
  $statusFormContainer.hide();

  if (statusEnum === undefined
   || statusEnum === PublicationStatus.Unknown
   || statusEnum === PublicationStatus.Error
   || statusEnum === PublicationStatus.Canceled
   || statusEnum === PublicationStatus.Replaced
   || statusEnum === PublicationStatus.Confirmed) {
    $statusFormContainer.filter('.form-status-edit-or-republish').show();
  } else {
    $statusFormContainer.filter('.form-status-edit').show();
  }
}

// Dialog helpers
export function confirmAndContinue(dialogConstructor, form?: FormBase, onContinue?) {
  if (form && form.modified) {
    new dialogConstructor(() => {
      // Assigning to access mode forces the form to reset
      form.accessMode = form.accessMode;
      if (onContinue) {
        onContinue();
      }
    }).open();
  } else if (onContinue) {
    onContinue();
  }
}

export function confirmAndContinueForm(onContinue, condition = true) {
  if (condition) {
    new ResetConfirmationDialog(onContinue).open();
  } else {
    onContinue();
  }
}

// fetch helpers
export function getData(url = '', data = {}) {
  const queryParams: string[] = [];
  Object.keys(data).forEach((key) => {
    if (Object.prototype.hasOwnProperty.call(data, key)) {
      queryParams.push(`${key}=${data[key]}`);
    }
  });
  url = `${url}?${queryParams.join('&')}`;
  return fetch(url, {
    method: 'GET',
    cache: 'no-cache',
    credentials: 'same-origin',
  })
  .then((response) => {
    if (!response.ok) {
      throw new Error(response.headers.get('Warning') || 'Unknown error');
    }
    return response.json();
  });
}

export function postData(url: string = '', data: object = {}, rawResponse: boolean = false) {
  const antiforgeryToken = document.querySelector('input[name="__RequestVerificationToken"]').getAttribute('value');
  const formData = Object.keys(data).map((key) => {
    if (Object.prototype.hasOwnProperty.call(data, key)) {
      return `${key}=${data[key]}`;
    }
    return null;
  }).filter((kvp) => kvp !== null).join('&');
  return fetch(url, {
    method: 'POST',
    cache: 'no-cache',
    headers: {
      'Content-Type': 'application/x-www-form-urlencoded',
      'RequestVerificationToken': antiforgeryToken,
    },
    credentials: 'same-origin',
    body: formData,
  })
  .then((response) => {
    if (!response.ok) {
      throw new Error(response.headers.get('Warning') || 'Unknown error');
    }
    return rawResponse
      ? response
      : response.json();
  });
}
