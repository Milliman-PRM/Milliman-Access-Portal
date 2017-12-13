// Contact Form

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
                click: function () {
                    if ($('#subject').val() && $('#message').val()) {
                        submitForm();
                        vex.closeAll();
                    } else {
                        toastr['warning']('Please provide a subject and message');
                        return false;
                    }
                }
            }),
            $.extend({}, vex.dialog.buttons.NO, {
                text: 'Reset',
                className: 'link-button',
                click: function () {
                    resetContactForm();
                    return false;
                }
            }),
        ],
        callback: function () {
            return false;
        }
    })
};

$("#contact-button").click(function () {
    initializeContactForm();
});

function resetContactForm() {
    $('#subject').val("");
    $('#message').val("");
}

function submitForm() {
    var form = $('#contact-form');

    // Serialize the form data.
    var formData = $(form).serialize();
    console.log(formData);

    // Submit the form using AJAX.
    $.ajax({
        type: 'POST',
        url: $(form).attr('action'),
        data: {
            'recipient': $('#contact-form #recipient').val(),
            'subject': $('#contact-form #subject').val(),
            'message': $('#contact-form #message').val()
        }
    }).done(function (response) {
        toastr["success"]("Your message has been sent");
    }).fail(function (response) {
        toastr["error"]("Your message was unable to be delivered");
    });

};
