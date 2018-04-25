import $ = require('jquery');
import card = require('./card');
import upload = require('./upload/upload');
import options = require('./lib-options');
import { Promise } from 'es6-promise';
import toastr = require('toastr');
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
import { PublicationUpload } from './upload/upload';
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

$(document).ready(() => {
  publicationGUID = generateGUID();

  const c = new card.FileUploadCard(
    'Content file'
  ).build();
  $('#card-list .admin-panel-content').empty().append(c);

  const u = new upload.PublicationUpload(
    c.find('.card-body-container')[0],
    publicationGUID,
    upload.PublicationComponent.Content,
    () => {}
  );

  toastr.info('Page loaded');
});

if (module.hot) {
  module.hot.accept();
}
