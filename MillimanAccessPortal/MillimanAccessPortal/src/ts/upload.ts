import $ = require('jquery');
import shared = require('./shared');

function displayProgress(progress: number): void {
  $('#file-progress').width((Math.round(progress * 10000) / 100) + '%');
}

export = function upload() {
  const data = new FormData(<HTMLFormElement> $('#upload-form')[0]);
  $.ajax({
    xhr: shared.xhrWithProgress(displayProgress),
    type: 'POST',
    cache: false,
    contentType: false,
    processData: false,
    url: 'ContentPublishing/Upload',
    data: data,
    headers: {
      RequestVerificationToken: $("input[name='__RequestVerificationToken']").val().toString()
    }
  }).done(function onDone(response) {
    toastr.success('File uploaded to ' + response.MasterFilePath);
  }).fail(function onFail(response) {
    toastr.warning('Error: ' + response.status);
  });
}
