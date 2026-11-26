/*
 * RISE – Runtime configuration.
 * The API base URL can be changed from Settings or by an installer without editing source files.
 */
(function () {
    const DEFAULT_API_BASE = "http://localhost:5000";
    const STORAGE_KEY = "rise_api_base";
    let configured = null;
    try { configured = localStorage.getItem(STORAGE_KEY); } catch {}
    window.RISE_API_BASE = (configured || DEFAULT_API_BASE).replace(/\/$/, "");
    window.RISE_setApiBase = function (url) {
        const clean = String(url || "").trim().replace(/\/$/, "");
        if (!/^https?:\/\//i.test(clean)) return false;
        localStorage.setItem(STORAGE_KEY, clean);
        window.RISE_API_BASE = clean;
        return true;
    };
})();
