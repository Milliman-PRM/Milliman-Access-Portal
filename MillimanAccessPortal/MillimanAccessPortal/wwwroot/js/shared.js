var shared = {};

(function () {
  var SHOW_DURATION = 50;
  var ajaxStatus = [];

  shared.filterTree = function () {
    var $filter = $(this);
    var $panel = $filter.closest('.admin-panel-container');
    var $content = $panel.find('ul.admin-panel-content');
    $content.children('.hr').hide();
    $content.find('[data-filter-string]').each(function (index, element) {
      var $element = $(element);
      if ($element.data('filter-string').indexOf($filter.val().toUpperCase()) > -1) {
        $element.show();
        $element.closest('li').nextAll('li.hr').first()
          .show();
      } else {
        $element.hide();
      }
    });
  };

  shared.updateToolbarIcons = function ($panel) {
    $panel.find('.action-icon-collapse').hide().filter(function anyMaximized() {
      return $panel.find('.card-expansion-container[maximized]').length;
    }).show();
    $panel.find('.action-icon-expand').hide().filter(function anyMinimized() {
      return $panel.find('.card-expansion-container:not([maximized])').length;
    }).show();
  };
  shared.toggleExpanded = function (event) {
    var $card = $(this);
    var $panel = $card.closest('.admin-panel-container');
    event.stopPropagation();
    $card.closest('.card-container')
      .find('.card-expansion-container')
      .attr('maximized', function (index, attr) {
        var data = (attr === '')
          ? { text: 'Expand card', rv: null }
          : { text: 'Collapse card', rv: '' };
        $card.find('.tooltip').tooltipster('content', data.text);
        return data.rv;
      });
    shared.updateToolbarIcons($panel);
  };
  shared.expandAll = function (event) {
    var $panel = $(this).closest('.admin-panel-container');
    event.stopPropagation();
    $panel.find('.card-expansion-container').attr('maximized', '');
    shared.updateToolbarIcons($panel);
  };
  shared.collapseAll = function (event) {
    var $panel = $(this).closest('.admin-panel-container');
    event.stopPropagation();
    $panel.find('.card-expansion-container[maximized]').removeAttr('maximized');
    shared.updateToolbarIcons($panel);
  };

  shared.wrapCardCallback = function (callback) {
    return function () {
      var $card = $(this);
      var $panel = $card.closest('.admin-panel-container');
      var sameCard = ($card[0] === $panel.find('[selected]')[0]);

      var clearSelection = function () {
        $panel.find('.card-container').removeAttr('editing selected');
      };
      var showDetails = function () {
        $panel.next().show(SHOW_DURATION);
      };
      var hideDetails = function () {
        $panel.nextAll().hide(SHOW_DURATION);
      };
      var openCard = function () {
        clearSelection();
        hideDetails();
        $card.attr('selected', '');
        callback($card);
        showDetails();
      };

      if ($panel.has('[selected]').length) {
        // TODO: wrap if-else with confirmAndReset
        if (sameCard) {
          clearSelection();
          hideDetails();
        } else {
          openCard();
        }
      } else {
        openCard();
      }
    };
  };

  shared.get = function (url, callback) {
    return function ($card) {
      var $panel = $card
        ? $card.closest('.admin-panel-container').next()
        : $('.admin-panel-container').first();
      var $loading = $panel.find('.loading-wrapper');
      var data = $card && $card.data();

      ajaxStatus[callback.name] = data; // or some hash of the data
      $loading.show();

      $.ajax({
        type: 'GET',
        url: url,
        data: data
      }).done(function (response) {
        callback(response);
        $loading.hide();
      }).fail(function (response) {
        var warning = response.getResponseHeader('Warning');
        toastr.warning(warning || 'An unknown error has occurred.');
        $loading.hide();
      });
    };
  };
}());
