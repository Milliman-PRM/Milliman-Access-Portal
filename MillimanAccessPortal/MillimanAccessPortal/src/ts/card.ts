import * as toastr from 'toastr';

import * as shared from './shared';
import { SelectionGroupSummary } from './view-models/content-access-admin';
import { RootContentItemSummary, UserInfo } from './view-models/content-publishing';

const card = {};

// tslint:disable:object-literal-sort-keys
const cardLayout = {
  card: {
    body: {
      main: {
        icons: {
          icon: {},
        },
        text: {
          primaryText: {},
          primaryTextBox: {},
          secondaryText: {},
          progressInfo: {},
        },
        statistics: {
          statistic: {},
        },
        side: {
          button: {},
        },
      },
      detail: {
        detailText: {},
        toggle: {},
        detailItem: {},
        userList: {
          user: {},
        },
        userCreate: {},
      },
      action: {},
      insert: {},
    },
    status: {},
  },
};

const components = Object.assign(
  {},
  {
    action: {
      count: '?',
      selector: '.card-body-primary-text',
      html: [
        '<h2 class="card-body-primary-text">',
        '  <svg class="action-card-icon">',
        '    <use href=""></use>',
        '  </svg>',
        '  <span></span>',
        '</h2>',
      ].join(''),
      render(component) {
        return function(properties) {
          this.verify(component);
          this.attr(component, { href: '#action-icon-' + properties.icon }, '[href]');
          this.html(component, properties.text, 'span');
        };
      },
    },
    insert: {
      count: '?',
      selector: '.card-body-primary-text',
      html: [
        '<div class="card-body-main-container content-item-flex-1">',
        '  <div class="card-body-primary-container">',
        '    <h2 class="card-body-primary-text indent-level-1">',
        '      <span></span>',
        '      <svg class="new-child-icon">',
        '        <use href=""></use>',
        '      </svg>',
        '    </h2>',
        '  </div>',
        '</div>',
      ].join(''),
      render(component) {
        return function(properties) {
          this.verify(component);
          this.attr(component, { href: '#action-icon-' + properties.icon }, '[href]');
          this.html(component, properties.text, 'span');
        };
      },
    },
    primaryText: {
      count: '1',
      selector: '.card-body-primary-text',
      html: [
        '<h2 class="card-body-primary-text"></h2>',
        '<stub />',
      ].join(''),
      render(component) {
        return function(properties) {
          this.verify(component);
          this.html(component, properties.text);
        };
      },
    },
    primaryTextBox: {
      count: '1',
      selector: '.card-body-primary-text-box',
      html: [
        '<div class="card-body-primary-text-box">',
        '  <h2></h2>',
        '  <input placeholder="Untitled" />',
        '</div>',
        '<stub />',
      ].join(''),
      render(component) {
        return function(properties) {
          this.verify(component);
          this.html(component, properties.text, 'h2');
          this.val(component, properties.text, 'input');
          this.click(component, (event) => {
            event.stopPropagation();
          }, 'input');
        };
      },
    },
    secondaryText: {
      count: '*',
      selector: '.card-body-secondary-text',
      html: [
        '<p class="card-body-secondary-text"></p>',
        '<stub />',
      ].join(''),
      render(component) {
        return function(properties) {
          this.add(component);
          this.html(component, properties.text);
        };
      },
    },
    statistic: {
      count: '*',
      selector: '.card-stat-container',
      html: [
        '<div class="card-stat-container">',
        '  <svg class="card-stat-icon">',
        '    <use href=""></use>',
        '  </svg>',
        '  <h4 class="card-stat-value"></h4>',
        '</div>',
        '<stub />',
      ].join(''),
      render(component) {
        return function(properties) {
          this.add(component);
          this.attr(component, { href: '#action-icon-' + properties.icon }, '[href]');
          this.html(component, properties.value, '.card-stat-value');
          this.tooltip(component, properties.tooltip);
        };
      },
    },
    button: {
      count: '*',
      selector: '.card-button-background',
      html: [
        '<div class="card-button-background tooltip" title="">',
        '  <svg class="card-button-icon">',
        '    <use href=""></use>',
        '  </svg>',
        '  <div class="card-button-clickable"></div>',
        '</div>',
        '<stub />',
      ].join(''),
      render(component) {
        return function(properties) {
          if (this.readonly || this.disabled) {
            return;
          }
          this.add(component);
          this.attr(component, { href: '#action-icon-' + properties.icon }, '[href]');
          this.addClass(component, 'card-button-' + properties.color);
          this.addClass(component, 'card-button-' + properties.icon);
          this.tooltip(component, properties.tooltip);
          this.click(component, properties.callback, '.card-button-clickable');
          if (properties.dynamic) {
            this.addClass(component, 'card-button-dynamic');
          }
        };
      },
    },
    icon: {
      count: '*',
      selector: 'svg',
      html: [
        '<svg class="">',
        '  <use href=""></use>',
        '</svg>',
        '<stub />',
      ].join(''),
      render(component) {
        return function(properties) {
          this.add(component);
          this.attr(component, { href: '#action-icon-' + properties.icon }, '[href]');
          this.addClass(component, properties.class);
        };
      },
    },
    detailText: {
      count: '?',
      selector: '.card-expansion-category-label',
      html: [
        '<h4 class="card-expansion-category-label"></h4>',
        '<stub />',
      ].join(''),
      render(component) {
        return function(properties) {
          this.verify(component);
          this.html(component, properties.text);
        };
      },
    },
    toggle: {
      count: '*',
      selector: '.switch-container',
      html: [
        '<div class="switch-container">',
        '  <div class="toggle-switch">',
        '    <input type="checkbox" class="toggle-switch-checkbox" name="" id="">',
        '    <label class="toggle-switch-label" for="">',
        '      <span class="toggle-switch-inner"></span>',
        '      <span class="toggle-switch-switch"></span>',
        '    </label>',
        '  </div>',
        '  <label class="switch-label"></label>',
        '</div>',
        '<stub />',
      ].join(''),
      render(component) {
        return function(properties) {
          this.add(component);
          this.attr(
            component,
            Object.assign(
              {
                name: properties.id,
                id: properties.id,
              },
              toAttr(properties.data),
            ),
            '.toggle-switch-checkbox',
          );
          this.prop(component, { checked: properties.checked }, '.toggle-switch-checkbox');
          this.click(component, properties.callback, '.toggle-switch-checkbox');
          this.attr(component, { for: properties.id }, '.toggle-switch-label');
          this.html(component, properties.text, '.switch-label');
        };
      },
    },
    userList: {
      count: '1',
      selector: '.detail-item-user-list',
      html: [
        '<ul class="detail-item-user-list">',
        '  <stub />',
        '</ul>',
        '<stub />',
      ].join(''),
      render(component) {
        return function(properties) {
          this.verify(component);
        };
      },
    },
    // If you make and changes to this component, also change updateMemberList in shared.ts
    user: {
      count: '*',
      selector: '.detail-item-user',
      html: [
        '<li>',
        '  <span class="detail-item-user">',
        '    <div class="detail-item-user-icon">',
        '      <svg class="card-user-icon">',
        '        <use href="#action-icon-user"></use>',
        '      </svg>',
        '    </div>',
        '    <div class="detail-item-user-remove">',
        '      <div class="card-button-background card-button-delete">',
        '        <svg class="card-button-icon">',
        '          <use href="#action-icon-remove-circle"></use>',
        '        </svg>',
        '      </div>',
        '    </div>',
        '    <div class="detail-item-user-name">',
        '      <h4 class="first-last"></h4>',
        '      <span class="user-name"></span>',
        '    </div>',
        '  </span>',
        '</li>',
        '<stub />',
      ].join(''),
      render(component) {
        return function(properties) {
          this.add(component);
          this.attr(
            component,
            toAttr(properties.data),
            '.detail-item-user',
          );
          this.click(component, properties.callback, '.detail-item-user-remove');
          this.html(component, properties.username, '.user-name');
          this.html(component, properties.firstlast, '.first-last');
        };
      },
    },
    userCreate: {
      count: '1',
      selector: '.detail-item-user-create',
      html: [
        '<span class="detail-item-user-create">',
        '  <div class="detail-item-user-add">',
        '    <div class="card-button-background card-button-add">',
        '      <svg class="card-button-icon">',
        '        <use href="#action-icon-add-circle"></use>',
        '      </svg>',
        '    </div>',
        '  </div>',
        '  <div class="detail-item-user-input">',
        '    <input class="typeahead" name="username" placeholder="Add user" required />',
        '  </div>',
        '</span>',
        '<stub />',
      ].join(''),
      render(component) {
        return function(properties) {
          this.add(component);
          this.click(component, properties.addCallback, '.detail-item-user-add');
          this.click(component, properties.inputCallback, '.detail-item-user-input');
          this.key(component, properties.keyCallback, '.detail-item-user-input');
        };
      },
    },
    detailItem: {
      count: '*',
      selector: '.detail-item',
      html: [
        '<h4 class="detail-item"></h4>',
        '<stub />',
      ].join(''),
      render(component) {
        return function(properties) {
          this.add(component);
          this.html(component, properties.text);
        };
      },
    },
    status: {
      count: '?',
      selector: '.card-status-container',
      html: [
        '<div class="card-status-container status-0">',
        '  <span>',
        '    <strong></strong>',
        '    <em>Name</em>',
        '  </span>',
        '</div>',
      ].join(''),
      render(component) {
        return function() {
          this.verify(component);
        };
      },
    },
  },
  {
    card: {
      count: '1',
      selector: '.card-container',
      html: [
        '<li>',
        '  <div class="card-container">',
        '    <stub />',
        '  </div>',
        '</li>',
      ].join(''),
      render(component) {
        return function(properties) {
          this.verify(component);
          if (Object.hasOwnProperty.call(properties, 'id')) {
            this.attr(component, { id: properties.id });
          }
          if (Object.hasOwnProperty.call(properties, 'class')) {
            this.addClass(component, properties.class);
          }
          if (this.readonly || this.disabled) {
            this.attr(component, { disabled: '' });
          }
        };
      },
    },
    body: {
      count: '1',
      selector: '.card-body-container',
      html: [
        '<div class="card-body-container">',
        '  <stub />',
        '</div>',
        '<stub />',
      ].join(''),
      render(component) {
        return function(properties) {
          this.verify(component);
          if (Object.hasOwnProperty.call(properties, 'id')) {
            this.addClass(component, 'card-100 action-card');
          }
        };
      },
    },
    main: {
      count: '1',
      selector: '.card-body-main-container',
      html: [
        '<div class="card-body-main-container">',
        '  <stub />',
        '</div>',
        '<stub />',
      ].join(''),
      render(component) {
        return function() {
          this.verify(component);
        };
      },
    },
    icons: {
      count: '1',
      selector: '.card-body-secondary-container',
      html: [
        '<div class="card-body-secondary-container">',
        '  <stub />',
        '</div>',
        '<stub />',
      ].join(''),
      render(component) {
        return function() {
          this.verify(component);
        };
      },
    },
    text: {
      count: '1',
      selector: '.card-body-primary-container',
      html: [
        '<div class="card-body-primary-container">',
        '  <stub />',
        '</div>',
        '<stub />',
      ].join(''),
      render(component) {
        return function() {
          this.verify(component);
        };
      },
    },
    statistics: {
      count: '1',
      selector: '.card-stats-container',
      html: [
        '<div class="card-stats-container">',
        '  <stub />',
        '</div>',
        '<stub />',
      ].join(''),
      render(component) {
        return function(properties) {
          this.verify(component);
          this.click(component, properties.click);
        };
      },
    },
    side: {
      count: '1',
      selector: '.card-button-side-container',
      html: [
        '<div class="card-button-side-container">',
        '  <stub />',
        '</div>',
        '<stub />',
      ].join(''),
      render(component) {
        return function() {
          this.verify(component);
        };
      },
    },
    detail: {
      count: '1',
      selector: '.card-expansion-container',
      html: [
        '<div class="card-expansion-container">',
        '  <stub />',
        '  <div class="card-button-bottom-container">',
        '    <div class="card-button-background card-button-expansion">',
        '      <svg class="card-button-icon">',
        '        <use href="#action-icon-expand-card"></use>',
        '      </svg>',
        '    </div>',
        '  </div>',
        '</div>',
      ].join(''),
      render(component) {
        return function() {
          this.verify(component);
          this.click(component, shared.toggleExpandedListener, '.card-button-background');
          this.tooltip(component, 'Expand card', '.card-button-background');
        };
      },
    },
    progressInfo: {
      count: '?',
      selector: '.card-body-primary-container',
      html: [
        '<div class="card-progress">',
        '  <div class="card-progress-status">',
        '    <p class="card-progress-status-text"></p>',
        '    <div class="card-progress-status-btn btn-cancel">',
        '      <svg class="card-button-icon">',
        '        <use href="#action-icon-cancel"></use>',
        '      </svg>',
        '    </div>',
        '  </div>',
        '  <div class="card-progress-bars">',
        '    <div class="card-progress-bar-1"></div>',
        '    <div class="card-progress-bar-2"></div>',
        '  </div>',
        '</div>',
      ].join(''),
      render(component) {
        return function() {
          this.verify(component);
        };
      },
    },
  },
);
// tslint:enable:object-literal-sort-keys

