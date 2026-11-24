const API_BASE_URL = "http://localhost:5000/api";
const FARMERS_ENDPOINT = "/farmers";
const mainContent = document.getElementById("mainContent");
const farmerModal = new bootstrap.Modal(document.getElementById("farmerModal"));
const profileModal = new bootstrap.Modal(document.getElementById("profileModal"));
let farmers = [];
let supplies = [];
let currentView = "dashboard";
let moduleModal;
let activeDistributionFarmerId = null;
let distributionHistoryFarmerId = null;

const formatFarmerId = id => (id ? `FRM-${String(id).padStart(3, "0")}` : "—");

const api = {
    get: endpoint => apiRequest(endpoint),
    post: (endpoint, body) => apiRequest(endpoint, { method: "POST", body: JSON.stringify(body) }),
    put: (endpoint, body) => apiRequest(endpoint, { method: "PUT", body: JSON.stringify(body) }),
    delete: endpoint => apiRequest(endpoint, { method: "DELETE" })
};

async function apiRequest(endpoint, options = {}) {
    const url = `${API_BASE_URL}${endpoint}`;
    const method = options.method || "GET";
    const headers = new Headers(options.headers);
    if (!headers.has("Accept")) headers.set("Accept", "application/json");
    if (options.body != null && !headers.has("Content-Type")) headers.set("Content-Type", "application/json");

    // Add auth token if available
    const token = localStorage.getItem("rise_auth_token");
    if (token) headers.set("Authorization", `Bearer ${token}`);

    let response;
    try {
        response = await fetch(url, { ...options, headers });
    } catch (error) {
        throw new Error(`Could not connect to the RISE API at ${API_BASE_URL}. Make sure the API is running.`);
    }

    if (response.status === 401) {
        localStorage.removeItem("rise_auth_token");
        localStorage.removeItem("rise_auth_user");
        if (window.location.pathname.includes("index.html") || window.location.pathname.endsWith("/") || window.location.pathname === "") {
            window.location.href = "login.html";
        }
        throw new Error("Session expired. Please log in again.");
    }

    if (!response.ok) {
        const text = await response.text();
        let problem = null;
        try { problem = text ? JSON.parse(text) : null; } catch {}
        const message = problem?.errors
            ? Object.values(problem.errors).flat().join(" ")
            : problem?.detail || problem?.title || text || `API request failed (${response.status}).`;
        throw new Error(message);
    }

    if (response.status === 204 || response.status === 205) return null;
    const text = await response.text();
    return text.trim() ? JSON.parse(text) : null;
}

function escapeHtml(value) {
    const span = document.createElement("span");
    span.textContent = value ?? "";
    return span.innerHTML;
}

function printDocumentHeader(title, subtitle) {
    const date = new Date().toLocaleDateString("en-PH", { year: "numeric", month: "long", day: "numeric" });
    return `<div class="print-header" aria-hidden="true"><div class="print-brand">RISE · Rice Farmer Management System with Data Analytics</div><div class="print-title">${escapeHtml(title)}</div><div class="print-subtitle">${escapeHtml(subtitle || "")}</div><div class="print-meta">Generated: ${escapeHtml(date)}</div></div>`;
}

function printDocumentFooter() {
    const date = new Date().toLocaleDateString("en-PH", { year: "numeric", month: "long", day: "numeric" });
    return `<div class="print-footer" aria-hidden="true">RISE system-generated report · Generated ${escapeHtml(date)} · Not an official receipt</div>`;
}

function countBy(items, getValue) {
    return items.reduce((counts, item) => {
        const key = getValue(item) || "Unspecified";
        counts[key] = (counts[key] || 0) + 1;
        return counts;
    }, {});
}

function showToast(message, type = "success") {
    const container = document.getElementById("toastContainer");
    const toast = document.createElement("div");
    toast.className = `toast align-items-center text-bg-${type} border-0`;
    toast.setAttribute("role", "alert");
    toast.innerHTML = `<div class="d-flex"><div class="toast-body">${escapeHtml(message)}</div><button type="button" class="btn-close btn-close-white me-2 m-auto" data-bs-dismiss="toast"></button></div>`;
    container.appendChild(toast);
    new bootstrap.Toast(toast, { delay: 3500 }).show();
    toast.addEventListener("hidden.bs.toast", () => toast.remove());
}

async function loadFarmers() {
    try {
        const params = new URLSearchParams();
        const search = document.getElementById("searchInput")?.value.trim();
        const sort = document.getElementById("farmerSort")?.value || "name";
        if (search) params.set("search", search);
        if (sort) params.set("sort", sort);
        const query = params.toString();
        farmers = await api.get(`${FARMERS_ENDPOINT}${query ? `?${query}` : ""}`);
        if (currentView === "farmers") renderFarmers();
    } catch (error) {
        farmers = [];
        if (currentView === "farmers") renderFarmers(error.message);
        else showToast(error.message, "danger");
    }
}

function setView(view) {
    currentView = view;
    syncHeaderAddButton();
    document.querySelectorAll(".nav-item[data-view]").forEach(item => item.classList.toggle("active", item.dataset.view === view));
    const titles = { dashboard: "Dashboard", farmers: "Farmer Records", distribution: "Assistance Distribution",
        supplies: "Supply Management", utilization: "Utilization Monitoring", reports: "Reports and Analytics" };
    document.getElementById("pageTitle").textContent = titles[view] || "RISE";
    document.getElementById("breadcrumbCurrent").textContent = titles[view] || "RISE";
    closeMobileNav();
    // Fade-out the current view before swapping content.
    const mainContent = document.getElementById("mainContent");
    mainContent.classList.remove("view-ready");
    if (view === "dashboard") renderRiseDashboard();
    else if (view === "farmers") { renderFarmerView(); loadFarmers(); }
    else if (view === "distribution") renderDistributionModule();
    else if (view === "supplies") renderSupplyModule();
    else if (view === "utilization") renderUtilizationModule();
    else if (view === "reports") renderRiseReports();
    // Fade-in the new view once content is in the DOM.
    requestAnimationFrame(() => {
        requestAnimationFrame(() => mainContent.classList.add("view-ready"));
    });
}

function renderFarmerView(errorMessage = "") {
    mainContent.innerHTML = `${printDocumentHeader("Farmer Records Report", "Registered rice farmers and their information")}
        <div class="view-header">
            <div><h2>Farmer Registry</h2><p>Register, search and maintain rice farmer information.</p></div>
            <div class="d-flex gap-2"><button class="btn btn-back" type="button" data-action="print"><i class="bi bi-printer me-1"></i> Print</button><button class="btn btn-success" type="button" data-action="register"><i class="bi bi-plus-lg me-1"></i> Register farmer</button></div>
        </div>
        ${errorMessage ? `<div class="alert alert-danger">${escapeHtml(errorMessage)}</div>` : ""}
        <section class="content-panel">
            <form class="filter-bar" id="filterForm">
                <input class="form-control" id="searchInput" type="search" placeholder="Search farmer name or Farmer ID">
                <select class="form-select" id="farmerSort">
                    <option value="name">Name (A–Z)</option>
                    <option value="newest">Newest to oldest</option>
                    <option value="oldest">Oldest to newest</option>
                </select>
                <button class="btn btn-success" type="submit"><i class="bi bi-search me-1"></i> Search</button>
            </form>
            <div id="farmerTableContainer" class="table-responsive">
                <table class="table align-middle mb-0 farmers-table">
                    <colgroup><col class="col-lastname"><col class="col-firstname"><col class="col-middlename"><col class="col-contact"><col class="col-barangay"><col class="col-municipality"><col class="col-province"><col class="col-status"><col class="col-actions"></colgroup>
                    <thead><tr><th class="tl">Last Name</th><th class="tl">First Name</th><th class="tl">Middle Name</th><th class="tl">Contact</th><th class="tl">Barangay</th><th class="tl">Municipality</th><th class="tl">Province</th><th class="tc">Status</th><th class="tc">Actions</th></tr></thead>
                    <tbody id="farmerTableBody"><tr><td colspan="9" class="text-center py-5 text-secondary">Loading farmers...</td></tr></tbody>
                </table>
            </div>
        </section>
        ${printDocumentFooter()}`;

    bindDynamicActions();
    document.getElementById("filterForm").addEventListener("submit", event => { event.preventDefault(); loadFarmers(); });
    document.getElementById("farmerSort").addEventListener("change", () => loadFarmers());
    renderFarmers();
}

function renderFarmers(errorMessage = "") {
    const body = document.getElementById("farmerTableBody");
    if (!body) return;
    if (errorMessage) {
        body.innerHTML = `<tr><td colspan="9" class="empty-state">${escapeHtml(errorMessage)}</td></tr>`;
        return;
    }
    if (!farmers.length) {
        body.innerHTML = '<tr><td colspan="9" class="empty-state"><i class="bi bi-person-plus"></i>No farmers match your search</td></tr>';
        return;
    }
    body.innerHTML = farmers.map(farmer => `
        <tr>
            <td class="tl">${escapeHtml(farmer.lastName || farmer.fullName || "—")}</td>
            <td class="tl">${escapeHtml(farmer.firstName || "—")}</td>
            <td class="tl">${escapeHtml(farmer.middleName || "—")}</td>
            <td class="tl col-text-nowrap">${escapeHtml(farmer.contactNumber || "—")}</td>
            <td class="tl col-text-wrap">${escapeHtml(farmer.barangay || "—")}</td>
            <td class="tl col-text-wrap">${escapeHtml(farmer.municipality || "—")}</td>
            <td class="tl col-text-wrap">${escapeHtml(farmer.province || "—")}</td>
            <td class="tc status-cell"><span class="activity-status">${farmer.isActive ? "Active" : "Archived"}</span></td>
            <td class="tc action-buttons">
                <button class="btn btn-sm btn-outline-secondary" type="button" data-action="profile" data-id="${farmer.farmerId}" title="View"><i class="bi bi-eye"></i></button>
                <button class="btn btn-sm btn-outline-success" type="button" data-action="edit" data-id="${farmer.farmerId}" title="Edit"><i class="bi bi-pencil"></i></button>
                <button class="btn btn-sm btn-outline-danger" type="button" data-action="archive" data-id="${farmer.farmerId}" title="Archive"><i class="bi bi-archive"></i></button>
            </td>
        </tr>`).join("");
    body.querySelectorAll("[data-action]").forEach(button => button.addEventListener("click", handleDynamicAction));
}

function distributionStatusMeta(status) {
    const value = String(status || "Pending");
    const isReceived = value === "Distributed";
    const isCancelled = value === "Cancelled";
    const label = isReceived ? "Received" : isCancelled ? "Cancelled" : value === "Approved" ? "Approved" : "Pending";
    const cssClass = isReceived ? "status-received" : isCancelled ? "status-cancelled" : "status-pending";
    return { label, cssClass };
}

