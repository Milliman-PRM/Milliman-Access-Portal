import $ = require('jquery');
import card = require('./card');
import upload = require('./upload');
import options = require('./lib-options');
import { Promise } from 'es6-promise';
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
import { PublicationUpload } from './upload';
const appSettings = require('../../appsettings.json');


let publicationGUID: string;



let uploads: {
  content: upload.PublicationUpload;
};


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
  console.log('setting state to ' + state);
  switch (state) {
    case uploadState.initial:
      $('input.upload').attr('disabled', '');
      $('#file-browse input').removeAttr('disabled');
      break;
    case uploadState.uploading:
      $('input.upload').attr('disabled', '');
      $('#file-browse input').attr('disabled', '');
      $('#btn-cancel').removeAttr('disabled');
      $('#btn-pause').removeAttr('disabled');
      break;
    case uploadState.paused:
      $('input.upload').attr('disabled', '');
      $('#btn-cancel').removeAttr('disabled');
      $('#btn-resume').removeAttr('disabled');
      break;
  }
  currentState = state;
}

function configureControlButtons() {
  $('#btn-cancel').click(() => {
    setUploadState(uploadState.initial);
    uploads.content.resumable.cancel();
  });
  $('#btn-pause').click(() => {
    setUploadState(uploadState.paused);
    uploads.content.resumable.pause();
  });
  $('#btn-resume').click(() => {
    setUploadState(uploadState.uploading);
    uploads.content.resumable.upload();
  });
}


$(document).ready(function(): void {
  publicationGUID = generateGUID();

  const c = new card.FileUploadCard(
    'Content file'
  ).build();
  $('#card-list .admin-panel-content').append(c);

  const u = new upload.PublicationUpload(
    c.find('.card-body-container')[0],
    publicationGUID,
    upload.PublicationComponent.Content,
    () => console.log('update state')
  )
});