// Compute select properties
(() => {
  const setParents = (layout, parent) => {
    const keys = Object.keys(layout);
    for (const key of keys) {
      if (Object.hasOwnProperty.call(layout, key)) {
        components[key].parent = parent;
        setParents(layout[key], key);
      }
    }
  };
  setParents(cardLayout, '');
  Object.keys(components).forEach((key) => {
    components[key].render = components[key].render(key);
  });
})();

// Helper function definitions
function toAttr(data) {
  const attrs = {};
  Object.keys(data).forEach((key) => {
    if (Object.hasOwnProperty.call(data, key)) {
      attrs['data-' + key] = data[key];
    }
  });
  return attrs;
}

// Class definitions
export function Card(representation) {
  this.components = [];
  this.data = {};
  this.callback = () => undefined;
  this.readonly = false;
  this.disabled = false;
  this.$representation = $(representation || components.card.html);
}

Card.prototype.exists = function(name) {
  if (name === '') {
    return true; // base case
  }
  if (!Object.hasOwnProperty.call(components, name)) {
    // invalid component
    return false;
  }
  return this.$representation.find(components[name].selector).length > 0;
};

Card.prototype.addComponent = function(name, properties) {
  if (!Object.hasOwnProperty.call(components, name)) {
    // invalid component
    return;
  }
  if (Object.hasOwnProperty.call(properties, 'callback') && !properties.callback) {
    // A required callback was not provided - do not render the component.
    return;
  }
  if (!this.components[name]) {
    this.components[name] = [];
  }
  this.components[name].push(properties);
};

