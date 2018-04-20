import $ = require('jquery');
import upload = require('./upload');
import forge = require('node-forge');
import options = require('./lib-options');
import { Promise } from 'es6-promise';
const resumable = require('resumablejs');
require('tooltipster');
require('./navbar');

import 'bootstrap/scss/bootstrap-reboot.scss';
import 'selectize/src/less/selectize.default.less';
import 'toastr/toastr.scss';
import 'tooltipster/src/css/tooltipster.css';
import 'tooltipster/src/css/plugins/tooltipster/sideTip/tooltipster-sideTip.css';
import 'vex-js/sass/vex.sass';
import '../scss/map.scss';
import { randomBytes } from 'crypto';
const appSettings = require('../../appsettings.json');


let publishingGUID: string;

class Upload {
  r: any; // resumable.js instance, don't have typings for this
  rootElement: HTMLElement;
  checksum: string;
  stats: upload.ResumableProgressStats;

  constructor(rootElement: HTMLElement) {
    this.r = new resumable(Object.assign({}, options.resumableOptions, {
      target: '/ContentPublishing/UploadChunk',
      headers: () => {
        return {
          RequestVerificationToken: $("input[name='__RequestVerificationToken']").val().toString(),
        };
      },
      query: () => {
        return {
          Checksum: this.checksum,
        }
      },
      generateUniqueIdentifier: (file: File, event: Event) => {
        if (publishingGUID === undefined) {
          throw new Error('GUID has not been initialized.')
        }
        return `publication-${publishingGUID}-${/*some way to identify which file on the page it is*/0}`;
      }
    }));
    if (!this.r.support) {
      throw new Error('This browser does not support resumable file uploads.');
    }
    this.r.assignBrowse(rootElement, false);
    this.r.on('fileAdded', (file) => {
      $('#file-name-resumable').html(file.fileName);
      this.generateChecksum(file.file)
        .then(() => this.getChunkStatus())
        .then(() => {
          this.r.upload();
          setUploadState(uploadState.uploading)
        })
        .then(() => this.updateUploadProgress());
    });
    this.r.on('complete', () => {
      // send request to concatenate chunks
    });
    this.rootElement = rootElement;
    this.stats = new upload.ResumableProgressStats(10);
  }

  private generateChecksum(file: File) {
    return new Promise((resolve, reject) => {
      const self = this;
      const md = forge.md.sha1.create();
      const reader = new FileReader();
      const chunkSize = (2 ** 20); // 1 MiB
      let offset = 0;
      reader.onload = function () {
        md.update(this.result);
        offset += chunkSize;
        if (offset >= file.size) {
          self.renderChecksumProgress(1);
          self.checksum = md.digest().toHex();
          resolve();
        } else {
          self.renderChecksumProgress(offset / file.size);
          reader.readAsBinaryString(file.slice(offset, offset + chunkSize));
        }
      };
      reader.onerror = () => reject;
      reader.readAsBinaryString(file.slice(offset, offset + chunkSize));
    })
  }

  private getChunkStatus() {
    // Not implemented
    // TODO: get request for already-received chunks
    // TODO: set `this.r.files[0].chunks[n].tested = true;` for already received
  }

  private renderChecksumProgress(progress: number) {
    const precision = 2;
    const progressFmt = `${Math.floor(progress * 100 * (10 ** precision)) / (10 ** precision)}%`;
    $('#checksum-progress-resumable').width(progressFmt);
  }

  private updateUploadProgress() {
    setTimeout(() => {
      this.stats.update(this.r);
      this.stats.render();
      if (this.r.progress() < 1) {
        this.updateUploadProgress();
      }
    }, 1000);
  }
}

let uploads: [Upload];


function setUnloadAlert(value: boolean) {
  window.onbeforeunload = value
    ? (e) => {
      // In modern browsers, a generic message is displayed instead.
      const dialogText = 'Are you sure you want to leave this page? File upload progress will be lost.';
      e.returnValue = dialogText;
      return dialogText;
    }
    : undefined;
}

function generateGUID() {
  return randomBytes(8).toString('hex');
}

// WIP jQuery upload state management
const uploadState = {
  initial: 0,
  uploading: 1,
  paused: 2,
}
let currentState = uploadState.initial;

function setUploadState(state: number) {
  switch (state) {
    case uploadState.initial:
      $('input.upload').attr('disabled', '');
      break;
    case uploadState.uploading:
      $('input.upload').attr('disabled', '');
      $('#btn-cancel').removeAttr('disabled');
      $('#btn-pause').removeAttr('disabled');
      break;
    case uploadState.paused:
      $('input.upload').attr('disabled', '');
      $('#btn-cancel').removeAttr('disabled');
      $('#btn-resume').removeAttr('disabled');
      break;
  }
}

function configureControlButtons() {
  $('#btn-cancel').click(() => {
    setUploadState(uploadState.initial);
    uploads[0].r.cancel();
  });
  $('#btn-pause').click(() => {
    setUploadState(uploadState.paused);
    uploads[0].r.pause();
  });
  $('#btn-resume').click(() => {
    setUploadState(uploadState.uploading);
    uploads[0].r.upload();
  });
}


$(document).ready(function(): void {
  publishingGUID = generateGUID();
  configureControlButtons();
  setUploadState(uploadState.initial);
  uploads = [
    new Upload($('#file-browse')[0]),
  ]
});
