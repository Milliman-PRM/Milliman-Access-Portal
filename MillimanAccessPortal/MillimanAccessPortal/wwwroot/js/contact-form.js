// Contact Form
$("#contact-button").click(function () {
    contactFormToggle();
});

$("#modal-background").click(function (e) {
    if (e.target.id === 'modal-background' || e.target.id === 'contact-close') {
        contactFormToggle();
    }
});

function contactFormToggle() {
    $('#modal-background').toggleClass('hide show');
}

function resetContactForm() {
    $('#topic-select').val("");
    $('#body-text').val("");
}

$(function () {
    var form = $('#contact-form');

    $(form).submit(function (event) {
        // Stop the browser from performing its default behavior of submitting the form to make this asynchronous
        event.preventDefault();

        // Serialize the form data.
        var formData = $(form).serialize();

        // Submit the form using AJAX.
        $.ajax({
            type: 'POST',
            url: $(form).attr('action'),
            data: formData
        })

            // Success
            .done(function (response) {
                toastr["success"]("Your message has been sent");
                resetContactForm();
                contactFormToggle();
            })

            // Failure
            .fail(function (response) {
                toastr["error"]("Your message was unable to be delivered");
            });

    });

});
