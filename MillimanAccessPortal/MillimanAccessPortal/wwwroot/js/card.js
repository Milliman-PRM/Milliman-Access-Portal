// Class declarations
var Card;
var ActionCard;
var ClientCard;
var UserCard;

(function () {
  // Helper function declarations
  var toAttr;
  var findComponent;

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
      action: {}
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
          '  <svg class="">',
          '    <use href=""></use>',
          '  </svg>',
          '  <span></span>',
          '</h2>'
        ].join(''),
        render: function (properties) {
          this.verify();
          this.addClass('action-card-icon', 'svg');
          this.attr({ href: properties.icon }, '[href]');
          this.html(properties.text, 'span');
        }
      },
      primaryText: {
        count: '1',
        selector: '.card-body-primary-text',
        html: [
          '<h2 class="card-body-primary-text"></h2>',
          '<stub />'
        ].join(''),
        render: function (properties) {
          this.verify();
          this.html(properties.text);
        }
      },
      secondaryText: {
        count: '*',
        selector: '.card-body-secondary-text',
        html: [
          '<p class="card-body-secondary-text"></p>',
          '<stub />'
        ].join(''),
        render: function (properties) {
          this.add();
          this.html(properties.text);
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
        render: function (properties) {
          this.add();
          this.attr({ href: properties.icon }, '[href]');
          this.html(properties.value, '.card-stat-value');
          this.tooltip(properties.tooltip);
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
        render: function (properties) {
          this.add();
          this.attr({ href: properties.icon }, '[href]');
          this.addClass('card-button-' + properties.color);
          this.tooltip(properties.tooltip);
          this.click(properties.callback);
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
        render: function (properties) {
          this.add();
          this.attr({ href: properties.icon }, '[href]');
          this.addClass(properties.class);
        }
      },
      detailText: {
        count: '?',
        selector: '.card-expansion-category-label',
        html: [
          '<h4 class="card-expansion-category-label"></h4>',
          '<stub />'
        ].join(''),
        render: function (properties) {
          this.verify();
          this.html(properties.text);
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
        render: function (properties) {
          this.add();
          this.attr(
            Object.assign(
              {
                name: properties.id,
                id: properties.id
              },
              toAttr(properties.data)
            ),
            '.toggle-switch-checkbox'
          );
          this.prop({ checked: properties.checked }, '.toggle-switch-checkbox');
          this.click(properties.callback, '.toggle-switch-checkbox');
          this.attr({ for: properties.id }, '.toggle-switch-label');
          this.html(properties.text, '.switch-label');
          // if (!this.vars.canManage) this.attr({ disabled: '' }, '.toggle-switch-checkbox');
        }
      },
      listItem: {
        count: '*'
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
        render: function (properties) {
          this.verify();
          // this.attr({ id: properties.id });
          // this.addClass('card-100 action-card');
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
        render: function () {
          this.verify();
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
        render: function () {
          this.verify();
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
        render: function () {
          this.verify();
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
        render: function () {
          this.verify();
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
        render: function () {
          this.verify();
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
        render: function (properties) {
          this.verify();
          this.click(properties.click, '.card-button-background');
        }
      }
    }
  );

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
    if (!this.components[name]) {
      this.components[name] = [];
    }
    this.components[name].push(properties);
  };

  Card.prototype.setData = function (data) {
    this.lastComponent = 'card';
    this.attr(toAttr(data));
  };

  Card.prototype.setCallback = function (callback) {
    this.callback = callback;
  };

  Card.prototype.renderComponent = function (name, properties) {
    var parent;
    if (!Object.hasOwnProperty.call(components, name)) {
      // invalid component
      return;
    }
    parent = components[name].parent;
    if (Object.hasOwnProperty.call(properties, parent) || !this.exists(parent)) {
      this.renderComponent(parent, (properties && properties.parent) || properties);
    }
    this.lastComponent = name;
    components[name].render.call(this, properties);
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
    this.$representation.find('.card-container').click(this.callback);
    return this.$representation;
  };


  ActionCard = function (icon, text, callback) {
    Card.call(this);

    this.addComponent('action', {
      icon: icon,
      text: text,
      parent: {
        id: text.toLowerCase().split(' ').join('-') + '-card'
      }
    });
    this.setCallback(callback);
  };
  ActionCard.prototype = Object.create(Card.prototype);
  ActionCard.prototype.constructor = ActionCard;

  ClientCard = function (
    clientName, clientCode, userCount, reportCount,
    callback, deleteCallback, editCallback, newChildCallback
  ) {
    Card.call(this);

    this.addComponent('primaryText', { text: clientName });
    this.addComponent('secondaryText', { text: clientCode });
    this.addComponent('statistic', { icon: 'user', value: userCount });
    this.addComponent('statistic', { icon: '', value: reportCount });
    this.addComponent('button', { icon: '', color: 'red', callback: deleteCallback });
    this.addComponent('button', { icon: '', color: 'blue', callback: editCallback });
    this.addComponent('button', { icon: '', color: 'green', callback: newChildCallback });

    this.setData({
      'search-string': [clientName, clientCode].join('|').toUpperCase(),
      'client-id': 1
    });

    this.setCallback(callback);
  };
  ClientCard.prototype = Object.create(Card.prototype);
  ClientCard.prototype.constructor = ClientCard;

  UserCard = function (firstName, lastName, userName, email, userId, clientId) {
    Card.call(this);

    /*
    var userId = this.$representation
      .find(components.card.selector)
      .attr('data-user-id');
    var id = 'user-role-' + userId + '-' + roleEnum;
    this.lastComponent = 'toggle';
     */
  };
  UserCard.prototype = Object.create(Card.prototype);
  UserCard.prototype.constructor = UserCard;


  ['html', 'attr', 'prop', 'addClass', 'click'].forEach(function (func) {
    Card.prototype[func] = function (value, selector) {
      this.findComponent(this.lastComponent, selector)[func](value);
    };
  });

  Card.prototype.tooltip = function (value, selector) {
    var $component = findComponent(this.lastComponent, selector);
    $component.addClass('tooltip');
    $component.attr('title', value);
    return this;
  };

  Card.prototype.componentPath = function (name) {
    var parent = components[name].parent;
    if (Object.hasOwnProperty.call(components, parent)) {
      return this.componentPath(parent).concat([name]);
    }
    return [name];
  };

  Card.prototype.verify = function (partialPath) {
    var self = this;
    var path = partialPath || this.componentPath(this.lastComponent);
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

  Card.prototype.add = function (partialPath) {
    var path = partialPath || this.componentPath(this.lastComponent);
    var newElement = path.pop();
    var prevSelector = this.verify(path);
    this.$representation
      .find(prevSelector + ' > stub')
      .replaceWith(components[newElement].html);
    return prevSelector + ' ' + components[newElement].selector;
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


  // outdated, soon to be moved

  Card.prototype.roleExpansion = function () {
    var onClick = function (event) {
      event.stopPropagation();
      $(this).closest('.card-container')
        .find('div.card-expansion-container')
        .attr('maximized', function (index, attr) {
          if (attr === '') {
            $(this).find('.tooltip').tooltipster('content', 'Expand user card');
            return null;
          }
          $(this).find('.tooltip').tooltipster('content', 'Collapse user card');
          return '';
        });
      showRelevantUserActionIcons();
    };
    this.lastComponent = '';
    /* eslint-disable indent */
      return this
        .expansionLabel('User roles')
        .expansionButton('#action-icon-expand-card')
          .tooltip('Expand user card', '.card-button-background')
          .click(onClick, '.card-button-background')
        .main()
          .click(onClick);
      /* eslint-enable indent */
  };

  Card.prototype.roleToggles = function (roles) {
    var self = this;
    var onClick = function (event) {
      userCardRoleToggleClickHandler(event);
    };
    this.lastComponent = '';
    $.each(roles, function (index, assignment) {
      self.roleToggle(
        assignment.RoleEnum,
        assignment.RoleDisplayValue,
        assignment.IsAssigned
      ).click(onClick, '.toggle-switch-checkbox');
    });
    return this;
  };

  Card.prototype.buildNewChildClient = function (level) {
    /* eslint-disable indent */
      return this
        .newCard()
          .class('client-insert')
          .class('card-' + (100 - (10 * level)))
        .container()
          .class('card-container flex-container flex-row-no-wrap items-align-center')
        .actionText('New Sub-Client')
        .actionIcon('#action-icon-expand-card')
          .class('new-child-icon')
        .main()
          .class('content-item-flex-1')
          .class('indent-level-' + level, '.card-body-primary-text')
        .build();
      /* eslint-enable indent */
  };
}());