async function renderDistributionModule(errorMessage = "") {
    const search = document.getElementById("distributionSearch")?.value.trim() || "";
    const sort = document.getElementById("distributionSort")?.value || "name";
    let summaries = [];
    if (!errorMessage) {
        try {
            const params = new URLSearchParams();
            if (search) params.set("search", search);
            if (sort) params.set("sort", sort);
            summaries = await api.get(`/distributions/by-farmer${params.size ? `?${params}` : ""}`);
        } catch (error) {
            errorMessage = error.message;
        }
    }

    mainContent.innerHTML = `
        <div class="view-header">
            <div><h2>Assistance Distribution</h2><p>Record assistance items for registered farmers and confirm when they are received.</p></div>
            <button class="btn btn-success" type="button" data-distribution-action="add"><i class="bi bi-plus-lg me-1"></i> New distribution</button>
        </div>
        ${errorMessage ? `<div class="alert alert-danger">${escapeHtml(errorMessage)}</div>` : ""}
        <section class="content-panel">
            <form class="filter-bar" id="distributionFilter">
                <input class="form-control" id="distributionSearch" type="search" placeholder="Search farmer name or Farmer ID">
                <select class="form-select" id="distributionSort">
                    <option value="name">Name (A–Z)</option>
                    <option value="newest">Newest to oldest</option>
                    <option value="oldest">Oldest to newest</option>
                </select>
                <button class="btn btn-success" type="submit"><i class="bi bi-search me-1"></i> Search</button>
            </form>
            <div id="distributionTableContainer" class="table-responsive">
                <table class="table align-middle mb-0 farmers-table distribution-overview">
                    <colgroup>
                        <col class="col-lastname"><col class="col-firstname"><col class="col-middlename"><col class="col-barangay"><col class="col-municipality"><col class="col-province"><col class="col-distributions"><col class="col-dist-date"><col class="col-status"><col class="col-actions"></colgroup>
                    <thead><tr>
                        <th class="tl">Last Name</th><th class="tl">First Name</th><th class="tl">Middle Name</th>
                        <th class="tl">Barangay</th><th class="tl">Municipality</th><th class="tl">Province</th>
                        <th class="tc">Distributions</th><th class="tc">Last Distribution</th>
                        <th class="tc">Status</th><th class="tc">Actions</th>
                    </tr></thead>
                    <tbody id="distributionTableBody"><tr><td colspan="10" class="empty-state">Loading assistance distributions...</td></tr></tbody>
                </table>
            </div>
        </section>`;

    document.getElementById("distributionFilter").addEventListener("submit", event => {
        event.preventDefault();
        renderDistributionModule();
    });
    document.getElementById("distributionSort").addEventListener("change", () => renderDistributionModule());
    renderDistributionRows(summaries, errorMessage);
    mainContent.querySelectorAll("[data-distribution-action]").forEach(button => button.addEventListener("click", () => handleDistributionAction(button)));
}

function renderDistributionRows(summaries, errorMessage) {
    const body = document.getElementById("distributionTableBody");
    if (!body) return;
    if (errorMessage) {
        body.innerHTML = `<tr><td colspan="10" class="empty-state">${escapeHtml(errorMessage)}</td></tr>`;
        return;
    }
    if (!summaries.length) {
        body.innerHTML = `<tr><td colspan="10" class="empty-state"><i class="bi bi-inbox"></i>No matching assistance distributions</td></tr>`;
        return;
    }
    body.innerHTML = summaries.map(summary => {
        const meta = distributionStatusMeta(summary.status);
        const farmerId = Number(summary.farmerId || 0);
        return `<tr>
            <td class="tl">${escapeHtml(summary.lastName || "—")}</td>
            <td class="tl">${escapeHtml(summary.firstName || "—")}</td>
            <td class="tl">${escapeHtml(summary.middleName || "—")}</td>
            <td class="tl col-text-wrap">${escapeHtml(summary.barangay || "—")}</td>
            <td class="tl col-text-wrap">${escapeHtml(summary.municipality || "—")}</td>
            <td class="tl col-text-wrap">${escapeHtml(summary.province || "—")}</td>
            <td class="tc col-text-nowrap">${escapeHtml(summary.totalDistributions ?? 0)}</td>
            <td class="tc col-text-nowrap">${escapeHtml(summary.lastDistributionDate ?? "")}</td>
            <td class="tc status-cell"><span class="distribution-status-badge ${meta.cssClass}"><span class="status-dot"></span>${escapeHtml(meta.label)}</span></td>
            <td class="tc action-buttons"><button class="dist-action-btn dist-btn-history" type="button" data-distribution-action="history" data-id="${farmerId}" title="View distribution history"><i class="bi bi-eye"></i> Distribution History</button></td>
        </tr>`;
    }).join("");
    body.querySelectorAll("[data-distribution-action]").forEach(button => button.addEventListener("click", () => handleDistributionAction(button)));
}

async function handleDistributionAction(button) {
    const action = button.dataset.distributionAction;
    const id = Number(button.dataset.id);
    if (action === "back") {
        distributionHistoryFarmerId = null;
        setView("distribution");
        return;
    }
    if (action === "history") {
        await renderFarmerDistributionHistory(id);
        return;
    }
    if (action === "add" || action === "edit" || action === "view") {
        await openDistributionModal(action, action === "add" ? null : id);
        return;
    }
    const paths = { approve: "approve", distribute: "distribute", cancel: "cancel" };
    try {
        if (action === "delete") {
            if (!window.confirm("Delete this draft or cancelled distribution?")) return;
            await api.delete(`/distributions/${id}`);
        } else {
            const confirmations = {
                approve: "Approve this distribution?",
                distribute: "Confirm that the farmer received this assistance?",
                cancel: "Cancel this distribution?"
            };
            if (!window.confirm(confirmations[action])) return;
            await api.post(`/distributions/${id}/${paths[action]}`, {});
        }
        showToast(action === "distribute" ? "Assistance marked as received." : "Distribution updated.");
        await refreshDistributionData();
    } catch (error) {
        showToast(error.message, "danger");
    }
}

async function refreshDistributionData() {
    if (distributionHistoryFarmerId) {
        await renderFarmerDistributionHistory(distributionHistoryFarmerId);
        return;
    }
    await renderDistributionModule();
    await loadFarmers();
}

async function renderFarmerDistributionHistory(farmerId) {
    const farmerIdentifier = Number(farmerId);
    let farmer = null;
    let history = null;
    try {
        [farmer, history] = await Promise.all([
            api.get(`/farmers/${farmerIdentifier}`),
            api.get(`/distributions/by-farmer/${farmerIdentifier}`)
        ]);
    } catch (error) {
        showToast(error.message, "danger");
        setView("distribution");
        return;
    }

    activeDistributionFarmerId = farmerIdentifier;
    distributionHistoryFarmerId = farmerIdentifier;
    const farmerName = farmer?.fullName || history?.farmerName || "Farmer";
    const historyItems = history?.history || [];
    const stats = [
        { label: "Total distributions", value: String(history?.totalDistributions ?? historyItems.length ?? 0), icon: "bi-boxes" },
        { label: "Last distribution", value: history?.lastDistributionDate ? String(history.lastDistributionDate) : "—", icon: "bi-calendar-date" }
    ];

    const historyState = { newestFirst: true, search: "", category: "" };
    const categories = [...new Set(historyItems.map(item => item.categoryName).filter(Boolean))].sort((a, b) => a.localeCompare(b));

    const renderHistoryRows = () => {
        const body = document.getElementById("farmerHistoryTableBody");
        if (!body) return;
        const items = [...historyItems].sort((a, b) => {
            const dateA = a.distributionDate ? new Date(`${a.distributionDate}T00:00:00`).getTime() : 0;
            const dateB = b.distributionDate ? new Date(`${b.distributionDate}T00:00:00`).getTime() : 0;
            const diff = dateB - dateA;
            if (historyState.newestFirst) return diff || Number(b.distributionId || 0) - Number(a.distributionId || 0);
            return -diff || Number(a.distributionId || 0) - Number(b.distributionId || 0);
        }).filter(item => {
            if (historyState.category && item.categoryName !== historyState.category) return false;
            const term = historyState.search.trim().toLowerCase();
            if (!term) return true;
            return [item.referenceNumber, item.supplyName, item.categoryName, item.unitOfMeasure, item.remarks]
                .filter(Boolean).some(value => String(value).toLowerCase().includes(term));
        });
        if (!items.length) {
            body.innerHTML = `<tr><td colspan="7" class="empty-state"><i class="bi bi-inbox"></i>No matching distribution history</td></tr>`;
            return;
        }
        body.innerHTML = items.map(item => {
            const meta = distributionStatusMeta(item.status);
            const receivedAt = item.receivedAt ? new Date(`${item.receivedAt}T00:00:00`).toLocaleDateString("en-PH", { year: "numeric", month: "short", day: "numeric" }) : "—";
            const reference = item.referenceNumber ? String(item.referenceNumber) : `#${item.distributionId ?? ""}`;
            return `<tr>
                <td class="tl">${escapeHtml(item.distributionDate ?? "")}</td>
                <td class="tl col-text-wrap">${escapeHtml(item.supplyName || "—")}</td>
                <td class="tl col-text-wrap">${escapeHtml(item.categoryName || "—")}</td>
                <td class="tc col-text-nowrap">${escapeHtml(item.quantity ?? 0)}</td>
                <td class="tc col-text-nowrap">${escapeHtml(item.unitOfMeasure || "—")}</td>
                <td class="tc status-cell"><span class="distribution-status-badge ${meta.cssClass}"><span class="status-dot"></span>${escapeHtml(meta.label)}</span></td>
                <td class="tc action-buttons"><button class="btn btn-sm btn-outline-secondary" type="button" data-distribution-action="view" data-id="${item.distributionId}" title="View distribution details"><i class="bi bi-eye"></i></button><button class="btn btn-sm btn-outline-secondary" type="button" data-distribution-action="edit" data-id="${item.distributionId}" title="Edit distribution draft"><i class="bi bi-pencil"></i></button>${item.status === "Draft" || item.status === "Cancelled" ? `<button class="btn btn-sm btn-outline-danger" type="button" data-distribution-action="delete" data-id="${item.distributionId}" title="Delete distribution"><i class="bi bi-trash3"></i></button>` : `<button class="btn btn-sm btn-outline-danger is-disabled" type="button" disabled title="Deletion not allowed for ${escapeHtml(item.status || "this")} records"><i class="bi bi-trash3"></i></button>`}</td>
            </tr>`;
        }).join("");
        body.querySelectorAll("[data-distribution-action]").forEach(button => button.addEventListener("click", () => handleDistributionAction(button)));
    };

    mainContent.innerHTML = `${printDocumentHeader("Distribution History Report", farmerName)}
        <div class="view-header">
            <div><h2>Distribution History</h2><p>Individual assistance transactions recorded for this farmer.</p></div>
            <div class="d-flex gap-2"><button class="btn btn-back" type="button" id="farmerHistoryPrint"><i class="bi bi-printer me-2"></i> Print</button><button class="btn btn-back" type="button" id="farmerHistoryBack" data-distribution-action="back"><i class="bi bi-arrow-left me-2"></i> Back to Assistance Distribution</button></div>
        </div>
        <section class="content-panel">
            <div class="farmer-history-head">
                <div>
                    <h3 class="h5 mb-1">${escapeHtml(farmerName)}</h3>
                    <p class="mb-0">${escapeHtml(farmer?.barangay || history?.barangay || "—")} · ${escapeHtml(farmer?.municipality || history?.municipality || "—")} · ${escapeHtml(farmer?.province || history?.province || "—")}</p>
                </div>
            </div>
            <div class="history-stat-grid">
                ${stats.map(stat => `<article class="stat-card glass-card"><div class="stat-card-top"><span>${escapeHtml(stat.label)}</span></div><strong>${escapeHtml(stat.value)}</strong></article>`).join("")}
            </div>
            <form class="filter-bar" id="farmerHistoryFilter">
                <input class="form-control" id="farmerHistorySearch" type="search" placeholder="Search supply or reference">
                <select class="form-select" id="farmerHistoryCategory">
                    <option value="">All categories</option>
                    ${categories.map(category => `<option value="${escapeHtml(category)}">${escapeHtml(category)}</option>`).join("")}
                </select>
                <button class="btn btn-success" type="submit">Filter</button>
                <button class="btn btn-light" type="button" id="farmerHistorySortBtn"><i class="bi bi-arrow-down-up me-1"></i> Sort: ${historyState.newestFirst ? "Newest first" : "Oldest first"}</button>
            </form>
            <div class="table-responsive">
                <table class="table align-middle mb-0 farmers-table">
                    <colgroup>
                        <col class="col-dist-date"><col class="col-dist-supply"><col class="col-dist-category"><col class="col-dist-quantity"><col class="col-dist-unit"><col class="col-dist-status"><col class="col-dist-actions"></colgroup>
                    <thead><tr>
                        <th class="tl">Date</th><th class="tl">Supply</th><th class="tl">Category</th><th class="tc">Quantity</th><th class="tc">Unit</th><th class="tc">Status</th><th class="tc">Actions</th>
                    </tr></thead>
                    <tbody id="farmerHistoryTableBody"><tr><td colspan="7" class="empty-state">Loading distribution history...</td></tr></tbody>
                </table>
            </div>
        </section>
        ${printDocumentFooter()}`;

    const historyFilter = document.getElementById("farmerHistoryFilter");
    const historySearchInput = document.getElementById("farmerHistorySearch");
    const historyCategory = document.getElementById("farmerHistoryCategory");
    const historySortBtn = document.getElementById("farmerHistorySortBtn");
    const historyBackBtn = document.getElementById("farmerHistoryBack");

    const goBackToDistributions = () => {
        distributionHistoryFarmerId = null;
        setView("distribution");
    };
    historyBackBtn?.addEventListener("click", goBackToDistributions);
    document.getElementById("farmerHistoryPrint")?.addEventListener("click", () => window.print());

    historyFilter.addEventListener("submit", event => { event.preventDefault(); });
    historySortBtn?.addEventListener("click", () => {
        historyState.newestFirst = !historyState.newestFirst;
        const btn = historySortBtn;
        if (btn) btn.innerHTML = `<i class="bi bi-arrow-down-up me-1"></i> Sort: ${historyState.newestFirst ? "Newest first" : "Oldest first"}`;
        renderHistoryRows();
    });
    historyCategory?.addEventListener("change", () => {
        historyState.category = historyCategory.value;
        renderHistoryRows();
    });
    historySearchInput?.addEventListener("input", () => { historyState.search = historySearchInput.value; renderHistoryRows(); });
    renderHistoryRows();
}

