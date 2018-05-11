import $ = require('jquery');
require('tooltipster');
import shared = require('../shared');
import toastr = require('toastr');
import { randomBytes } from 'crypto';
import { FileUploadCard } from '../card';
import { PublicationUpload, PublicationComponent } from './publication-upload';
import { ContentPublishingDOMMethods } from './dom-methods';

require('../navbar');
import 'bootstrap/scss/bootstrap-reboot.scss';
import 'tooltipster/src/css/tooltipster.css';
import 'tooltipster/src/css/plugins/tooltipster/sideTip/tooltipster-sideTip.css';
import 'toastr/toastr.scss';
import '../../scss/map.scss';

$(document).ready(() => {
  ContentPublishingDOMMethods.setup();
  toastr.info('Page loaded');  // TODO: Remove for production
});

if (module.hot) {
  module.hot.accept();
}
