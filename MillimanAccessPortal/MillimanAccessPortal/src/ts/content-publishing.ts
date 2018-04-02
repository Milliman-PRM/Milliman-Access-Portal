import $ = require('jquery');
import upload = require('./upload');
import resumable = require('resumablejs');
import 'bootstrap/scss/bootstrap-reboot.scss';
import 'selectize/src/less/selectize.default.less';
import 'toastr/toastr.scss';
import 'tooltipster/src/css/tooltipster.css';
import 'tooltipster/src/css/plugins/tooltipster/sideTip/tooltipster-sideTip.css';
import 'vex-js/sass/vex.sass';
import '../scss/map.scss';

$(document).ready(function(): void {
  const r = new resumable({
    target: '/ContentPublishing/UploadResumable',
    headers: {
      RequestVerificationToken: $("input[name='__RequestVerificationToken']").val().toString()
    },
    query: function () {
      return {
        RootContentItemId: $('#rci-resumable').val().toString(),
      }
    },
    simultaneousUploads: 1
  });
  if (!r.support) {
    alert('not supported');
  }
  r.assignBrowse($('#upload-form-resumable span')[0], false);
  $('#upload-form input.submit').click(function (event): void {
    event.preventDefault();
    upload.upload();
  });
  $('#upload-form-resumable input.submit').click(function (event): void {
    event.preventDefault();
    upload.uploadResumable(r);
  })
});
