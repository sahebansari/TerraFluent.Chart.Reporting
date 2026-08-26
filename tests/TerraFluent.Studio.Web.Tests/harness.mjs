/* ==========================================================================
   Minimal browser stubs so the SPA's ES modules can be imported under Node.

   The Studio ships as dependency-free browser modules, so rather than pull in a
   full DOM implementation we install only what module-level code touches:
   `document` (core.js caches `#content` at import time), `localStorage`
   (settings are read at import time) and `fetch` (the API client). Anything a
   test needs beyond that, it installs itself.
   ========================================================================== */
import { pathToFileURL } from "node:url";
import { dirname, resolve } from "node:path";
import { fileURLToPath } from "node:url";

const here = dirname(fileURLToPath(import.meta.url));

/** Absolute path to the SPA's js directory. */
export const JS_DIR = resolve(here, "../../src/TerraFluent.Chart.Reporting.Web/wwwroot/js");

/** An element stub recording enough state for the render helpers to be exercised. */
function makeElement(tag = "div") {
  const el = {
    tagName: tag.toUpperCase(),
    children: [],
    dataset: {},
    style: {},
    hidden: false,
    innerHTML: "",
    textContent: "",
    className: "",
    value: "",
    disabled: false,
    classList: {
      _set: new Set(),
      add(...c) { c.forEach(x => this._set.add(x)); },
      remove(...c) { c.forEach(x => this._set.delete(x)); },
      toggle(c, force) { force ? this._set.add(c) : this._set.delete(c); },
      contains(c) { return this._set.has(c); },
    },
    querySelector: () => null,
    querySelectorAll: () => [],
    addEventListener() {},
    removeEventListener() {},
    appendChild(child) { this.children.push(child); return child; },
    setAttribute(name, value) { this[name] = value; },
    getAttribute(name) { return this[name] ?? null; },
    closest: () => null,
    click() {},
    focus() {},
    remove() {},
  };
  return el;
}

/** In-memory Storage implementation matching the Web Storage API surface we use. */
function makeStorage(seed = {}) {
  const map = new Map(Object.entries(seed));
  return {
    getItem: (k) => (map.has(k) ? map.get(k) : null),
    setItem: (k, v) => void map.set(k, String(v)),
    removeItem: (k) => void map.delete(k),
    clear: () => map.clear(),
    get length() { return map.size; },
    _map: map,
  };
}

/**
 * Installs the browser globals the SPA modules expect. Returns handles the test
 * can inspect or replace (notably `fetchCalls` and `storage`).
 */
export function installBrowserGlobals({ seedStorage = {} } = {}) {
  const elements = new Map();
  const getElement = (sel) => {
    if (!elements.has(sel)) elements.set(sel, makeElement());
    return elements.get(sel);
  };

  globalThis.document = {
    querySelector: (sel) => getElement(sel),
    querySelectorAll: () => [],
    createElement: (tag) => makeElement(tag),
    addEventListener() {},
    removeEventListener() {},
    body: makeElement("body"),
    documentElement: makeElement("html"),
  };

  const storage = makeStorage(seedStorage);
  globalThis.localStorage = storage;
  globalThis.sessionStorage = makeStorage();

  globalThis.window = {
    matchMedia: () => ({ matches: false, addEventListener() {}, removeEventListener() {} }),
    addEventListener() {},
    location: { href: "http://localhost/", origin: "http://localhost" },
  };

  const fetchCalls = [];
  globalThis.fetch = async (url, opts = {}) => {
    fetchCalls.push({ url, opts });
    return jsonResponse({});
  };

  globalThis.setTimeout ??= (fn) => fn();
  globalThis.clearTimeout ??= () => {};

  return { elements, storage, fetchCalls, getElement };
}

/** Builds a Response-shaped object for the API client to consume. */
export function jsonResponse(body, { ok = true, status = 200 } = {}) {
  return {
    ok,
    status,
    headers: { get: (h) => (h.toLowerCase() === "content-type" ? "application/json" : null) },
    json: async () => body,
    text: async () => JSON.stringify(body),
  };
}

/** Imports a SPA module by file name (e.g. "core.js") with a cache-busting suffix. */
export async function importModule(name, cacheKey = "") {
  const url = pathToFileURL(resolve(JS_DIR, name)).href;
  return import(cacheKey ? `${url}?k=${cacheKey}` : url);
}
