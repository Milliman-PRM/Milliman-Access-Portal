// Class declarations
var Card;
var ActionCard;
var AddClientActionCard;
var AddUserActionCard;
var InsertCard;
var AddChildInsertCard;
var ClientCard;
var UserCard;

(function () {
  // Helper function declarations
  var toAttr;

  // General click handler declarations
  var expandCollapse;

  var cardLayout = {
    card: {
      main: {
        icons: {
          icon: {}
        },
        text: {
          primaryText: {},
          secondaryText: {}
        },
        statistics: {
          statistic: {}
        },
        side: {
          button: {}
        }
      },
      detail: {
        detailText: {},
        toggle: {},
        listItem: {}
      },
      action: {},
      insert: {}
    }
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
          '</h2>'
        ].join(''),
        render: function (component) {
          return function (properties) {
            this.verify(component);
            this.attr(component, { href: '#action-icon-' + properties.icon }, '[href]');
            this.html(component, properties.text, 'span');
          };
        }
      },
      insert: {
        count: '?',
        selector: '.card-body-primary-text',
        html: [
          '<h2 class="card-body-primary-text">',
          '  <span></span>',
          '  <svg class="new-child-icon">',
          '    <use href=""></use>',
          '  </svg>',
          '</h2>'
        ].join(''),
        render: function (component) {
          return function (properties) {
            this.verify(component);
            this.attr(component, { href: '#action-icon-' + properties.icon }, '[href]');
            this.html(component, properties.text, 'span');
          };
        }
      },
      primaryText: {
        count: '1',
        selector: '.card-body-primary-text',
        html: [
          '<h2 class="card-body-primary-text"></h2>',
          '<stub />'
        ].join(''),
        render: function (component) {
          return function (properties) {
            this.verify(component);
            this.html(component, properties.text);
          };
        }
      },
      secondaryText: {
        count: '*',
        selector: '.card-body-secondary-text',
        html: [
          '<p class="card-body-secondary-text"></p>',
          '<stub />'
        ].join(''),
        render: function (component) {
          return function (properties) {
            this.add(component);
            this.html(component, properties.text);
          };
        }
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
          '<stub />'
        ].join(''),
        render: function (component) {
          return function (properties) {
            this.add(component);
            this.attr(component, { href: '#action-icon-' + properties.icon }, '[href]');
            this.html(component, properties.value, '.card-stat-value');
            this.tooltip(component, properties.tooltip);
          };
        }
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
          '<stub />'
        ].join(''),
        render: function (component) {
          return function (properties) {
            this.add(component);
            this.attr(component, { href: '#action-icon-' + properties.icon }, '[href]');
            this.addClass(component, 'card-button-' + properties.color);
            this.tooltip(component, properties.tooltip);
            this.click(component, properties.callback);
          };
        }
      },
      icon: {
        count: '*',
        selector: 'svg',
        html: [
          '<svg class="">',
          '  <use href=""></use>',
          '</svg>',
          '<stub />'
        ].join(''),
        render: function (component) {
          return function (properties) {
            this.add(component);
            this.attr(component, { href: '#action-icon-' + properties.icon }, '[href]');
            this.addClass(component, properties.class);
          };
        }
      },
      detailText: {
        count: '?',
        selector: '.card-expansion-category-label',
        html: [
          '<h4 class="card-expansion-category-label"></h4>',
          '<stub />'
        ].join(''),
        render: function (component) {
          return function (properties) {
            this.verify(component);
            this.html(component, properties.text);
          };
        }
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
          '<stub />'
        ].join(''),
        render: function (component) {
          return function (properties) {
            this.add(component);
            this.attr(
              component,
              Object.assign(
                {
                  name: properties.id,
                  id: properties.id
                },
                toAttr(properties.data)
              ),
              '.toggle-switch-checkbox'
            );
            this.prop(component, { checked: properties.checked }, '.toggle-switch-checkbox');
            this.click(component, properties.callback, '.toggle-switch-checkbox');
            this.attr(component, { for: properties.id }, '.toggle-switch-label');
            this.html(component, properties.text, '.switch-label');
          };
        }
      },
      listItem: {
        count: '*',
        render: function () { return undefined; }
      }
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
          '</li>'
        ].join(''),
        render: function (component) {
          return function (properties) {
            this.verify(component);
            if (Object.hasOwnProperty.call(properties, 'id')) {
              this.attr(component, { id: properties.id });
              this.addClass(component, 'card-100 action-card');
            }
            if (Object.hasOwnProperty.call(properties, 'class')) {
              this.addClass(component, properties.class);
            }
          };
        }
      },
      main: {
        count: '1',
        selector: '.card-body-main-container',
        html: [
          '<div class="card-body-main-container">',
          '  <stub />',
          '</div>',
          '<stub />'
        ].join(''),
        render: function (component) {
          return function () {
            this.verify(component);
          };
        }
      },
      icons: {
        count: '1',
        selector: '.card-body-secondary-container',
        html: [
          '<div class="card-body-secondary-container">',
          '  <stub />',
          '</div>',
          '<stub />'
        ].join(''),
        render: function (component) {
          return function () {
            this.verify(component);
          };
        }
      },
      text: {
        count: '1',
        selector: '.card-body-primary-container',
        html: [
          '<div class="card-body-primary-container">',
          '  <stub />',
          '</div>',
          '<stub />'
        ].join(''),
        render: function (component) {
          return function () {
            this.verify(component);
          };
        }
      },
      statistics: {
        count: '1',
        selector: '.card-stats-container',
        html: [
          '<div class="card-stats-container">',
          '  <stub />',
          '</div>',
          '<stub />'
        ].join(''),
        render: function (component) {
          return function () {
            this.verify(component);
          };
        }
      },
      side: {
        count: '1',
        selector: '.card-button-side-container',
        html: [
          '<div class="card-button-side-container">',
          '  <stub />',
          '</div>',
          '<stub />'
        ].join(''),
        render: function (component) {
          return function () {
            this.verify(component);
          };
        }
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
          '</div>'
        ].join(''),
        render: function (component) {
          return function () {
            this.verify(component);
            this.click(component, expandCollapse, '.card-button-background');
            this.tooltip(component, 'Expand card', '.card-button-background');
          };
        }
      }
    }
  );

  // Compute select properties
  (function () {
    var setParents = function (layout, parent) {
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
    Object.keys(components).forEach(function (key) {
      components[key].render = components[key].render(key);
    });
  }());


  // Helper function definitions
  toAttr = function (data) {
    var attrs = {};
    Object.keys(data).forEach(function (key) {
      if (Object.hasOwnProperty.call(data, key)) {
        attrs['data-' + key] = data[key];
      }
    });
    return attrs;
  };


  // General click handler definitions
  expandCollapse = function (event) {
    event.stopPropagation();
    $(this).closest('.card-container')
      .find('div.card-expansion-container')
      .attr('maximized', function (index, attr) {
        if (attr === '') {
          $(this).find('.tooltip').tooltipster('content', 'Expand card');
          return null;
        }
        $(this).find('.tooltip').tooltipster('content', 'Collapse card');
        return '';
      });
    // showRelevantUserActionIcons();
  };


  // Class definitions
  Card = function (representation) {
    this.components = [];
    this.$representation = $(representation || components.card.html);
  };

  Card.prototype.exists = function (name) {
    if (name === '') return true; // base case
    if (!Object.hasOwnProperty.call(components, name)) {
      // invalid component
      return false;
    }
    return this.$representation.find(components[name].selector).length > 0;
  };

  Card.prototype.addComponent = function (name, properties) {
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

  Card.prototype.setData = function (data) {
    this.data = data;
  };

  Card.prototype.setCallback = function (callback) {
    this.callback = callback;
  };

  Card.prototype.renderComponent = function (component, properties) {
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

  Card.prototype.build = function () {
    var self = this;
    this.$representation = $('<stub />'); // own function
    Object.keys(this.components).forEach(function (key) {
      if (Object.hasOwnProperty.call(self.components, key)) {
        self.components[key].forEach(function (properties) {
          self.renderComponent(key, properties);
        });
      }
    });
    if (this.data) {
      this.attr('card', toAttr(this.data));
    }
    this.$representation.find('.card-container').click(this.callback);
    this.$representation.find('stub').remove();
    return this.$representation;
  };


  ActionCard = function (icon, text, callback) {
    Card.call(this);

    this.addComponent('card', {
      id: text.toLowerCase().split(' ').join('-') + '-card'
    });
    this.addComponent('action', {
      icon: icon,
      text: text
    });
    this.setCallback(callback);
  };
  ActionCard.prototype = Object.create(Card.prototype);
  ActionCard.prototype.constructor = ActionCard;

  AddClientActionCard = function (callback) {
    ActionCard.call(this, 'add', 'New Client', callback);
  };
  AddClientActionCard.prototype = Object.create(ActionCard.prototype);
  AddClientActionCard.prototype.constructor = AddClientActionCard;

  AddUserActionCard = function (callback) {
    ActionCard.call(this, 'add', 'Add User', callback);
  };
  AddUserActionCard.prototype = Object.create(ActionCard.prototype);
  AddUserActionCard.prototype.constructor = AddUserActionCard;

  InsertCard = function (icon, text, level, callback) {
    Card.call(this);

    this.addComponent('card', {
      class: [
        'card-container',
        'flex-container',
        'flex-row-no-wrap',
        'items-align-center',
        'client-insert',
        'card-' + (100 - (10 * level))
      ].join(' ')
    });
    this.addComponent('insert', {
      icon: icon,
      text: text
    });
    this.setCallback(callback);
  };
  InsertCard.prototype = Object.create(Card.prototype);
  InsertCard.prototype.constructor = InsertCard;

  AddChildInsertCard = function (level, callback) {
    InsertCard.call(this, 'expand-card', 'New Sub-Client', level, callback);
  };
  AddChildInsertCard.prototype = Object.create(InsertCard.prototype);
  AddChildInsertCard.prototype.constructor = AddChildInsertCard;


  ClientCard = function (
    clientName, clientCode, userCount, reportCount, level, clientId,
    callback, deleteCallback, editCallback, newChildCallback
  ) {
    Card.call(this);

    this.addComponent('card', { class: 'card-' + (100 - (10 * level)) });
    this.addComponent('primaryText', { text: clientName });
    this.addComponent('secondaryText', { text: clientCode });
    this.addComponent('statistic', {
      icon: 'users',
      value: userCount,
      tooltip: 'Assigned users'
    });
    this.addComponent('statistic', {
      icon: 'reports',
      value: reportCount,
      tooltip: 'Reports'
    });
    this.addComponent('button', {
      icon: 'delete',
      color: 'red',
      tooltip: 'Delete client',
      callback: deleteCallback
    });
    this.addComponent('button', {
      icon: 'edit',
      color: 'blue',
      tooltip: 'Edit client',
      callback: editCallback
    });
    this.addComponent('button', {
      icon: 'add',
      color: 'green',
      tooltip: 'Add sub-client',
      callback: newChildCallback
    });

    this.setData({
      'search-string': [clientName, clientCode].join('|').toUpperCase(),
      'client-id': clientId
    });

    this.setCallback(callback);
  };
  ClientCard.prototype = Object.create(Card.prototype);
  ClientCard.prototype.constructor = ClientCard;

  UserCard = function (
    firstName, lastName, userName, email, userId, clientId,
    roles, roleCallback, removeCallback
  ) {
    var names = [];

    Card.call(this);

    names.push(email);
    if (userName !== email) {
      names.push(userName);
    }
    if (firstName && lastName) {
      names.push([firstName, lastName].join(' '));
    }

    this.addComponent('icon', { icon: 'user', class: 'card-user-icon' });
    this.addComponent('icon', { icon: 'add', class: 'card-user-role-indicator' });
    this.addComponent('primaryText', { text: names.pop() });
    names.reverse().forEach(function (name) {
      this.addComponent('secondaryText', { text: name });
    }, this);
    this.addComponent('button', {
      icon: 'remove',
      color: 'red',
      tooltip: 'Remove user',
      callback: removeCallback
    });
    this.addComponent('detailText', { text: 'User roles' });
    roles.forEach(function (role) {
      this.addComponent('toggle', {
        text: role.RoleDisplayValue,
        id: 'user-role-' + userId + '-' + role.RoleEnum,
        data: {
          'role-enum': role.RoleEnum
        },
        checked: role.IsAssigned,
        callback: roleCallback
      });
    }, this);
    this.setData({ 'user-id': userId });
    this.setCallback(expandCollapse);
  };
  UserCard.prototype = Object.create(Card.prototype);
  UserCard.prototype.constructor = UserCard;


  ['html', 'attr', 'prop', 'addClass', 'click'].forEach(function (func) {
    Card.prototype[func] = function (component, value, selector) {
      this.findComponent(component, selector)[func](value);
    };
  });

  Card.prototype.tooltip = function (component, value, selector) {
    var $component = this.findComponent(component, selector);
    $component.addClass('tooltip');
    $component.attr('title', value);
  };


  Card.prototype.componentPath = function (component) {
    var parent = components[component].parent;
    if (Object.hasOwnProperty.call(components, parent)) {
      return this.componentPath(parent).concat([component]);
    }
    return [component];
  };

  Card.prototype.verify = function (component, partialPath) {
    var self = this;
    var path = partialPath || this.componentPath(component);
    var prevSelector = '';
    $.each(path, function () {
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

  Card.prototype.add = function (component, partialPath) {
    var path = partialPath || this.componentPath(component);
    var newElement = path.pop();
    var prevSelector = this.verify(component, path);
    this.$representation
      .find(prevSelector + 'stub')
      .replaceWith(components[newElement].html);
    return prevSelector + components[newElement].selector;
  };

  Card.prototype.findComponent = function (component, selector) {
    var $component = components[component]
      ? this.$representation
        .find(components[component].selector)
        .last()
      : this.$representation;
    var $subcomponent = $component.find(selector);
    return $subcomponent.length ? $subcomponent : $component;
  };
}());
