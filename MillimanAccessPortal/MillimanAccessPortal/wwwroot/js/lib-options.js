// Configure default vex options
vex.defaultOptions = {
    content: '',
    unsafeContent: '',
    showCloseButton: true,
    escapeButtonCloses: true,
    overlayClosesOnClick: true,
    appendLocation: 'body',
    className: 'vex-theme-default screen-center',
    overlayClassName: '',
    contentClassName: '',
    closeClassName: '',
    closeAllOnPopState: false,
}

// Configure toastr options
toastr.options = {
    closeButton: false,
    debug: false,
    newestOnTop: false,
    progressBar: false,
    positionClass: "toast-bottom-right",
    preventDuplicates: false,
    onclick: null,
    showDuration: "300",
    hideDuration: "1000",
    timeOut: "5000",
    extendedTimeOut: "1000",
    showEasing: "swing",
    hideEasing: "swing",
    showMethod: "show",
    hideMethod: "hide",
};

// Configure jquery validation overrides
var domainValRegex = /^[A-Za-z0-9.-]+\.[A-Za-z]{2,}$/;
var emailValRegex = /^(([^<>()\[\]\\.,;:\s@"]+(\.[^<>()\[\]\\.,;:\s@"]+)*)|(".+"))@((\[[0-9]{1,3}\.[0-9]{1,3}\.[0-9]{1,3}\.[0-9]{1,3}\])|(([a-zA-Z\-0-9]+\.)+[a-zA-Z]{2,}))$/;
$.validator.methods.email = function (value, element) {
    return this.optional(element) || emailValRegex.test(value);
}
