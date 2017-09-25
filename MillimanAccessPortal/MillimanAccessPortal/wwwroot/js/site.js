// Configure toastr options
toastr.options = {
    "closeButton": false,
    "debug": false,
    "newestOnTop": false,
    "progressBar": false,
    "positionClass": "toast-top-right",
    "preventDuplicates": false,
    "onclick": null,
    "showDuration": "300",
    "hideDuration": "1000",
    "timeOut": "5000",
    "extendedTimeOut": "1000",
    "showEasing": "swing",
    "hideEasing": "swing",
    "showMethod": "show",
    "hideMethod": "hide"
}

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
