/**
 * Choose Search Engine in Address Bar
 * 
 * Adds quick search engine selection buttons to the address bar dropdown.
 * Allows switching between search engines without typing keywords.
 * 
 * @title Search Engine Selector
 * @description Add search engine buttons to address bar dropdown
 * @version 2.0.0
 * @author Tam710562
 * @license MIT
 * @compatibility Vivaldi 6.0+
 */

(async () => {
  "use strict";

  /**
   * Configuration object for search engine selector behavior
   */
  const config = {
    oneClick: false,  // If true, selecting an engine immediately executes the search
  };

  const gnoh = {
    uuid: {
      generate(ids) {
        let d = Date.now() + performance.now();
        let r;
        const id = "xxxxxxxx-xxxx-4xxx-yxxx-xxxxxxxxxxxx".replace(/[xy]/g, (c) => {
          r = (d + Math.random() * 16) % 16 | 0;
          d = Math.floor(d / 16);
          return (c === "x" ? r : (r & 0x3 | 0x8)).toString(16);
        });

        if (Array.isArray(ids) && ids.includes(id)) {
          return this.generate(ids);
        }
        return id;
      },
    },
    addStyle(css, id) {
      this.styles = this.styles || {};
      if (Array.isArray(css)) {
        css = css.join("");
      }
      id = id || this.uuid.generate(Object.keys(this.styles));
      this.styles[id] = this.createElement("style", {
        html: css || "",
        "data-id": id,
      }, document.head);
      return this.styles[id];
    },
    encode: {
      regex(str) {
        return !str ? str : str.replace(/[-\/\\^$*+?.()|[\]{}]/g, "\\$&");
      },
    },
    createElement(tagName, attribute, parent, inner, options) {
      if (typeof tagName === "undefined") {
        return;
      }
      options = options || {};
      options.isPrepend = options.isPrepend || false;

      const el = document.createElement(tagName);
      if (attribute && typeof attribute === "object") {
        for (const key in attribute) {
          if (key === "text") {
            el.textContent = attribute[key];
          } else if (key === "html") {
            el.innerHTML = attribute[key];
          } else if (key === "style" && typeof attribute[key] === "object") {
            for (const css in attribute.style) {
              el.style.setProperty(css, attribute.style[css]);
            }
          } else if (key === "events" && typeof attribute[key] === "object") {
            for (const event in attribute.events) {
              if (typeof attribute.events[event] === "function") {
                el.addEventListener(event, attribute.events[event]);
              }
            }
          } else if (typeof el[key] !== "undefined") {
            el[key] = attribute[key];
          } else {
            if (typeof attribute[key] === "object") {
              attribute[key] = JSON.stringify(attribute[key]);
            }
            el.setAttribute(key, attribute[key]);
          }
        }
      }
      if (inner) {
        if (!Array.isArray(inner)) {
          inner = [inner];
        }
        for (let i = 0; i < inner.length; i++) {
          if (inner[i].nodeName) {
            el.append(inner[i]);
          } else {
            el.append(this.createElementFromHTML(inner[i]));
          }
        }
      }
      if (typeof parent === "string") {
        parent = document.querySelector(parent);
      }
      if (parent) {
        if (options.isPrepend) {
          parent.prepend(el);
        } else {
          parent.append(el);
        }
      }
      return el;
    },
    createElementFromHTML(html) {
      return this.createElement("template", {
        html: (html || "").trim(),
      }).content;
    },
    observeDOM(obj, callback, config) {
      const obs = new MutationObserver((mutations, observer) => {
        if (config) {
          callback(mutations, observer);
        } else {
          if (mutations[0].addedNodes.length || mutations[0].removedNodes.length) {
            callback(mutations, observer);
          }
        }
      });
      obs.observe(obj, config || {
        childList: true,
        subtree: true,
      });
    },
    override(obj, functionName, callback, condition, runBefore) {
      this._overrides = this._overrides || {};
      let subKey = "";
      try {
        if (obj.ownerDocument === document) {
          this._overrides._elements = this._overrides._elements || [];
          const element = this._overrides._elements.find((item) => item.element === obj);
          let id;
          if (element) {
            id = element.id;
          } else {
            id = this.uuid.generate(this._overrides._elements.map((item) => item.id));
            this._overrides._elements.push({
              element: obj,
              id: id,
            });
          }
          subKey = "_" + id;
        }
      } catch (e) { }
      const key = functionName + "_" + obj.constructor.name + subKey;
      if (!this._overrides[key]) {
        this._overrides[key] = [];
        obj[functionName] = ((_super) => function () {
          let result;
          let shouldRun = true;
          for (let i = 0; i < gnoh._overrides[key].length; i++) {
            shouldRun = shouldRun && (typeof gnoh._overrides[key][i].condition !== "function" && gnoh._overrides[key][i].condition !== false || typeof gnoh._overrides[key][i].condition === "function" && !!gnoh._overrides[key][i].condition.apply(this, arguments));
            if (shouldRun === false) {
              continue;
            }
            if (gnoh._overrides[key][i].runBefore === true) {
              gnoh._overrides[key][i].callback.apply(this, arguments);
            }
          }
          if (shouldRun) {
            result = _super.apply(this, arguments);
          }
          for (let i = 0; i < gnoh._overrides[key].length; i++) {
            if (gnoh._overrides[key][i].runBefore !== true) {
              const args = Array.from(arguments);
              args.push(result);
              gnoh._overrides[key][i].callback.apply(this, args);
            }
          }
          return result;
        })(obj[functionName]);
      }

      this._overrides[key].push({
        callback: callback,
        condition: condition,
        runBefore: runBefore,
      });
      return key;
    },
    getReactPropsKey(element) {
      if (!this.reactPropsKey) {
        if (!element) {
          element = document.getElementById("browser");
        } else if (typeof element === "string") {
          element = document.querySelector(element);
        }
        if (!element || element.ownerDocument !== document) {
          return;
        }
        this.reactPropsKey = Object.keys(element).find((key) => key.startsWith("__reactProps"));
      }
      return this.reactPropsKey;
    }
  };

  const styles = [
    ".UrlBar-AddressField .OmniDropdown .search-engines-in-address-bar { position: sticky; top: 0; right: 0; left: 0; margin-left: -4px; margin-right: -4px; transform: translateY(-5px); background: var(--colorBg); height: 32px; box-shadow: 0px -1px var(--colorBorder) inset; z-index: 1; }",
    ".UrlBar-AddressField .OmniDropdown .search-engines-in-address-bar button { background: transparent; border: 0; width: 32px; height: 32px; border-radius: 0; display: inline-flex; align-items: center; justify-content: center; border: 1px solid transparent; }",
    ".UrlBar-AddressField .OmniDropdown .search-engines-in-address-bar button:hover { background-color: var(--colorFgAlpha); }",
    ".UrlBar-AddressField .OmniDropdown .search-engines-in-address-bar button.active { background: var(--colorBgIntense); border-left-color: var(--colorBorder); border-right-color: var(--colorBorder); border-top-color: var(--colorBorder); }",
    ".UrlBar-AddressField .OmniDropdown .search-engines-in-address-bar button.disabled { pointer-events: none; }",
    ".UrlBar-AddressField .OmniDropdown .search-engines-in-address-bar button:first-child { border-left-color: transparent; }"
  ];

  const settings = {
    searchEngines: {
      default: undefined,
      defaultPrivate: undefined,
      engines: {}
    },
  };

  const pattern = {
    searchEngines: undefined
  };

  let searchEngineButtons;
  let reactPropsKey;

  /**
   * Creates regex pattern from search engine collection
   * @param {Array} searchEngineCollection - Array of search engine objects
   */
  const createPatternSearchEngines = (searchEngineCollection) => {
    settings.searchEngines = {
      default: undefined,
      defaultPrivate: undefined,
      engines: {}
    };
    pattern.searchEngines = undefined;
    if (searchEngineCollection.length > 0) {
      const regKeywords = [];
      searchEngineCollection.forEach((engine) => {
        settings.searchEngines.engines[engine.keyword] = engine;
        regKeywords.push(gnoh.encode.regex(engine.keyword));
      });

      pattern.searchEngines = new RegExp("^(" + regKeywords.join("|") + ")\\s(.*)", "i");
    }
  };

  /**
   * Sets the active state of search engine buttons
   * @param {string} keyword - Keyword of the active search engine
   */
  const setActiveSearchEngineButton = (keyword) => {
    searchEngineButtons.forEach((seb) => {
      if (seb.keyword === keyword) {
        seb.element.classList.add("active");
        if (!config.oneClick) {
          seb.element.classList.add("disabled");
        }
      } else {
        seb.element.classList.remove("active");
        if (!config.oneClick) {
          seb.element.classList.remove("disabled");
        }
      }
    });
  };

  /**
   * Creates the search engine button bar in the dropdown
   * @param {HTMLElement} omniDropdown - The dropdown element to add buttons to
   */
  const createSearchEnginesInAddressBar = (omniDropdown) => {
    const searchEnginesInAddressBar = gnoh.createElement("div", {
      class: "search-engines-in-address-bar"
    }, omniDropdown, null, {
      isPrepend: true
    });

    const addressfieldEl = document.querySelector("input[type=\"text\"].url.vivaldi-addressfield");

    searchEngineButtons = [];

    Object.values(settings.searchEngines.engines).forEach((engine) => {
      const searchEngineButton = gnoh.createElement("button", {
        class: "search-engine-button",
        title: engine.keyword + " : " + engine.name,
        events: {
          mousedown(event) {
            event.preventDefault();
            event.stopPropagation();
            if (!addressfieldEl) {
              return;
            }
            const match = addressfieldEl.value.match(pattern.searchEngines);
            let value = "";
            if (match) {
              if (match[1] === engine.keyword && !config.oneClick) {
                return;
              }
              value = engine.keyword + " " + match[2];
            } else {
              value = engine.keyword + " " + addressfieldEl.value;
            }
            if (config.oneClick) {
              gnoh.observeDOM(addressfieldEl, (mutations, observer) => {
                addressfieldEl[reactPropsKey].onKeyDown(new KeyboardEvent("keydown", { key: "Enter", metaKey: true }));
                observer.disconnect();
              }, {
                attributeFilter: ["value"]
              });
              addressfieldEl[reactPropsKey].onChange({ currentTarget: { value: value } });
            } else {
              addressfieldEl[reactPropsKey].onChange({ currentTarget: { value: value } });
              setActiveSearchEngineButton(engine.keyword);
            }
          }
        }
      }, searchEnginesInAddressBar);
      const icon = engine.faviconUrl.startsWith("data:image") ? engine.faviconUrl : "chrome://favicon/size/16@1x/iconurl/" + engine.faviconUrl + " 1x,chrome://favicon/size/16@2x/iconurl/" + engine.faviconUrl + " 2x";
      const searchEngineIcon = gnoh.createElement("img", {
        class: "search-engine-icon",
        srcset: icon,
        width: 16,
        height: 16
      }, searchEngineButton);
      searchEngineButtons.push({
        keyword: engine.keyword,
        element: searchEngineButton
      });
    });

    const removeSearchEngineButton = gnoh.createElement("button", {
      class: "remove-search-engine-button",
      events: {
        mousedown(event) {
          event.preventDefault();
          event.stopPropagation();
          if (!addressfieldEl) {
            return;
          }
          const match = addressfieldEl.value.match(pattern.searchEngines);
          let value = "";
          if (match) {
            value = match[2];
            addressfieldEl[reactPropsKey].onChange({ currentTarget: { value: value } });
          }
          setActiveSearchEngineButton("");
        }
      }
    }, searchEnginesInAddressBar, '<svg style="width:16px; height:16px" viewBox="0 0 24 24"><path fill="currentColor" d="M12,2C17.53,2 22,6.47 22,12C22,17.53 17.53,22 12,22C6.47,22 2,17.53 2,12C2,6.47 6.47,2 12,2M15.59,7L12,10.59L8.41,7L7,8.41L10.59,12L7,15.59L8.41,17L12,13.41L15.59,17L17,15.59L13.41,12L17,8.41L15.59,7Z"/></svg>');

    searchEngineButtons.push({
      keyword: "",
      element: removeSearchEngineButton
    });

    const match = addressfieldEl.value.match(pattern.searchEngines);
    setActiveSearchEngineButton(match ? match[1] : "");

    if (!addressfieldEl.dataset.searchEnginesInAddressBar) {
      addressfieldEl.dataset.searchEnginesInAddressBar = "";

      const valueSetter = Object.getOwnPropertyDescriptor(addressfieldEl, "value").set;
      const prototype = Object.getPrototypeOf(addressfieldEl);
      const prototypeValueSetter = Object.getOwnPropertyDescriptor(prototype, "value").set;

      Object.defineProperty(addressfieldEl, "value", {
        set(value) {
          const match = value.match(pattern.searchEngines);
          setActiveSearchEngineButton(match ? match[1] : "");

          if (valueSetter && valueSetter !== prototypeValueSetter) {
            prototypeValueSetter.apply(this, arguments);
          } else {
            valueSetter.apply(this, arguments);
          }
        }
      });
    }
  };

  /**
   * Initializes the search engine selector
   */
  const initialize = async () => {
    gnoh.addStyle(styles, "search-engines-in-address-bar");

    vivaldi.searchEngines.getTemplateUrls().then((res) => {
      createPatternSearchEngines(res.templateUrls);
    });

    vivaldi.searchEngines.onTemplateUrlsChanged.addListener(() => {
      vivaldi.searchEngines.getTemplateUrls().then((res) => {
        createPatternSearchEngines(res.templateUrls);
      });
    });

    gnoh.override(HTMLDivElement.prototype, "appendChild", function (element) {
      reactPropsKey = gnoh.getReactPropsKey(this);
      if (this[reactPropsKey] && this[reactPropsKey].className === "observer" && element[reactPropsKey] && element[reactPropsKey].className.indexOf("OmniDropdown") > -1) {
        createSearchEnginesInAddressBar(element);
      }
    });
  };

  try {
    await initialize();
  } catch (error) {
    console.error("Failed to initialize Search Engine Selector:", error);
  }
})();