import $ = require('jquery');
require('jquery-validation');
import toastr = require('toastr');
const vex = require('vex-js');
vex.registerPlugin(require('vex-dialog'));
const appSettings = require('../../appsettings.json');

// Configure jQuery validation overrides
// See https://jqueryvalidation.org/jQuery.validator.methods/
$.validator.methods.email = function(value: string, element: any) {
  return this.optional(element) || appSettings.Global.EmailValidationRegex;
}
// Configure default vex options
vex.defaultOptions = $.extend(
  {}, vex.defaultOptions,
  {
    className: 'vex-theme-default screen-center',
    closeAllOnPopState: false
  }
);

vex.dialog.buttons.yes = (text: string, color: string) => {
  return $.extend({}, vex.dialog.buttons.YES, { text: text, className: color + '-button' });
};

vex.dialog.buttons.no = (text: string) => {
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
  showDuration: 300,
  hideDuration: 1000,
  timeOut: 5000,
  extendedTimeOut: 1000,
  showEasing: 'swing',
  hideEasing: 'swing',
  showMethod: 'show',
  hideMethod: 'hide'
};

export const resumableOptions = {
  simultaneousUploads: 3,
  maxFiles: 1,
  maxFileSize: appSettings.Global.MaxFileUploadSize,
  permanentErrors: [400, 401, 404, 409, 415, 500, 501],
  chunkNumberParameterName: 'chunkNumber',
  totalChunksParameterName: 'totalChunks',
  identifierParameterName: 'uid',
  typeParameterName: 'type',
  chunkSizeParameterName: 'chunkSize',
  totalSizeParameterName: 'totalSize',
  fileNameParameterName: 'fileName',
  relativePathParameterName: '',
  currentChunkSizeParameterName: '',
};
