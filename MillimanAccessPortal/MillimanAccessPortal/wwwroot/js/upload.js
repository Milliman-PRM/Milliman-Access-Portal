/* global shared */

function upload() {
  var data = new FormData($('#upload-form')[0]);
  $.ajax({
    xhr: shared.xhrWithProgress(console.log),
    type: 'POST',
    cache: false,
    contentType: false,
    processData: false,
    url: 'ContentPublishing/Upload',
    data: data,
    headers: {
      RequestVerificationToken: $("input[name='__RequestVerificationToken']").val()
    }
  }).done(function onDone(response) {
    toastr.success(response);
  }).fail(function onFail(response) {
    toastr.warning(response);
  });
}

$(document).ready(function () {
  $('#upload-form input.submit').click(function (event) {
    event.preventDefault();
    upload();
  });
});
