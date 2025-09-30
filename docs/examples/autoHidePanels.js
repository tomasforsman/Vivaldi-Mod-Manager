/**
 * Panel Hover
 * 
 * Auto-open panels on hover and auto-close when leaving the panel area.
 * Configurable delays for open, switch, and close actions.
 * 
 * @title Panel Hover
 * @description Auto-open panels on hover and auto-close when leaving
 * @version 1.0.0
 * @author Anonymous
 * @license MIT
 * @compatibility Vivaldi 6.0+
 */

(async () => {
  "use strict";

  /**
   * Configuration object for panel behavior timing and features
   */
  const config = {
    autoClose: true,              // Automatically close panels when mouse leaves panel area
    closeFixed: false,            // Allow closing fixed (non-overlay) panels
    openDelay: 280,               // Delay in ms before opening a panel on hover
    switchDelay: 40,              // Delay in ms when switching between open panels
    closeDelay: 280,              // Delay in ms before closing a panel
    downloadCloseDelay: 20000,    // Delay in ms before auto-closing download panel after download starts
  };

  let panelToggleTimeout;

  /**
   * Validates configuration values and applies defaults for invalid entries
   * @param {Object} cfg - Configuration object to validate
   */
  const validateConfig = (cfg) => {
    const delays = ['openDelay', 'switchDelay', 'closeDelay', 'downloadCloseDelay'];
    delays.forEach(key => {
      if (typeof cfg[key] !== 'number' || cfg[key] < 0) {
        console.warn(`Invalid ${key}, using default 280ms`);
        cfg[key] = key === 'downloadCloseDelay' ? 20000 : 280;
      }
    });
  };

  /**
   * Creates and applies a new stylesheet to the document
   * @param {string} css - CSS rules to add
   */
  const addStyleSheet = (css) => {
    const styleSheet = new CSSStyleSheet();
    styleSheet.replaceSync(css);
    document.adoptedStyleSheets = [...document.adoptedStyleSheets, styleSheet];
  };

  /**
   * Disables pointer events on webview when hovering over panels
   * Prevents accidental interactions with page content while using panels
   */
  const preventWebViewMouseEventsWhenPanelHovered = () => {
    addStyleSheet(`
      #main:has(#panels-container:hover) #webview-container {
        pointer-events: none !important;
      }
    `);
  };

  /**
   * Waits for an element to appear in the DOM
   * @param {string} selector - CSS selector for the target element
   * @param {Node} startNode - Node to start searching from
   * @param {number} timeoutMs - Maximum time to wait in milliseconds
   * @returns {Promise<Element>} The found element
   * @throws {Error} If element is not found within timeout period
   */
  const waitForElement = (selector, startNode = document, timeoutMs = 10000) => {
    return new Promise((resolve, reject) => {
      const checkInterval = 100;
      let elapsedTime = 0;

      const timerId = setInterval(() => {
        const elem = startNode.querySelector(selector);

        if (elem) {
          clearInterval(timerId);
          resolve(elem);
        }

        elapsedTime += checkInterval;
        if (elapsedTime >= timeoutMs) {
          clearInterval(timerId);
          reject(new Error(`Element ${selector} not found within ${timeoutMs}ms`));
        }
      }, checkInterval);
    });
  };

  /**
   * Simulates a complete click event sequence on an element
   * Dispatches pointerdown, mousedown, pointerup, mouseup, and click events
   * @param {Element} element - The element to click
   */
  const simulateClick = (element) => {
    element.dispatchEvent(
      new PointerEvent("pointerdown", { bubbles: true, pointerId: 1 })
    );
    element.dispatchEvent(
      new PointerEvent("mousedown", { bubbles: true, detail: 1 })
    );
    element.dispatchEvent(
      new PointerEvent("pointerup", { bubbles: true, pointerId: 1 })
    );
    element.dispatchEvent(
      new PointerEvent("mouseup", { bubbles: true, detail: 1 })
    );
    element.dispatchEvent(new PointerEvent("click", { bubbles: true }));
  };

  /**
   * Gets the button element of the currently active panel
   * @returns {Element|null} The active panel button or null if none active
   */
  const getActivePanelButton = () =>
    document.querySelector("#panels .active > button");

  /**
   * Toggles a panel with optional delay
   * @param {Element} button - The panel button to toggle
   * @param {boolean} useDelay - Whether to apply delay based on panel state
   */
  const togglePanel = (button, useDelay) => {
    const delay = useDelay
      ? getActivePanelButton()
        ? config.switchDelay
        : config.openDelay
      : 0;

    clearTimeout(panelToggleTimeout);
    panelToggleTimeout = setTimeout(() => {
      simulateClick(button);
    }, delay);
  };

  /**
   * Closes the currently active panel if conditions are met
   * Only closes overlay panels by default unless closeFixed is enabled
   */
  const closePanel = () => {
    if (
      !config.closeFixed &&
      !document.querySelector("#panels-container.overlay")
    ) {
      return;
    }

    setTimeout(() => {
      const activeButton = getActivePanelButton();
      if (activeButton) {
        simulateClick(activeButton);
      }
    }, config.closeDelay);
  };

  /**
   * Checks if an element is a valid panel button
   * Excludes the web panel button from matching
   * @param {Element} element - The element to check
   * @returns {boolean} True if element is a panel button
   */
  const isPanelButton = (element) =>
    element.matches(
      'button:is([name^="Panel"], [name^="WEBPANEL_"]):not([name="PanelWeb"])'
    );

  /**
   * Checks if an event has any keyboard modifiers pressed
   * @param {Event} event - The event to check
   * @returns {boolean} True if any modifier key is pressed
   */
  const hasKeyboardModifiers = (event) =>
    event.altKey || event.ctrlKey || event.shiftKey || event.metaKey;

  /**
   * Sets up automatic closing of download panel after downloads start
   * Listens for download creation events and closes panel after configured delay
   */
  const setupDownloadPanelAutoClose = () => {
    if (chrome && chrome.downloads) {
      chrome.downloads.onCreated.addListener((downloadItem) => {
        console.log("Download created:", downloadItem);
        
        setTimeout(() => {
          const activeButton = getActivePanelButton();
          const isDownloadPanelActive = activeButton?.getAttribute('name') === 'PanelDownloads';
          
          if (isDownloadPanelActive) {
            simulateClick(activeButton);
          }
        }, config.downloadCloseDelay);
      });
    }
  };

  /**
   * Sets up event listeners for panel hover behavior
   * Handles mouseenter, mouseleave, and dragenter events on panel buttons
   * Also sets up auto-close behavior when mouse enters webview area
   */
  const setupPanelHoverBehavior = () => {
    const handlePanelEvent = (event) => {
      if (isPanelButton(event.target) && !hasKeyboardModifiers(event)) {
        switch (event.type) {
          case "mouseenter":
            togglePanel(event.target, true);
            break;
          case "mouseleave":
            clearTimeout(panelToggleTimeout);
            break;
          case "dragenter":
            togglePanel(event.target, false);
            break;
        }
      }
    };

    if (config.autoClose) {
      const webviewContainer = document.querySelector("#webview-container");
      webviewContainer.addEventListener("mouseenter", closePanel);
      // Close panel when new webview becomes visible
      webviewContainer.addEventListener("animationstart", (event) => {
        if (
          event.target.matches("webview") &&
          event.animationName === "delay_visibility"
        ) {
          closePanel();
        }
      });
    }

    const panels = document.querySelector("#panels");
    panels.addEventListener("mouseenter", handlePanelEvent, { capture: true });
    panels.addEventListener("mouseleave", handlePanelEvent, { capture: true });
    panels.addEventListener("dragenter", handlePanelEvent, { capture: true });
  };

  // Initialize the mod
  try {
    validateConfig(config);
    await waitForElement("#browser");
    preventWebViewMouseEventsWhenPanelHovered();
    setupPanelHoverBehavior();
    setupDownloadPanelAutoClose();
  } catch (error) {
    console.error("Failed to initialize panel hover script:", error);
  }
})();