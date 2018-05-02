import $ = require('jquery');
require('jquery-validation');
import toastr = require('toastr');
const vex = require('vex-js');
vex.registerPlugin(require('vex-dialog'));
const resumable = require('resumablejs');
const initialAppSettings = require('../../appsettings.json');


interface GlobalSettings {
  domainValidationRegex: string;
  emailValidationRegex: string;
  maxFileUploadSize: number;
}

export let globalSettings: GlobalSettings = {
  domainValidationRegex: initialAppSettings.Global.DomainValidationRegex,
  emailValidationRegex: initialAppSettings.Global.EmailValidationRegex,
  maxFileUploadSize: initialAppSettings.Global.MaxFileUploadSize,
};
$(document).on('ready', () => {
  globalSettings = $('#global-settings').data() as GlobalSettings;
  console.log('Updated global settings');
});


// Configure jQuery validation overrides
// See https://jqueryvalidation.org/jQuery.validator.methods/
$.validator.methods.email = function(value: string, element: any) {
  return this.optional(element)
    || new RegExp(globalSettings.emailValidationRegex).test(value);
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
  testChunks: false,
  simultaneousUploads: 3,
  maxFiles: 1,
  maxFileSize: globalSettings.maxFileUploadSize,
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
