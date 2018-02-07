var Card;

(function private() {
  var verify;
  var add;
  var findComponent;
  var html;

  Card = {

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
        selector: 'svg',
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
      action: {
        selector: '.card-body-primary-text',
        html: [
          '<h2 class="card-body-primary-text">',
          '<stub />',
          '</h2>'
        ].join('')
      },
      actionText: {
        selector: 'span',
        html: [
          '<span></span>',
          '<stub/>'
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

    attr: function attr(value, selector) {
      var $component = findComponent(this.vars.lastComponent, selector);
      $component.attr(value);
      return this;
    },

    prop: function prop(value, selector) {
      var $component = findComponent(this.vars.lastComponent, selector);
      $component.prop(value);
      return this;
    },

    class: function class_(value, selector) {
      var $component = findComponent(this.vars.lastComponent, selector);
      $component.addClass(value);
      return this;
    },

    tooltip: function tooltip(value, selector) {
      var $component = findComponent(this.vars.lastComponent, selector);
      $component.addClass('tooltip');
      $component.attr('title', value);
      return this;
    },

    click: function click(onClick, selector) {
      var $component = findComponent(this.vars.lastComponent, selector);
      $component.click(onClick);
      return this;
    },


    newCard: function newCard() {
      this.vars.$card = $(this.templates.container.html);
      this.vars.lastComponent = '';
      return this;
    },

    container: function container(canManage) {
      this.vars.canManage = canManage;
      this.vars.lastComponent = 'container';
      if (typeof canManage === 'boolean' && !this.vars.canManage) this.attr({ disabled: '' });
      return this;
    },

    main: function main() {
      this.vars.lastComponent = 'main';
      verify(['main']);
      return this;
    },

    primaryInfo: function primaryInfo(text) {
      this.vars.lastComponent = 'primary';
      add(['main', 'text', 'primary']);
      return html(text);
    },

    secondaryInfo: function secondaryInfo(text) {
      this.vars.lastComponent = 'secondary';
      add(['main', 'text', 'secondary']);
      return html(text);
    },

    info: function info(textList) {
      this.vars.lastComponent = '';
      textList.reverse();
      this.primaryInfo(textList.pop());
      while (textList.length) {
        this.secondaryInfo(textList.pop());
      }
      return this;
    },

    action: function action() {
      this.vars.lastComponent = 'action';
      verify(['main', 'text', 'action']);
      return this;
    },

    actionText: function actionText(text) {
      this.vars.lastComponent = 'actionText';
      add(['main', 'text', 'action', 'actionText']);
      return html(text);
    },

    icon: function icon(iconName) {
      this.vars.lastComponent = 'icon';
      add(['main', 'icons', 'icon']);
      return this.attr({ href: iconName }, '[href]');
    },

    actionIcon: function actionIcon(iconName) {
      this.vars.lastComponent = 'icon';
      add(['main', 'text', 'action', 'icon']);
      return this.attr({ href: iconName }, '[href]');
    },

    cardStat: function cardStat(iconName, value) {
      this.vars.lastComponent = 'stat';
      add(['main', 'stats', 'stat']);
      this.attr({ href: iconName }, '[href]');
      return html(value, '.card-stat-value');
    },

    sideButton: function sideButton(iconName) {
      if (!this.vars.canManage) {
        this.vars.lastComponent = 'button';
        return this;
      }
      this.vars.lastComponent = 'button';
      add(['main', 'side', 'button']);
      return this.attr({ href: iconName }, '[href]');
    },

    expansionLabel: function expansionLabel(label) {
      this.vars.lastComponent = 'expansionLabel';
      verify(['expansion', 'expansionLabel']);
      return html(label);
    },

    expansionButton: function expansionButton(iconName) {
      this.vars.lastComponent = 'bottom';
      verify(['expansion', 'bottom']);
      return this.attr({ href: iconName }, '[href]');
    },

    roleExpansion: function roleExpansion() {
      var onClick = function toggleCard(event) {
        event.stopPropagation();
        $(this).closest('.card-container')
          .find('div.card-expansion-container')
          .attr('maximized', function toggle(index, attr) {
            if (attr === '') {
              $(this).find('.tooltip').tooltipster('content', 'Expand user card');
              return null;
            }
            $(this).find('.tooltip').tooltipster('content', 'Collapse user card');
            return '';
          });
        showRelevantUserActionIcons();
      };
      this.vars.lastComponent = '';
      /* eslint-disable indent */
      return this
        .expansionLabel('User roles')
        .expansionButton('#action-icon-expand-card')
          .tooltip('Expand user card', '.card-button-background')
          .click(onClick, '.card-button-background')
        .main()
          .click(onClick);
      /* eslint-enable indent */
    },

    roleToggle: function roleToggle(roleEnum, roleName, roleAssigned) {
      var userId = this.vars.$card
        .find(this.templates.container.selector)
        .attr('data-user-id');
      var id = 'user-role-' + userId + '-' + roleEnum;
      this.vars.lastComponent = 'toggle';
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
    },

    roleToggles: function roleToggles(roles) {
      var self = this;
      var onClick = function onClick(event) {
        userCardRoleToggleClickHandler(event);
      };
      this.vars.lastComponent = '';
      $.each(roles, function createAndAssign(index, assignment) {
        self.roleToggle(
          assignment.RoleEnum,
          assignment.RoleDisplayValue,
          assignment.IsAssigned
        ).click(onClick, '.toggle-switch-checkbox');
      });
      return this;
    },

    build: function build() {
      this.vars.$card.find('stub').remove();
      return this.vars.$card;
    },

    buildNewClient: function buildNewClient() {
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
    },

    buildNewChildClient: function buildNewChildClient(level) {
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
    },

    buildAddUser: function buildAddUser() {
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
    }

  };
  verify = function _verify(path) {
    var prevSelector = Card.templates.container.selector;
    $.each(path, function create() {
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
  add = function _add(path) {
    var newElement = path.pop();
    var prevSelector = verify(path);
    Card.vars.$card
      .find(prevSelector + ' > stub')
      .replaceWith(Card.templates[newElement].html);
    return prevSelector + ' > ' + Card.templates[newElement].selector;
  };
  findComponent = function _findComponent(component, selector) {
    var $component = Card.templates[component]
      ? Card.vars.$card
        .find(Card.templates[component].selector)
        .last()
      : Card.vars.$card;
    var $subcomponent = $component.find(selector);
    return $subcomponent.length ? $subcomponent : $component;
  };
  html = function _html(value, selector) {
    var $component = findComponent(Card.vars.lastComponent, selector);
    $component.html(value);
    return Card;
  };
}());
