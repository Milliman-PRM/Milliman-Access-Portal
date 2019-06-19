document.addEventListener('DOMContentLoaded', () => {
    handleScroll();
});

function handleScroll() {
    const element = document.getElementById('disclaimer-container');

    if (element.scrollHeight - element.scrollTop === element.clientHeight) {
      (document.getElementById('accept-button') as HTMLButtonElement ).disabled = false;
    }
}
