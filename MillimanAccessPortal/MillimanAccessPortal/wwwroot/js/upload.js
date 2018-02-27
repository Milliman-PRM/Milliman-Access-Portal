function upload() {
  var data = new FormData();
  data.append('file', $('#file')[0].files[0]);
  data.append('RootContentItemId', $('#rci').val());
  $.ajax({
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
    event.stopPropagation();
    event.preventDefault();
    upload();
  });
});
