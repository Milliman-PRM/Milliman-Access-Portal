import $ = require('jquery');
import upload = require('./upload');
import 'bootstrap/scss/bootstrap-reboot.scss';
import 'selectize/src/less/selectize.default.less';
import 'toastr/toastr.scss';
import 'tooltipster/src/css/tooltipster.css';
import 'tooltipster/src/css/plugins/tooltipster/sideTip/tooltipster-sideTip.css';
import 'vex-js/sass/vex.sass';
import '../scss/map.scss';

$(document).ready(function(): void {
  $('#upload-form input.submit').click(function (event): void {
    event.preventDefault();
    upload();
  })
});
