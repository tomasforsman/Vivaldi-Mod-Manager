/**
 * Easy Files
 *
 * Enhances file input elements with clipboard and download history integration.
 * Allows selecting files from clipboard or recent downloads instead of file picker.
 *
 * @title Easy Files
 * @description Enhanced file input with clipboard and downloads
 * @version 2.0.0
 * @author Tam710562
 * @license MIT
 * @compatibility Vivaldi 6.0+
 */

(async () => {
  "use strict";

  /**
   * Configuration object for Easy Files behavior
   */
  const config = {
    chunkSize: 1024 * 1024 * 10,      // 10MB chunk size for file transfer
    maxAllowedSize: 1024 * 1024 * 5,  // 5MB max file size
    nameKey: "easy-files",            // Identifier for message passing
  };

  /**
   * Validates configuration values and applies sane defaults where needed
   * @param {Object} cfg - Configuration object
   */
  const validateConfig = (cfg) => {
    const numericKeys = ["chunkSize", "maxAllowedSize"];
    numericKeys.forEach((key) => {
      if (typeof cfg[key] !== "number" || cfg[key] <= 0) {
        const fallback = key === "chunkSize" ? 1024 * 1024 : 5 * 1024 * 1024;
        console.warn(`Invalid ${key}, using default ${fallback}`);
        cfg[key] = fallback;
      }
    });
    // Not an error, but helpful if noticed during debugging
    if (cfg.chunkSize > cfg.maxAllowedSize) {
      console.warn(
        `chunkSize (${cfg.chunkSize}) is larger than maxAllowedSize (${cfg.maxAllowedSize}). ` +
        `This is allowed, but means most files will be a single chunk.`
      );
    }
    if (typeof cfg.nameKey !== "string" || !cfg.nameKey.trim()) {
      cfg.nameKey = "easy-files";
    }
  };

  /**
   * Creates and applies a new stylesheet to the document
   * @param {string} css - CSS rules to add
   */
  const addStyleSheet = (css) => {
    try {
      const sheet = new CSSStyleSheet();
      sheet.replaceSync(css);
      document.adoptedStyleSheets = [...document.adoptedStyleSheets, sheet];
    } catch {
      // Fallback for environments without Constructable Stylesheets
      const style = document.createElement("style");
      style.textContent = css;
      document.head.appendChild(style);
    }
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
      let elapsed = 0;

      const timerId = setInterval(() => {
        const el = startNode.querySelector(selector);
        if (el) {
          clearInterval(timerId);
          resolve(el);
        }
        elapsed += checkInterval;
        if (elapsed >= timeoutMs) {
          clearInterval(timerId);
          reject(new Error(`Element ${selector} not found within ${timeoutMs}ms`));
        }
      }, checkInterval);
    });
  };

  // --------------------------
  // Minimal helper namespace
  // --------------------------
  const gnoh = {
    stream: {
      async compress(input, outputType = "arrayBuffer", format = "gzip") {
        const compressedStream = new Response(input).body
          .pipeThrough(new CompressionStream(format));
        return await new Response(compressedStream)[outputType]();
      },
    },
    file: {
      readableFileSize(size) {
        const i = Math.floor(Math.log(size) / Math.log(1024));
        return `${(size / Math.pow(1024, i)).toFixed(2)} ${["B", "kB", "MB", "GB", "TB"][i]}`;
      },
      getFileExtension(fileName) {
        return /(?:\.([^.]+))?$/.exec(fileName)[1];
      },
      verifyAccept({ fileName, mimeType }, accept) {
        if (!accept) return true;

        const parts = accept.split(",")
          .map(x => x.trim())
          .filter(x => !!x && (x.startsWith(".") || /\w+\/([-+.\w]+|\*)/.test(x)));

        if (!parts.length) return true;

        for (const mt of parts) {
          const ok = mt.startsWith(".")
            ? new RegExp(mt.replace(".", ".+\\.") + "$").test(fileName)
            : new RegExp(mt.replace("*", ".+")).test(mimeType);
          if (ok) return true;
        }
        return false;
      },
    },
    i18n: {
      getMessageName(message, type) {
        message = (type ? type + "\x04" : "") + message;
        return message.replace(/[^a-z0-9]/g, (i) => "_" + i.codePointAt(0) + "_") + "0";
      },
      getMessage(message, type) {
        return chrome.i18n.getMessage(this.getMessageName(message, type)) || message;
      },
    },
    array: {
      chunks(arrOrString, n) {
        const s = Array.isArray(arrOrString) ? arrOrString : String(arrOrString);
        const out = [];
        for (let i = 0; i < s.length; i += n) out.push(s.slice(i, i + n));
        return out;
      },
    },
    element: {
      getStyle(element) {
        return getComputedStyle(element);
      },
    },
    createElement(tagName, attribute, parent, inner, options) {
      if (typeof tagName === "undefined") return;
      options = options || {};
      options.isPrepend = options.isPrepend || false;

      const el = document.createElement(tagName);
      if (attribute && typeof attribute === "object") {
        for (const key in attribute) {
          if (key === "text") el.textContent = attribute[key];
          else if (key === "html") el.innerHTML = attribute[key];
          else if (key === "style" && typeof attribute[key] === "object") {
            for (const css in attribute.style) el.style.setProperty(css, attribute.style[css]);
          } else if (key === "events" && typeof attribute[key] === "object") {
            for (const evt in attribute.events) {
              if (typeof attribute.events[evt] === "function") el.addEventListener(evt, attribute.events[evt]);
            }
          } else if (typeof el[key] !== "undefined") {
            el[key] = attribute[key];
          } else {
            el.setAttribute(key, typeof attribute[key] === "object" ? JSON.stringify(attribute[key]) : attribute[key]);
          }
        }
      }
      if (inner) {
        const arr = Array.isArray(inner) ? inner : [inner];
        for (const child of arr) {
          if (child?.nodeName) el.append(child);
          else el.append(this.createElementFromHTML(child));
        }
      }
      if (typeof parent === "string") parent = document.querySelector(parent);
      if (parent) options.isPrepend ? parent.prepend(el) : parent.append(el);
      return el;
    },
    createElementFromHTML(html) {
      return this.createElement("template", { html: (html || "").trim() }).content;
    },
    string: {
      toHashCode(str) {
        let hash = 0;
        for (let i = 0; i < str.length; i++) {
          hash = ((hash << 5) - hash) + str.charCodeAt(i);
          hash |= 0;
        }
        return hash;
      },
      toColorRgb(str) {
        const hash = this.toHashCode(str);
        return {
          r: (hash >> (0 * 8)) & 0xff,
          g: (hash >> (1 * 8)) & 0xff,
          b: (hash >> (2 * 8)) & 0xff,
        };
      },
    },
    color: {
      rgbToHex(r, g, b) {
        return "#" + ((1 << 24) + (r << 16) + (g << 8) + b).toString(16).slice(1);
      },
      getLuminance(r, g, b) {
        return 0.2126 * r + 0.7152 * g + 0.0722 * b;
      },
      isLight(r, g, b) {
        return this.getLuminance(r, g, b) < 156;
      },
      shadeColor(r, g, b, percent) {
        const t = percent < 0 ? 0 : 255 * percent;
        const p = percent < 0 ? 1 + percent : 1 - percent;
        return {
          r: Math.round(parseInt(r) * p + t),
          g: Math.round(parseInt(g) * p + t),
          b: Math.round(parseInt(b) * p + t),
        };
      },
    },
    object: {
      isObject(item) {
        return (item && typeof item === "object" && !Array.isArray(item));
      },
      merge(target, source) {
        let output = Object.assign({}, target);
        if (this.isObject(target) && this.isObject(source)) {
          for (const key in source) {
            if (this.isObject(source[key])) {
              if (!(key in target)) Object.assign(output, { [key]: source[key] });
              else output[key] = this.merge(target[key], source[key]);
            } else {
              Object.assign(output, { [key]: source[key] });
            }
          }
        }
        return output;
      },
    },
  };

  // --------------------------
  // i18n strings
  // --------------------------
  const langs = {
    showMore: gnoh.i18n.getMessage("Show more"),
    chooseAFile: gnoh.i18n.getMessage("Choose a File..."),
    clipboard: gnoh.i18n.getMessage("Clipboard"),
    downloads: gnoh.i18n.getMessage("Downloads"),
  };

  // --------------------------
  // UI sheet (dialog styling)
  // --------------------------
  const dialogCss = (nameKey) => `
    .${nameKey}.dialog-custom .dialog-content { flex-flow: wrap; gap: 18px; }
    .${nameKey}.dialog-custom .dialog-content .selectbox-wrapper { overflow: hidden; margin: -2px; padding: 2px; }
    .${nameKey}.dialog-custom .dialog-content .selectbox-wrapper .selectbox-container { overflow: auto; margin: -2px; padding: 2px; flex: 0 1 auto; }
    .${nameKey}.dialog-custom .dialog-content .selectbox-wrapper .selectbox-container .selectbox-image { background-color: var(--colorBgLighter); width: 120px; height: 120px; display: flex; justify-content: center; align-items: center; }
    .${nameKey}.dialog-custom .dialog-content .selectbox-wrapper .selectbox-container .selectbox-image:hover { box-shadow: 0 0 0 2px var(--colorHighlightBg); }
    .${nameKey}.dialog-custom .dialog-content .selectbox-wrapper .selectbox-container .selectbox-image.preview img { object-fit: cover; width: 120px; height: 120px; flex: 0 0 auto; }
    .${nameKey}.dialog-custom .dialog-content .selectbox-wrapper .selectbox-container .selectbox-image.icon .file-icon { width: 54px; height: 69px; padding: 15px 0 0; position: relative; font-family: sans-serif; }
    .${nameKey}.dialog-custom .dialog-content .selectbox-wrapper .selectbox-container .selectbox-image.icon .file-icon:before { position: absolute; content: ''; top: 0; left: 0; height: 15px; right: 15px; background-color: var(--colorFileIconBg, #007bff); }
    .${nameKey}.dialog-custom .dialog-content .selectbox-wrapper .selectbox-container .selectbox-image.icon .file-icon:after { position: absolute; content: ''; width: 0; height: 0; border-style: solid; border-width: 15.5px 0 0 15.5px; border-color: transparent transparent transparent var(--colorFileIconBgLighter, #66b0ff); top: 0; right: 0; }
    .${nameKey}.dialog-custom .dialog-content .selectbox-wrapper .selectbox-container .selectbox-image.icon .file-icon .file-icon-content { background-color: var(--colorFileIconBg, #007bff); color: var(--colorFileIconFg, #fff); position: absolute; left: 0; right: 0; top: 15px; bottom: 0; padding: 24.75px 0.3em 0; font-size: 19.5px; font-weight: 500; white-space: nowrap; text-overflow: ellipsis; overflow: hidden; }
    .${nameKey}.dialog-custom .dialog-content .selectbox-wrapper .selectbox-container .selectbox-title { width: 120px; }
    .${nameKey}.dialog-custom .dialog-content .selectbox-wrapper .selectbox-container .selectbox-title .filename-container { display: flex; flex-direction: row; overflow: hidden; width: 120px; }
    .${nameKey}.dialog-custom .dialog-content .selectbox-wrapper .selectbox-container .selectbox-title .filename-container .filename-text { white-space: nowrap; text-overflow: ellipsis; overflow: hidden; }
    .${nameKey}.dialog-custom .dialog-content .selectbox-wrapper .selectbox-container .selectbox-title .filename-container .filename-extension { white-space: nowrap; }
  `;

  // --------------------------
  // Dialog builder (Vivaldi UI)
  // --------------------------
  const dialog = (() => {
    const constant = {
      dialogButtons: {
        submit: { label: gnoh.i18n.getMessage("OK"), type: "submit" },
        cancel: { label: gnoh.i18n.getMessage("Cancel"), cancel: true },
      },
    };

    const make = (title, content, buttons = [], configLocal = {}) => {
      let modalBg, formEl, cancelEvent;
      const id = crypto.randomUUID?.() ?? String(Math.random()).slice(2);
      const inner = document.querySelector("#main > .inner, #main > .webpageview");

      if (typeof configLocal.autoClose === "undefined") configLocal.autoClose = true;

      function onKeyCloseDialog(windowId, key) {
        if (windowId === vivaldiWindowId && key === "Esc") closeDialog(true);
      }
      function onClickCloseDialog(windowId, mousedown, _button, clientX, clientY) {
        if (configLocal.autoClose &&
            windowId === vivaldiWindowId &&
            mousedown &&
            !document.elementFromPoint(clientX, clientY)?.closest(`.dialog-custom[data-dialog-id="${id}"]`)) {
          closeDialog(true);
        }
      }
      function closeDialog(isCancel) {
        if (isCancel === true && cancelEvent) cancelEvent.bind(this)();
        modalBg?.remove();
        try {
          vivaldi.tabsPrivate.onKeyboardShortcut.removeListener(onKeyCloseDialog);
          vivaldi.tabsPrivate.onWebviewClickCheck.removeListener(onClickCloseDialog);
        } catch {}
      }

      try {
        vivaldi.tabsPrivate.onKeyboardShortcut.addListener(onKeyCloseDialog);
        vivaldi.tabsPrivate.onWebviewClickCheck.addListener(onClickCloseDialog);
      } catch {}

      const btnElems = [];
      for (let btn of buttons) {
        btn.type = btn.type || "button";
        const clickEvent = btn.click;
        if (btn.cancel === true && typeof clickEvent === "function") cancelEvent = clickEvent;
        btn.events = {
          click(event) {
            event.preventDefault();
            if (typeof clickEvent === "function") clickEvent.bind(this)();
            if (btn.closeDialog !== false) closeDialog();
          },
        };
        delete btn.click;
        if (btn.label) {
          btn.value = btn.label;
          delete btn.label;
        }
        btn.element = gnoh.createElement("input", btn);
        btnElems.push(btn.element);
      }

      const focusTrap = gnoh.createElement("span", { class: "focus_modal", tabindex: "0" });
      const container = gnoh.createElement("div", {
        style: { width: configLocal.width ? configLocal.width + "px" : "", margin: "0 auto" }
      });

      formEl = gnoh.createElement("form", {
        "data-dialog-id": id,
        class: `dialog-custom ${config.nameKey}`,
      }, container);

      if (configLocal.class) formEl.classList.add(configLocal.class);
      const header = gnoh.createElement("header", { class: "dialog-header" }, formEl, `<h1>${title || ""}</h1>`);
      const contentEl = gnoh.createElement("div", { class: "dialog-content", style: { maxHeight: "65vh" } }, formEl, content);
      if (buttons?.length) gnoh.createElement("footer", { class: "dialog-footer" }, formEl, btnElems);

      modalBg = gnoh.createElement("div", { id: "modal-bg", class: "slide" }, inner, [focusTrap.cloneNode(true), container, focusTrap.cloneNode(true)]);
      return { dialog: formEl, dialogHeader: header, dialogContent: contentEl, modalBg, buttons: btnElems, close: closeDialog, constant };
    };

    return { make };
  })();

  // --------------------------
  // Clipboard utilities
  // --------------------------

  /**
   * Simulates paste event to read clipboard data
   * @returns {Promise<{items: Array, isRealFile: boolean}>}
   */
  const simulatePaste = async () =>
    new Promise((resolve) => {
      document.addEventListener(
        "paste",
        (e) => {
          e.preventDefault();
          const items = [];
          let isRealFile = true;

          for (const item of e.clipboardData.items) {
            const file = item.getAsFile();
            const entry = item.webkitGetAsEntry?.();
            if (file) {
              if (!entry || entry.isFile) {
                items.push({ file, isFile: true, isRealFile: !!entry });
              } else if (entry.isDirectory) {
                items.push({ file, isDirectory: true });
              }
            }
          }

          resolve({ items, isRealFile });
        },
        { once: true }
      );

      document.execCommand("paste");
    });

  /**
   * Converts PNG blob to JPEG
   * @param {Blob} blob - PNG blob to convert
   * @returns {Promise<Blob>} JPEG blob
   */
  const convertPngToJpeg = async (blob) => {
    const image = gnoh.createElement("img", { src: URL.createObjectURL(blob) });
    await image.decode();

    const canvas = gnoh.createElement("canvas", { width: image.width, height: image.height });
    const ctx = canvas.getContext("2d");
    ctx.drawImage(image, 0, 0);

    return new Promise((resolve) => {
      canvas.toBlob((b) => {
        URL.revokeObjectURL(image.src);
        if (b) resolve(b);
      }, "image/jpeg");
    });
  };

  /**
   * Reads files from clipboard
   * @param {string} accept - File accept attribute filter
   * @returns {Promise<Array>} Array of clipboard files
   */
  const readClipboard = async (accept) => {
    const clipboardFiles = [];
    try {
      const supportedTypes = [
        { extension: "png",  mimeType: "image/png"  },
        { extension: "jpeg", mimeType: "image/jpeg" },
        { extension: "jpg",  mimeType: "image/jpeg" },
      ];
      const supportedType = supportedTypes.find(s =>
        gnoh.file.verifyAccept({ fileName: "image." + s.extension, mimeType: s.mimeType }, accept)
      );

      const pasteData = await simulatePaste();

      for (const item of pasteData.items) {
        const file = item.file;
        let checkType = false;

        if (item.isFile) {
          if (item.isRealFile) {
            checkType = gnoh.file.verifyAccept({ fileName: file.name, mimeType: file.type }, accept);
          } else {
            checkType = supportedType && file.type === "image/png";
          }
        }

        if (checkType && (!config.maxAllowedSize || file.size <= config.maxAllowedSize)) {
          let blob = new Blob([file], { type: file.type });

          if (!item.isRealFile && supportedType?.mimeType === "image/jpeg") {
            blob = await convertPngToJpeg(blob);
          }

          const arrayBuffer = await blob.arrayBuffer();
          const compressedArrayBuffer = await gnoh.stream.compress(arrayBuffer);
          const compressedBase64String = btoa(
            new Uint8Array(compressedArrayBuffer).reduce((data, byte) => data + String.fromCharCode(byte), "")
          );
          const fileData = gnoh.array.chunks(compressedBase64String, config.chunkSize);
          const clipboardFile = {
            fileData,
            fileDataLength: fileData.length,
            mimeType: blob.type,
            size: blob.size,
            category: "clipboard",
          };

          if (item.isRealFile) {
            clipboardFile.fileName = file.name;
          } else {
            clipboardFile.extension = supportedType?.extension;
          }

          switch (clipboardFile.mimeType) {
            case "image/jpeg":
            case "image/png":
            case "image/svg+xml":
            case "image/webp":
            case "image/gif":
            case "image/bmp":
              try {
                clipboardFile.previewUrl = await vivaldi.utilities.storeImage({ data: arrayBuffer, mimeType: blob.type });
              } catch (error) {
                console.warn("Failed to create preview for clipboard image", error);
              }
              break;
          }

          clipboardFiles.push(clipboardFile);
        }
      }
    } catch (error) {
      console.error(error);
    }
    return clipboardFiles;
  };

  /**
   * Gets list of downloaded files matching accept filter
   * @param {string} accept - File accept attribute filter
   * @returns {Promise<Array>} Array of downloaded files
   */
  const getDownloadedFiles = async (accept) => {
    const downloadedFiles = await chrome.downloads.search({ exists: true, state: "complete", orderBy: ["-startTime"] });
    const result = {};
    for (let df of downloadedFiles) {
      if (
        df.mime &&
        df.mime !== "application/x-msdownload" &&
        gnoh.file.verifyAccept({ fileName: df.filename, mimeType: df.mime }, accept)
      ) {
        df = (await chrome.downloads.search({ id: df.id }))[0];
        if (
          df &&
          df.exists === true &&
          df.state === "complete" &&
          (!config.maxAllowedSize || df.fileSize <= config.maxAllowedSize) &&
          !result[df.filename]
        ) {
          const file = {
            mimeType: df.mime,
            path: df.filename,
            fileName: df.filename.replace(/^.*[\\/]/, ""),
            size: df.fileSize,
            category: "downloaded-file",
          };

          switch (file.mimeType) {
            case "image/jpeg":
            case "image/png":
            case "image/svg+xml":
            case "image/webp":
            case "image/gif":
            case "image/bmp":
              try {
                file.previewUrl = await vivaldi.utilities.storeImage({ url: file.path });
              } catch (error) {
                console.warn("Failed to create preview for file:", file.path, error);
              }
              break;
          }
          result[df.filename] = file;
        }
      }
    }
    return Object.values(result);
  };

  // --------------------------
  // Content script injector
  // --------------------------

  /**
   * Function executed inside target tab frames to intercept <input type="file"> clicks
   * @param {string} nameKey - Identifier for the script messaging
   */
  const injectContent = (nameKey) => {
    if (window.easyFiles) return;
    window.easyFiles = true;

    const fileData = [];
    let fileInput = null;
    let elementClickedRect = null;
    const pointer = { x: 0, y: 0 }; // content-context pointer (separate from UI pointer)

    const decompressArrayBuffer = async (input) => {
      const decompressedStream = new Response(input).body.pipeThrough(new DecompressionStream("gzip"));
      return await new Response(decompressedStream).arrayBuffer();
    };

    const getRect = (element) => {
      const rect = element.getBoundingClientRect().toJSON();
      while ((element = element.offsetParent)) {
        if (getComputedStyle(element).overflow !== "visible") {
          const parentRect = element.getBoundingClientRect();
          rect.left = Math.max(rect.left, parentRect.left);
          rect.top = Math.max(rect.top, parentRect.top);
          rect.right = Math.min(rect.right, parentRect.right);
          rect.bottom = Math.min(rect.bottom, parentRect.bottom);
          rect.width = rect.right - rect.left;
          rect.height = rect.bottom - rect.top;
          rect.x = rect.left;
          rect.y = rect.top;
        }
      }
      return rect;
    };

    const handleMouseDown = (event) => {
      elementClickedRect = getRect(event.target);
      pointer.x = event.clientX;
      pointer.y = event.clientY;
    };

    const handleClick = (event) => {
      if (event.target.matches("input[type=file]:not([webkitdirectory])")) {
        event.preventDefault();
        event.stopPropagation();

        fileInput = event.target;

        if (
          event.isTrusted &&
          fileInput.checkVisibility?.({
            opacityProperty: true,
            visibilityProperty: true,
            contentVisibilityAuto: true,
          })
        ) {
          elementClickedRect = getRect(fileInput);
        }

        const attributes = {};
        for (const attr of fileInput.attributes) attributes[attr.name] = attr.value;

        fileData.length = 0;

        // Convert rect to be relative to pointer (will be reversed by UI script)
        elementClickedRect.left -= pointer.x;
        elementClickedRect.top  -= pointer.y;
        elementClickedRect.right -= pointer.x;
        elementClickedRect.bottom -= pointer.y;
        elementClickedRect.x    -= pointer.x;
        elementClickedRect.y    -= pointer.y;

        chrome.runtime.sendMessage({
          type: nameKey,
          action: "click",
          attributes,
          elementClickedRect,
        });
      }
    };

    const changeFile = (dataTransfer) => {
      fileInput.files = dataTransfer.files;
      fileInput.dispatchEvent(new Event("input",  { bubbles: true }));
      fileInput.dispatchEvent(new Event("change", { bubbles: true }));
    };

    document.addEventListener("mousedown", handleMouseDown);
    document.addEventListener("click", handleClick);

    chrome.runtime.onMessage.addListener(async (info) => {
      if (info.type !== nameKey) return;

      switch (info.action) {
        case "file": {
          fileData[info.file.fileDataIndex] = info.file.fileData;
          if (Object.entries(fileData).length === info.file.fileDataLength) {
            const dataTransfer = new DataTransfer();
            const base64String = fileData.join("");
            const unit8Array = Uint8Array.from(atob(base64String), c => c.charCodeAt(0));
            const decompressedArrayBuffer = await decompressArrayBuffer(unit8Array);
            dataTransfer.items.add(new File([decompressedArrayBuffer], info.file.fileName, { type: info.file.mimeType }));
            changeFile(dataTransfer);
          }
          break;
        }
        case "picker": {
          fileInput?.showPicker?.();
          break;
        }
      }
    });
  };

  // --------------------------
  // UI-side helpers and flows
  // --------------------------

  const uiPointer = { x: 0, y: 0 }; // UI-context pointer

  /**
   * Creates a file icon element with color coding
   * @param {string} extension - File extension
   * @returns {HTMLElement} File icon element
   */
  const createFileIcon = (extension) => {
    const rgb = extension ? gnoh.string.toColorRgb(extension) : { r: 255, g: 255, b: 255 };
    const isLightBg = gnoh.color.isLight(rgb.r, rgb.g, rgb.b);
    const lighter = gnoh.color.shadeColor(rgb.r, rgb.g, rgb.b, isLightBg ? 0.4 : -0.4);

    const icon = gnoh.createElement("div", {
      class: "file-icon",
      style: {
        "--colorFileIconBg": gnoh.color.rgbToHex(rgb.r, rgb.g, rgb.b),
        "--colorFileIconBgLighter": gnoh.color.rgbToHex(lighter.r, lighter.g, lighter.b),
        "--colorFileIconFg": isLightBg ? "#f6f6f6" : "#111111",
      }
    });
    gnoh.createElement("div", { class: "file-icon-content", text: extension }, icon);
    return icon;
  };

  /**
   * Builds a selectable file box in the dialog
   * @param {Object} sender - Message sender information
   * @param {Object} file - File object
   * @param {Object} dlg - Dialog object
   * @returns {Promise<HTMLElement>} Selectbox element
   */
  const createSelectbox = async (sender, file, dlg) => {
    const selectbox = gnoh.createElement("button", {
      title: `${file.fileName ? file.fileName + "\n" : ""}Size: ${gnoh.file.readableFileSize(file.size)}`,
      class: "selectbox",
      events: {
        async click(event) {
          event.preventDefault();
          dlg.close();

          switch (file.category) {
            case "downloaded-file":
              if (!file.fileData) {
                const arrayBuffer = await vivaldi.mailPrivate.readFileToBuffer(file.path);
                const compressedArrayBuffer = await gnoh.stream.compress(arrayBuffer);
                const compressedBase64String = btoa(
                  new Uint8Array(compressedArrayBuffer).reduce((data, byte) => data + String.fromCharCode(byte), "")
                );
                file.fileData = gnoh.array.chunks(compressedBase64String, config.chunkSize);
                file.fileDataLength = file.fileData.length;
              }
              break;
            case "clipboard":
              if (!file.fileName) {
                const d = new Date();
                const stamp = `${d.getFullYear()}-${String(d.getMonth() + 1).padStart(2,"0")}-${String(d.getDate()).padStart(2,"0")}_` +
                              `${String(d.getHours()).padStart(2,"0")}${String(d.getMinutes()).padStart(2,"0")}${String(d.getSeconds()).padStart(2,"0")}${String(d.getMilliseconds()).padStart(3,"0")}`;
                file.fileName = `image_${stamp}.${file.extension}`;
              }
              break;
          }

          chooseFile(sender, file);
        },
      },
    });

    const selectboxImage = gnoh.createElement("div", { class: "selectbox-image" }, selectbox);
    if (file.previewUrl) selectboxImage.classList.add("preview"); else selectboxImage.classList.add("icon");

    if (file.previewUrl) {
      gnoh.createElement("img", { src: file.previewUrl }, selectboxImage);
    } else {
      const extension = file.extension || gnoh.file.getFileExtension(file.fileName);
      selectboxImage.append(createFileIcon(extension));
    }

    const title = gnoh.createElement("div", { class: "selectbox-title" }, selectbox);
    const nameRow = gnoh.createElement("div", { class: "filename-container" }, title);

    if (file.fileName) {
      const extension = file.extension || gnoh.file.getFileExtension(file.fileName);
      const name = extension ? file.fileName.slice(0, -extension.length - 1) : file.fileName;
      gnoh.createElement("div", { class: "filename-text", text: name }, nameRow);
      if (extension) gnoh.createElement("div", { class: "filename-extension", text: "." + extension }, nameRow);
    }

    return selectbox;
  };

  /**
   * Sends selected file chunks to content script
   * @param {Object} sender - Message sender information
   * @param {Object} file - File object to send
   */
  const chooseFile = (sender, file) => {
    if (!file.fileData.length) file.fileData.push([]);

    for (const [index, chunk] of file.fileData.entries()) {
      chrome.tabs.sendMessage(sender.tab.id, {
        type: config.nameKey,
        action: "file",
        tabId: sender.tab.id,
        frameId: sender.frameId,
        file: {
          fileData: chunk,
          fileDataIndex: index,
          fileDataLength: file.fileData.length,
          fileName: file.fileName,
          mimeType: file.mimeType,
        },
      }, { frameId: sender.frameId });
    }
  };

  /**
   * Shows native file picker in the content frame
   * @param {Object} sender - Message sender information
   */
  const showAllFiles = (sender) => {
    chrome.tabs.sendMessage(sender.tab.id, {
      type: config.nameKey,
      action: "picker",
      tabId: sender.tab.id,
      frameId: sender.frameId,
    }, { frameId: sender.frameId });
  };

  /**
   * Shows dialog for choosing files from clipboard or downloads
   * @param {Object} params - { info, sender, clipboardFiles, downloadedFiles }
   */
  const showDialogChooseFile = async ({ info, sender, clipboardFiles, downloadedFiles }) => {
    let disconnectResizeObserver;

    const btnShowAll = gnoh.object.merge({ ...dialog.make("", "", [], {}).constant?.dialogButtons?.submit }, {
      label: langs.showMore,
      click() {
        showAllFiles(sender);
        disconnectResizeObserver && disconnectResizeObserver();
      },
    });

    const btnCancel = gnoh.object.merge({ ...dialog.make("", "", [], {}).constant?.dialogButtons?.cancel }, {
      click() {
        disconnectResizeObserver && disconnectResizeObserver();
      },
    });

    const dlg = dialog.make(langs.chooseAFile, null, [btnShowAll, btnCancel], { class: config.nameKey });
    dlg.dialog.style.maxWidth = "570px";

    dlg.modalBg.style.height = "fit-content";
    dlg.modalBg.style.position = "fixed";
    dlg.modalBg.style.margin = "unset";
    dlg.modalBg.style.minWidth = "unset";
    dlg.modalBg.style.left = "unset";
    dlg.modalBg.style.top = "unset";
    dlg.modalBg.style.right = "unset";
    dlg.modalBg.style.bottom = "unset";

    const setPosition = (entries) => {
      for (const entry of entries) {
        const rect = entry.contentRect;

        if (info.elementClickedRect.left < 0) {
          dlg.modalBg.style.left = "0px";
        } else if (info.elementClickedRect.right > window.innerWidth) {
          dlg.modalBg.style.right = "0px";
        } else if (info.elementClickedRect.left + rect.width > window.innerWidth) {
          dlg.modalBg.style.left = Math.max((info.elementClickedRect.right - rect.width), 0) + "px";
        } else {
          dlg.modalBg.style.left = info.elementClickedRect.left + "px";
        }

        if (info.elementClickedRect.bottom < 0) {
          dlg.modalBg.style.top = "0px";
        } else if (info.elementClickedRect.bottom + rect.height > window.innerHeight) {
          dlg.modalBg.style.top = Math.max((info.elementClickedRect.top - rect.height), 0) + "px";
        } else {
          dlg.modalBg.style.top = info.elementClickedRect.bottom + "px";
        }
      }
    };

    const resizeObserver = new ResizeObserver(setPosition);
    resizeObserver.observe(dlg.dialog);
    disconnectResizeObserver = () => resizeObserver.unobserve(dlg.dialog);

    if (clipboardFiles.length) {
      const wrap = gnoh.createElement("div", { class: "selectbox-wrapper" });
      gnoh.createElement("h3", { text: langs.clipboard }, wrap);
      const cont = gnoh.createElement("div", { class: "selectbox-container" }, wrap);
      for (const cf of clipboardFiles) cont.append(await createSelectbox(sender, cf, dlg));
      dlg.dialogContent.append(wrap);
    }

    if (downloadedFiles.length) {
      const wrap = gnoh.createElement("div", { class: "selectbox-wrapper" });
      gnoh.createElement("h3", { text: langs.downloads }, wrap);
      const cont = gnoh.createElement("div", { class: "selectbox-container" }, wrap);
      for (const df of downloadedFiles) cont.append(await createSelectbox(sender, df, dlg));
      dlg.dialogContent.append(wrap);
    }
  };

  // --------------------------
  // Initialize (UI context)
  // --------------------------
  const initialize = async () => {
    validateConfig(config);
    addStyleSheet(dialogCss(config.nameKey));

    // Track pointer in UI context so we can anchor dialog near input
    try {
      vivaldi.tabsPrivate.onWebviewClickCheck.addListener((windowId, mousedown, button, clientX, clientY) => {
        if (windowId === vivaldiWindowId && mousedown && button === 0) {
          uiPointer.x = clientX;
          uiPointer.y = clientY;
        }
      });
    } catch {}

    // Listen for clicks coming from content script
    chrome.runtime.onMessage.addListener(async (info, sender) => {
      if (sender?.tab?.windowId !== vivaldiWindowId || info.type !== config.nameKey) return;

      switch (info.action) {
        case "click": {
          const [clipboardFiles, downloadedFiles] = await Promise.all([
            readClipboard(info.attributes?.accept),
            getDownloadedFiles(info.attributes?.accept),
          ]);

          if (clipboardFiles.length || downloadedFiles.length) {
            const webview = window[sender.tab.id] || document.elementFromPoint(uiPointer.x, uiPointer.y);
            const zoom = parseFloat(gnoh.element.getStyle(webview).getPropertyValue("--uiZoomLevel"));
            const webviewZoom = await new Promise((resolve) => webview.getZoom((z) => resolve(z)));
            const ratio = webviewZoom / zoom;

            // Convert rect back to absolute viewport coords using UI pointer and zoom ratio
            info.elementClickedRect.left   = info.elementClickedRect.left   * ratio + uiPointer.x;
            info.elementClickedRect.top    = info.elementClickedRect.top    * ratio + uiPointer.y;
            info.elementClickedRect.right  = info.elementClickedRect.right  * ratio + uiPointer.x;
            info.elementClickedRect.bottom = info.elementClickedRect.bottom * ratio + uiPointer.y;
            info.elementClickedRect.width  = info.elementClickedRect.width  * ratio;
            info.elementClickedRect.height = info.elementClickedRect.height * ratio;
            info.elementClickedRect.x      = info.elementClickedRect.x      * ratio + uiPointer.x;
            info.elementClickedRect.y      = info.elementClickedRect.y      * ratio + uiPointer.y;

            showDialogChooseFile({ info, sender, clipboardFiles, downloadedFiles });
          } else {
            showAllFiles(sender);
          }
          break;
        }
      }
    });

    // Inject content handler into all tabs/frames of this Vivaldi window
    const doInjectAll = () => {
      chrome.tabs.query({ windowId: window.vivaldiWindowId, windowType: "normal" }, (tabs) => {
        tabs.forEach((tab) => {
          chrome.scripting.executeScript({
            target: { tabId: tab.id, allFrames: true },
            func: injectContent,
            args: [config.nameKey],
          });
        });
      });
    };

    // On navigation, re-inject for the committed frame
    chrome.webNavigation.onCommitted.addListener((details) => {
      if (details.tabId !== -1) {
        chrome.scripting.executeScript({
          target: { tabId: details.tabId, frameIds: [details.frameId] },
          func: injectContent,
          args: [config.nameKey],
        });
      }
    });

    // Wait for Vivaldi browser shell, then inject
    await waitForElement("#browser");
    doInjectAll();
  };

  // Boot
  try {
    await initialize();
  } catch (error) {
    console.error("Failed to initialize Easy Files:", error);
  }
})();
