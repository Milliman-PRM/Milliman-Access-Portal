// Contact Form

function submitForm() {
  var formRecipient = $('#contact-form #recipient').val();
  var formSubject = $('#contact-form #subject').val();
  var formMessage = $('#contact-form #message').val();

  $.ajax({
    type: 'POST',
    url: 'Message/SendEmailFromUser',
    data: {
      recipient: formRecipient,
      subject: formSubject,
      message: formMessage,
    },
    headers: {
      RequestVerificationToken: $("input[name='__RequestVerificationToken']").val(),
    },
  }).done(() => {
    toastr.success('Your message has been sent');
    vex.closeAll();
  }).fail(() => {
    toastr.error('Your message was unable to be delivered');
  });
}

function resetContactForm() {
  $('#subject').val('');
  $('#message').val('');
}

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
        click() {
          if ($('#subject').val() && $('#message').val()) {
            submitForm();
          } else {
            toastr.warning('Please provide a subject and message');
            return false;
          }
          return true;
        },
      }),
      $.extend({}, vex.dialog.buttons.NO, {
        text: 'Reset',
        className: 'link-button',
        click() {
          resetContactForm();
          return false;
        },
      }),
    ],
    callback() {
      return false;
    },
  });
}

$('#contact-button').click(() => {
  initializeContactForm();
});
