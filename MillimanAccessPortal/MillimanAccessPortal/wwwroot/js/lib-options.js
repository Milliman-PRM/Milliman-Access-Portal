// Configure jQuery validation overrides
$.validator.methods.email = function validateEmail(value, element) {
  return this.optional(element) || emailValRegex.test(value);
};

// Configure default vex options
vex.defaultOptions = $.extend(
  {}, vex.defaultOptions,
  {
    className: 'vex-theme-default screen-center',
    closeAllOnPopState: false
  }
);

vex.dialog.buttons.yes = function yes(text, color) {
  return $.extend({}, vex.dialog.buttons.YES, { text: text, className: color + '-button' });
};

vex.dialog.buttons.no = function no(text) {
  return $.extend({}, vex.dialog.buttons.NO, { text: text, className: 'link-button' });
};

// Configure toastr options
toastr.options = {
  closeButton: false,
  debug: false,
  newestOnTop: false,
  progressBar: false,
  positionClass: 'toast-bottom-right',
  preventDuplicates: false,
  onclick: null,
  showDuration: '300',
  hideDuration: '1000',
  timeOut: '5000',
  extendedTimeOut: '1000',
  showEasing: 'swing',
  hideEasing: 'swing',
  showMethod: 'show',
  hideMethod: 'hide'
};
