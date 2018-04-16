import jQuery = require('jquery');

(function ($) {
  const filterTree = function (filterString) {
    return this.filter(function () {
      var data = $(this).find('.card-container').data();
      return (data !== undefined
        && Object.prototype.hasOwnProperty.call(data, 'filterString')
        && (data.filterString.toUpperCase()
          .indexOf(filterString.toUpperCase()) > -1));
    });
  };
  const filterSelections = function (filterString) {
    return this.filter(function () {
      var data = $(this).find('.selection-option-container').data();
      return (data !== undefined
        && Object.prototype.hasOwnProperty.call(data, 'selectionValue')
        && (data.selectionValue.toUpperCase()
          .indexOf(filterString.toUpperCase()) > -1));
    });
  };
}(jQuery));
