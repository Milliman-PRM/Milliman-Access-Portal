var Card = {

  settings: {
  },

  /* eslint-disable indent */
  templates: {
    container: {
      selector: '.card-container',
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
      html: [
        '<div class="card-body-main-container">',
          '<stub />',
        '</div>',
        '<stub />'
      ].join('')
    },
    icons: {
      selector: '.card-body-secondary-container',
      html: [
        '<div class="card-body-secondary-container">',
          '<stub />',
        '</div>',
        '<stub />'
      ].join('')
    },
    icon: {
      selector: '.card-user-icon',
      html: [
        '<svg class="">',
          '<use href=""></use>',
        '</svg>',
        '<stub />'
      ].join('')
    },
    text: {
      selector: '.card-body-primary-container',
      html: [
        '<div class="card-body-primary-container">',
          '<stub />',
        '</div>',
        '<stub />'
      ].join('')
    },
    primary: {
      selector: '.card-body-primary-text',
      html: [
        '<h2 class="card-body-primary-text"></h2>',
        '<stub />'
      ].join('')
    },
    secondary: {
      selector: '.card-body-secondary-text',
      html: [
        '<p class="card-body-secondary-text"></p>',
        '<stub />'
      ].join('')
    },
    stats: {
      selector: '.card-stats-container',
      html: [
        '<div class="card-stats-container">',
          '<stub />',
        '</div>',
        '<stub />'
      ].join('')
    },
    stat: {
      selector: '.card-stat-container',
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
      html: [
        '<div class="card-button-side-container">',
          '<stub />',
        '</div>',
        '<stub />'
      ].join('')
    },
    button: {
      selector: '.card-button-background',
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
      html: [
        '<div class="card-expansion-container">',
          '<stub />',
        '</div>'
      ].join('')
    },
    expansionLabel: {
      selector: '.card-expansion-category-label',
      html: [
        '<h4 class="card-expansion-category-label">Client Roles</h4>',
        '<stub />'
      ].join('')
    },
    toggle: {
      selector: '.switch-container',
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
  },
  /* eslint-enable indent */

  vars: {
  },

  init: function init() {
  },

  verify: function verify(path) {
    var self = this;
    var prevSelector = self.templates.container.selector;
    $.each(path, function create() {
      var nextSelector = prevSelector + ' > ' + self.templates[this].selector;
      var element = self.vars.$card.find(nextSelector);
      if (!element.length) {
        self.vars.$card
          .find(prevSelector + ' > stub')
          .replaceWith(self.templates[this].html);
      }
      prevSelector = nextSelector;
    });
    return prevSelector;
  },

  add: function add(path) {
    var newElement = path.pop();
    var prevSelector = this.verify(path);
    this.vars.$card
      .find(prevSelector + ' > stub')
      .replaceWith(this.templates[newElement].html);
    return prevSelector + ' > ' + this.templates[newElement].selector;
  },


  findComponent: function findComponent(component, selector) {
    var $component = this.vars.$card
      .find(this.templates[component].selector)
      .last();
    var $subcomponent = $component.find(selector);
    return $subcomponent.length ? $subcomponent : $component;
  },

  html: function html(component, value, selector) {
    var $component = this.findComponent(component, selector);
    $component.html(value);
  },

  prop: function prop(component, value, selector) {
    var $component = this.findComponent(component, selector);
    return $component.prop(value);
  },

  attr: function attr(component, value, selector) {
    var $component = this.findComponent(component, selector);
    return $component.attr(value);
  },

  addClass: function addClass(component, value, selector) {
    var $component = this.findComponent(component, selector);
    $component.addClass(value);
  },

  tooltip: function tooltip(component, value, selector) {
    var $component = this.findComponent(component, selector);
    $component.addClass('tooltip');
    $component.attr('title', value);
  },

  click: function click(component, onClick, selector) {
    var $component = this.findComponent(component, selector);
    $component.click(onClick);
  },


  newCard: function newCard(classes, searchTerms, clientId, userId, canManage) {
    this.vars.$card = $(this.templates.container.html);
    this.vars.canManage = canManage;
    this.addClass('container', classes);
    this.attr('container', {
      'data-search-string': searchTerms.join('|'),
      'data-client-id': clientId,
      'data-user-id': userId
    });
    if (!this.vars.canManage) this.attr('container', { disabled: '' });
    return this;
  },

  primaryInfo: function primaryInfo(text) {
    this.add(['main', 'text', 'primary']);
    this.html('primary', text);
    return this;
  },

  secondaryInfo: function secondaryInfo(text) {
    this.add(['main', 'text', 'secondary']);
    this.html('secondary', text);
    return this;
  },

  info: function info(textList) {
    textList.reverse();
    this.primaryInfo(textList.pop());
    while (textList.length) {
      this.secondaryInfo(textList.pop());
    }
  },

  icon: function icon(iconName, classes) {
    this.add(['main', 'icons', 'icon']);
    this.attr('icon', { href: iconName }, '[href]');
    this.addClass('icon', classes);
    return this;
  },

  cardStat: function cardStat(iconName, value, tooltip) {
    this.add(['main', 'stats', 'stat']);
    this.attr('stat', { href: iconName }, '[href]');
    this.html('stat', value, '.card-stat-value');
    if (tooltip) this.tooltip('stat', tooltip);
    return this;
  },

  sideButton: function sideButton(iconName, classes, onClick, tooltip) {
    if (!this.vars.canManage) return this;
    this.add(['main', 'side', 'button']);
    this.attr('button', { href: iconName }, '[href]');
    this.addClass('button', classes);
    this.click('button', onClick);
    if (tooltip) this.tooltip('button', tooltip);
    return this;
  },

  expansion: function expansion(label, onClick) {
    this.verify(['expansion', 'expansionLabel']);
    this.verify(['expansion', 'bottom']);
    this.html('expansionLabel', label);
    this.attr('bottom', { href: '#action-icon-expand-card' }, '[href]');
    this.tooltip('bottom', 'Expand user card', '.card-button-background');
    this.click('bottom', onClick, '.card-button-background');
    this.click('container', onClick);
    return this;
  },

  roleToggle: function roleToggle(roleEnum, roleName, roleAssigned, onClick) {
    var userId = this.attr('container', 'data-user-id');
    var id = 'user-role-' + userId + '-' + roleEnum;
    this.add(['expansion', 'toggle']);
    this.attr('toggle', {
      'data-role-enum': roleEnum,
      name: id,
      id: id
    }, '.toggle-switch-checkbox');
    if (!this.vars.canManage) this.attr('toggle', { disabled: '' }, '.toggle-switch-checkbox');
    this.attr('toggle', { for: id }, '.toggle-switch-label');
    this.html('toggle', roleName, '.switch-label');
    this.prop('toggle', { checked: roleAssigned }, '.toggle-switch-checkbox');
    this.click('toggle', onClick, '.toggle-switch-checkbox');
    return this;
  },

  roleToggles: function roleToggles(roles, onClick) {
    var self = this;
    $.each(roles, function createAndAssign(index, assignment) {
      self.roleToggle(
        assignment.RoleEnum,
        assignment.RoleDisplayValue,
        assignment.IsAssigned,
        onClick
      );
    });
    return this;
  },

  build: function build() {
    this.vars.$card.find('stub').remove();
    return this.vars.$card;
  }

};
