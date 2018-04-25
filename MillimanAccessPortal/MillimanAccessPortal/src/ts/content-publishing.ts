import $ = require('jquery');
import toastr = require('toastr');
import { randomBytes } from 'crypto';
import { FileUploadCard } from './card';
import { PublicationUpload, PublicationComponent } from './upload/upload';
require('./navbar');

import 'bootstrap/scss/bootstrap-reboot.scss';
import 'toastr/toastr.scss';
import '../scss/map.scss';

let publicationGUID: string;
let uploads: {
  content: PublicationUpload;
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

  const componentDisplayNames: Array<{comp: PublicationComponent, name: string}> = [
    {comp: PublicationComponent.Content, name: 'Content'},
    {comp: PublicationComponent.Image, name: 'Cover image'},
    {comp: PublicationComponent.UserGuide, name: 'User guide'},
  ];

  $('#card-list .admin-panel-content').empty();
  componentDisplayNames.forEach((component) => {
    const componentCard = new FileUploadCard(component.name).build();
    $('#card-list .admin-panel-content').append(componentCard);
    const publicationUploads = new PublicationUpload(
      componentCard.find('.card-body-container')[0],
      (a) => console.log(`"${component.comp}" upload set unload requirement to: ${a}`),
      publicationGUID,
      component.comp,
    );
  });

  toastr.info('Page loaded');
});

if (module.hot) {
  module.hot.accept();
}
