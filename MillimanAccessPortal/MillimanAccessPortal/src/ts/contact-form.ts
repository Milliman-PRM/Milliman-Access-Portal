import $ = require('jquery');
import toastr = require('toastr');
require('./lib-options');
import vex = require('vex-js');

require('bootstrap/scss/bootstrap-reboot.scss');
require('toastr/toastr.scss');
require('vex-js/sass/vex.sass');
require('vex-js/sass/vex-theme-default.sass');
require('../scss/map.scss');

/**
 * Submit the contact form
 * @return {undefined}
 */
function submitForm() {
  var $contactForm = $('#contact-form');
  var formRecipient = $contactForm.find('#recipient').val();
  var formSubject = $contactForm.find('#subject').val();
  var formMessage = $contactForm.find('#message').val();

  $.ajax({
    type: 'POST',
    url: 'Message/SendEmailFromUser',
    data: {
      recipient: formRecipient,
      subject: formSubject,
      message: formMessage
    },
    headers: {
      RequestVerificationToken: $("input[name='__RequestVerificationToken']").val().toString()
    }
  }).done(function onDone() {
    toastr.success('Your message has been sent');
    vex.closeAll();
  }).fail(function onFail() {
    toastr.error('Your message was unable to be delivered');
  });
}

/**
 * Reset the contact form
 * @return {undefined}
 */
function resetContactForm() {
  $('#subject').val('');
  $('#message').val('');
}

/**
 * Initialize the contact form
 * @return {undefined}
 */
function initializeContactForm() {
  vex.dialog.open({
    input: [
      '<h2 id="contact-title">Contact Support</h2>',
      '<form id="contact-form" asp-controller="Message" asp-action="SendEmailFromUser" method="post">',
      '<input id="recipient" type="hidden" name="recipient" value="prm.support@milliman.com" />',
      '<div>',
      '<select id="subject" name="subject" required>',
      '<option value="">Please Select a Topic</option>',
      '<option value="Account Inquiry">Account Inquiry</option>',
      '<option value="Bug Report">Report a Bug</option>',
      '<option value="Other Support Question">Other</option>',
      '</select>',
      '</div>',
      '<div>',
      '<textarea id="message" name="message" placeholder="Message" required></textarea>',
      '</div>',
      '</form>'
    ].join(''),
    buttons: [
      $.extend({}, vex.dialog.buttons.NO, {
        text: 'SUBMIT',
        className: 'blue-button',
        click: function onClick() {
          if ($('#subject').val() && $('#message').val()) {
            submitForm();
          } else {
            toastr.warning('Please provide a subject and message');
            return false;
          }
          return true;
        }
      }),
      $.extend({}, vex.dialog.buttons.NO, {
        text: 'Reset',
        className: 'link-button',
        click: function onClick() {
          resetContactForm();
          return false;
        }
      })
    ],
    callback: function callback() {
      return false;
    }
  });
}

$(document).ready(function onReady() {

  $('#contact-button').click(function onClick() {
    initializeContactForm();
  });

});