Card.prototype.renderComponent = function(component, properties) {
  if (!Object.hasOwnProperty.call(components, component)) {
    // invalid component
    return;
  }
  const parent = components[component].parent;
  if (Object.hasOwnProperty.call(properties, parent) || !this.exists(parent)) {
    this.renderComponent(parent, (properties && properties.parent) || properties);
  }
  components[component].render.call(this, properties);
};

Card.prototype.build = function() {
  const self = this;
  this.$representation = $('<stub />'); // own function
  Object.keys(this.components).forEach((key) => {
    if (Object.hasOwnProperty.call(self.components, key)) {
      self.components[key].forEach((properties) => {
        self.renderComponent(key, properties);
      });
    }
  });
  if (this.data) {
    this.attr('card', toAttr(this.data));
  }
  if (!this.disabled) {
    this.click('body', this.callback);
  }
  this.$representation.find('stub').remove();
  if (Object.hasOwnProperty.call(this, 'afterBuild')) {
    this.afterBuild();
  }
  return this.$representation;
};

Card.prototype.html = function(component, value, selector) {
  this.findComponent(component, selector).html(value);
};
Card.prototype.attr = function(component, value, selector) {
  this.findComponent(component, selector).attr(value);
};
Card.prototype.prop = function(component, value, selector) {
  this.findComponent(component, selector).prop(value);
};
Card.prototype.addClass = function(component, value, selector) {
  this.findComponent(component, selector).addClass(value);
};
Card.prototype.val = function(component, value, selector) {
  this.findComponent(component, selector).val(value);
};

