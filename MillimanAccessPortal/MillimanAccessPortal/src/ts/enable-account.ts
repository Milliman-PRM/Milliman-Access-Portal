import '../images/map-logo.svg';

import $ = require('jquery');
require('jquery-validation');
require('jquery-validation-unobtrusive');
import './check-password';

require('../scss/map.scss');

$(document).ready(function () {
  // Don't ignore hidden fields in jquery Validator
  $("form").data("validator").settings.ignore = "";

  $("input").on("keyup", function () {
    if ($('form').validate().checkForm()) {
      $('button[type="submit"]').removeAttr('disabled');
    } else {
      $('button[type="submit"]').attr('disabled', 'disabled');
    }
  })
})
