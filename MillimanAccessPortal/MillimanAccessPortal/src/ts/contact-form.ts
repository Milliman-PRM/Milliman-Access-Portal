import $ = require('jquery');
import toastr = require('toastr');
require('./lib-options');
import vex = require('vex-js');

require('toastr/toastr.scss');
require('vex-js/sass/vex.sass');
require('vex-js/sass/vex-theme-default.sass');
require('../scss/map.scss');

/**
 * Submit the contact form
 * @return {undefined}
 */
function submitForm() {
  const $contactForm = $('#contact-form');
  const formRecipient = $contactForm.find('#recipient').val();
  const formSubject = $contactForm.find('#subject').val();
  const formMessage = $contactForm.find('#message').val();

  $.ajax({
    data: {
      message: formMessage,
      recipient: formRecipient,
      subject: formSubject,
    },
    headers: {
      RequestVerificationToken: $('input[name="__RequestVerificationToken"]').val().toString(),
    },
    type: 'POST',
    url: 'Message/SendEmailFromUser',
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
  (vex as any).dialog.open({
    buttons: [
      $.extend({}, (vex as any).dialog.buttons.NO, {
        className: 'blue-button',
        click: function onClick() {
          if ($('#subject').val() && $('#message').val()) {
            submitForm();
          } else {
            toastr.warning('Please provide a subject and message');
            return false;
          }
          return true;
        },
        text: 'SUBMIT',
      }),
      $.extend({}, (vex as any).dialog.buttons.NO, {
        className: 'link-button',
        click: function onClick() {
          resetContactForm();
          return false;
        },
        text: 'Reset',
      }),
    ],
    callback: function callback() {
      return false;
    },
    input: [
      '<h2 id="contact-title">Contact Support</h2>',
      '<form id="contact-form" asp-controller="Message" asp-action="SendEmailFromUser" method="post">',
      '<input id="recipient" type="hidden" name="recipient" value="support.78832.5ad4ee0bf11242a6@helpscout.net" />',
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
      '</form>',
    ].join(''),
  });
}

$(document).ready(function onReady() {

  $('#contact-button').click(function onClick() {
    initializeContactForm();
  });

});
