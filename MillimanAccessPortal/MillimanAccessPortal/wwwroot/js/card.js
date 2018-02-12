var Card;
var ActionCard;
var ClientCard;
var UserCard;

(function () {
  var verify;
  var add;
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
      }
    }
  };

  var components = Object.assign(
    {},
    {
      action: {
        count: '?',
        selector: '',
        render: function (properties) {
          var id = properties.text.toLowerCase().split(' ').join('-') + '-card';
          /* eslint-disable indent */
          this
            .container()
              .attr({ id: id })
              .class('card-100 action-card')
            .actionIcon(properties.icon)
              .class('action-card-icon')
            .actionText(properties.text);
          /* eslint-enable indent */
        }
      },
      primaryText: { count: '1' },
      secondaryText: { count: '*' },
      statistic: { count: '*' },
      button: { count: '*' },
      icon: { count: '*' },
      detailText: { count: '?' },
      toggle: { count: '*' },
      listItem: { count: '*' }
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
          this.container();
          this.attr({ id: properties.id });
          this.class('card-100 action-card');
        }
      },
      main: {},
      icons: {},
      text: {},
      statistics: {},
      side: {},
      detail: {}
    }
  );

  /* eslint-disable indent */
  var templates = {
    card: {
      selector: '.card-container',
      path: [],
      html: [
        '<li>',
          '<div class="card-container">',
            '<stub />',
          '</div>',
        '</li>'
      ].join('')
    },
    main: {
      selector: '.card-body-main-container',
      path: ['main'],
      html: [
        '<div class="card-body-main-container">',
          '<stub />',
        '</div>',
        '<stub />'
      ].join('')
    },
    icons: {
      selector: '.card-body-secondary-container',
      path: ['main', 'icons'],
      html: [
        '<div class="card-body-secondary-container">',
          '<stub />',
        '</div>',
        '<stub />'
      ].join('')
    },
    icon: {
      selector: 'svg',
      path: ['main', 'icons', 'icon'],
      html: [
        '<svg class="">',
          '<use href=""></use>',
        '</svg>',
        '<stub />'
      ].join('')
    },
    text: {
      selector: '.card-body-primary-container',
      path: ['main', 'text'],
      html: [
        '<div class="card-body-primary-container">',
          '<stub />',
        '</div>',
        '<stub />'
      ].join('')
    },
    primaryText: {
      selector: '.card-body-primary-text',
      path: ['main', 'text', 'primaryText'],
      html: [
        '<h2 class="card-body-primary-text"></h2>',
        '<stub />'
      ].join('')
    },
    secondaryText: {
      selector: '.card-body-secondary-text',
      path: ['main', 'text', 'secondaryText'],
      html: [
        '<p class="card-body-secondary-text"></p>',
        '<stub />'
      ].join('')
    },
    action: {
      selector: '.card-body-primary-text',
      path: ['main', 'action'],
      html: [
        '<h2 class="card-body-primary-text">',
        '<stub />',
        '</h2>'
      ].join('')
    },
    actionText: {
      selector: 'span',
      path: ['main', 'action', 'actionText'],
      html: [
        '<span></span>',
        '<stub/>'
      ].join('')
    },
    stats: {
      selector: '.card-stats-container',
      path: ['main', 'stats'],
      html: [
        '<div class="card-stats-container">',
          '<stub />',
        '</div>',
        '<stub />'
      ].join('')
    },
    stat: {
      selector: '.card-stat-container',
      path: ['main', 'stats', 'stat'],
      html: [
        '<div class="card-stat-container tooltip" title="">',
            '<svg class="card-stat-icon">',
                '<use href=""></use>',
            '</svg>',
            '<h4 class="card-stat-value"></h4>',
        '</div>',
        '<stub />'
      ].join('')
    },
    side: {
      selector: '.card-button-side-container',
      path: ['main', 'side'],
      html: [
        '<div class="card-button-side-container">',
          '<stub />',
        '</div>',
        '<stub />'
      ].join('')
    },
    button: {
      selector: '.card-button-background',
      path: ['main', 'side', 'button'],
      html: [
        '<div class="card-button-background tooltip" title="">',
            '<svg class="card-button-icon">',
                '<use href=""></use>',
            '</svg>',
        '</div>',
        '<stub />'
      ].join('')
    },
    expansion: {
      selector: '.card-expansion-container',
      path: ['expansion'],
      html: [
        '<div class="card-expansion-container">',
          '<stub />',
        '</div>'
      ].join('')
    },
    expansionLabel: {
      selector: '.card-expansion-category-label',
      path: ['expansion', 'expansionLabel'],
      html: [
        '<h4 class="card-expansion-category-label">Client Roles</h4>',
        '<stub />'
      ].join('')
    },
    toggle: {
      selector: '.switch-container',
      path: ['expansion', 'toggle'],
      html: [
        '<div class="switch-container">',
          '<div class="toggle-switch">',
            '<input type="checkbox" class="toggle-switch-checkbox" data-role-enum="" name="" id="">',
            '<label class="toggle-switch-label" for="">',
              '<span class="toggle-switch-inner"></span>',
              '<span class="toggle-switch-switch"></span>',
            '</label>',
          '</div>',
          '<label class="switch-label"></label>',
        '</div>',
        '<stub />'
      ].join('')
    },
    bottom: {
      selector: '.card-button-bottom-container',
      path: ['expansion', 'bottom'],
      html: [
        '<stub />',
        '<div class="card-button-bottom-container">',
          '<div class="card-button-background card-button-expansion">',
            '<svg class="card-button-icon">',
              '<use href="#action-icon-expand-card"></use>',
            '</svg>',
          '</div>',
        '</div>'
      ].join('')
    }
  };
  /* eslint-enable indent */

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

  Card = function (representation) {
    this.components = [];
    this.$representation = $(representation || '');
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
    this.components.push({ name: properties });
  };

  Card.prototype.renderComponent = function (name, properties) {
    var parent;
    if (!Object.hasOwnProperty.call(components, name)) {
      // invalid component
      return;
    }
    parent = components[name].parent;
    if (Object.hasOwnProperty.call(properties, parent) || !this.exists(parent)) {
      this.renderComponent(parent, properties && properties.parent);
    }
    components[name].render(properties);
  };

  Card.prototype.build = function () {
    this.components.foreach(function (name, properties) {
      this.renderComponent(name, properties);
    });
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
    this.setCallback(callback);
  };

  ['html', 'attr', 'prop', 'addClass', 'click'].foreach(function (func) {
    Card.prototype[func] = function (value, selector) {
      findComponent(this.lastTemplate, selector)[func](value);
    };
  });

  Card.prototype.tooltip = function (value, selector) {
    var $component = findComponent(this.vars.lastTemplate, selector);
    $component.addClass('tooltip');
    $component.attr('title', value);
    return this;
  };

  Card.prototype.newCard = function () {
    this.vars.$card = $(this.templates.container.html);
    this.vars.lastTemplate = '';
    return this;
  };

  Card.prototype.add = function (name) {
    if (!Object.hasOwnProperty.call(templates, name)) {
      // invalid template
    }
    this.vars.lastTemplate = name;
    add(templates[name].path);
  };


  Card.prototype.container = function (searchTerms, clientId, userId, canManage) {
    var searchString;
    this.vars.canManage = canManage;
    this.vars.lastTemplate = 'container';
    if (searchTerms) {
      searchString = $.map(searchTerms, function (term) {
        return (term || '').toUpperCase();
      }).join('|');
      this.attr({ 'data-search-string': searchString });
    }
    if (clientId) this.attr({ 'data-client-id': clientId });
    if (userId) this.attr({ 'data-user-id': userId });
    if (typeof canManage === 'boolean' && !this.vars.canManage) this.attr({ disabled: '' });
    return this;
  };

  Card.prototype.main = function () {
    this.vars.lastTemplate = 'main';
    verify(['main']);
    return this;
  };

  Card.prototype.primaryInfo = function (text) {
    this.vars.lastTemplate = 'primary';
    add(['main', 'text', 'primary']);
    return html(text);
  };

  Card.prototype.secondaryInfo = function (text) {
    this.vars.lastTemplate = 'secondary';
    add(['main', 'text', 'secondary']);
    return html(text);
  };

  Card.prototype.info = function (textList) {
    this.vars.lastTemplate = '';
    textList.reverse();
    this.primaryInfo(textList.pop());
    while (textList.length) {
      this.secondaryInfo(textList.pop());
    }
    return this;
  };

  Card.prototype.action = function () {
    this.vars.lastTemplate = 'action';
    verify(['main', 'text', 'action']);
    return this;
  };

  Card.prototype.actionText = function (text) {
    this.vars.lastTemplate = 'actionText';
    add(['main', 'text', 'action', 'actionText']);
    return html(text);
  };

  Card.prototype.icon = function (iconName) {
    this.vars.lastTemplate = 'icon';
    add(['main', 'icons', 'icon']);
    return this.attr({ href: iconName }, '[href]');
  };

  Card.prototype.actionIcon = function (iconName) {
    this.vars.lastTemplate = 'icon';
    add(['main', 'text', 'action', 'icon']);
    return this.attr({ href: iconName }, '[href]');
  };

  Card.prototype.cardStat = function (iconName, value) {
    this.vars.lastTemplate = 'stat';
    add(['main', 'stats', 'stat']);
    this.attr({ href: iconName }, '[href]');
    return html(value, '.card-stat-value');
  };

  Card.prototype.sideButton = function (iconName) {
    if (!this.vars.canManage) {
      this.vars.lastTemplate = 'button';
      return this;
    }
    this.vars.lastTemplate = 'button';
    add(['main', 'side', 'button']);
    return this.attr({ href: iconName }, '[href]');
  };

  Card.prototype.expansionLabel = function (label) {
    this.vars.lastTemplate = 'expansionLabel';
    verify(['expansion', 'expansionLabel']);
    return html(label);
  };

  Card.prototype.expansionButton = function (iconName) {
    this.vars.lastTemplate = 'bottom';
    verify(['expansion', 'bottom']);
    return this.attr({ href: iconName }, '[href]');
  };

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
    this.vars.lastTemplate = '';
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

  Card.prototype.roleToggle = function (roleEnum, roleName, roleAssigned) {
    var userId = this.vars.$card
      .find(this.templates.container.selector)
      .attr('data-user-id');
    var id = 'user-role-' + userId + '-' + roleEnum;
    this.vars.lastTemplate = 'toggle';
    add(['expansion', 'toggle']);
    if (!this.vars.canManage) this.attr({ disabled: '' }, '.toggle-switch-checkbox');
    return html(roleName, '.switch-label')
      .attr({
        'data-role-enum': roleEnum,
        name: id,
        id: id
      }, '.toggle-switch-checkbox')
      .attr({ for: id }, '.toggle-switch-label')
      .prop({ checked: roleAssigned }, '.toggle-switch-checkbox');
  };

  Card.prototype.roleToggles = function (roles) {
    var self = this;
    var onClick = function (event) {
      userCardRoleToggleClickHandler(event);
    };
    this.vars.lastTemplate = '';
    $.each(roles, function (index, assignment) {
      self.roleToggle(
        assignment.RoleEnum,
        assignment.RoleDisplayValue,
        assignment.IsAssigned
      ).click(onClick, '.toggle-switch-checkbox');
    });
    return this;
  };

  Card.prototype.build = function () {
    this.vars.$card.find('stub').remove();
    return this.vars.$card;
  };

  Card.prototype.buildNewClient = function () {
    /* eslint-disable indent */
      return this
        .newCard()
        .container()
          .attr({ id: 'create-new-client-card' }, '.card-container')
          .class('card-100 action-card', '.card-container')
        .actionIcon('#action-icon-add')
          .class('action-card-icon')
        .actionText('New Client')
        .build();
      /* eslint-enable indent */
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

  Card.prototype.buildAddUser = function () {
    /* eslint-disable indent */
      return this
        .newCard()
        .container()
          .attr({ id: 'add-user-card' })
          .class('card-100 action-card')
        .actionIcon('#action-icon-add')
          .class('action-card-icon')
        .actionText('Add User')
        .build();
      /* eslint-enable indent */
  };


  verify = function (path) {
    var prevSelector = Card.templates.container.selector;
    $.each(path, function () {
      var nextSelector = prevSelector + ' > ' + Card.templates[this].selector;
      var element = Card.vars.$card.find(nextSelector);
      if (!element.length) {
        Card.vars.$card
          .find(prevSelector + ' > stub')
          .replaceWith(Card.templates[this].html);
      }
      prevSelector = nextSelector;
    });
    return prevSelector;
  };

  add = function (path) {
    var newElement = path.pop();
    var prevSelector = verify(path);
    Card.vars.$card
      .find(prevSelector + ' > stub')
      .replaceWith(Card.templates[newElement].html);
    return prevSelector + ' > ' + Card.templates[newElement].selector;
  };

  findComponent = function (component, selector) {
    var $component = Card.templates[component]
      ? Card.vars.$card
        .find(Card.templates[component].selector)
        .last()
      : Card.vars.$card;
    var $subcomponent = $component.find(selector);
    return $subcomponent.length ? $subcomponent : $component;
  };
}());
