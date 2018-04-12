import $ = require('jquery');
import upload = require('./upload');
import forge = require('node-forge');
import resumable = require('resumablejs');
import tooltipster = require('tooltipster');
import options = require('./lib-options');
import { Promise } from 'es6-promise';
import 'bootstrap/scss/bootstrap-reboot.scss';
import 'selectize/src/less/selectize.default.less';
import 'toastr/toastr.scss';
import 'tooltipster/src/css/tooltipster.css';
import 'tooltipster/src/css/plugins/tooltipster/sideTip/tooltipster-sideTip.css';
import 'vex-js/sass/vex.sass';
import '../scss/map.scss';
const appSettings = require('../../appsettings.json');

function setUnloadAlert(value: boolean) {
  window.onbeforeunload = value
    ? () => { return true; }
    : null;
}

function renderChecksumProgress(progress: number) {
  $('#checksum-progress-resumable').width((Math.round(progress * 10000) / 100) + '%');
}

function generateUIDFromSHA1(render: (progress: number) => void) {
  return (file: File, event: Event) => {
    return new Promise((resolve, reject) => {
      const filename = file.name.split('.').join('_');
      const md = forge.md.sha1.create();
      const reader = new FileReader();
      const chunkSize = (2 ** 20); // 1 MiB
      let offset = 0;
      reader.onload = function () {
        render(offset / file.size);
        md.update(this.result);
        offset += chunkSize;
        if (offset >= file.size) {
          render(1);
          const checksum = md.digest().toHex();
          resolve(`${filename}-${checksum}`);
        } else {
          reader.readAsBinaryString(
            file.slice(offset, offset + chunkSize));
        }
      };
      reader.onerror = () => reject(reader.error);
      reader.readAsBinaryString(
        file.slice(offset, offset + chunkSize));
    });
  };
}

$(document).ready(function(): void {
  // Alert the user if leaving the page during an upload
  setUnloadAlert(false);
  const r = new resumable($.extend({}, options.resumableOptions, {
    target: '/ContentPublishing/UploadAndPublish',
    testTarget: '/ContentPublishing/ChunkStatus',
    headers: function() {
      return {
        RequestVerificationToken: $("input[name='__RequestVerificationToken']").val().toString()
      };
    },
    query: function() {
      return {
        rootContentItemId: '1', // TODO: get this from DOM
      };
    },
    generateUniqueIdentifier: generateUIDFromSHA1(renderChecksumProgress),
  }));
  const progressStats = new upload.ResumableProgressStats(10);
  if (!r.support) {
    alert('not supported'); // TODO: tell user to use a modern browser
  }
  r.assignBrowse($('#upload-form-resumable span')[0], false);
  r.on('complete', () => {
    setUnloadAlert(false);
  });
  $('#upload-form-resumable input.submit').click(function (event): void {
    event.preventDefault();
    setUnloadAlert(true);
    r.upload();
    (function updateProgress() {
      setTimeout(() => {
        progressStats.update(r);
        progressStats.render();
        if (r.progress() < 1) {
          updateProgress();
        }
      }, 1000);
    })();
  });
});
