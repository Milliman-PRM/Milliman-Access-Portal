import $ = require('jquery');
import shared = require('./shared');
import { Resumable } from 'resumablejs';

function displayProgress(progress: number): void {
  $('#file-progress').width((Math.round(progress * 10000) / 100) + '%');
}

export function uploadResumable(r: Resumable) {
  r.upload();
}