Card.prototype.click = function(component, value, selector) {
  const $component = this.findComponent(component, selector);
  $component.click(component !== 'body' && (this.readonly || this.disabled)
    ? (event) => {
      event.preventDefault();
    }
    : value);
};
Card.prototype.key = function(component, value, selector) {
  const $component = this.findComponent(component, selector);
  $component.on('keydown', value);
};

Card.prototype.tooltip = function(component, value, selector) {
  const $component = this.findComponent(component, selector);
  $component.addClass('tooltip');
  $component.attr('title', value);
};

Card.prototype.findComponent = function(component, selector) {
  const $component = components[component]
    ? this.$representation
      .find(components[component].selector)
      .last()
    : this.$representation;
  const $subcomponent = $component.find(selector);
  return $subcomponent.length ? $subcomponent : $component;
};

Card.prototype.componentPath = function(component) {
  const parent = components[component].parent;
  if (Object.hasOwnProperty.call(components, parent)) {
    return this.componentPath(parent).concat([component]);
  }
  return [component];
};

Card.prototype.verify = function(component, partialPath) {
  const self = this;
  const path = partialPath || this.componentPath(component);
  let prevSelector = '';
  $.each(path, function() {
    const nextSelector = prevSelector + components[this].selector;
    const element = self.$representation.find(nextSelector);
    if (!element.length) {
      if (prevSelector) {
        self.$representation
          .find(prevSelector + 'stub')
          .replaceWith(components[this].html);
      } else {
        self.$representation = $(components[this].html);
      }
    }
    prevSelector = nextSelector + ' > ';
  });
  return prevSelector;
};

