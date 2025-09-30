/**
 * Color Tabs
 * 
 * Colors tabs based on their favicon or theme color.
 * Applies accent colors from page favicons to inactive tabs.
 * 
 * @title Color Tabs
 * @description Color tabs based on favicon colors
 * @version 2.0.0
 * @author Anonymous
 * @license MIT
 * @compatibility Vivaldi 6.0+
 * @requires chroma-js (loaded via Vivaldi)
 */

(async () => {
  "use strict";

  /**
   * Configuration object for color tabs behavior
   */
  const config = {
    updateDelay: 100,              // Delay in ms for delayed color updates
    luminanceThreshold: 0.4,       // Threshold to determine if color is bright
    darkenFactorMedium: 0.4,      // Darken factor for accent bg dark
    darkenFactorHeavy: 1,         // Darken factor for accent bg darker
    alphaLight: 0.45,             // Alpha for light backgrounds
    alphaDark: 0.55,              // Alpha for dark backgrounds
    alphaLightHeavy: 0.25,        // Heavy alpha for light backgrounds
    alphaDarkHeavy: 0.35,         // Heavy alpha for dark backgrounds
    fgAlpha: 0.15,                // Alpha for foreground
    fgAlphaHeavy: 0.05,           // Heavy alpha for foreground
  };

  const WHITE = chroma("#FFF");
  const BLACK = chroma("#000");

  const STYLE = `
    .tab .favicon:not(.svg) {
      filter: drop-shadow(1px 0 0 rgba(246, 246, 246, 0.75)) drop-shadow(-1px 0 0 rgba(246, 246, 246, 0.75)) drop-shadow(0 1px 0 rgba(246, 246, 246, 0.75)) drop-shadow(0 -1px 0 rgba(246, 246, 246, 0.75));
    }
  `;

  const INTERNAL_PAGES = [
    "chrome://",
    "vivaldi://",
    "devtools://",
    "chrome-extension://"
  ];

  /**
   * Main ColorTabs class
   */
  class ColorTabs {
    #observer = null;

    constructor() {
      this.#addStyle();
      this.#colorTabs();
      this.#addListeners();
    }

    /**
     * Adds stylesheet to the document
     */
    #addStyle() {
      const style = document.createElement("style");
      style.innerHTML = STYLE;
      this.#head.appendChild(style);
    }

    /**
     * Adds event listeners for tab and theme changes
     */
    #addListeners() {
      chrome.tabs.onCreated.addListener(() => this.#colorTabsDelayed());
      chrome.tabs.onActivated.addListener(() => this.#colorTabsDelayed());
      vivaldi.tabsPrivate.onThemeColorChanged.addListener(() => this.#colorTabsDelayed());

      vivaldi.prefs.onChanged.addListener((info) => {
        if (info.path.startsWith("vivaldi.themes")) {
          this.#colorTabsDelayed();
        }
      });
    }

    /**
     * Colors tabs with a delay for better performance
     */
    #colorTabsDelayed() {
      this.#colorTabs();
      setTimeout(() => this.#colorTabs(), config.updateDelay);
    }

    /**
     * Colors all tabs based on current theme settings
     */
    async #colorTabs() {
      const tabs = document.querySelectorAll("div.tab");
      const theme = await this.#getCurrentTheme();

      const accentFromPage = theme.accentFromPage;
      const transparencyTabs = theme.transparencyTabs;
      const tabColorAllowed = accentFromPage && !transparencyTabs;

      if (tabColorAllowed) {
        const accentOnWindow = theme.accentOnWindow;
        const colorAccentBg = chroma(theme.colorAccentBg);
        const accentSaturationLimit = theme.accentSaturationLimit;
        tabs.forEach((tab) => this.#setTabColor(tab, accentOnWindow, colorAccentBg, accentSaturationLimit));
      } else {
        tabs.forEach((tab) => this.#resetTabColor(tab));
      }
    }

    /**
     * Resets tab colors to default
     * @param {HTMLElement} tab - Tab element to reset
     */
    async #resetTabColor(tab) {
      tab.style.backgroundColor = null;
      tab.style.color = null;
    }

    /**
     * Sets custom color for a tab based on its favicon
     * @param {HTMLElement} tab - Tab element to color
     * @param {boolean} accentOnWindow - Whether accent is applied to window
     * @param {Object} colorAccentBg - Base accent color
     * @param {number} accentSaturationLimit - Saturation limit for colors
     */
    async #setTabColor(tab, accentOnWindow, colorAccentBg, accentSaturationLimit) {
      const tabId = this.#getTabId(tab);
      const chromeTab = await this.#getChromeTab(tabId);
      const isInternalPage = this.#isInternalPage(chromeTab.url);

      if (!isInternalPage) {
        const image = tab.querySelector("img");
        if (image) {
          const palette = this.#getPalette(image);
          if (palette && palette.length > 0) {
            colorAccentBg = chroma(palette[0]);
          }
        }
      }

      const saturation = colorAccentBg.get("hsl.s");
      colorAccentBg = colorAccentBg.set("hsl.s", saturation * accentSaturationLimit);
      const isBright = colorAccentBg.luminance() > config.luminanceThreshold;
      const colorAccentFg = isBright ? BLACK : WHITE;

      if (isInternalPage) {
        if (this.#isTabActive(tab)) {
          this.#setAccentColors(colorAccentBg, colorAccentFg, isBright);
          tab.style.backgroundColor = accentOnWindow ? "var(--colorBg)" : "var(--colorAccentBg)";
        } else {
          tab.style.backgroundColor = "var(--colorBgDark)";
        }
        tab.style.color = "var(--colorFg)";
        return;
      }

      if (this.#isTabActive(tab)) {
        this.#setAccentColors(colorAccentBg, colorAccentFg, isBright);
        if (accentOnWindow) {
          tab.style.backgroundColor = tab.classList.contains("active") ? "var(--colorBg)" : "var(--colorBgDark)";
          tab.style.color = "var(--colorFg)";
        }
        return;
      }

      tab.style.backgroundColor = colorAccentBg.css();
      tab.style.color = colorAccentFg.css();
    }

    /**
     * Sets CSS custom properties for accent colors
     * @param {Object} colorAccentBg - Background accent color
     * @param {Object} colorAccentFg - Foreground accent color
     * @param {boolean} isBright - Whether the background is bright
     */
    #setAccentColors(colorAccentBg, colorAccentFg, isBright) {
      this.#setColor("--colorAccentBg", colorAccentBg);
      this.#setColor("--colorAccentBgDark", colorAccentBg.darken(config.darkenFactorMedium));
      this.#setColor("--colorAccentBgDarker", colorAccentBg.darken(config.darkenFactorHeavy));
      this.#setColor("--colorAccentBgAlpha", colorAccentBg.alpha(isBright ? config.alphaLight : config.alphaDark));
      this.#setColor("--colorAccentBgAlphaHeavy", colorAccentBg.alpha(isBright ? config.alphaLightHeavy : config.alphaDarkHeavy));

      this.#setColor("--colorAccentFg", colorAccentFg);
      this.#setColor("--colorAccentFgAlpha", colorAccentFg.alpha(config.fgAlpha));
      this.#setColor("--colorAccentFgAlphaHeavy", colorAccentFg.alpha(config.fgAlphaHeavy));
    }

    /**
     * Extracts color palette from favicon image
     * @param {HTMLImageElement} image - Favicon image element
     * @returns {Array} Array of RGB color arrays
     */
    #getPalette(image) {
      const w = image.width;
      const h = image.height;

      const canvas = document.createElement("canvas");
      canvas.width = w;
      canvas.height = h;

      const context = canvas.getContext("2d");
      context.imageSmoothingEnabled = false;
      context.drawImage(image, 0, 0, w, h);

      const pixelData = context.getImageData(0, 0, w, h).data;
      const pixelCount = pixelData.length / 4;

      const colorPalette = [];

      for (let pixelIndex = 0; pixelIndex < pixelCount; pixelIndex++) {
        const offset = 4 * pixelIndex;
        const red = pixelData[offset];
        const green = pixelData[offset + 1];
        const blue = pixelData[offset + 2];
        let colorIndex;

        if (!(red === 0 || red > 240 && green > 240 && blue > 240)) {
          for (let colorIndexIterator = 0; colorIndexIterator < colorPalette.length; colorIndexIterator++) {
            const currentColor = colorPalette[colorIndexIterator];
            if (red === currentColor[0] && green === currentColor[1] && blue === currentColor[2]) {
              colorIndex = colorIndexIterator;
              break;
            }
          }
          if (colorIndex === undefined) {
            colorPalette.push([red, green, blue, 1]);
          } else {
            colorPalette[colorIndex][3]++;
          }
        }
      }
      colorPalette.sort((a, b) => b[3] - a[3]);
      const topColors = colorPalette.slice(0, Math.min(10, colorPalette.length));
      return topColors.map(color => [color[0], color[1], color[2]]);
    }

    /**
     * Sets a CSS custom property
     * @param {string} property - CSS property name
     * @param {Object} color - Chroma color object
     */
    #setColor(property, color) {
      this.#browser.style.setProperty(property, color.css());
    }

    /**
     * Gets tab ID from tab element
     * @param {HTMLElement} tab - Tab element
     * @returns {string} Tab ID
     */
    #getTabId(tab) {
      return tab.getAttribute("data-id").slice(4);
    }

    /**
     * Checks if URL is an internal page
     * @param {string} url - URL to check
     * @returns {boolean} True if internal page
     */
    #isInternalPage(url) {
      return INTERNAL_PAGES.some((p) => url.startsWith(p));
    }

    /**
     * Gets Chrome tab object by ID
     * @param {string} tabId - Tab ID
     * @returns {Promise<Object>} Chrome tab object
     */
    async #getChromeTab(tabId) {
      return tabId.length < 16 ? await chrome.tabs.get(Number(tabId)) : await this.#getFirstChromeTabInGroup(tabId);
    }

    /**
     * Gets first Chrome tab in a group
     * @param {string} groupId - Group ID
     * @returns {Promise<Object>} First tab in group
     */
    async #getFirstChromeTabInGroup(groupId) {
      const tabs = await chrome.tabs.query({ currentWindow: true });
      return tabs.find((tab) => {
        const vivExtData = JSON.parse(tab.vivExtData);
        return vivExtData.group === groupId;
      });
    }

    /**
     * Checks if tab is active
     * @param {HTMLElement} tab - Tab element
     * @returns {boolean} True if tab is active
     */
    #isTabActive(tab) {
      return tab.classList.contains("active");
    }

    /**
     * Gets current theme settings
     * @returns {Promise<Object>} Current theme object
     */
    async #getCurrentTheme() {
      const themeId = await vivaldi.prefs.get("vivaldi.themes.current");
      const themes = Array.prototype.concat(await vivaldi.prefs.get("vivaldi.themes.system"), await vivaldi.prefs.get("vivaldi.themes.user"));
      return themes.find(theme => theme.id === themeId);
    }

    get #browser() {
      return document.querySelector("#browser");
    }

    get #head() {
      return document.querySelector("head");
    }
  }

  /**
   * Waits for browser element and initializes ColorTabs
   * @param {number} timeout - Maximum time to wait in ms
   * @param {number} interval - Check interval in ms
   */
  const waitAndInitialize = (timeout = 1000, interval = 100) => {
    setTimeout(() => {
      const checkInterval = setInterval(() => {
        if (document.querySelector("#browser")) {
          window.colorTabs = new ColorTabs();
          clearInterval(checkInterval);
        }
      }, interval);
    }, timeout);
  };

  try {
    waitAndInitialize();
  } catch (error) {
    console.error("Failed to initialize Color Tabs:", error);
  }
})();