async function openDistributionModal(mode, id) {
    let record = null;
    let supplies = [];
    let selectedFarmer = null;
    try {
        if (id) record = await api.get(`/distributions/${id}`);
        supplies = await api.get("/supplies") || [];
        if (record?.farmerId) {
            try { selectedFarmer = await api.get(`/farmers/${record.farmerId}`); } catch {}
        }
    } catch (error) {
        showToast(error.message, "danger");
        return;
    }

    const readOnly = mode === "view";
    const defaultDate = new Date().toISOString().slice(0, 10);
    const supplyCategory = supplyId => supplies.find(s => Number(s.supplyId) === Number(supplyId))?.categoryName || "";
    const monthNames = ["January", "February", "March", "April", "May", "June", "July", "August", "September", "October", "November", "December"];
    const monthOptions = monthNames.map((name, index) => `<option value="${String(index + 1).padStart(2, "0")}">${name}</option>`).join("");
    const targetBase = new Date(`${record?.distributionDate || defaultDate}T00:00:00`);
    targetBase.setMonth(targetBase.getMonth() + (utilTargetMonths || 3));
    const defaultTargetMonth = String(targetBase.getMonth() + 1).padStart(2, "0");
    const defaultTargetYear = targetBase.getFullYear();
    const supplySelect = selectedId => `<option value="">Select assistance item...</option>${supplies.map(supply => `<option value="${supply.supplyId}" data-unit="${escapeHtml(supply.unitOfMeasure)}" data-category="${escapeHtml(supply.categoryName)}" data-stock="${supply.stockQuantity}"${Number(selectedId) === Number(supply.supplyId) ? " selected" : ""}>${escapeHtml(`${supply.name} (${supply.unitOfMeasure})`)}</option>`).join("")}`;

    document.getElementById("recordModalHost")?.remove();
    const host = document.createElement("div");
    host.id = "recordModalHost";
    host.innerHTML = `<div class="modal fade" id="distributionModal" tabindex="-1"><div class="modal-dialog modal-xl modal-dialog-centered"><div class="modal-content"><form id="distributionForm">
        <div class="modal-header">${readOnly ? `<div><p class="eyebrow mb-1">DISTRIBUTION DETAILS</p><h2 class="modal-title fs-5">Distribution details</h2></div>` : `<h2 class="modal-title fs-5">${mode === "add" ? "New Assistance Distribution" : "Edit Distribution"}</h2>`}<button class="btn-close" type="button" data-bs-dismiss="modal" aria-label="Close"></button></div>
        ${readOnly ? `
        <div class="modal-body">
            <dl class="row mb-0">
                <dt class="col-5">Farmer</dt><dd class="col-7">${escapeHtml(selectedFarmer?.fullName || record?.farmerName || "—")}</dd>
                <dt class="col-5">Distribution date</dt><dd class="col-7">${escapeHtml(record?.distributionDate || defaultDate)}</dd>
                <dt class="col-5">Status</dt><dd class="col-7">${escapeHtml(record?.status === "Distributed" ? "Received" : (record?.status || "—"))}</dd>
                <dt class="col-5">Notes</dt><dd class="col-7">${escapeHtml(record?.notes || "—")}</dd>
            </dl>
            <h3 class="h6 mt-4 mb-2">Assistance items</h3>
            <div class="table-responsive"><table class="table distribution-items-table"><colgroup><col class="col-di-item"><col class="col-di-category"><col class="col-di-qty"><col class="col-di-unit"><col class="col-di-target"></colgroup><thead><tr><th>Assistance item</th><th>Category</th><th>Quantity</th><th>Unit</th><th>Target month/year</th></tr></thead><tbody>
                ${(record?.items || []).map(item => `<tr><td>${escapeHtml(item.supplyName || supplies.find(s => s.supplyId === item.supplyId)?.name || "—")}</td><td>${escapeHtml(supplyCategory(item.supplyId) || "—")}</td><td>${escapeHtml(item.quantity ?? 0)}</td><td>${escapeHtml(item.unitOfMeasure || "—")}</td><td>${escapeHtml(item.targetMonitoringDate || "—")}</td></tr>`).join("")}
            </tbody></table></div>
        </div>` : `
        <div class="modal-body"><div class="row g-3">
            <div class="col-md-6"><label class="form-label">Farmer</label>
                <div class="farmer-autocomplete" id="farmerAutocomplete">
                    <div class="farmer-search-input-wrap">
                        <input class="form-control" id="farmerSearchInput" type="text" placeholder="Search farmer by name or ID..." autocomplete="off" value="${escapeHtml(selectedFarmer?.fullName || "")}">
                        <button class="farmer-search-clear" type="button" id="farmerSearchClear" title="Clear selected farmer" ${selectedFarmer ? "" : "hidden"}><i class="bi bi-x"></i></button>
                        <i class="bi bi-check-circle-fill farmer-search-check" id="farmerSearchCheck"${selectedFarmer ? "" : " hidden"}></i>
                    </div>
                    <input type="hidden" name="farmerId" id="farmerSearchValue" value="${selectedFarmer?.farmerId || ""}">
                    <div class="farmer-search-menu" id="farmerSearchMenu" hidden></div>
                    <div class="farmer-search-feedback small" id="farmerSearchFeedback"></div>
                </div>
            </div>
            <div class="col-md-6"><label class="form-label">Distribution date</label><input class="form-control" name="distributionDate" type="date" value="${escapeHtml(record?.distributionDate || defaultDate)}" required></div>
            <div class="col-12"><label class="form-label">Notes</label><textarea class="form-control" name="notes" rows="2">${escapeHtml(record?.notes || "")}</textarea></div>
            <div class="col-12"><div class="d-flex justify-content-between align-items-center mb-2"><h3 class="h6 mb-0">Assistance items</h3><button class="btn btn-sm btn-outline-success" type="button" id="addDistributionItem">Add item</button></div><div class="table-responsive"><table class="table distribution-items-table"><colgroup><col class="col-di-item"><col class="col-di-category"><col class="col-di-qty"><col class="col-di-unit"><col class="col-di-target"><col class="col-di-remove"></colgroup><thead><tr><th>Assistance item</th><th>Category</th><th>Quantity / Available stock</th><th>Unit</th><th>Target month/year</th><th></th></tr></thead><tbody id="distributionItems"></tbody></table></div></div>
        </div></div>`}
        <div class="modal-footer"><button class="btn btn-light" type="button" data-bs-dismiss="modal">Close</button>${readOnly ? "" : '<button class="btn btn-success" type="submit">Save Distribution</button>'}</div>
    </form></div></div></div>`;
    document.body.appendChild(host);
    moduleModal = new bootstrap.Modal(document.getElementById("distributionModal"));
    moduleModal.show();

    if (!readOnly) {
        const input = document.getElementById("farmerSearchInput");
        const hiddenInput = document.getElementById("farmerSearchValue");
        const clearBtn = document.getElementById("farmerSearchClear");
        const menu = document.getElementById("farmerSearchMenu");
        const feedback = document.getElementById("farmerSearchFeedback");
        const container = document.getElementById("farmerAutocomplete");
        let debounceTimer = null;
        let sequence = 0;

        const clearSelection = () => {
            hiddenInput.value = "";
            input.classList.remove("is-valid");
            clearBtn.hidden = true;
            document.getElementById("farmerSearchCheck").hidden = true;
            feedback.textContent = "";
        };
        const selectFarmer = farmer => {
            hiddenInput.value = farmer.farmerId;
            input.value = farmer.fullName;
            input.classList.add("is-valid");
            clearBtn.hidden = false;
            document.getElementById("farmerSearchCheck").hidden = false;
            menu.hidden = true;
            feedback.textContent = `Selected: ${farmer.fullName}`;
        };
        input.addEventListener("input", () => {
            clearSelection();
            window.clearTimeout(debounceTimer);
            const term = input.value.trim();
            if (term.length < 2) { menu.hidden = true; return; }
            feedback.textContent = "Searching farmers...";
            debounceTimer = window.setTimeout(async () => {
                const current = ++sequence;
                try {
                    const results = await api.get(`/farmers/search?q=${encodeURIComponent(term)}&limit=8`) || [];
                    if (current !== sequence) return;
                    feedback.textContent = "";
                    if (!results.length) {
                        menu.innerHTML = `<div class="farmer-search-empty">No matching farmers found</div>`;
                        menu.hidden = false;
                        return;
                    }
                    menu.innerHTML = results.map(farmer => `<button class="farmer-search-item" type="button" data-farmer-id="${farmer.farmerId}"><strong>${escapeHtml(farmer.fullName)}</strong><span>${escapeHtml(farmer.barangay || "—")}</span></button>`).join("");
                    menu.hidden = false;
                    menu.querySelectorAll("[data-farmer-id]").forEach(button => button.addEventListener("click", () => {
                        const farmer = results.find(f => Number(f.farmerId) === Number(button.dataset.farmerId));
                        if (farmer) selectFarmer(farmer);
                    }));
                } catch (error) {
                    if (current !== sequence) return;
                    menu.hidden = true;
                    feedback.textContent = error.message;
                }
            }, 250);
        });
        clearBtn?.addEventListener("click", () => { clearSelection(); input.value = ""; input.focus(); });
        const closeMenu = event => { if (container && !container.contains(event.target)) menu.hidden = true; };
        document.addEventListener("click", closeMenu);
        document.getElementById("distributionModal").addEventListener("hidden.bs.modal", () => document.removeEventListener("click", closeMenu));
        if (selectedFarmer) {
            input.classList.add("is-valid");
            clearBtn.hidden = false;
            document.getElementById("farmerSearchCheck").hidden = false;
            feedback.textContent = `Selected: ${selectedFarmer.fullName}`;
        }

        const itemBody = document.getElementById("distributionItems");
        const appendItem = item => {
            const row = document.createElement("tr");
            const targetDate = item?.targetMonitoringDate ? String(item.targetMonitoringDate) : `${defaultTargetYear}-${defaultTargetMonth}-01`;
            const targetMonth = targetDate.slice(5, 7);
            row.innerHTML = `<td><select class="form-select" name="supplyId" required>${supplySelect(item?.supplyId)}</select></td>
                <td class="distribution-category">${escapeHtml(supplyCategory(item?.supplyId) || "—")}</td>
                <td><input class="form-control" name="quantity" type="number" min="0.001" step="0.001" value="${escapeHtml(item?.quantity ?? "")}" required>
                    <div class="stock-available small" data-stock-available></div>
                    <div class="stock-feedback small" data-stock-feedback></div></td>
                <td class="distribution-unit">${escapeHtml(item?.unitOfMeasure || "")}</td>
                <td class="target-cell"><div class="target-controls">
                    <div class="target-field"><span class="target-label">Month</span><select class="form-select form-select-sm" name="targetMonth" aria-label="Target month" title="Target month">${monthOptions}</select></div>
                    <div class="target-field"><span class="target-label">Year</span><input class="form-control form-control-sm" name="targetYear" type="number" min="2000" max="2100" value="${escapeHtml(targetDate.slice(0, 4))}" aria-label="Target year" title="Target year"></div>
                </div></td>
                <td><button class="btn btn-sm btn-outline-danger" type="button" data-remove-item aria-label="Remove item"><i class="bi bi-trash"></i></button></td>`;
            itemBody.appendChild(row);
            row.querySelector('[name="targetMonth"]').value = targetMonth;
            const select = row.querySelector('[name="supplyId"]');
            const quantityInput = row.querySelector('[name="quantity"]');
            const stockEl = row.querySelector("[data-stock-available]");
            const feedbackEl = row.querySelector("[data-stock-feedback]");
            const unitEl = row.querySelector(".distribution-unit");
            const categoryEl = row.querySelector(".distribution-category");

            const validate = () => {
                const supply = supplies.find(s => Number(s.supplyId) === Number(select.value));
                const available = supply ? Number(supply.stockQuantity || 0) : 0;
                const unit = supply?.unitOfMeasure || "";
                stockEl.textContent = supply ? `Available: ${available} ${unit}` : "Available: —";
                feedbackEl.innerHTML = "";
                quantityInput.classList.remove("is-invalid");
                if (!supply) {
                    if (quantityInput.value.trim()) feedbackEl.textContent = "Select a valid assistance item.";
                    return false;
                }
                const raw = quantityInput.value.trim();
                const value = Number(raw);
                if (!raw || !Number.isFinite(value) || value <= 0) {
                    feedbackEl.innerHTML = `<span class="text-warning">Invalid Quantity — enter a value greater than zero.</span>`;
                    quantityInput.classList.add("is-invalid");
                    return false;
                }
                if (value > available) {
                    feedbackEl.innerHTML = `<span class="text-danger"><strong>Insufficient Stock!</strong> Only ${escapeHtml(String(available))} ${escapeHtml(unit)} are currently available. Please enter a quantity within the available stock.</span>`;
                    quantityInput.classList.add("is-invalid");
                    return false;
                }
                feedbackEl.innerHTML = `<span class="text-success">Within available stock</span>`;
                return true;
            };
            row.validateStock = validate;
            const syncSupply = () => {
                const supply = supplies.find(s => Number(s.supplyId) === Number(select.value));
                unitEl.textContent = supply?.unitOfMeasure || "";
                categoryEl.textContent = supply?.categoryName || "—";
                validate();
            };
            select.addEventListener("change", syncSupply);
            quantityInput.addEventListener("input", validate);
            quantityInput.addEventListener("change", validate);
            syncSupply();
            row.querySelector("[data-remove-item]").addEventListener("click", () => row.remove());
        };
        (record?.items || [null]).forEach(appendItem);
        document.getElementById("addDistributionItem")?.addEventListener("click", () => appendItem(null));

        const form = document.getElementById("distributionForm");
        const submitBtn = form.querySelector('button[type="submit"]');
        form.addEventListener("submit", async event => {
            event.preventDefault();
            if (!form.reportValidity()) return;
            const farmerId = Number(hiddenInput.value);
            if (!farmerId) {
                showToast("Select a registered farmer before saving.", "danger");
                input.focus();
                return;
            }
            const rows = [...itemBody.querySelectorAll("tr")];
            if (!rows.length) {
                showToast("Add at least one supply item.", "danger");
                return;
            }
            if (!rows.every(row => row.validateStock())) {
                showToast("Correct the invalid assistance item before saving.", "danger");
                return;
            }
            if (!rows.every(row => /^\d{4}$/.test(row.querySelector('[name="targetYear"]').value))) {
                showToast("Enter a valid target monitoring year.", "danger");
                return;
            }
            const items = rows.map(row => ({
                supplyId: Number(row.querySelector('[name="supplyId"]').value),
                quantity: Number(row.querySelector('[name="quantity"]').value),
                unitCost: 0,
                lotNumber: null,
                targetMonitoringDate: `${row.querySelector('[name="targetYear"]').value}-${row.querySelector('[name="targetMonth"]').value}-01`
            }));
            if (new Set(items.map(item => item.supplyId)).size !== items.length) {
                showToast("Each supply can only appear once in a distribution.", "danger");
                return;
            }
            const payload = {
                farmerId,
                distributionDate: form.elements.distributionDate.value,
                notes: form.elements.notes.value.trim() || null,
                items
            };
            submitBtn.disabled = true;
            try {
                if (id) await api.put(`/distributions/${id}`, payload);
                else await api.post("/distributions", payload);
                moduleModal.hide();
                showToast("Distribution saved.");
                await renderDistributionModule();
                await loadFarmers();
            } catch (error) {
                showToast(error.message, "danger");
                submitBtn.disabled = false;
            }
        });
    }
}

