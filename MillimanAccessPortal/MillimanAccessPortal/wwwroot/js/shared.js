var shared = {};

(function () {
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
}());
