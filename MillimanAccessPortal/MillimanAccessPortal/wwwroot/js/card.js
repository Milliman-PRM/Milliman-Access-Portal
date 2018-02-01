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
        '</div>'
      ].join('')
    },
    cardIcons: {
      selector: '.card-body-secondary-container',
      html: [
        '<div class="card-body-secondary-container">',
          '<stub />',
        '</div>'
      ].join('')
    },
    cardIcon: {
      selector: '.card-user-icon',
      html: [
        '<svg class="card-user-icon">',
          '<use xlink:href=""></use>',
        '</svg>',
        '<stub />'
      ].join('')
    },
    text: {
      selector: '.card-body-primary-container',
      html: [
        '<div class="card-body-primary-container">',
          '<stub />',
        '</div>'
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

  newCard: function newCard(primaryText) {
    this.vars.$card = $(this.templates.container.html);
    this.add(['main', 'text', 'primary']);
    this.vars.$card.find(this.templates.primary.selector).html(primaryText);
  },

  build: function build() {
    this.vars.$card.find('stub').remove();
    return this.vars.$card;
  }

};