let riseCharts = [];
async function renderRiseDashboard() {
    mainContent.innerHTML = `<div class="view-header"><div><p class="eyebrow">RICE FARMER ASSISTANCE</p><h2>System overview</h2><p>Live totals from farmer, distribution and utilization records.</p></div></div><div class="content-panel p-5">Loading dashboard data…</div>`;
    try {
        const d = await api.get("/reports/dashboard");
        riseCharts.forEach(c => c.destroy()); riseCharts = [];
        const cards = [["Registered farmers",d.totalFarmers],["Farmers received assistance",d.receivedFarmers],
            ["Not yet received",d.notReceivedFarmers],["Quantity distributed",d.totalDistributed],
            ["Quantity used",d.quantityUsed],["Still stocked",d.quantityStillStocked],
            ["Not yet used",d.quantityNotYetUsed],["Disposed",d.quantityDisposed],["Unreported quantity",d.quantityUnreported]];
        mainContent.innerHTML = `<div class="view-header"><div><p class="eyebrow">RICE FARMER ASSISTANCE</p><h2>System overview</h2><p>Statistics update automatically from saved database records.</p></div></div>
        <section class="stat-grid">${cards.map(x=>`<article class="stat-card glass-card"><div class="stat-card-top"><span>${escapeHtml(x[0])}</span></div><strong>${Number(x[1]||0).toLocaleString()}</strong></article>`).join("")}</section>
        <section class="dashboard-grid analytics-grid">
          <article class="panel glass-panel"><div class="panel-header"><h3>Assistance status</h3></div><div class="chart-wrap"><canvas id="riseFarmersChart"></canvas></div></article>
          <article class="panel glass-panel"><div class="panel-header"><h3>Distribution by assistance type</h3></div><div class="chart-wrap"><canvas id="riseTypeChart"></canvas></div></article>
          <article class="panel glass-panel"><div class="panel-header"><h3>Utilization by status</h3></div><div class="chart-wrap"><canvas id="riseUtilChart"></canvas></div></article>
          <article class="panel glass-panel"><div class="panel-header"><h3>Farmers assisted by barangay</h3></div><div class="chart-wrap"><canvas id="riseBarangayChart"></canvas></div></article>
        </section>`;
        const make=(id,type,labels,data)=>{const el=document.getElementById(id);if(el)riseCharts.push(new Chart(el,{type,data:{labels,datasets:[{label:"Count / quantity",data,borderWidth:1}]},options:{responsive:true,maintainAspectRatio:false,plugins:{legend:{display:type!=="bar"}}}}));};
        make("riseFarmersChart","doughnut",d.farmersByStatus.map(x=>x.name),d.farmersByStatus.map(x=>x.count));
        make("riseTypeChart","bar",d.distributionByType.map(x=>x.name),d.distributionByType.map(x=>x.quantity));
        make("riseUtilChart","doughnut",d.utilizationByStatus.map(x=>x.name),d.utilizationByStatus.map(x=>x.quantity));
        make("riseBarangayChart","bar",d.distributionByBarangay.map(x=>x.name),d.distributionByBarangay.map(x=>x.count));
    } catch(error) {
        mainContent.innerHTML=`<div class="view-header"><div><h2>Dashboard</h2><p>Live database overview</p></div></div><div class="alert alert-danger">${escapeHtml(error.message)}</div>`;
    }
}

let utilRows = [];
let utilFarmers = [];
let utilDistributionDates = new Map();
let utilTargetMonths = 3;
let utilActiveTab = "to-monitor";
const utilTabFilters = {
    "to-monitor": { search: "", barangay: "", year: "", status: "" },
    "monitored": { search: "", barangay: "", year: "", status: "" }
};
let refreshUtilFarmerModal = null;

function monitoringTargetInfo(item, today = new Date()) {
    const stored = item.targetMonitoringDate;
    let target;
    if (stored && stored !== "0001-01-01") {
        target = new Date(`${stored}T00:00:00`);
    } else {
        const source = utilDistributionDates.get(Number(item.distributionId)) || "";
        if (!source) return null;
        target = new Date(`${source}T00:00:00`);
        target.setMonth(target.getMonth() + utilTargetMonths);
    }
    const targetDate = target.toISOString().slice(0, 10);
    const days = Math.ceil((target.getTime() - today.getTime()) / 86400000);
    const monitoredDate = item.monitoringDate && item.monitoringDate !== "0001-01-01" ? String(item.monitoringDate) : null;
    return {
        targetDate,
        days,
        monitored: item.utilizationMonitoringId !== 0,
        monitoredDate,
        late: Boolean(monitoredDate && monitoredDate > targetDate)
    };
}

function utilizationItemStatus(item) {
    const info = monitoringTargetInfo(item);
    if (info && !info.monitored && info.days < 0) return { label: "Overdue", cssClass: "util-status-overdue" };
    if (info && !info.monitored && info.days <= 30) return { label: "Due soon", cssClass: "util-status-due-soon" };
    if (!info && item.utilizationMonitoringId === 0) return { label: "Not monitored", cssClass: "util-status-none" };
    if (info && info.late) return { label: "Late", cssClass: "util-status-late" };
    if (item.unreportedQuantity > 0) return { label: "Incomplete", cssClass: "util-status-partial" };
    return { label: "Complete", cssClass: "util-status-complete" };
}

function utilizationFarmerStatus(items) {
    const monitored = items.filter(item => item.utilizationMonitoringId !== 0);
    const overdue = items.some(item => { const info = monitoringTargetInfo(item); return info && !info.monitored && info.days < 0; });
    const late = items.some(item => { const info = monitoringTargetInfo(item); return info && info.late; });
    if (monitored.length === 0) return overdue
        ? { label: "Overdue", cssClass: "util-status-overdue" }
        : { label: "Not Monitored", cssClass: "util-status-none" };
    if (monitored.length === items.length && items.every(item => item.unreportedQuantity === 0)) return late
        ? { label: "Completed Late", cssClass: "util-status-late" }
        : { label: "Complete", cssClass: "util-status-complete" };
    return overdue
        ? { label: "Overdue", cssClass: "util-status-overdue" }
        : { label: "Partially Monitored", cssClass: "util-status-partial" };
}

