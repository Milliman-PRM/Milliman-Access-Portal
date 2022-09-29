require('../scss/map.scss');

const currentUrl: string = window.location.href;

// Set up a loop to poll for a redirect
const checkContainerStatus = () => {
  setTimeout(() => {
    fetch(currentUrl, { method: 'GET' })
      .then((response) => {
        if (response.redirected) {
          location.reload();
        } else {
          checkContainerStatus();
        }
      });
  }, 5000);
};

// Initialize the polling
checkContainerStatus();
