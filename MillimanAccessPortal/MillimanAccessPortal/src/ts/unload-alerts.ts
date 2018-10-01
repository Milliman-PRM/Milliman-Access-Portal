export function setUnloadAlert(fn: () => boolean) {
  window.onbeforeunload = (e) => {
    // If the assigned callback is false, do not prompt for confirmation.
    if (!fn()) { return; }
    // In modern browsers, a generic message is displayed instead.
    const dialogText = 'Are you sure you want to leave this page? Unsaved items will be lost.';
    e.returnValue = dialogText;
    return dialogText;
  };
}
