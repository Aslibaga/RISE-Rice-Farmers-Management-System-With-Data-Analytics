/*
 * RISE formal print engine.
 *
 * Produces stand-alone, print-only documents. The on-screen interface is never
 * modified; a document is mounted into a hidden host only while printing and
 * removed immediately after the print dialog closes.
 */
(function () {
    "use strict";

    const contexts = new Map();

    function escapeHtml(value) {
        return String(value === null || value === undefined ? "" : value)
            .replace(/[&<>"']/g, character => ({
                "&": "&amp;", "<": "&lt;", ">": "&gt;", '"': "&quot;", "'": "&#39;"
            })[character]);
    }

    function text(value, fallback) {
        const result = value === null || value === undefined || value === "" ? fallback : value;
        return escapeHtml(result === null || result === undefined ? "" : result);
    }

    function generatedAt() {
        return new Date().toLocaleString("en-PH", {
            year: "numeric", month: "long", day: "numeric", hour: "2-digit", minute: "2-digit"
        });
    }

    function currentUser() {
        try {
            return (window.RISEAuth && window.RISEAuth.getUser()) || null;
        } catch (error) {
            return null;
        }
    }

    function header(documentData) {
        const user = currentUser();
        const preparedBy = documentData.preparedBy !== false && user && user.displayName ? user.displayName : "";
        const meta = (documentData.meta || []).filter(row => row && row.value !== undefined && row.value !== null && row.value !== "");
        const reference = documentData.reference
            ? `<div class="print-doc-reference">Reference: ${escapeHtml(documentData.reference)}</div>`
            : "";
        return `<header class="print-doc-header">
            <div class="print-doc-identity">
                <div class="print-doc-brand">RISE &middot; Rice Information, Supply and Evaluation</div>
                <div class="print-doc-system">Rice Farmer Assistance and Analytics System</div>
                ${preparedBy ? `<div class="print-doc-officer">Prepared by: ${escapeHtml(preparedBy)}</div>` : ""}
            </div>
            <div class="print-doc-heading">
                <h1>${escapeHtml(documentData.title || "Report")}</h1>
                ${documentData.subtitle ? `<p class="print-doc-subtitle">${escapeHtml(documentData.subtitle)}</p>` : ""}
                ${reference}
                <div class="print-doc-date">Date generated: ${escapeHtml(generatedAt())}</div>
            </div>
            ${documentData.featured ? `<div class="print-doc-featured"><span>${escapeHtml(documentData.featuredLabel || "Recipient")}</span><strong>${escapeHtml(documentData.featured)}</strong></div>` : ""}
            ${meta.length ? `<table class="print-doc-meta"><tbody>${meta.map(row =>
                `<tr><th>${escapeHtml(row.label)}</th><td>${text(row.value, "—")}</td></tr>`
            ).join("")}</tbody></table>` : ""}
        </header>`;
    }

    function section(title, body) {
        if (!body) return "";
        return `<section class="print-doc-section">${title ? `<h2>${escapeHtml(title)}</h2>` : ""}${body}</section>`;
    }

    function fields(pairs, columns) {
        const items = (pairs || []).filter(pair => pair && pair[0]);
        if (!items.length) return "";
        return `<dl class="print-doc-fields${columns === 2 ? " print-doc-fields-two" : ""}">${items.map(pair =>
            `<div class="print-doc-field"><dt>${escapeHtml(pair[0])}</dt><dd>${text(pair[1], "—")}</dd></div>`
        ).join("")}</dl>`;
    }

    function table(columns, rows, options) {
        const settings = options || {};
        const body = Array.isArray(rows) ? rows : [];
        const head = `<thead><tr>${columns.map(column =>
            `<th${column.align ? ` class="print-align-${column.align}"` : ""}${column.width ? ` style="width:${escapeHtml(column.width)}"` : ""}>${escapeHtml(column.label)}</th>`
        ).join("")}</tr></thead>`;
        const bodyRows = body.length
            ? body.map(row => `<tr>${columns.map(column => {
                const value = column.raw ? row[column.key] : text(row[column.key], "—");
                return `<td${column.align ? ` class="print-align-${column.align}"` : ""}>${value}</td>`;
            }).join("")}</tr>`).join("")
            : `<tr><td class="print-empty" colspan="${columns.length}">${escapeHtml(settings.emptyMessage || "No records for the current selection.")}</td></tr>`;
        const foot = settings.totals && settings.totals.length
            ? `<tfoot><tr>${settings.totals.map(total =>
                `<td${total.align ? ` class="print-align-${total.align}"` : ""}${total.colspan ? ` colspan="${total.colspan}"` : ""}>${total.raw ? total.value : escapeHtml(total.value)}</td>`
            ).join("")}</tr></tfoot>`
            : "";
        return `<table class="print-doc-table">${head}<tbody>${bodyRows}</tbody>${foot}</table>`;
    }

    function paragraphs(lines) {
        const items = (lines || []).filter(Boolean);
        if (!items.length) return "";
        return items.map(line => `<p class="print-doc-note">${escapeHtml(line)}</p>`).join("");
    }

    function signatures(roles) {
        const items = (roles || []).filter(Boolean);
        if (!items.length) return "";
        return `<div class="print-doc-signatures">${items.map(role =>
            `<div class="print-signature"><span>${escapeHtml(role)}</span><div class="print-signature-line"></div><small>${escapeHtml(role)}</small></div>`
        ).join("")}</div>`;
    }

    function compose(documentData) {
        const parts = [header(documentData)];
        (documentData.sections || []).forEach(item => {
            parts.push(section(item.title, item.html || (item.fields ? fields(item.fields, item.columns) : "")));
        });
        (documentData.blocks || []).forEach(block => {
            if (block.columns && block.rows) parts.push(section(block.title, table(block.columns, block.rows, block)));
            else if (block.table) parts.push(section(block.title, table(block.columns, block.rows, block)));
            else if (block.fields) parts.push(section(block.title, fields(block.fields, block.columns)));
            else if (block.html) parts.push(section(block.title, block.html));
            else if (block.notes) parts.push(section(block.title, paragraphs(block.notes)));
        });
        if (documentData.notes) parts.push(paragraphs(documentData.notes));
        if (documentData.signatures) parts.push(signatures(documentData.signatures));
        return parts.join("");
    }

    function mount(documentData) {
        let host = document.getElementById("risePrintHost");
        if (!host) {
            host = document.createElement("div");
            host.id = "risePrintHost";
            document.body.appendChild(host);
        }
        host.className = documentData.landscape ? "rise-print-landscape" : "";
        host.innerHTML = `<article class="rise-print-sheet${documentData.landscape ? " landscape" : ""}">${compose(documentData)}</article>`;
        document.body.classList.add("rise-print-active");
        return host;
    }

    function unmount() {
        const host = document.getElementById("risePrintHost");
        if (host) host.innerHTML = "";
        document.body.classList.remove("rise-print-active");
    }

    function setContext(key, provider) {
        contexts.set(key, provider);
    }

    function print(key) {
        const provider = contexts.get(key);
        if (typeof provider !== "function") return false;
        const documentData = provider();
        if (!documentData) return false;
        mount(documentData);
        const cleanup = () => {
            window.removeEventListener("afterprint", cleanup);
            unmount();
        };
        window.addEventListener("afterprint", cleanup);
        window.print();
        // Some browsers do not fire afterprint (for example when preview is cancelled).
        window.setTimeout(() => {
            if (document.body.classList.contains("rise-print-active")) cleanup();
        }, 1500);
        return true;
    }

    window.RISEPrint = {
        setContext,
        print,
        compose,
        mount,
        unmount,
        escapeHtml,
        text,
        fields,
        table,
        section,
        paragraphs,
        signatures
    };
})();
