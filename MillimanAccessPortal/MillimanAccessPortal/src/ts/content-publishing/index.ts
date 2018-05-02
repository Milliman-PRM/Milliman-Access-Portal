import $ = require('jquery');
require('tooltipster');
import shared = require('../shared');
import toastr = require('toastr');
import { randomBytes } from 'crypto';
import { FileUploadCard } from '../card';
import { PublicationUpload, PublicationComponent, PublicationComponentInfo } from './publication-upload';
import { renderClientTree } from './dom-methods';

require('../navbar');
import 'bootstrap/scss/bootstrap-reboot.scss';
import 'tooltipster/src/css/tooltipster.css';
import 'tooltipster/src/css/plugins/tooltipster/sideTip/tooltipster-sideTip.css';
import 'toastr/toastr.scss';
import '../../scss/map.scss';


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
  const publicationGUID = generateGUID();
  const unloadAlertStates: Array<boolean> = [];

  $('#card-list .admin-panel-content').empty();
  PublicationComponentInfo.forEach((componentInfo, component) => {
    const componentCard = new FileUploadCard(componentInfo.displayName).build();
    $('#card-list .admin-panel-content').append(componentCard);
    const publicationUpload = new PublicationUpload(
      componentCard.find('.card-body-container')[0],
      (a) => {
        unloadAlertStates[component] = a;
        setUnloadAlert(unloadAlertStates.reduce((prev, cur) => prev || cur, false));
      },
      publicationGUID,
      component,
    );
    unloadAlertStates.push(false);
  });

  shared.get(
    'ContentPublishing/ClientTree',
    [ renderClientTree ],
  )();

  $('.action-icon-expand').click(shared.expandAllListener);
  $('.action-icon-collapse').click(shared.collapseAllListener);
  $('.admin-panel-searchbar-tree').keyup(shared.filterTreeListener);
  $('.admin-panel-searchbar-form').keyup(shared.filterFormListener);

  $('.tooltip').tooltipster();

  // TODO: Remove for production
  toastr.info('Page loaded');
});

if (module.hot) {
  module.hot.accept();
}
