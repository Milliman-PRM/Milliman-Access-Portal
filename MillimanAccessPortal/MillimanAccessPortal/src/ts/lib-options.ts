import toastr = require('toastr');
const initialAppSettings = require('../../appsettings.json');

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
  maxChunkRetries: 3,
  maxFileSize: initialAppSettings.Global.MaxFileUploadSize,
  maxFiles: 1,
  permanentErrors: [400, 401, 404, 409, 415, 500, 501],
  relativePathParameterName: '',
  simultaneousUploads: 3,
  testChunks: false,
  totalChunksParameterName: 'totalChunks',
  totalSizeParameterName: 'totalSize',
  typeParameterName: 'type',
};
