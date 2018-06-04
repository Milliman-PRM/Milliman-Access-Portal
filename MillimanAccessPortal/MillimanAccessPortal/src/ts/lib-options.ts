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
};
// Configure default vex options
vex.defaultOptions = $.extend(
  {}, vex.defaultOptions,
  {
    className: 'vex-theme-default screen-center',
    closeAllOnPopState: false,
  },
);

vex.dialog.buttons.yes = (text: string, color: string) => {
  return $.extend({}, vex.dialog.buttons.YES, { text, className: color + '-button' });
};

vex.dialog.buttons.no = (text: string) => {
  return $.extend({}, vex.dialog.buttons.NO, { text, className: 'link-button' });
};

// Configure toastr options
toastr.options = {
  closeButton: false,
  debug: false,
  extendedTimeOut: 1000,
  hideDuration: 1000,
  hideEasing: 'swing',
  hideMethod: 'hide',
  newestOnTop: false,
  onclick: null,
  positionClass: 'toast-bottom-right',
  preventDuplicates: false,
  progressBar: false,
  showDuration: 300,
  showEasing: 'swing',
  showMethod: 'show',
  timeOut: 5000,
};

export const resumableOptions = {
  chunkNumberParameterName: 'chunkNumber',
  chunkSizeParameterName: 'chunkSize',
  currentChunkSizeParameterName: '',
  fileNameParameterName: 'fileName',
  identifierParameterName: 'uid',
  maxFileSize: globalSettings.maxFileUploadSize,
  maxFiles: 1,
  permanentErrors: [400, 401, 404, 409, 415, 500, 501],
  relativePathParameterName: '',
  simultaneousUploads: 3,
  testChunks: false,
  totalChunksParameterName: 'totalChunks',
  totalSizeParameterName: 'totalSize',
  typeParameterName: 'type',
};
