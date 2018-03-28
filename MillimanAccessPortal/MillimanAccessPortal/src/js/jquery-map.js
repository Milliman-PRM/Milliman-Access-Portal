var jQuery = require('jquery');

/* eslint-disable no-param-reassign */
(function ($) {
  $.fn.filterTree = function (filterString) {
    return this.filter(function () {
      var data = $(this).find('.card-container').data();
      return Object.prototype.hasOwnProperty.call(data, 'filterString')
        && (data.filterString.toUpperCase()
          .indexOf(filterString.toUpperCase()) > -1);
    });
  };
}(jQuery));
/* eslint-enable no-param-reassign */
