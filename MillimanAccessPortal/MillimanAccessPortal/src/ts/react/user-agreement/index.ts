document.addEventListener('DOMContentLoaded', () => {
    handleScroll();
});

function handleScroll() {
    let element = document.getElementById('disclaimer-container');

    if (element.scrollHeight - element.scrollTop === element.clientHeight) {
        (<HTMLInputElement> document.getElementById('accept-button')).disabled = false;
    }
}
