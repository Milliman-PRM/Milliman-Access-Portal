// Contact Form
$("#contact-button").click(function () {
    contactFormToggle();
})

$("#modal-background").click(function (e) {
    if (e.target.id === 'modal-background' || e.target.id === 'contact-close') {
        contactFormToggle();
    }
})

function contactFormToggle() {
    $('#modal-background').toggleClass('hide show');
}

function resetContactForm() {
    $('#topic-select').val("");
    $('#body-text').val("");
}