function openUtilizationFarmerModal(farmerId) {
    const farmer = utilFarmers.find(f => Number(f.farmerId) === farmerId) || {};
    const items = utilRows.filter(row => Number(row.farmerId) === farmerId);
    const farmerName = farmer.fullName || items[0]?.farmerName || "Farmer";
    const pendingOnly = utilActiveTab === "to-monitor";
    const needsMonitoring = row => row.utilizationMonitoringId === 0 || Number(row.unreportedQuantity) > 0;
    const farmerMeta = [farmer.barangay, farmer.municipality, farmer.contactNumber].filter(Boolean).map(value => escapeHtml(value)).join(" · ");
    const itemCategories = [...new Set(items.map(item => item.categoryName).filter(Boolean))].sort((a, b) => a.localeCompare(b));
    const itemYears = [...new Set(items.map(item => item.monitoringDate).filter(date => date && date !== "0001-01-01").map(date => String(date).slice(0, 4)))].sort().reverse();
    const monthOptions = [["01", "January"], ["02", "February"], ["03", "March"], ["04", "April"], ["05", "May"], ["06", "June"], ["07", "July"], ["08", "August"], ["09", "September"], ["10", "October"], ["11", "November"], ["12", "December"]].map(([value, label]) => `<option value="${value}">${label}</option>`).join("");
    const itemFilters = { month: "", year: "", category: "" };
    document.getElementById("utilizationFarmerModalHost")?.remove();
    const host = document.createElement("div");
    host.id = "utilizationFarmerModalHost";
    host.innerHTML = `<div class="modal fade" id="utilizationFarmerModal" tabindex="-1"><div class="modal-dialog modal-xl modal-dialog-centered"><div class="modal-content">
        <div class="modal-header"><div><p class="eyebrow mb-1">FARMER UTILIZATION</p><h2 class="modal-title fs-5">${escapeHtml(farmerName)}</h2>
            <p class="mb-0 small"${farmerMeta ? "" : " hidden"}>${farmerMeta}</p></div>
            <button class="btn-close" type="button" data-bs-dismiss="modal" aria-label="Close"></button></div>
        <div class="modal-body">${pendingOnly ? "" : `<form class="filter-bar" id="utilItemFilter">
            <select class="form-select" id="utilItemMonth"><option value="">All Months</option>${monthOptions}</select>
            <select class="form-select" id="utilItemYear"><option value="">All Years</option>${itemYears.map(year => `<option value="${year}">${year}</option>`).join("")}</select>
            <select class="form-select" id="utilItemCategory"><option value="">All Categories</option>${itemCategories.map(category => `<option value="${escapeHtml(category)}">${escapeHtml(category)}</option>`).join("")}</select>
            <button class="btn btn-light" type="button" id="utilItemClear">Clear</button>
            <span class="text-muted small align-self-center">Month / year / category</span>
        </form>`}<div id="utilFarmerItems"></div></div>
        <div class="modal-footer"><button class="btn btn-light" type="button" data-bs-dismiss="modal">Close</button></div>
    </div></div></div>`;
    document.body.appendChild(host);
    const itemHost = host.querySelector("#utilFarmerItems");

    const renderFarmerItems = () => {
        const currentItems = utilRows.filter(row => Number(row.farmerId) === farmerId && (!pendingOnly || needsMonitoring(row)) &&
            (!itemFilters.category || row.categoryName === itemFilters.category) &&
            (!itemFilters.month || (row.monitoringDate && row.monitoringDate !== "0001-01-01" && String(row.monitoringDate).slice(5, 7) === itemFilters.month)) &&
            (!itemFilters.year || (row.monitoringDate && row.monitoringDate !== "0001-01-01" && String(row.monitoringDate).slice(0, 4) === itemFilters.year))).sort((a, b) => {
            const rank = item => item.utilizationMonitoringId === 0 ? 0 : (item.unreportedQuantity > 0 ? 1 : 2);
            const dateA = String(utilDistributionDates.get(Number(a.distributionId)) || "");
            const dateB = String(utilDistributionDates.get(Number(b.distributionId)) || "");
            return rank(a) - rank(b) || dateB.localeCompare(dateA) || String(a.itemName).localeCompare(String(b.itemName));
        });
        itemHost.innerHTML = currentItems.length ? `<div class="table-responsive"><table class="table align-middle util-child-table">
            <thead><tr><th class="tl">Distributed supply</th><th class="tc">Received${pendingOnly ? "" : "</th><th class=\"tc\">Used</th><th class=\"tc\">Stocked</th><th class=\"tc\">Disposed</th><th class=\"tc\">Monitored"}</th><th class="tc">Status</th><th class="tc">Actions</th></tr></thead>
            <tbody>${currentItems.map(item => {
                const itemStatus = utilizationItemStatus(item);
                const targetInfo = monitoringTargetInfo(item);
                const targetNote = targetInfo ? `Target: ${escapeHtml(targetInfo.targetDate)} · ${targetInfo.days < 0 ? `${Math.abs(targetInfo.days)}d overdue` : `${targetInfo.days}d left`}${targetInfo.late ? " · monitored late" : ""}` : "";
                return `<tr>
                    <td class="tl col-text-wrap">${escapeHtml(item.itemName)}${targetNote ? `<div class="small text-muted">${targetNote}</div>` : ""}</td>
                    <td class="tc">${escapeHtml(item.receivedQuantity)} ${escapeHtml(item.unit)}</td>
${pendingOnly ? "" : `<td class="tc">${item.quantityUsed ?? "—"}</td>
                    <td class="tc">${item.quantityStillStocked ?? "—"}</td>
                    <td class="tc">${item.quantityDisposed ?? "—"}</td>
                    <td class="tc col-text-nowrap">${item.monitoringDate && item.monitoringDate !== "0001-01-01" ? escapeHtml(new Date(`${item.monitoringDate}T00:00:00`).toLocaleDateString("en-PH", { year: "numeric", month: "short", day: "numeric" })) : "—"}</td>`}
                    <td class="tc"><span class="util-status-badge ${itemStatus.cssClass}"><span class="status-dot"></span>${escapeHtml(itemStatus.label)}</span></td>
                    <td class="tc"><button class="btn btn-sm btn-outline-success" type="button" data-util-edit="${item.distributionItemId}">${item.utilizationMonitoringId === 0 ? "Monitor" : "Update"}</button></td>
                </tr>`;
            }).join("")}</tbody></table></div>` : `<div class="empty-state">${pendingOnly ? "All supplies for this farmer have been monitored. This farmer is now listed in the Monitored tab." : "No monitoring records for this farmer."}${pendingOnly ? `<div class="mt-3"><button class="btn btn-sm btn-outline-success" type="button" id="utilGoMonitoredBtn">View in Monitored</button></div>` : ""}</div>`;
        document.getElementById("utilGoMonitoredBtn")?.addEventListener("click", () => {
            utilActiveTab = "monitored";
            bootstrap.Modal.getInstance(host.querySelector(".modal"))?.hide();
            renderUtilizationModule();
        });
        itemHost.querySelectorAll("[data-util-edit]").forEach(button => button.addEventListener("click", () => {
            const item = utilRows.find(row => row.distributionItemId === Number(button.dataset.utilEdit));
            if (item) openUtilizationForm(item, () => renderFarmerItems());
        }));
    };

    document.getElementById("utilItemMonth")?.addEventListener("change", () => {
        itemFilters.month = document.getElementById("utilItemMonth").value;
        renderFarmerItems();
    });
    document.getElementById("utilItemYear")?.addEventListener("change", () => {
        itemFilters.year = document.getElementById("utilItemYear").value;
        renderFarmerItems();
    });
    document.getElementById("utilItemCategory")?.addEventListener("change", () => {
        itemFilters.category = document.getElementById("utilItemCategory").value;
        renderFarmerItems();
    });
    document.getElementById("utilItemClear")?.addEventListener("click", () => {
        itemFilters.month = "";
        itemFilters.year = "";
        itemFilters.category = "";
        document.getElementById("utilItemMonth").value = "";
        document.getElementById("utilItemYear").value = "";
        document.getElementById("utilItemCategory").value = "";
        renderFarmerItems();
    });

    refreshUtilFarmerModal = renderFarmerItems;
    renderFarmerItems();
    const modal = new bootstrap.Modal(host.querySelector(".modal"));
    modal.show();
    host.querySelector(".modal").addEventListener("hidden.bs.modal", () => {
        if (refreshUtilFarmerModal === renderFarmerItems) refreshUtilFarmerModal = null;
        host.remove();
    });
}

