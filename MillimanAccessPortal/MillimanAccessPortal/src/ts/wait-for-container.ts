require('../scss/map.scss');

const currentUrl: string = window.location.href;
let errorCount: number = 0;

// Set up a loop to poll for a redirect
const checkContainerStatus = () => {
  setTimeout(() => {
    fetch(currentUrl, { method: 'GET' })
      .then((response) => {
        if (response.redirected) {
          location.reload();
        }
        checkContainerStatus();
      })
      .catch((_error) => {
        // console.log(_error);
        errorCount++;
      });
  }, 5000);
};

// Initialize the polling
checkContainerStatus();
