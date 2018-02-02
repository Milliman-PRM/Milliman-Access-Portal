var Card = {

  settings: {
  },

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
        '<svg class="card-user-icon">',
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
            '<input type="checkbox" class="toggle-switch-checkbox">',
            '<label class="toggle-switch-label">',
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
        '<div class="card-button-bottom-container">',
          '<div class="card-button-background tooltip" title="">',
            '<svg class="card-button-icon">',
              '<use href=""></use>',
            '</svg>',
          '</div>',
        '</div>',
        '<stub />'
      ].join('')
    }
  },

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

  attr: function attr(component, value, selector) {
    var $component = this.findComponent(component, selector);
    $component.attr(value);
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


  newCard: function newCard(primaryText) {
    this.vars.$card = $(this.templates.container.html);
    this.add(['main', 'text', 'primary']);
    this.html('primary', primaryText);
    return this;
  },

  secondaryInfo: function secondaryInfo(secondaryText) {
    this.add(['main', 'text', 'secondary']);
    this.html('secondary', secondaryText);
    return this;
  },

  cardStat: function cardStat(icon, value, tooltip) {
    this.add(['main', 'stats', 'stat']);
    this.html('stat', value, '.card-stat-value');
    if (tooltip) this.tooltip('stat', tooltip);
    return this;
  },

  build: function build() {
    this.vars.$card.find('stub').remove();
    return this.vars.$card;
  }

};