async function renderUtilizationModule(errorMessage = "") {
    const filters = utilTabFilters[utilActiveTab] || (utilTabFilters[utilActiveTab] = { search: "", barangay: "", year: "", status: "" });
    const search = document.getElementById("utilSearch")?.value.trim() ?? filters.search;
    const status = document.getElementById("utilStatus")?.value ?? filters.status;
    const year = document.getElementById("utilYear")?.value.trim() ?? filters.year;
    filters.search = search;
    filters.status = status;
    filters.year = year;

    try {
        [utilRows, utilFarmers] = await Promise.all([api.get("/utilization"), api.get("/farmers")]);
        utilRows = utilRows || [];
        utilFarmers = utilFarmers || [];
    } catch (error) {
        errorMessage = error.message;
    }
    try {
        const distributions = await api.get("/distributions") || [];
        utilDistributionDates = new Map(distributions.map(d => [Number(d.distributionId), d.distributionDate]));
    } catch {
        utilDistributionDates = new Map();
    }

    try {
        const settings = await api.get("/utilization/settings");
        utilTargetMonths = Number(settings?.targetMonths) || 3;
    } catch {
        utilTargetMonths = 3;
    }

    const farmersById = new Map(utilFarmers.map(f => [Number(f.farmerId), f]));
    const byFarmer = new Map();
    utilRows.forEach(row => {
        const key = Number(row.farmerId || 0);
        if (!byFarmer.has(key)) byFarmer.set(key, []);
        byFarmer.get(key).push(row);
    });

    const allGroups = [...byFarmer.entries()].map(([farmerId, items]) => {
        const farmer = farmersById.get(farmerId) || {};
        const monitoredCount = items.filter(item => item.utilizationMonitoringId !== 0).length;
        const needsAttention = items.filter(item => item.utilizationMonitoringId === 0 || item.unreportedQuantity > 0).length;
        const priority = monitoredCount === 0 ? 0 : needsAttention > 0 ? 1 : 2;
        const latestDistributionDate = items.reduce((latest, item) => {
            const date = utilDistributionDates.get(Number(item.distributionId));
            return date && String(date) > latest ? String(date) : latest;
        }, "");
        const lastMonitoredDate = items.reduce((latest, item) => {
            const date = item.monitoringDate && item.monitoringDate !== "0001-01-01" ? String(item.monitoringDate) : "";
            return date > latest ? date : latest;
        }, "");
        const overdue = items.some(item => { const info = monitoringTargetInfo(item); return info && !info.monitored && info.days < 0; });
        return {
            farmerId,
            farmer,
            farmerName: farmer.fullName || items[0]?.farmerName || "—",
            items,
            monitoredCount,
            itemsToMonitor: needsAttention,
            summary: utilizationFarmerStatus(items),
            priority,
            latestDistributionDate,
            lastMonitoredDate,
            overdue
        };
    });

    // Tab membership comes from actual distribution/monitoring records, never from labels.
    const toMonitorCount = allGroups.filter(group => group.itemsToMonitor > 0).length;
    const monitoredCount = allGroups.filter(group => group.itemsToMonitor === 0).length;
    const overdueCount = allGroups.filter(group => group.overdue).length;
    const term = search.trim().toLowerCase();
    const groups = allGroups
        .filter(group => utilActiveTab === "monitored" ? group.itemsToMonitor === 0 : group.itemsToMonitor > 0)
        .filter(group => {
            if (utilActiveTab === "monitored" && year && !String(group.lastMonitoredDate || "").startsWith(year)) return false;
            if (term) {
                const haystack = [group.farmerName, group.farmer.firstName, group.farmer.lastName,
                    group.farmer.middleName, group.farmer.barangay, group.farmer.municipality, group.farmer.province,
                    group.farmer.contactNumber, ...group.items.map(item => item.itemName)].filter(Boolean).join(" ").toLowerCase();
                if (!haystack.includes(term)) return false;
            }
            if (utilActiveTab === "to-monitor" && status) {
                if (status === "Not Monitored") return group.monitoredCount === 0;
                if (status === "Partially Monitored") return group.monitoredCount > 0 && group.itemsToMonitor > 0;
                return group.summary.label === status;
            }
            return true;
        })
        .sort((a, b) => Number(b.overdue) - Number(a.overdue) ||
            a.farmerName.localeCompare(b.farmerName, undefined, { sensitivity: "base" }));

    mainContent.innerHTML = `<div class="view-header"><div><h2>Utilization Monitoring</h2><p>Monitor distributed supplies by farmer. Unfinished records are listed first.</p></div></div>
        ${errorMessage ? `<div class="alert alert-danger">${escapeHtml(errorMessage)}</div>` : ""}
        <div class="util-tabs" role="tablist">
            <button class="util-tab${utilActiveTab === "to-monitor" ? " active" : ""}" type="button" data-util-tab="to-monitor" role="tab" aria-selected="${utilActiveTab === "to-monitor"}">To Monitor <span class="util-tab-counter">${toMonitorCount}</span></button>
            <button class="util-tab util-tab-monitored${utilActiveTab === "monitored" ? " active" : ""}" type="button" data-util-tab="monitored" role="tab" aria-selected="${utilActiveTab === "monitored"}"><i class="bi bi-check-circle me-1"></i>Monitored <span class="util-tab-counter">${monitoredCount}</span></button>
            <span class="util-overdue-count" title="Farmers with supplies past the monitoring target date"><i class="bi bi-exclamation-triangle me-1"></i>Overdue: ${overdueCount}</span>
        </div>
        <section class="content-panel">${utilActiveTab === "to-monitor" ? `<form class="filter-bar" id="utilFilter">
            <input id="utilSearch" class="form-control" type="search" value="${escapeHtml(search)}" placeholder="Search farmer name, barangay or supply">
            <select id="utilStatus" class="form-select"><option value="">All Status</option>${["Not Monitored", "Partially Monitored"].map(x => `<option value="${x}"${status === x ? " selected" : ""}>${x}</option>`).join("")}</select>
            <button class="btn btn-success">Filter</button>
        </form>` : `<form class="filter-bar" id="utilFilter">
            <input id="utilSearch" class="form-control" type="search" value="${escapeHtml(search)}" placeholder="Search farmer name or supply">
            <input id="utilYear" class="form-control" type="number" min="2000" max="2100" value="${escapeHtml(year)}" placeholder="Year monitored">
            <button class="btn btn-success">Filter</button>
        </form>`}<div class="table-responsive"><table class="table align-middle utilization-table">
            <thead><tr><th class="tl">Farmer Name</th><th class="tl">Barangay</th><th class="tc">${utilActiveTab === "monitored" ? "Items Monitored" : "Items to Monitor"}</th>${utilActiveTab === "monitored" ? `<th class="tc">Last Monitored</th>` : ""}<th class="tc">Status</th><th class="tc">Action</th></tr></thead>
            <tbody>${groups.length ? groups.map(group => `<tr>
                <td class="tl col-text-wrap">${escapeHtml(group.farmerName)}</td>
                <td class="tl col-text-wrap">${escapeHtml(group.farmer.barangay || "—")}</td>
                <td class="tc">${utilActiveTab === "monitored" ? group.monitoredCount : group.itemsToMonitor}</td>
                ${utilActiveTab === "monitored" ? `<td class="tc col-text-nowrap">${group.lastMonitoredDate ? escapeHtml(new Date(`${group.lastMonitoredDate}T00:00:00`).toLocaleDateString("en-PH", { year: "numeric", month: "short", day: "numeric" })) : "—"}</td>` : ""}
                <td class="tc"><span class="util-status-badge ${group.summary.cssClass}"><span class="status-dot"></span>${escapeHtml(group.summary.label)}</span></td>
                <td class="tc action-buttons"><button class="btn btn-sm btn-outline-success" type="button" data-util-farmer-monitor="${group.farmerId}">${utilActiveTab === "monitored" ? "View" : (group.monitoredCount === 0 ? "Monitor" : "Continue")}</button></td>
            </tr>`).join("") : `<tr><td colspan="${utilActiveTab === "monitored" ? "6" : "5"}" class="empty-state">No farmers match this filter.</td></tr>`}</tbody>
        </table></div></section>`;

    document.querySelectorAll("[data-util-tab]").forEach(tab => tab.addEventListener("click", () => {
        utilActiveTab = tab.dataset.utilTab;
        renderUtilizationModule();
    }));
    document.getElementById("utilFilter")?.addEventListener("submit", event => { event.preventDefault(); renderUtilizationModule(); });
    mainContent.querySelectorAll("[data-util-farmer-monitor]").forEach(button => button.addEventListener("click", () => {
        openUtilizationFarmerModal(Number(button.dataset.utilFarmerMonitor));
    }));
}

async function openUtilizationForm(r, onSaved) {
    if(!r)return;
    const host=document.createElement("div");
    host.innerHTML=`<div class="modal fade" tabindex="-1"><div class="modal-dialog modal-lg modal-dialog-centered"><div class="modal-content"><form id="utilForm">
    <div class="modal-header"><h5 class="modal-title">Monitor ${escapeHtml(r.itemName)} — ${escapeHtml(r.farmerName)}</h5><button class="btn-close" type="button" data-bs-dismiss="modal"></button></div>
    <div class="modal-body"><p>Received: <strong>${r.receivedQuantity} ${escapeHtml(r.unit)}</strong>. The reported total cannot exceed this amount. Leave a field blank if it has not been reported.</p><div class="row g-3">
    ${[["quantityUsed","Quantity used"],["quantityStillStocked","Quantity still stocked"],["quantityDisposed","Quantity disposed"]].map(([n,l])=>`<div class="col-md-4"><label class="form-label">${l}</label><input class="form-control" name="${n}" type="number" min="0" max="${r.receivedQuantity}" step="0.001" value="${r[n]??""}"></div>`).join("")}
    <input type="hidden" name="quantityNotYetUsed" value="${r.quantityNotYetUsed??""}">
    <div class="col-md-6"><label class="form-label">Monitoring date</label><input class="form-control" name="monitoringDate" type="date" value="${r.monitoringDate&&r.monitoringDate!=="0001-01-01"?r.monitoringDate:new Date().toISOString().slice(0,10)}" required></div>
    <div class="col-12"><label class="form-label">Remarks</label><textarea class="form-control" name="remarks" rows="2">${escapeHtml(r.remarks||"")}</textarea></div></div><div class="alert alert-info mt-3 mb-0" id="utilTotal"></div></div>
    <div class="modal-footer"><button type="button" class="btn btn-light" data-bs-dismiss="modal">Cancel</button><button class="btn btn-success">Save monitoring</button></div></form></div></div></div>`;
    document.body.appendChild(host);const modal=new bootstrap.Modal(host.querySelector(".modal"));modal.show();
    const form=host.querySelector("#utilForm");
    const updateTotal=()=>{const sum=["quantityUsed","quantityStillStocked","quantityNotYetUsed","quantityDisposed"].reduce((a,n)=>a+(Number(form.elements[n].value)||0),0);host.querySelector("#utilTotal").textContent=`Reported total: ${sum} / ${r.receivedQuantity} ${r.unit} · Unreported: ${Math.max(0,r.receivedQuantity-sum)}`;return sum;};
    form.addEventListener("input",updateTotal);updateTotal();
    form.addEventListener("submit",async e=>{e.preventDefault();const sum=updateTotal();if(sum>r.receivedQuantity){showToast("Reported quantities cannot exceed the received quantity.","danger");return;}
      const payload={};["quantityUsed","quantityStillStocked","quantityNotYetUsed","quantityDisposed"].forEach(n=>payload[n]=form.elements[n].value===""?null:Number(form.elements[n].value));payload.monitoringDate=form.elements.monitoringDate.value;payload.remarks=form.elements.remarks.value.trim()||null;
      try{await api.put(`/utilization/${r.distributionItemId}`,payload);modal.hide();showToast("Utilization monitoring saved.");await renderUtilizationModule();if(typeof onSaved==="function") onSaved();}catch(err){showToast(err.message,"danger");}
    });
    host.querySelector(".modal").addEventListener("hidden.bs.modal",()=>host.remove());
}

async function loadSupplies() {
    const search = document.getElementById("supplySearch")?.value.trim() || "";
    let rows = [];
    try {
        const p = new URLSearchParams();
        if (search) p.set("search", search);
        rows = await api.get(`/supplies${p.size ? `?${p}` : ""}`);
    } catch (error) {
        if (currentView === "supplies") renderSupplies(error.message);
        else showToast(error.message, "danger");
        return;
    }
    supplies = rows;
    if (currentView === "supplies") renderSupplies();
}

async function renderSupplyModule(errorMessage = "") {
    const search = document.getElementById("supplySearch")?.value.trim() || "";
    const sort = document.getElementById("supplySort")?.value || "name-asc";
    let rows = [];
    try {
        if (!errorMessage) {
            const p = new URLSearchParams();
            if (search) p.set("search", search);
            rows = await api.get(`/supplies${p.size ? `?${p}` : ""}`);
        }
    } catch (error) {
        errorMessage = error.message;
    }
    supplies = rows;
    const sortOptions = [
        ["name-asc", "Name (A–Z)"],
        ["stock-desc", "Stock: Highest to lowest"],
        ["stock-asc", "Stock: Lowest to highest"]
    ].map(([value, label]) => `<option value="${value}"${sort === value ? " selected" : ""}>${label}</option>`).join("");
    mainContent.innerHTML = `${printDocumentHeader("Supply Management Report", "Assistance supplies and current stock levels")}<div class="view-header"><div><h2>Supply Management</h2><p>Track and manage stock of assistance supplies such as seeds, fertilizers, and tools.</p></div><div class="d-flex gap-2"><button class="btn btn-back" type="button" data-supply-action="print"><i class="bi bi-printer me-1"></i> Print</button><button class="btn btn-success" type="button" data-supply-action="add"><i class="bi bi-plus-lg me-1"></i> Add supply</button></div></div>
        ${errorMessage ? `<div class="alert alert-danger">${escapeHtml(errorMessage)}</div>` : ""}
        <section class="content-panel"><form class="filter-bar" id="supplyFilter">
            <input id="supplySearch" class="form-control" type="search" value="${escapeHtml(search)}" placeholder="Search supply name or category">
            <select id="supplySort" class="form-select">${sortOptions}</select>
            <button class="btn btn-success" type="submit">Filter</button>
        </form><div class="table-responsive"><table class="table align-middle mb-0 supplies-table">
            <colgroup><col class="col-supply-name"><col class="col-supply-category"><col class="tc"><col class="col-supply-unit"><col class="col-supply-actions"></colgroup>
            <thead><tr><th class="tl">Name</th><th class="tl">Category</th><th class="tc">Stock Quantity</th><th class="tc">Unit</th><th class="tc">Actions</th></tr></thead><tbody id="supplyTableBody"></tbody></table></div></section>
        ${printDocumentFooter()}`;
    document.getElementById("supplyFilter").addEventListener("submit", event => { event.preventDefault(); renderSupplyModule(); });
    document.getElementById("supplySort").addEventListener("change", () => renderSupplies());
    mainContent.querySelectorAll("[data-supply-action]").forEach(button => button.addEventListener("click", () => handleSupplyAction(button)));
    renderSupplies();
}

