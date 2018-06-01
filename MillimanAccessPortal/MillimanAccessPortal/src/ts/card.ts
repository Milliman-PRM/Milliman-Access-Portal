import shared = require('./shared');

var card = {};

var cardLayout = {
  card: {
    body: {
      main: {
        icons: {
          icon: {},
        },
        text: {
          primaryText: {},
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
      },
      action: {},
      insert: {},
    },
    status: {},
  },
};

var components = Object.assign(
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
      render: function(component) {
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
      render: function(component) {
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
      render: function(component) {
        return function(properties) {
          this.verify(component);
          this.html(component, properties.text);
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
      render: function(component) {
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
      render: function(component) {
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
        '</div>',
        '<stub />',
      ].join(''),
      render: function(component) {
        return function(properties) {
          if (this.readonly || this.disabled) {
            return;
          }
          this.add(component);
          this.attr(component, { href: '#action-icon-' + properties.icon }, '[href]');
          this.addClass(component, 'card-button-' + properties.color);
          this.addClass(component, 'card-button-' + properties.icon);
          this.tooltip(component, properties.tooltip);
          this.click(component, properties.callback);
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
      render: function(component) {
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
      render: function(component) {
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
      render: function(component) {
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
    detailItem: {
      count: '*',
      selector: '.detail-item',
      html: [
        '<h4 class="detail-item"></h4>',
        '<stub />',
      ].join(''),
      render: function(component) {
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
      render: function(component) {
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
      render: function(component) {
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
      render: function(component) {
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
      render: function(component) {
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
      render: function(component) {
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
      render: function(component) {
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
      render: function(component) {
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
      render: function(component) {
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
      render: function(component) {
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
      render: function(component) {
        return function() {
          this.verify(component);
        };
      },
    },
  },
);

// Compute select properties
(function() {
  var setParents = function(layout, parent) {
    var keys = Object.keys(layout);
    var i;
    for (i = 0; i < keys.length; i += 1) {
      if (Object.hasOwnProperty.call(layout, keys[i])) {
        components[keys[i]].parent = parent;
        setParents(layout[keys[i]], keys[i]);
      }
    }
  };
  setParents(cardLayout, '');
  Object.keys(components).forEach(function(key) {
    components[key].render = components[key].render(key);
  });
}());

// Helper function definitions
function toAttr(data) {
  var attrs = {};
  Object.keys(data).forEach(function(key) {
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
  var parent;
  if (!Object.hasOwnProperty.call(components, component)) {
    // invalid component
    return;
  }
  parent = components[component].parent;
  if (Object.hasOwnProperty.call(properties, parent) || !this.exists(parent)) {
    this.renderComponent(parent, (properties && properties.parent) || properties);
  }
  components[component].render.call(this, properties);
};

Card.prototype.build = function() {
  var self = this;
  this.$representation = $('<stub />'); // own function
  Object.keys(this.components).forEach(function(key) {
    if (Object.hasOwnProperty.call(self.components, key)) {
      self.components[key].forEach(function(properties) {
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
  return this.$representation;
}

['html', 'attr', 'prop', 'addClass'].forEach((func) => {
  Card.prototype[func] = function(component, value, selector) {
    this.findComponent(component, selector)[func](value);
  };
});

Card.prototype.click = function(component, value, selector) {
  var $component = this.findComponent(component, selector);
  $component.click(component !== 'body' && (this.readonly || this.disabled)
    ? function(event) {
      event.preventDefault();
    }
    : value);
  $component.mousedown(function(event) {
    event.preventDefault();
  });
};

Card.prototype.tooltip = function(component, value, selector) {
  var $component = this.findComponent(component, selector);
  $component.addClass('tooltip');
  $component.attr('title', value);
};

Card.prototype.findComponent = function(component, selector) {
  var $component = components[component]
    ? this.$representation
      .find(components[component].selector)
      .last()
    : this.$representation;
  var $subcomponent = $component.find(selector);
  return $subcomponent.length ? $subcomponent : $component;
};

Card.prototype.componentPath = function(component) {
  var parent = components[component].parent;
  if (Object.hasOwnProperty.call(components, parent)) {
    return this.componentPath(parent).concat([component]);
  }
  return [component];
};

Card.prototype.verify = function(component, partialPath) {
  var self = this;
  var path = partialPath || this.componentPath(component);
  var prevSelector = '';
  $.each(path, function() {
    var nextSelector = prevSelector + components[this].selector;
    var element = self.$representation.find(nextSelector);
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
  var path = partialPath || this.componentPath(component);
  var newElement = path.pop();
  var prevSelector = this.verify(component, path);
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
    icon: icon,
    text: text,
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
    icon: icon,
    text: text,
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
    value: userCount,
    tooltip: 'Assigned users',
  });
  this.addComponent('statistic', {
    icon: 'reports',
    value: reportCount,
    tooltip: 'Reports',
  });
  this.addComponent('button', {
    icon: 'delete',
    color: 'red',
    tooltip: 'Delete client',
    callback: deleteCallback,
  });
  this.addComponent('button', {
    icon: 'edit',
    color: 'blue',
    tooltip: 'Edit client',
    callback: editCallback,
  });
  this.addComponent('button', {
    icon: 'add',
    color: 'green',
    tooltip: 'Add sub-client',
    callback: newChildCallback,
  });
  this.data = {
    'filter-string': [client.Name, client.ClientCode].join('|').toUpperCase(),
    'client-id': client.Id,
  };
  this.callback = callback;
}
ClientCard.prototype = Object.create(Card.prototype);
ClientCard.prototype.constructor = ClientCard;

export function RootContentItemCard(
  rootContentItem, groupCount, userCount,
  callback, publishCallback?, deleteCallback?, cancelCallback?, goLiveCallback?,
) {
  Card.call(this);

  this.addComponent('primaryText', { text: rootContentItem.ContentName });
  this.addComponent('secondaryText', { text: rootContentItem.ContentTypeName });
  this.addComponent('statistic', {
    icon: 'users',
    value: groupCount,
    tooltip: 'Selection groups',
  });
  this.addComponent('statistic', {
    icon: 'user',
    value: userCount,
    tooltip: 'Eligible users',
  });
  this.addComponent('button', {
    icon: 'delete',
    color: 'red',
    tooltip: 'Delete root content item',
    callback: deleteCallback,
  });
  this.addComponent('button', {
    icon: 'file-upload',
    color: 'green',
    tooltip: 'Republish',
    callback: publishCallback,
    dynamic: true,
  });
  this.addComponent('button', {
    icon: 'cancel',
    color: 'red',
    tooltip: 'Cancel Request',
    callback: cancelCallback,
    dynamic: true,
  });
  this.addComponent('button', {
    icon: 'add',
    color: 'blue',
    tooltip: 'Go Live',
    callback: goLiveCallback,
    dynamic: true,
  });
  this.addComponent('status', {});

  this.data = {
    'filter-string': [
      rootContentItem.ContentName,
      rootContentItem.ContentTypeName,
    ].join('|').toUpperCase(),
    'root-content-item-id': rootContentItem.Id,
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
  selectionGroup, members,
  callback, deleteCallback, userCallback,
) {
  var memberInfo;
  Card.call(this);

  memberInfo = $.map(members, function toString(member) {
    return [member.FirstName + ' ' + member.LastName, member.Email, member.UserName];
  }).reduce(function concat(acc, cur) {
    return acc.concat(cur);
  }, []);

  this.addComponent('primaryText', { text: selectionGroup.GroupName });
  this.addComponent('statistic', {
    icon: 'users',
    value: members.length,
    tooltip: 'Members',
  });
  this.addComponent('button', {
    icon: 'delete',
    color: 'red',
    tooltip: 'Delete selection group',
    callback: deleteCallback,
  });
  this.addComponent('button', {
    icon: 'edit',
    color: 'blue',
    tooltip: 'Add/remove users',
    callback: userCallback,
  });
  this.addComponent('statistics', { click: shared.toggleExpandedListener });
  if (members.length) {
    this.addComponent('detailText', { text: 'Members' });
  }
  members.forEach(function(member) {
    this.addComponent('detailItem', { text: member.Email });
  }, this);
  this.addComponent('status', {});

  this.data = {
    'filter-string': memberInfo.concat([selectionGroup.GroupName]).join('|').toUpperCase(),
    'selection-group-id': selectionGroup.Id,
  };

  this.callback = callback;
}
SelectionGroupCard.prototype = Object.create(Card.prototype);
SelectionGroupCard.prototype.constructor = SelectionGroupCard;

export function UserCard(
  user, client,
  roleCallback, removeCallback,
) {
  var names = [];

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
    icon: 'remove',
    color: 'red',
    tooltip: 'Remove user',
    callback: removeCallback,
  });
  this.addComponent('detailText', { text: 'User roles' });
  user.UserRoles.forEach(function(role) {
    this.addComponent('toggle', {
      text: role.RoleDisplayValue,
      id: 'user-role-' + user.Id + '-' + role.RoleEnum,
      data: {
        'role-enum': role.RoleEnum,
      },
      checked: role.IsAssigned,
      callback: roleCallback,
    });
  }, this);
  this.data = {
    'filter-string': names.join('|').toUpperCase(),
    'user-id': user.Id,
    'client-id': client.Id,
  };
  this.callback = shared.toggleExpandedListener;
}
UserCard.prototype = Object.create(Card.prototype);
UserCard.prototype.constructor = UserCard;
