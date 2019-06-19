document.addEventListener('DOMContentLoaded', function () {
    handleScroll();
});
function handleScroll() {
    var element = document.getElementById('disclaimer-container');
    if (element.scrollHeight - element.scrollTop === element.clientHeight) {
        document.getElementById('accept-button').disabled = false;
    }
}
//# sourceMappingURL=index.js.map