function renderSupplies(errorMessage = "") {
    const body = document.getElementById("supplyTableBody");
    if (!body) return;
    if (errorMessage) {
        body.innerHTML = `<tr><td colspan="5" class="empty-state">${escapeHtml(errorMessage)}</td></tr>`;
        return;
    }
    const sort = document.getElementById("supplySort")?.value || "name-asc";
    const rows = supplies.filter(s => s.isActive).sort((a, b) => {
        if (sort === "stock-desc" || sort === "stock-asc") {
            const diff = Number(a.stockQuantity || 0) - Number(b.stockQuantity || 0);
            if (diff !== 0) return sort === "stock-desc" ? -diff : diff;
        }
        return String(a.name || "").localeCompare(String(b.name || ""), undefined, { sensitivity: "base" });
    });
    if (!rows.length) {
        body.innerHTML = `<tr><td colspan="5" class="empty-state"><i class="bi bi-box-seam"></i>No supplies match your search</td></tr>`;
        return;
    }
    body.innerHTML = rows.map(supply => {
        const qty = Math.round(Number(supply.stockQuantity || 0));
        const stockCell = supply.isActive && qty <= 0
            ? `<span class="text-danger fw-semibold">Out of stock</span>`
            : `${qty.toLocaleString()}`;
        return `
        <tr>
            <td class="tl">${escapeHtml(supply.name)}</td>
            <td class="tl col-text-wrap">${escapeHtml(supply.categoryName)}</td>
            <td class="tc col-text-nowrap">${stockCell}</td>
            <td class="tc col-text-nowrap">${escapeHtml(supply.unitOfMeasure)}</td>
            <td class="tc action-buttons">
                <button class="btn btn-sm btn-outline-secondary" type="button" data-supply-action="view" data-id="${supply.supplyId}" title="View supply details"><i class="bi bi-eye"></i></button>
                <button class="btn btn-sm btn-outline-success" type="button" data-supply-action="edit" data-id="${supply.supplyId}" title="Edit supply"><i class="bi bi-pencil"></i></button>
                <button class="btn btn-sm btn-outline-danger" type="button" data-supply-action="delete" data-id="${supply.supplyId}" title="Delete supply"><i class="bi bi-trash"></i></button>
                <button class="btn btn-sm btn-outline-warning has-label" type="button" data-supply-action="add_stock" data-id="${supply.supplyId}" title="Add stock"><i class="bi bi-arrow-up-square"></i><span>Add Stock</span></button>
            </td>
        </tr>
        `;
    }).join("");
    body.querySelectorAll("[data-supply-action]").forEach(button => button.addEventListener("click", () => handleSupplyAction(button)));
}

async function showSupplySummary(supplyId) {
    let supply;
    try {
        supply = await api.get(`/supplies/${supplyId}`);
    } catch (error) {
        showToast(error.message, "danger");
        return;
    }
    const fields = {
        supplySummaryTitle: supply.name,
        supplySummaryCategory: supply.categoryName || "—",
        supplySummaryUnit: supply.unitOfMeasure || "—",
        supplySummaryStock: supply.isActive ? `${Math.round(Number(supply.stockQuantity || 0)).toLocaleString()} ${escapeHtml(supply.unitOfMeasure)}` : "Archived"
    };
    Object.entries(fields).forEach(([id, value]) => {
        const el = document.getElementById(id);
        if (el) el.textContent = value;
    });
    new bootstrap.Modal(document.getElementById("supplySummaryModal")).show();
}

async function openSupplyModal(mode, id) {
    let record = null;
    try {
        if (id) record = await api.get(`/supplies/${id}`);
    } catch (error) {
        showToast(error.message, "danger");
        return;
    }
    const readOnly = mode === "view";
    const saveLabel = mode === "add" ? "Save supply" : "Save changes";
    const readonlyAttr = readOnly ? "readonly" : "";
    // Stock is adjusted through Add Stock; only the create form sets an initial quantity.
    const showStockField = mode !== "edit";
    const unitColClass = showStockField ? "col-md-4" : "col-md-6";
    const stockField = showStockField
        ? `<div class="col-md-4"><label class="form-label">Stock quantity${readOnly ? "" : ` <span class="text-danger">*</span>`}</label><input class="form-control" name="stockQuantity" type="number" min="0" step="0.001" value="${escapeHtml(record?.stockQuantity ?? "")}" required ${readonlyAttr}></div>`
        : "";
    document.getElementById("recordModalHost")?.remove();
    const host = document.createElement("div");
    host.id = "recordModalHost";
    host.innerHTML = `<div class="modal fade" id="supplyModal" tabindex="-1"><div class="modal-dialog modal-lg modal-dialog-centered"><div class="modal-content"><form id="supplyForm">
        <div class="modal-header"><div><p class="eyebrow mb-1">SUPPLY RECORD</p><h2 class="modal-title fs-5" id="supplyModalTitle">${mode === "add" ? "Add supply" : mode === "edit" ? "Edit supply" : "Supply details"}</h2></div><button class="btn-close" type="button" data-bs-dismiss="modal" aria-label="Close"></button></div>
        <div class="modal-body"><div class="row g-3">
            <div class="col-md-6"><label class="form-label">Category <span class="text-danger">*</span></label><input class="form-control" name="categoryName" type="text" maxlength="100" value="${escapeHtml(record?.categoryName || "")}" placeholder="e.g. Seeds, Fertilizer, Tools" required ${readonlyAttr}></div>
            <div class="col-md-6"><label class="form-label">Name <span class="text-danger">*</span></label><input class="form-control" name="name" type="text" maxlength="200" value="${escapeHtml(record?.name || "")}" required ${readonlyAttr}></div>
            <div class="${unitColClass}"><label class="form-label">Unit of measure <span class="text-danger">*</span></label><input class="form-control" name="unitOfMeasure" type="text" maxlength="30" value="${escapeHtml(record?.unitOfMeasure || "")}" placeholder="e.g. kg, bag, piece" required ${readonlyAttr}></div>
            ${stockField}
        </div></div>
        <div class="modal-footer"><button class="btn btn-light" type="button" data-bs-dismiss="modal">${readOnly ? "Close" : "Cancel"}</button>${readOnly ? "" : `<button class="btn btn-success" type="submit">${saveLabel}</button>`}</div>
    </form></div></div></div>`;
    document.body.appendChild(host);
    const supplyModal = new bootstrap.Modal(document.getElementById("supplyModal"));
    supplyModal.show();
    if (!readOnly) {
        document.getElementById("supplyForm").addEventListener("submit", async event => {
            event.preventDefault();
            const form = event.currentTarget;
            if (!form.reportValidity()) return;
            const payload = {
                categoryName: form.elements.categoryName.value.trim(),
                name: form.elements.name.value.trim(),
                unitOfMeasure: form.elements.unitOfMeasure.value.trim(),
            };
            if (mode === "add") payload.stockQuantity = Number(form.elements.stockQuantity.value);
            try {
                if (id) await api.put(`/supplies/${id}`, payload);
                else await api.post("/supplies", payload);
                supplyModal.hide();
                showToast(id ? "Supply updated." : "Supply added.");
                await renderSupplyModule();
            } catch (error) {
                showToast(error.message, "danger");
            }
        }, { once: true });
    }
    host.querySelector(".modal").addEventListener("hidden.bs.modal", () => host.remove());
}

async function handleSupplyAction(button) {
    const action = button.dataset.supplyAction;
    const id = Number(button.dataset.id);
    if (action === "print") { window.print(); return; }
    if (action === "add" || action === "edit") {
        await openSupplyModal(action, id);
        return;
    }
    if (action === "view") {
        await showSupplySummary(id);
        return;
    }
    if (action === "add_stock") {
        await openAddStockModal(id);
        return;
    }
    if (action === "delete") {
        if (!window.confirm("Delete this supply? Related distribution history will be retained.")) return;
        try {
            await api.delete(`/supplies/${id}`);
            showToast("Supply deleted.");
            await renderSupplyModule();
        } catch (error) {
            showToast(error.message, "danger");
        }
    }
}

async function openAddStockModal(supplyId) {
    const record = await api.get(`/supplies/${supplyId}`);
    document.getElementById("recordModalHost")?.remove();
    const host = document.createElement("div");
    host.id = "recordModalHost";
    host.innerHTML = `<div class="modal fade" id="supplyModal" tabindex="-1"><div class="modal-dialog modal-lg modal-dialog-centered"><div class="modal-content"><form id="supplyForm">
        <div class="modal-header"><div><p class="eyebrow mb-1">ADD STOCK</p><h2 class="modal-title fs-5" id="supplyModalTitle">Add stock — ${escapeHtml(record.name)}</h2></div><button class="btn-close" type="button" data-bs-dismiss="modal" aria-label="Close"></button></div>
        <div class="modal-body">
            <div class="mb-3">
                <label class="form-label">Current stock</label>
                <div class="display-6 text-success" id="addStockCurrent">${Math.round(Number(record.stockQuantity || 0)).toLocaleString()} ${escapeHtml(record.unitOfMeasure)}</div>
            </div>
            <div class="mb-3">
                <label class="form-label" for="addStockQty">Add quantity <span class="text-danger">*</span></label>
                <input class="form-control" id="addStockQty" name="stockQuantity" type="number" min="0.001" step="0.001" required autofocus>
            </div>
        </div>
        <div class="modal-footer"><button class="btn btn-light" type="button" data-bs-dismiss="modal">Cancel</button><button class="btn btn-success" type="submit">Add stock</button></div>
    </form></div></div></div>`;
    document.body.appendChild(host);
    const supplyModal = new bootstrap.Modal(document.getElementById("supplyModal"));
    supplyModal.show();
    document.getElementById("supplyForm").addEventListener("submit", async event => {
        event.preventDefault();
        const form = event.currentTarget;
        if (!form.reportValidity()) return;
        const payload = { stockQuantity: Number(form.elements.stockQuantity.value) };
        try {
            await api.post(`/supplies/${supplyId}/add-stock`, payload);
            supplyModal.hide();
            showToast("Stock added.");
            await renderSupplyModule();
        } catch (error) {
            showToast(error.message, "danger");
        }
    }, { once: true });
    host.querySelector(".modal").addEventListener("hidden.bs.modal", () => host.remove());
}