Card.prototype.add = function(component, partialPath) {
  const path = partialPath || this.componentPath(component);
  const newElement = path.pop();
  const prevSelector = this.verify(component, path);
  this.$representation
    .find(prevSelector + 'stub')
    .replaceWith(components[newElement].html);
  return prevSelector + components[newElement].selector;
};

export function ActionCard(icon, text, callback) {
  Card.call(this);

  this.addComponent('body', {
    id: text.toLowerCase().split(' ').join('-') + '-card',
  });
  this.addComponent('action', {
    icon,
    text,
  });
  this.callback = callback;
}
ActionCard.prototype = Object.create(Card.prototype);
ActionCard.prototype.constructor = ActionCard;

export function AddClientActionCard(callback) {
  ActionCard.call(this, 'add', 'New Client', callback);
}
AddClientActionCard.prototype = Object.create(ActionCard.prototype);
AddClientActionCard.prototype.constructor = AddClientActionCard;

export function AddUserActionCard(callback) {
  ActionCard.call(this, 'add', 'Add User', callback);
}
AddUserActionCard.prototype = Object.create(ActionCard.prototype);
AddUserActionCard.prototype.constructor = AddUserActionCard;

export function AddRootContentItemActionCard(callback) {
  ActionCard.call(this, 'add', 'Add Root Content Item', callback);
}
AddRootContentItemActionCard.prototype = Object.create(ActionCard.prototype);
AddRootContentItemActionCard.prototype.constructor = AddRootContentItemActionCard;

export function AddSelectionGroupActionCard(callback) {
  ActionCard.call(this, 'add', 'Add Selection Group', callback);
}
AddSelectionGroupActionCard.prototype = Object.create(ActionCard.prototype);
AddSelectionGroupActionCard.prototype.constructor = AddSelectionGroupActionCard;

export function InsertCard(icon, text, level, callback) {
  Card.call(this);

  this.addComponent('card', {
    class: [
      'card-container',
      'flex-container',
      'flex-row-no-wrap',
      'items-align-center',
      'insert-card',
      'card-' + (100 - (10 * level)),
    ].join(' '),
  });
  this.addComponent('insert', {
    icon,
    text,
  });
  this.callback = callback;
}
InsertCard.prototype = Object.create(Card.prototype);
InsertCard.prototype.constructor = InsertCard;

export function AddChildInsertCard(level, callback?) {
  InsertCard.call(this, 'expand-card', 'New Sub-Client', level, callback);
}
AddChildInsertCard.prototype = Object.create(InsertCard.prototype);
AddChildInsertCard.prototype.constructor = AddChildInsertCard;

export function ClientCard(
  client, userCount, reportCount, level,
  callback, deleteCallback?, editCallback?, newChildCallback?,
) {
  Card.call(this);

  this.addComponent('card', { class: 'card-' + (100 - (10 * level)) });
  this.addComponent('primaryText', { text: client.Name });
  this.addComponent('secondaryText', { text: client.ClientCode });
  this.addComponent('statistic', {
    icon: 'users',
    tooltip: 'Assigned users',
    value: userCount,
  });
  this.addComponent('statistic', {
    icon: 'reports',
    tooltip: 'Reports',
    value: reportCount,
  });
  this.addComponent('button', {
    callback: deleteCallback,
    color: 'red',
    icon: 'delete',
    tooltip: 'Delete client',
  });
  this.addComponent('button', {
    callback: editCallback,
    color: 'blue',
    icon: 'edit',
    tooltip: 'Edit client',
  });
  this.addComponent('button', {
    callback: newChildCallback,
    color: 'green',
    icon: 'add',
    tooltip: 'Add sub-client',
  });
  this.data = {
    'client-id': client.Id,
    'filter-string': [client.Name, client.ClientCode].join('|').toUpperCase(),
  };
  this.callback = callback;
}
ClientCard.prototype = Object.create(Card.prototype);
ClientCard.prototype.constructor = ClientCard;

