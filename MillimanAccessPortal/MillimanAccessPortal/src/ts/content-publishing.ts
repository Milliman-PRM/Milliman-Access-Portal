import * as $ from 'jquery';
import * as upload from './upload';
import * as forge from 'node-forge';
import * as resumable from 'resumablejs';
import * as tooltipster from 'tooltipster';
import { Promise } from 'es6-promise';
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
    testTarget: '/ContentPublishing/ChunkStatus',
    headers: function() {
      return {
        RequestVerificationToken: $("input[name='__RequestVerificationToken']").val().toString()
      };
    },
    query: function() {
      return {
        rootContentItemId: '1',
      };
    },
    simultaneousUploads: 3,
    maxFiles: 1,
    generateUniqueIdentifier: function (file: File, event: Event) {
      return new Promise((resolve, reject) => {
        const filename = file.name.split('.').join('_');
        const md = forge.md.sha1.create();
        const reader = new FileReader();
        const chunkSize = 1024 * 1024;
        let offset = 0;
        reader.onload = function() {
          $('#checksum-progress-resumable').width((Math.round(offset / file.size * 10000) / 100) + '%');
          md.update(this.result);
          offset += chunkSize;
          if (offset >= file.size) {
            $('#checksum-progress-resumable').width('100%');
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
      })
    },
    maxFileSize: (5 * (2 ** 30)), // 5GiB
    chunkNumberParameterName: 'chunkNumber',
    totalChunksParameterName: 'totalChunks',
    identifierParameterName: 'uid',
    typeParameterName: 'type',
    chunkSizeParameterName: 'chunkSize',
    totalSizeParameterName: 'totalSize',
    fileNameParameterName: 'fileName',
    relativePathParameterName: '',
    currentChunkSizeParameterName: '',
  });
  const progressStats = new upload.ResumableProgressStats(10);
  if (!r.support) {
    alert('not supported'); // TODO: tell user to use a modern browser
  }
  r.assignBrowse($('#upload-form-resumable span')[0], false);
  $('#upload-form-resumable input.submit').click(function (event): void {
    event.preventDefault();
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
