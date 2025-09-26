// Auto-close Downloads Panel mod for Vivaldi (DOM-based approach)
// Monitors DOM changes to detect download panel opening and closes it after delay
(function autoCloseDownloadsPanel() {
  "use strict";

  const CLOSE_DELAY_MS = 20_000;
  const DOWNLOADS_TOGGLE_SELECTOR = 'button[data-id="downloads"]';
  const DOWNLOADS_PANEL_SELECTOR = '.downloads-panel, [data-panel="downloads"]';

  let pendingTimer = null;
  let lastPanelState = false;

  function getDownloadsToggle() {
    return document.querySelector(DOWNLOADS_TOGGLE_SELECTOR);
  }

  function isDownloadsPanelOpen(toggle) {
    if (!toggle) return false;
    return toggle.getAttribute("aria-pressed") === "true";
  }

  function getDownloadsPanel() {
    return document.querySelector(DOWNLOADS_PANEL_SELECTOR);
  }

  function scheduleClose() {
    const toggle = getDownloadsToggle();
    if (!toggle) {
      console.warn("Auto-close Downloads Panel: Toggle button not found");
      return;
    }

    if (pendingTimer) {
      clearTimeout(pendingTimer);
    }

    pendingTimer = setTimeout(() => {
      const currentToggle = getDownloadsToggle();
      if (currentToggle && isDownloadsPanelOpen(currentToggle)) {
        currentToggle.click();
        console.log("Auto-close Downloads Panel: Panel auto-closed after delay");
      }
      pendingTimer = null;
    }, CLOSE_DELAY_MS);

    console.log("Auto-close Downloads Panel: Close scheduled in", CLOSE_DELAY_MS / 1000, "seconds");
  }

  function checkPanelState() {
    const toggle = getDownloadsToggle();
    if (!toggle) return;

    const currentState = isDownloadsPanelOpen(toggle);

    // Panel just opened
    if (currentState && !lastPanelState) {
      console.log("Auto-close Downloads Panel: Panel opened, scheduling close");
      scheduleClose();
    }
    // Panel closed manually - cancel scheduled close
    else if (!currentState && lastPanelState && pendingTimer) {
      clearTimeout(pendingTimer);
      pendingTimer = null;
      console.log("Auto-close Downloads Panel: Panel closed manually, timer cancelled");
    }

    lastPanelState = currentState;
  }

  function observeDownloadsButton() {
    const toggle = getDownloadsToggle();
    if (!toggle) return false;

    // Watch for aria-pressed attribute changes
    const observer = new MutationObserver((mutations) => {
      mutations.forEach((mutation) => {
        if (mutation.type === "attributes" &&
          mutation.attributeName === "aria-pressed") {
          checkPanelState();
        }
      });
    });

    observer.observe(toggle, {
      attributes: true,
      attributeFilter: ["aria-pressed"]
    });

    // Initial state check
    checkPanelState();
    console.log("Auto-close Downloads Panel: Monitoring downloads panel state");
    return true;
  }

  function observeDownloadsArea() {
    // Watch for new download items being added to the panel
    const panel = getDownloadsPanel();
    if (!panel) return;

    const observer = new MutationObserver((mutations) => {
      let hasNewDownloadItems = false;

      mutations.forEach((mutation) => {
        if (mutation.type === "childList" && mutation.addedNodes.length > 0) {
          // Check if any added nodes look like download items
          mutation.addedNodes.forEach((node) => {
            if (node.nodeType === Node.ELEMENT_NODE &&
              (node.classList?.contains('download-item') ||
                node.querySelector?.('.download-item'))) {
              hasNewDownloadItems = true;
            }
          });
        }
      });

      if (hasNewDownloadItems) {
        console.log("Auto-close Downloads Panel: New download detected via DOM");
        scheduleClose();
      }
    });

    observer.observe(panel, {
      childList: true,
      subtree: true
    });
  }

  function initialize() {
    if (observeDownloadsButton()) {
      observeDownloadsArea();
      return;
    }

    // If toggle not found, wait for it to appear
    const observer = new MutationObserver(() => {
      if (getDownloadsToggle()) {
        observer.disconnect();
        initialize();
      }
    });

    observer.observe(document.body, {
      childList: true,
      subtree: true,
    });
  }

  // Start when DOM is ready
  if (document.readyState === "loading") {
    document.addEventListener("DOMContentLoaded", initialize, { once: true });
  } else {
    initialize();
  }

  console.log("Auto-close Downloads Panel: DOM-based script loaded");
})();