export function RootContentItemCard(
  rootContentItemDetail: any,
  callback, publishCallback?, deleteCallback?, cancelCallback?, goLiveCallback?,
) {
  Card.call(this);

  this.addComponent('primaryText', { text: rootContentItemDetail.ContentName });
  this.addComponent('secondaryText', { text: rootContentItemDetail.ContentTypeName });
  this.addComponent('statistic', {
    icon: 'users',
    tooltip: 'Selection groups',
    value: rootContentItemDetail.GroupCount,
  });
  this.addComponent('statistic', {
    icon: 'user',
    tooltip: 'Eligible users',
    value: rootContentItemDetail.EligibleUserList.length,
  });
  this.addComponent('button', {
    callback: deleteCallback,
    color: 'red',
    icon: 'delete',
    tooltip: 'Delete root content item',
  });
  this.addComponent('button', {
    callback: publishCallback,
    color: 'green',
    dynamic: true,
    icon: 'file-upload',
    tooltip: 'Republish',
  });
  this.addComponent('button', {
    callback: cancelCallback,
    color: 'red',
    dynamic: true,
    icon: 'cancel',
    tooltip: 'Cancel Request',
  });
  this.addComponent('button', {
    callback: goLiveCallback,
    color: 'blue',
    dynamic: true,
    icon: 'add',
    tooltip: 'Go Live',
  });
  this.addComponent('status', {});

  this.data = {
    'eligible-list': JSON.stringify(rootContentItemDetail.EligibleUserList),
    'filter-string': [
      rootContentItemDetail.ContentName,
      rootContentItemDetail.ContentTypeName,
    ].join('|').toUpperCase(),
    'root-content-item-id': rootContentItemDetail.Id,
  };

  this.callback = callback;
}
RootContentItemCard.prototype = Object.create(Card.prototype);
RootContentItemCard.prototype.constructor = RootContentItemCard;

export function FileUploadCard(
  contentName,
) {
  Card.call(this);

  this.addComponent('primaryText', { text: contentName });
  this.addComponent('secondaryText', { text: 'Click to select file...' });
  this.addComponent('progressInfo', {});
}
FileUploadCard.prototype = Object.create(Card.prototype);
FileUploadCard.prototype.constructor = FileUploadCard;