async function renderRiseReports() {
    const from=document.getElementById("riseReportFrom")?.value||"",to=document.getElementById("riseReportTo")?.value||"",barangay=document.getElementById("riseReportBarangay")?.value.trim()||"",type=document.getElementById("riseReportType")?.value.trim()||"",search=document.getElementById("riseReportSearch")?.value.trim().toLowerCase()||"";
    let rows=[],stats=null,error="";
    try{const p=new URLSearchParams();if(from)p.set("from",from);if(to)p.set("to",to);if(barangay)p.set("barangay",barangay);if(type)p.set("assistanceType",type);[rows,stats]=await Promise.all([api.get(`/reports/summary${p.size?`?${p}`:""}`),api.get("/reports/dashboard")]);}catch(e){error=e.message;}
    const filtered=rows.filter(r=>!search||[r.farmer,r.item,r.barangay,r.distributionId].some(x=>String(x??"").toLowerCase().includes(search)));
    const aggregate=(key)=>{const map=new Map();filtered.forEach(r=>{const k=r[key]||"Unspecified";const v=map.get(k)||{name:k,records:0,quantity:0};v.records++;v.quantity+=Number(r.quantity||0);map.set(k,v);});return [...map.values()].sort((a,b)=>String(a.name).localeCompare(String(b.name),undefined,{sensitivity:"base"}));};
    const byType=aggregate("item"), byBarangay=aggregate("barangay");
    mainContent.innerHTML=`<div class="view-header"><div><h2>Reports and Analytics</h2><p>Assistance distribution and utilization totals from the database.</p></div><button class="btn btn-outline-success" id="printRiseReport"><i class="bi bi-printer me-1"></i> Print / Save PDF</button></div>
    ${error?`<div class="alert alert-danger">${escapeHtml(error)}</div>`:""}
    ${stats?`<section class="stat-grid">${[["Registered farmers",stats.totalFarmers],["Received assistance",stats.receivedFarmers],["Not yet received",stats.notReceivedFarmers],["Distributed quantity",stats.totalDistributed],["Used",stats.quantityUsed],["Still stocked",stats.quantityStillStocked],["Not yet used",stats.quantityNotYetUsed],["Disposed",stats.quantityDisposed],["Unreported",stats.quantityUnreported]].map(x=>`<article class="stat-card glass-card"><div class="stat-card-top"><span>${x[0]}</span></div><strong>${Number(x[1]||0).toLocaleString()}</strong></article>`).join("")}</section>`:""}
    <section class="dashboard-grid analytics-grid">
      <article class="panel glass-panel"><div class="panel-header"><h3>Distribution by assistance type</h3></div><div class="table-responsive"><table class="table reports-type-table"><thead><tr><th class="tl">Assistance type</th><th class="tc">Records</th><th class="tc">Total quantity</th></tr></thead><tbody>${byType.length?byType.map(x=>`<tr><td class="tl">${escapeHtml(x.name)}</td><td class="tc">${x.records}</td><td class="tc">${x.quantity.toLocaleString()}</td></tr>`).join(""):`<tr><td colspan="3" class="empty-state">No records</td></tr>`}</tbody></table></div></article>
      <article class="panel glass-panel"><div class="panel-header"><h3>Distribution by barangay</h3></div><div class="table-responsive"><table class="table reports-barangay-table"><thead><tr><th class="tl">Barangay</th><th class="tc">Records</th><th class="tc">Total quantity</th></tr></thead><tbody>${byBarangay.length?byBarangay.map(x=>`<tr><td class="tl">${escapeHtml(x.name)}</td><td class="tc">${x.records}</td><td class="tc">${x.quantity.toLocaleString()}</td></tr>`).join(""):`<tr><td colspan="3" class="empty-state">No records</td></tr>`}</tbody></table></div></article>
    </section>
    <section class="content-panel"><form class="filter-bar" id="riseReportFilter">
      <input id="riseReportFrom" class="form-control" type="date" value="${escapeHtml(from)}"><input id="riseReportTo" class="form-control" type="date" value="${escapeHtml(to)}">
      <input id="riseReportBarangay" class="form-control" value="${escapeHtml(barangay)}" placeholder="Barangay"><input id="riseReportType" class="form-control" value="${escapeHtml(type)}" placeholder="Assistance type">
      <input id="riseReportSearch" class="form-control" value="${escapeHtml(search)}" placeholder="Search farmer, item, barangay"><button class="btn btn-success">Apply filters</button>
    </form><div class="table-responsive"><table class="table reports-main-table"><thead><tr><th class="tc">Date</th><th class="tl">Farmer</th><th class="tl">Barangay</th><th class="tl">Assistance type</th><th class="tc">Distributed</th><th class="tc">Used</th><th class="tc">Stocked</th><th class="tc">Not yet used</th><th class="tc">Disposed</th></tr></thead><tbody>
    ${filtered.length?filtered.map(r=>`<tr><td class="tc">${escapeHtml(r.distributionDate)}</td><td class="tl">${escapeHtml(r.farmer)}</td><td class="tl">${escapeHtml(r.barangay||"—")}</td><td class="tl">${escapeHtml(r.item)}</td><td class="tc">${r.quantity} ${escapeHtml(r.unit)}</td><td class="tc">${r.used??"—"}</td><td class="tc">${r.stocked??"—"}</td><td class="tc">${r.notYetUsed??"—"}</td><td class="tc">${r.disposed??"—"}</td></tr>`).join(""):`<tr><td colspan="9" class="empty-state">No matching assistance records.</td></tr>`}
    </tbody></table></div></section>`;
    document.getElementById("riseReportFilter")?.addEventListener("submit",e=>{e.preventDefault();renderRiseReports();});
    document.getElementById("printRiseReport")?.addEventListener("click",()=>window.print());
}

function bindDynamicActions() {
    mainContent.querySelectorAll("[data-action]").forEach(button => button.addEventListener("click", handleDynamicAction));
    mainContent.querySelectorAll("[data-view-link]").forEach(button => button.addEventListener("click", () => setView(button.dataset.viewLink)));
}

function handleDynamicAction(event) {
    const button = event.currentTarget;
    const action = button.dataset.action;
    if (action === "register") openCreateForm();
    if (action === "profile") showProfile(farmerFromButton(button));
    if (action === "edit") openEditForm(farmerFromButton(button));
    if (action === "archive") archiveFarmer(farmerFromButton(button));
    if (action === "print") window.print();
}

function farmerFromButton(button) {
    return farmers.find(farmer => farmer.farmerId === Number(button.dataset.id));
}

function openCreateForm() {
    document.getElementById("farmerForm").reset();
    document.getElementById("farmerId").value = "";
    document.getElementById("farmerModalTitle").textContent = "Register farmer";
    document.getElementById("saveFarmerButton").textContent = "Save farmer";
    farmerModal.show();
}

function openEditForm(farmer) {
    if (!farmer) return;
    document.getElementById("farmerId").value = farmer.farmerId;
    document.getElementById("lastName").value = farmer.lastName || farmer.fullName || "";
    document.getElementById("firstName").value = farmer.firstName || "";
    document.getElementById("middleName").value = farmer.middleName || "";
    document.getElementById("contactNumber").value = farmer.contactNumber;
    document.getElementById("barangay").value = farmer.barangay || "";
    document.getElementById("municipality").value = farmer.municipality || "";
    document.getElementById("province").value = farmer.province || "";
    document.getElementById("farmerModalTitle").textContent = "Edit farmer";
    document.getElementById("saveFarmerButton").textContent = "Save changes";
    farmerModal.show();
}

function showProfile(farmer) {
    if (!farmer) return;
    const fields = { profileName:farmer.fullName, profileContact:farmer.contactNumber,
        profileBarangay:farmer.barangay || "—", profileMunicipality:farmer.municipality || "—",
        profileProvince:farmer.province || "—" };
    Object.entries(fields).forEach(([id,value]) => {
        const el=document.getElementById(id); if(el) el.textContent=value;
    });
    profileModal.show();
}

async function archiveFarmer(farmer) {
    if (!farmer || !window.confirm(`Archive ${farmer.fullName}? The record will be retained.`)) return;
    try {
        await api.delete(`${FARMERS_ENDPOINT}/${farmer.farmerId}`);
        await loadFarmers();
        showToast("Farmer archived.");
    } catch (error) { showToast(error.message, "danger"); }
}

document.getElementById("farmerForm").addEventListener("submit", async event => {
    event.preventDefault();
    const form = event.currentTarget;
    if (!form.reportValidity()) return;
    const id = document.getElementById("farmerId").value;
    const payload = {
        firstName: document.getElementById("firstName").value.trim() || null,
        lastName: document.getElementById("lastName").value.trim() || null,
        middleName: document.getElementById("middleName").value.trim() || null,
        contactNumber: document.getElementById("contactNumber").value.trim(),
        barangay: document.getElementById("barangay").value.trim() || null,
        municipality: document.getElementById("municipality").value.trim() || null,
        province: document.getElementById("province").value.trim() || null,
    };
    try {
        if (id) await api.put(`${FARMERS_ENDPOINT}/${id}`, payload);
        else await api.post(FARMERS_ENDPOINT, payload);
        farmerModal.hide();
        await loadFarmers();
        showToast(id ? "Farmer information updated." : "Farmer registered.");
    } catch (error) { showToast(error.message, "danger"); }
});

document.querySelectorAll(".nav-item[data-view]").forEach(item => item.addEventListener("click", () => setView(item.dataset.view)));
document.getElementById("globalSearch").addEventListener("keydown", event => {
    if (event.key === "Enter") { setView("farmers"); setTimeout(() => { document.getElementById("searchInput").value = event.currentTarget.value; loadFarmers(); }, 0); }
});
function syncHeaderAddButton() {
    const button = document.getElementById("addRecordButton");
    const canAdd = currentView === "dashboard";
    button.hidden = !canAdd;
    if (!canAdd) return;
    const label = currentView === "distribution" ? "New distribution" : currentView === "supplies" ? "Add supply" : "Register farmer";
    button.setAttribute("aria-label", label);
    button.title = label;
    button.querySelector(".d-none.d-md-inline").textContent = label;
}

async function openHeaderCreateForm() {
    if (currentView === "dashboard" || currentView === "farmers") openCreateForm();
    else if (currentView === "distribution") await openDistributionModal("add");
    else if (currentView === "supplies") await openSupplyModal("add");
}

document.getElementById("addRecordButton").addEventListener("click", openHeaderCreateForm);
document.getElementById("notificationButton").hidden = true;

function updateSidebarToggleIcon(isCollapsed) {
    const btn = document.getElementById("mobileMenu");
    if (!btn) return;
    btn.setAttribute("aria-expanded", !isCollapsed);
    btn.title = isCollapsed ? "Show sidebar navigation" : "Hide sidebar navigation";
    const icon = btn.querySelector("i");
    if (icon) {
        icon.className = isCollapsed ? "bi bi-layout-sidebar" : "bi bi-layout-sidebar-inset";
    }
}

function toggleSidebar() {
    if (window.innerWidth <= 991.98) {
        const isOpen = document.getElementById("sidebar").classList.toggle("open");
        document.getElementById("sidebarBackdrop").classList.toggle("show", isOpen);
    } else {
        const isCollapsed = document.body.classList.toggle("sidebar-collapsed");
        localStorage.setItem("rise_sidebar_collapsed", isCollapsed ? "true" : "false");
        updateSidebarToggleIcon(isCollapsed);
    }
}

function closeMobileNav() {
    document.getElementById("sidebar").classList.remove("open");
    document.getElementById("sidebarBackdrop").classList.remove("show");
}

function initSidebarState() {
    if (window.innerWidth > 991.98) {
        const saved = localStorage.getItem("rise_sidebar_collapsed");
        if (saved === "true") {
            document.body.classList.add("sidebar-collapsed");
            updateSidebarToggleIcon(true);
        } else {
            updateSidebarToggleIcon(false);
        }
    }
}

document.getElementById("mobileMenu").addEventListener("click", toggleSidebar);
document.getElementById("sidebarClose").addEventListener("click", () => {
    if (window.innerWidth <= 991.98) closeMobileNav();
    else toggleSidebar();
});
document.getElementById("sidebarBackdrop").addEventListener("click", closeMobileNav);
document.getElementById("sidebarCollapseBtn")?.addEventListener("click", toggleSidebar);

initSidebarState();

syncHeaderAddButton();
renderRiseDashboard();
document.getElementById("mainContent").classList.add("view-ready");