export function SelectionGroupCard(
  selectionGroup: SelectionGroupSummary,
  eligibleUsers: UserInfo[],
  callback, deleteCallback, editCallback, confirmCallback,
) {
  Card.call(this);

  const self = this;
  const memberInfo = $.map(selectionGroup.MemberList, function toString(member) {
    return [member.FirstName + ' ' + member.LastName, member.Email, member.UserName];
  }).reduce(function concat(acc, cur) {
    return acc.concat(cur);
  }, []);

  this.addComponent('primaryTextBox', { text: selectionGroup.Name });
  this.addComponent('secondaryText', { text: selectionGroup.RootContentItemName });
  this.addComponent('statistic', {
    icon: 'users',
    tooltip: 'Members',
    value: selectionGroup.MemberList.length,
  });
  this.addComponent('button', {
    callback: deleteCallback,
    color: 'red',
    icon: 'delete',
    tooltip: 'Delete selection group',
  });
  this.addComponent('button', {
    callback: editCallback,
    color: 'blue',
    dynamic: true,
    icon: 'edit',
    tooltip: 'Edit selection group',
  });
  this.addComponent('button', {
    callback: confirmCallback,
    color: 'green',
    dynamic: true,
    icon: 'checkmark',
    tooltip: 'Save changes',
  });
  this.addComponent('statistics', { click: shared.toggleExpandedListener });
  this.addComponent('detailText', { text: 'Members' });
  this.addComponent('userList', {});
  selectionGroup.MemberList.forEach(function(member) {
    this.addComponent('user', {
      callback: (event) => shared.removeUserFromSelectionGroup(event, member, selectionGroup),
      data: {
        'user-id': member.Id,
      },
      firstlast: `${member.FirstName} ${member.LastName}`,
      username: member.UserName,
    });
  }, this);
  this.addComponent('userCreate', {
    addCallback: (event: Event) => {
      event.stopPropagation();
      const $ttInput = $(event.target)
        .closest('.detail-item-user-create').find('.tt-input');
      const data = $ttInput.val();
      $.post({
        data: {
          SelectionGroupId: $ttInput.closest('.card-container').data().selectionGroupId,
          email: data,
        },
        headers: {
          RequestVerificationToken: $('input[name="__RequestVerificationToken"]').val().toString(),
        },
        url: 'ContentAccessAdmin/AddUserToSelectionGroup/',
      }).done((response) => {
        shared.addUserToSelectionGroup(response);
        $ttInput.typeahead('val', '');
        $ttInput.focus();
        toastr.success(`Added ${data} to selection group ${selectionGroup.Name}.`);
      }).fail((response) => {
        toastr.warning(response.getResponseHeader('Warning')
          || 'An unknown error has occurred.');
      });
    },
    inputCallback: (event: Event) => {
      event.stopPropagation();
    },
    keyCallback: (event) => {
      // Using keyCode is deprecated but has the best support across browsers
      // Key code 13 is Enter
      if (event.keyCode === 13) {
        $(event.target).closest('.detail-item-user-create').find('.detail-item-user-add').click();
      }
    },
  });
  this.addComponent('status', {});

  this.data = {
    'filter-string': memberInfo.concat([selectionGroup.Name]).join('|').toUpperCase(),
    'member-list': JSON.stringify(selectionGroup.MemberList),
    'selection-group-id': selectionGroup.Id,
  };

  this.callback = callback;

  this.afterBuild = () => {
    this.$representation.find('.card-button-side-container .card-button-green').hide();
    this.$representation.find('.detail-item-user-create').hide();
    this.$representation.find('.detail-item-user-remove').hide();
    this.$representation.find('.card-body-primary-text-box input').hide();
    this.$representation.find('.typeahead').typeahead(
      {
        highlight: true,
        hint: true,
        minLength: 1,
      },
      {
        name: 'eligibleUsers',
        source: shared.eligibleUserMatcher,
        display(data: UserInfo) {
          return data.UserName;
        },
        templates: {
          suggestion(data: UserInfo) {
            return [
              '<div>',
              data.UserName + '',
              (data.UserName !== data.Email)
                ? '<br /> ' + data.Email
                : '',
              (data.FirstName && data.LastName)
                ? '<br /><span class="secondary-text">' + data.FirstName + ' ' + data.LastName + '</span>'
                : '',
              '</div>',
            ].join('');
          },
        },
      },
    ).focus();
  };
}
SelectionGroupCard.prototype = Object.create(Card.prototype);
SelectionGroupCard.prototype.constructor = SelectionGroupCard;

export function UserCard(
  user, client,
  roleCallback, removeCallback,
) {
  const names = [];

  Card.call(this);

  if (user.FirstName && user.LastName) {
    names.push([user.FirstName, user.LastName].join(' '));
  }
  if (user.UserName !== user.Email) {
    names.push(user.UserName);
  }
  names.push(user.Email);

  this.addComponent('icon', { icon: 'user', class: 'card-user-icon' });
  this.addComponent('icon', { icon: 'add', class: 'card-user-role-indicator' });
  this.addComponent('primaryText', { text: names[0] });
  names.slice(1).forEach(function(name) {
    this.addComponent('secondaryText', { text: name });
  }, this);
  this.addComponent('button', {
    callback: removeCallback,
    color: 'red',
    icon: 'remove',
    tooltip: 'Remove user',
  });
  this.addComponent('detailText', { text: 'User roles' });
  user.UserRoles.forEach(function(role) {
    this.addComponent('toggle', {
      callback: roleCallback,
      checked: role.IsAssigned,
      data: {
        'role-enum': role.RoleEnum,
      },
      id: 'user-role-' + user.Id + '-' + role.RoleEnum,
      text: role.RoleDisplayValue,
    });
  }, this);
  this.data = {
    'client-id': client.Id,
    'filter-string': names.join('|').toUpperCase(),
    'user-id': user.Id,
  };
  this.callback = shared.toggleExpandedListener;
}
UserCard.prototype = Object.create(Card.prototype);
UserCard.prototype.constructor = UserCard;
