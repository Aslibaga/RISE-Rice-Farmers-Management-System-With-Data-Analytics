const API_BASE_URL = (window.RISE_API_BASE || "http://localhost:5000") + "/api";
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
    const isSuccess = type === "success";
    toast.className = `toast align-items-center border-0 rise-toast ${isSuccess ? "rise-toast-success" : "rise-toast-error"}`;
    toast.setAttribute("role", "status");
    toast.innerHTML = `<div class="d-flex align-items-center gap-2"><span class="rise-toast-icon"><i class="bi ${isSuccess ? "bi-check-circle-fill" : "bi-exclamation-triangle-fill"}"></i></span><div class="toast-body fw-semibold">${escapeHtml(message)}</div><button type="button" class="btn-close btn-close-white me-2 m-auto" data-bs-dismiss="toast" aria-label="Close"></button></div>`;
    container.appendChild(toast);
    new bootstrap.Toast(toast, { delay: 5000 }).show();
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
    updateTopbarSeason();
    document.querySelectorAll(".nav-item[data-view]").forEach(item => item.classList.toggle("active", item.dataset.view === view));
    const titles = { dashboard: "Dashboard", farmers: "Farmer Records", distribution: "Assistance Distribution",
        supplies: "Supply Management", utilization: "Utilization Monitoring", history: "History", reports: "Reports and Analytics" };
    const pageTitle = document.getElementById("pageTitle");
    if (pageTitle) pageTitle.textContent = titles[view] || "RISE";
    const breadcrumb = document.getElementById("breadcrumbCurrent");
    if (breadcrumb) breadcrumb.textContent = titles[view] || "RISE";
    closeMobileNav();
    // Fade-out the current view before swapping content.
    const mainContent = document.getElementById("mainContent");
    mainContent.classList.remove("view-ready");
    if (view === "dashboard") renderRiseDashboard();
    else if (view === "farmers") { renderFarmerView(); loadFarmers(); }
    else if (view === "distribution") renderDistributionModule();
    else if (view === "supplies") renderSupplyModule();
    else if (view === "utilization") renderUtilizationModule();
    else if (view === "history") renderHistoryView();
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
            <div class="d-flex gap-2"><div class="dropdown"><button class="btn btn-outline-success dropdown-toggle" type="button" data-bs-toggle="dropdown" aria-expanded="false"><i class="bi bi-file-earmark-excel me-1"></i> Excel</button><ul class="dropdown-menu dropdown-menu-dark"><li><button class="dropdown-item" type="button" data-excel="farmers-export"><i class="bi bi-file-earmark-arrow-down me-2"></i>Export to Excel</button></li></ul></div><button class="btn btn-back" type="button" data-action="print"><i class="bi bi-printer me-1"></i> Print</button><button class="btn btn-success" type="button" data-action="register"><i class="bi bi-plus-lg me-1"></i> Register farmer</button></div>
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

    window.RISEPrint?.setContext("farmers", () => ({
        title: "Farmer Information Record",
        subtitle: "Registered rice farmers and their information",
        meta: [
            { label: "Report type", value: "Farmer registry" }
        ],
        blocks: [{
            title: "Farmer registration details",
            columns: [
                { key: "lastName", label: "Last name" },
                { key: "firstName", label: "First name" },
                { key: "middleName", label: "Middle name" },
                { key: "contactNumber", label: "Contact number" },
                { key: "barangay", label: "Barangay" },
                { key: "municipality", label: "Municipality" },
                { key: "province", label: "Province" },
                { key: "status", label: "Status", align: "center" }
            ],
            rows: farmers.map(farmer => ({
                lastName: farmer.lastName || farmer.fullName || "—",
                firstName: farmer.firstName || "—",
                middleName: farmer.middleName || "—",
                contactNumber: farmer.contactNumber || "—",
                barangay: farmer.barangay || "—",
                municipality: farmer.municipality || "—",
                province: farmer.province || "—",
                status: farmer.isActive ? "Active" : "Archived"
            }))
        }]
    }));
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
                        <col class="col-lastname"><col class="col-firstname"><col class="col-middlename"><col class="col-barangay"><col class="col-municipality"><col class="col-province"><col class="col-distributions"><col class="col-dist-date"><col class="col-actions"></colgroup>
                    <thead><tr>
                        <th class="tl">Last Name</th><th class="tl">First Name</th><th class="tl">Middle Name</th>
                        <th class="tl">Barangay</th><th class="tl">Municipality</th><th class="tl">Province</th>
                        <th class="tc">Distributions</th><th class="tc">Last Distribution</th>
                        <th class="tc">Actions</th>
                    </tr></thead>
                    <tbody id="distributionTableBody"><tr><td colspan="9" class="empty-state">Loading assistance distributions...</td></tr></tbody>
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
        body.innerHTML = `<tr><td colspan="9" class="empty-state">${escapeHtml(errorMessage)}</td></tr>`;
        return;
    }
    if (!summaries.length) {
        body.innerHTML = `<tr><td colspan="9" class="empty-state"><i class="bi bi-inbox"></i>No matching assistance distributions</td></tr>`;
        return;
    }
    body.innerHTML = summaries.map(summary => {
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
    const categories = [...new Set(historyItems.map(item => item.categoryName).filter(Boolean))].sort(compareCategoriesLast);
    let visibleHistoryRows = [];

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
        visibleHistoryRows = items;
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
                ${stats.map(stat => `<article class="stat-card stat-card-compact glass-card"><div class="stat-card-top"><span>${escapeHtml(stat.label)}</span></div><strong>${escapeHtml(stat.value)}</strong></article>`).join("")}
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
    document.getElementById("farmerHistoryPrint")?.addEventListener("click", () => window.RISEPrint?.print("farmerHistory"));
    window.RISEPrint?.setContext("farmerHistory", () => ({
        title: "Supply Distribution Record",
        subtitle: farmerName,
        featured: farmerName,
        featuredLabel: "Farmer",
        meta: [
            { label: "Farmer", value: farmerName },
            { label: "Address", value: [farmer?.barangay || history?.barangay, farmer?.municipality || history?.municipality, farmer?.province || history?.province].filter(Boolean).join(", ") }
        ],
        blocks: [{
            title: "Individual assistance transactions",
            columns: [
                { key: "date", label: "Distribution date" },
                { key: "supply", label: "Supply item" },
                { key: "category", label: "Category" },
                { key: "quantity", label: "Quantity", align: "right" },
                { key: "unit", label: "Unit", align: "center" }
            ],
            rows: visibleHistoryRows.map(item => ({
                date: item.distributionDate || "—",
                supply: item.supplyName || "—",
                category: item.categoryName || "—",
                quantity: Number(item.quantity || 0).toLocaleString(),
                unit: item.unitOfMeasure || "—"
            })),
            totals: [
                { value: `Total Items: ${visibleHistoryRows.length}`, colspan: 5 }
            ]
        }],
        signatures: ["Prepared by", "Received by"]
    }));

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
    const currentDate = new Date();
    const defaultTargetMonth = String(currentDate.getMonth() + 1).padStart(2, "0");
    const defaultTargetYear = currentDate.getFullYear();
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
            <div class="table-responsive"><table class="table distribution-items-table"><colgroup><col class="col-di-item"><col class="col-di-category"><col class="col-di-qty"><col class="col-di-unit"><col class="col-di-target"></colgroup><thead><tr><th>Assistance item</th><th>Category</th><th>Quantity</th><th>Unit</th><th>Target monitoring date</th></tr></thead><tbody>
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
            <div class="col-12"><div class="d-flex justify-content-between align-items-center mb-2"><h3 class="h6 mb-0">Assistance items</h3><button class="btn btn-sm btn-outline-success" type="button" id="addDistributionItem">Add item</button></div><div class="table-responsive"><table class="table distribution-items-table"><colgroup><col class="col-di-item"><col class="col-di-category"><col class="col-di-qty"><col class="col-di-unit"><col class="col-di-target"><col class="col-di-remove"></colgroup><thead><tr><th>Assistance item</th><th>Category</th><th>Quantity / Available stock</th><th>Unit</th><th>Target monitoring date</th><th></th></tr></thead><tbody id="distributionItems"></tbody></table></div></div>
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
let reportCharts = [];
function renderHistoryView() {
    mainContent.innerHTML = "";
    if (window.RISEDistributionSchedule) window.RISEDistributionSchedule.render();
    else mainContent.innerHTML = `<div class="alert alert-danger">History module is unavailable.</div>`;
}

function openQuantityDetailsModal(title, rows, quantityLabel) {
    const grouped = new Map();
    rows.forEach(row => {
        const category = row.category || "—";
        const unit = row.unit || "—";
        const key = `${category}||${unit}`;
        if (!grouped.has(key)) grouped.set(key, { category, unit, quantity: 0, date: row.date || "—", items: new Set() });
        const group = grouped.get(key);
        group.quantity += Number(row.quantity || 0);
        group.items.add(row.item || "—");
        if (String(row.date || "") > String(group.date)) group.date = row.date;
    });
    const summary = [...grouped.values()].sort((a, b) => compareCategoriesLast(a.category, b.category));
    document.getElementById("quantityDetailsHost")?.remove();
    const host = document.createElement("div");
    host.id = "quantityDetailsHost";
    host.innerHTML = `<div class="modal fade" tabindex="-1"><div class="modal-dialog modal-xl modal-dialog-centered"><div class="modal-content">
        <div class="modal-header"><h5 class="modal-title">${escapeHtml(title)}</h5><button class="btn-close" type="button" data-bs-dismiss="modal" aria-label="Close"></button></div>
        <div class="modal-body"><div class="table-responsive"><table class="table align-middle"><thead><tr><th class="tl">Category</th><th class="tc">${escapeHtml(quantityLabel)}</th><th class="tc">Unit</th><th class="tl">Date recorded</th></tr></thead><tbody>
            ${summary.length ? summary.map(group => `<tr><td class="tl col-text-wrap">${escapeHtml(group.category)}</td><td class="tc"><strong>${group.quantity.toLocaleString()}</strong></td><td class="tc">${escapeHtml(group.unit)}</td><td class="tl">${escapeHtml(group.date)}</td></tr>`).join("") : `<tr><td colspan="4" class="empty-state">No records found.</td></tr>`}
        </tbody></table></div></div>
        <div class="modal-footer"><button class="btn btn-light" type="button" data-bs-dismiss="modal">Close</button></div>
    </div></div></div>`;
    document.body.appendChild(host);
    const modal = new bootstrap.Modal(host.querySelector(".modal"));
    modal.show();
    host.querySelector(".modal").addEventListener("hidden.bs.modal", () => host.remove());
}

async function openDistributedQuantityModal() {
    try {
        const [distributions, supplies, seasonInfo] = await Promise.all([api.get("/distributions"), api.get("/supplies"), api.get("/monitoring-season").catch(() => null)]);
        const startedAt = seasonInfo?.startedAtUtc ? new Date(seasonInfo.startedAtUtc).getTime() : null;
        const categories = new Map((supplies || []).map(supply => [Number(supply.supplyId), supply.categoryName || "—"]));
        const rows = (distributions || []).filter(distribution =>
            !startedAt || parseApiDate(distribution.createdAtUtc) >= startedAt)
            .flatMap(distribution => (distribution.items || []).map(item => ({
            item: item.supplyName || "—",
            category: categories.get(Number(item.supplyId)) || "—",
            quantity: item.quantity,
            unit: item.unitOfMeasure || "—",
            date: distribution.distributionDate || "—"
        }))).sort((a, b) => String(b.date).localeCompare(String(a.date)));
        openQuantityDetailsModal("Total Quantity Distributed", rows, "Quantity distributed");
    } catch (error) {
        showToast(error.message, "danger");
    }
}

async function loadSeasonUtilizationRows() {
    const [utilization, distributions, seasonInfo] = await Promise.all([
        api.get("/utilization"), api.get("/distributions"), api.get("/monitoring-season").catch(() => null)
    ]);
    const startedAt = seasonInfo?.startedAtUtc ? new Date(seasonInfo.startedAtUtc).getTime() : null;
    const created = new Map((distributions || []).map(row => [Number(row.distributionId), row.createdAtUtc]));
    const dates = new Map((distributions || []).map(row => [Number(row.distributionId), row.distributionDate]));
    return (utilization || []).filter(row => {
        const value = created.get(Number(row.distributionId));
        return !startedAt || (value && parseApiDate(value) >= startedAt);
    }).map(row => ({
        item: row.itemName || "—",
        category: row.categoryName || "—",
        unit: row.unit || "—",
        date: dates.get(Number(row.distributionId)) || "—",
        used: row.quantityUsed,
        stocked: row.quantityStillStocked,
        disposed: row.quantityDisposed
    })).sort((a, b) => String(b.date).localeCompare(String(a.date)));
}

async function openUsedQuantityModal() {
    try {
        const rows = (await loadSeasonUtilizationRows())
            .filter(row => Number(row.used || 0) > 0)
            .map(row => ({ item: row.item, category: row.category, quantity: row.used, unit: row.unit, date: row.date }));
        openQuantityDetailsModal("Total Quantity Used", rows, "Quantity used");
    } catch (error) { showToast(error.message, "danger"); }
}

async function openStockedQuantityModal() {
    try {
        const rows = (await loadSeasonUtilizationRows())
            .filter(row => Number(row.stocked || 0) > 0)
            .map(row => ({ item: row.item, category: row.category, quantity: row.stocked, unit: row.unit, date: row.date }));
        openQuantityDetailsModal("Still Stocked", rows, "Quantity still stocked");
    } catch (error) { showToast(error.message, "danger"); }
}

async function openDisposedQuantityModal() {
    try {
        const rows = (await loadSeasonUtilizationRows())
            .filter(row => Number(row.disposed || 0) > 0)
            .map(row => ({ item: row.item, category: row.category, quantity: row.disposed, unit: row.unit, date: row.date }));
        openQuantityDetailsModal("Total Quantity Disposed", rows, "Quantity disposed");
    } catch (error) { showToast(error.message, "danger"); }
}

function openFarmerListModal(title, rows) {
    document.getElementById("farmerListModalHost")?.remove();
    const host = document.createElement("div");
    host.id = "farmerListModalHost";
    host.innerHTML = `<div class="modal fade" tabindex="-1"><div class="modal-dialog modal-xl modal-dialog-centered"><div class="modal-content">
        <div class="modal-header"><h5 class="modal-title">${escapeHtml(title)}</h5><button class="btn-close" type="button" data-bs-dismiss="modal" aria-label="Close"></button></div>
        <div class="modal-body"><div class="table-responsive"><table class="table align-middle"><thead><tr><th class="tl">Last Name</th><th class="tl">First Name</th><th class="tl">Middle Name</th><th class="tl">Barangay</th><th class="tl">Municipality</th><th class="tl">Province</th></tr></thead><tbody>
            ${rows.length ? rows.map(row => `<tr><td class="tl">${escapeHtml(row.lastName)}</td><td class="tl">${escapeHtml(row.firstName)}</td><td class="tl">${escapeHtml(row.middleName)}</td><td class="tl col-text-wrap">${escapeHtml(row.barangay)}</td><td class="tl col-text-wrap">${escapeHtml(row.municipality)}</td><td class="tl col-text-wrap">${escapeHtml(row.province)}</td></tr>`).join("") : `<tr><td colspan="6" class="empty-state">No farmers found.</td></tr>`}
        </tbody></table></div></div>
        <div class="modal-footer"><button class="btn btn-light" type="button" data-bs-dismiss="modal">Close</button></div>
    </div></div></div>`;
    document.body.appendChild(host);
    const modal = new bootstrap.Modal(host.querySelector(".modal"));
    modal.show();
    host.querySelector(".modal").addEventListener("hidden.bs.modal", () => host.remove());
}

async function openNotReceivedFarmersModal() {
    try {
        const [farmers, distributions, seasonInfo] = await Promise.all([api.get("/farmers"), api.get("/distributions"), api.get("/monitoring-season")]);
        const startedAt = seasonInfo?.startedAtUtc ? new Date(seasonInfo.startedAtUtc).getTime() : null;
        const inSeason = row => !startedAt || parseApiDate(row.createdAtUtc) >= startedAt;
        const received = new Set((distributions || [])
            .filter(row => row.status === "Distributed" && inSeason(row))
            .map(row => Number(row.farmerId)));
        const rows = (farmers || []).filter(farmer => !received.has(Number(farmer.farmerId))).map(farmer => ({
            lastName: farmer.lastName || "—",
            firstName: farmer.firstName || "—",
            middleName: farmer.middleName || "—",
            barangay: farmer.barangay || "—",
            municipality: farmer.municipality || "—",
            province: farmer.province || "—"
        })).sort((a, b) => String(a.lastName).localeCompare(String(b.lastName)) || String(a.firstName).localeCompare(String(b.firstName)));
        openFarmerListModal("Farmers Not Yet Received Assistance", rows);
    } catch (error) {
        showToast(error.message, "danger");
    }
}

function distributedPercentage(distributed, supplies) {
    const remaining = (supplies || []).reduce((sum, supply) => sum + Number(supply.stockQuantity || 0), 0);
    const total = Number(distributed || 0) + remaining;
    return total > 0 ? (Number(distributed || 0) / total) * 100 : 0;
}

async function renderRiseDashboard() {
    mainContent.innerHTML = `<div class="view-header"><div><p class="eyebrow">RICE FARMER ASSISTANCE</p><h2>Dashboard</h2><p>Live totals from farmer, distribution and utilization records.</p></div></div><div class="content-panel p-5">Loading dashboard data…</div>`;
    try {
        const d = await api.get("/reports/dashboard");
        let seasonStartedAt = null;
        let allDistributions = [];
        try {
            const seasonInfo = await api.get("/monitoring-season");
            if (seasonInfo?.startedAtUtc) seasonStartedAt = new Date(seasonInfo.startedAtUtc).getTime();
        } catch {}
        try { allDistributions = await api.get("/distributions") || []; } catch {}
        let allSupplies = [];
        try { allSupplies = await api.get("/supplies") || []; } catch {}
        const supplyCategory = new Map(allSupplies.map(supply => [Number(supply.supplyId), supply.categoryName || classifyAssistance(supply.name)]));
        const inSeason = row => !seasonStartedAt || new Date(parseApiDate(row.createdAtUtc)) >= seasonStartedAt;
        const distributedByUnit = [];
        allDistributions.filter(inSeason).forEach(distribution =>
            (distribution.items || []).forEach(item => distributedByUnit.push([
                supplyCategory.get(Number(item.supplyId)) || classifyAssistance(item.supplyName),
                item.unitOfMeasure, item.quantity])));
        const usedByUnit = [], stockedByUnit = [], disposedByUnit = [];
        const seasonDistributionIds = new Set(allDistributions.filter(inSeason).map(row => Number(row.distributionId)));
        let toMonitorFarmers = 0, overdueFarmers = 0, seasonStocked = 0, seasonDisposed = 0, seasonUsed = 0, seasonUnreported = 0, seasonRows = [];
        try {
            const utilizationRows = await api.get("/utilization") || [];
            const today = new Date().toISOString().slice(0, 10);
            seasonRows = utilizationRows.filter(row => seasonDistributionIds.has(Number(row.distributionId)));
            toMonitorFarmers = new Set(seasonRows
                .filter(row => row.utilizationMonitoringId === 0 || Number(row.unreportedQuantity) > 0)
                .map(row => Number(row.farmerId))).size;
            overdueFarmers = new Set(seasonRows
                .filter(row => row.utilizationMonitoringId === 0 && row.targetMonitoringDate &&
                    String(row.targetMonitoringDate) < today)
                .map(row => Number(row.farmerId))).size;
            seasonRows.forEach(row => {
                stockedByUnit.push([row.categoryName || classifyAssistance(row.itemName), row.unit, row.quantityStillStocked]);
                disposedByUnit.push([row.categoryName || classifyAssistance(row.itemName), row.unit, row.quantityDisposed]);
                usedByUnit.push([row.categoryName || classifyAssistance(row.itemName), row.unit, row.quantityUsed]);
                seasonStocked += Number(row.quantityStillStocked || 0);
                seasonDisposed += Number(row.quantityDisposed || 0);
                seasonUsed += Number(row.quantityUsed || 0);
            });
            seasonUnreported = seasonRows.reduce((sum, row) => sum + Number(row.unreportedQuantity || 0), 0);
        } catch {}
        const seasonTypeMap = new Map();
        const seasonBarangayMap = new Map();
        allDistributions.filter(inSeason).forEach(distribution => {
            (distribution.items || []).forEach(item => {
                const category = supplyCategory.get(Number(item.supplyId)) || classifyAssistance(item.supplyName);
                const key = `${category} (${item.unitOfMeasure || "unit"})`;
                seasonTypeMap.set(key, (seasonTypeMap.get(key) || 0) + Number(item.quantity || 0));
            });
            seasonBarangayMap.set(distribution.barangay || "—", (seasonBarangayMap.get(distribution.barangay || "—") || 0) + 1);
        });
        const cardIcons = ["bi-people", "bi-clipboard-check", "bi-box-seam", "bi-check2-circle", "bi-boxes", "bi-trash3"];
        const quantityEntries = [...distributedByUnit, ...usedByUnit, ...stockedByUnit, ...disposedByUnit];
        const quantityCategories = [...new Set(quantityEntries.map(entry => entry[0] || "Other"))].sort(compareCategoriesLast);
        const cards = [
            { label: "Total Farmers Beneficiaries", value: d.totalFarmers, button: "View Farmers", action: "farmers" },
            { label: "To Monitor", value: toMonitorFarmers, button: "Continue monitor", action: "utilization", badge: overdueFarmers ? `<i class="bi bi-exclamation-triangle me-1"></i>${overdueFarmers}` : "" },
            { label: "Total Quantity distributed", display: formatQuantityByUnit(distributedByUnit, quantityCategories), button: "View details", action: "distributed" },
            { label: "Total quantity used", display: formatQuantityByUnit(usedByUnit, quantityCategories), button: "View", action: "used" },
            { label: "Still stocked", display: formatQuantityByUnit(stockedByUnit, quantityCategories), button: "View supply", action: "supplies" },
            { label: "Disposed", display: formatQuantityByUnit(disposedByUnit, quantityCategories), button: "View", action: "disposed" }
        ];
        mainContent.innerHTML = `<div class="view-header"><div><p class="eyebrow">RICE FARMER ASSISTANCE</p><h2>Dashboard</h2><p>Monitor your registered farms and farming activities.</p></div></div>
        <section class="stat-grid card-duo">${cards.map((card, index) => `<article class="stat-card glass-card"><div class="stat-card-top"><span>${escapeHtml(card.label)}</span><span class="stat-card-icon" aria-hidden="true"><i class="bi ${cardIcons[index] || "bi-bar-chart"}"></i></span></div>${card.display ? `<div class="stat-card-value stat-card-value-detail">${card.display}</div>` : `<strong class="stat-card-value stat-card-number">${Number(card.value || 0).toLocaleString()}</strong>`}${card.button || card.badge ? `<div class="stat-card-foot">${card.badge ? `<span class="stat-card-badge" title="Overdue farmers">${card.badge}</span>` : ""}${card.button ? `<button class="stat-card-action" type="button" data-dashboard-card="${card.action}">${escapeHtml(card.button)}</button>` : ""}</div>` : ""}</article>`).join("")}</section>
        <section class="dashboard-grid analytics-grid">
          <article class="panel glass-panel"><div class="panel-header"><h3>Assistance status</h3><button class="btn btn-sm btn-outline-success" type="button" id="viewNotReceivedBtn">View not yet received</button></div><div class="chart-wrap"><canvas id="riseFarmersChart"></canvas></div></article>
          <article class="panel glass-panel"><div class="panel-header"><h3>Distribution by assistance type</h3></div><div class="chart-wrap"><canvas id="riseTypeChart"></canvas></div></article>
          <article class="panel glass-panel"><div class="panel-header"><h3>Farmers assisted by barangay</h3></div><div class="chart-wrap"><canvas id="riseBarangayChart"></canvas></div></article>
        </section>`;
        riseCharts.forEach(c => c.destroy()); riseCharts = [];
        const chartPalette = ["#2f7d55", "#e5b33d", "#6f9e7f", "#b9862f", "#4b6b57", "#9db9a2"];
        const make=(id,type,labels,data)=>{const el=document.getElementById(id);if(!el)return;const colors=labels.map((_,i)=>chartPalette[i%chartPalette.length]);riseCharts.push(new Chart(el,{type,data:{labels,datasets:[{label:"Count / quantity",data,backgroundColor:type==="bar"?colors.map(color=>`${color}cc`):colors,borderColor:colors,borderWidth:1,borderRadius:type==="bar"?5:0}]},options:{responsive:true,maintainAspectRatio:false,plugins:{legend:{display:type!=="bar",position:"bottom",labels:{color:"#d7e2d8",boxWidth:10,padding:10,font:{size:11}}}}}}));};
        const seasonReceivedIds = new Set(allDistributions
            .filter(row => inSeason(row) && (row.items || []).length > 0)
            .map(row => Number(row.farmerId)));
        make("riseFarmersChart","doughnut",["Received","Not yet received"],
            [seasonReceivedIds.size, Math.max(0, Number(d.totalFarmers || 0) - seasonReceivedIds.size)]);
        const typeEntries = [...seasonTypeMap.entries()].sort((a, b) => compareCategoriesLast(a[0], b[0]));
        make("riseTypeChart","bar",typeEntries.map(entry => entry[0]),typeEntries.map(entry => entry[1]));
        make("riseBarangayChart","bar",[...seasonBarangayMap.keys()],[...seasonBarangayMap.values()]);
        document.getElementById("viewNotReceivedBtn")?.addEventListener("click", () => openNotReceivedFarmersModal());
        mainContent.querySelectorAll("[data-dashboard-card]").forEach(button => button.addEventListener("click", () => {
            const action = button.dataset.dashboardCard;
            if (action === "distributed") openDistributedQuantityModal();
            else if (action === "used") openUsedQuantityModal();
            else if (action === "disposed") openDisposedQuantityModal();
            else if (action === "notreceived") openNotReceivedFarmersModal();
            else setView(action);
        }));
    } catch(error) {
        mainContent.innerHTML=`<div class="view-header"><div><h2>Dashboard</h2><p>Live database overview</p></div></div><div class="alert alert-danger">${escapeHtml(error.message)}</div>`;
    }
}

let utilRows = [];
let utilFarmers = [];
let utilDistributionDates = new Map();
let utilDistributionCreated = new Map();
let utilTargetMonths = 3;
let utilArchivedItemIds = new Set();
let utilActiveTab = "to-monitor";
const utilTabFilters = {
    "to-monitor": { search: "", barangay: "", status: "" },
    "monitored": { search: "", barangay: "", status: "" }
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
    const itemCategories = [...new Set(items.map(item => item.categoryName).filter(Boolean))].sort(compareCategoriesLast);
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
            <thead><tr><th class="tl">Distributed supply</th><th class="tc">Received</th>${pendingOnly ? `<th class="tc">Received date</th>` : `<th class="tc">Used</th><th class="tc">Stocked</th><th class="tc">Disposed</th><th class="tc">Monitored</th>`}<th class="tc">Status</th><th class="tc">Actions</th></tr></thead>
            <tbody>${currentItems.map(item => {
                const itemStatus = utilizationItemStatus(item);
                const targetInfo = monitoringTargetInfo(item);
                const distributionDate = utilDistributionDates.get(Number(item.distributionId)) || "";
                const receivedDate = distributionDate ? escapeHtml(new Date(`${distributionDate}T00:00:00`).toLocaleDateString("en-PH", { year: "numeric", month: "short", day: "numeric" })) : "—";
                const targetNote = targetInfo ? `Target: ${escapeHtml(targetInfo.targetDate)} · ${targetInfo.days < 0 ? `${Math.abs(targetInfo.days)}d overdue` : `${targetInfo.days}d left`}${targetInfo.late ? " · monitored late" : ""}` : "";
                return `<tr>
                    <td class="tl col-text-wrap">${escapeHtml(item.itemName)}${targetNote ? `<div class="small text-muted">${targetNote}</div>` : ""}</td>
                    <td class="tc">${escapeHtml(item.receivedQuantity)} ${escapeHtml(item.unit)}</td>
                    ${pendingOnly ? `<td class="tc col-text-nowrap">${receivedDate}</td>` : ""}
${pendingOnly ? "" : `<td class="tc">${item.quantityUsed ?? "—"}</td>
                    <td class="tc">${item.quantityStillStocked ?? "—"}</td>
                    <td class="tc">${item.quantityDisposed ?? "—"}</td>
                    <td class="tc col-text-nowrap">${item.monitoringDate && item.monitoringDate !== "0001-01-01" ? escapeHtml(new Date(`${item.monitoringDate}T00:00:00`).toLocaleDateString("en-PH", { year: "numeric", month: "short", day: "numeric" })) : "—"}</td>`}
                    <td class="tc"><span class="util-status-badge ${itemStatus.cssClass}"><span class="status-dot"></span>${escapeHtml(itemStatus.label)}</span></td>
                    <td class="tc action-buttons"><button class="btn btn-sm btn-outline-success" type="button" data-util-edit="${item.distributionItemId}">${item.utilizationMonitoringId === 0 ? "Monitor" : "Update"}</button>${pendingOnly ? `<button class="btn btn-sm btn-outline-danger" type="button" data-util-delete="${item.distributionItemId}" title="Delete this distribution item"><i class="bi bi-trash3"></i></button>` : ""}</td>
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
        itemHost.querySelectorAll("[data-util-delete]").forEach(button => button.addEventListener("click", async () => {
            if (!window.confirm("Delete this distributed item? The stock will be restored and this line will be removed from history.")) return;
            try {
                await api.delete(`/distribution-items/${button.dataset.utilDelete}`);
                showToast("Distribution item deleted and stock restored.");
                utilRows = utilRows.filter(row => row.distributionItemId !== Number(button.dataset.utilDelete));
                renderFarmerItems();
                renderUtilizationModule();
            } catch (error) {
                showToast(error.message, "danger");
            }
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
    const filters = utilTabFilters[utilActiveTab] || (utilTabFilters[utilActiveTab] = { search: "", barangay: "", status: "" });
    const search = document.getElementById("utilSearch")?.value.trim() ?? filters.search;
    const status = document.getElementById("utilStatus")?.value ?? filters.status;
    filters.search = search;
    filters.status = status;

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
        utilDistributionCreated = new Map(distributions.map(d => [Number(d.distributionId), d.createdAtUtc]));
    } catch {
        utilDistributionDates = new Map();
        utilDistributionCreated = new Map();
    }

    try {
        const seasonInfo = await api.get("/monitoring-season");
        if (seasonInfo?.startedAtUtc) {
            const startedAt = new Date(seasonInfo.startedAtUtc).getTime();
            utilRows = utilRows.filter(row => {
                const created = utilDistributionCreated.get(Number(row.distributionId));
                return !created || parseApiDate(created) >= startedAt;
            });
        } else if (seasonInfo?.startYear && seasonInfo?.startMonth) {
            const seasonStart = new Date(seasonInfo.startYear, Number(seasonInfo.startMonth) - 1, 1);
            utilRows = utilRows.filter(row => {
                const date = utilDistributionDates.get(Number(row.distributionId));
                return !date || new Date(`${date}T00:00:00`) >= seasonStart;
            });
        }
    } catch {}

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
    <div class="modal-body"><p>Received: <strong>${r.receivedQuantity} ${escapeHtml(r.unit)}</strong>. The reported total cannot exceed this amount. Quantity used is required; still stocked and disposed are recorded as 0 when left blank.</p><div class="row g-3">
    ${[["quantityUsed","Quantity used *",true],["quantityStillStocked","Quantity still stocked",false],["quantityDisposed","Quantity disposed",false]].map(([n,l,required])=>`<div class="col-md-4"><label class="form-label">${l}</label><input class="form-control" name="${n}" type="number" min="0" max="${r.receivedQuantity}" step="0.001" value="${r[n]??""}"${required?" required":""}></div>`).join("")}
    <div class="col-md-6"><label class="form-label">Monitoring date</label><input class="form-control" name="monitoringDate" type="date" value="${r.monitoringDate&&r.monitoringDate!=="0001-01-01"?r.monitoringDate:new Date().toISOString().slice(0,10)}" required></div>
    <div class="col-12"><label class="form-label">Remarks</label><textarea class="form-control" name="remarks" rows="2">${escapeHtml(r.remarks||"")}</textarea></div></div><div class="alert alert-info mt-3 mb-0" id="utilTotal"></div></div>
    <div class="modal-footer"><button type="button" class="btn btn-light" data-bs-dismiss="modal">Cancel</button><button class="btn btn-success">Save monitoring</button></div></form></div></div></div>`;
    document.body.appendChild(host);const modal=new bootstrap.Modal(host.querySelector(".modal"));modal.show();
    const form=host.querySelector("#utilForm");
    const updateTotal=()=>{const sum=["quantityUsed","quantityStillStocked","quantityDisposed"].reduce((a,n)=>a+(Number(form.elements[n].value)||0),0);host.querySelector("#utilTotal").textContent=`Reported total: ${sum} / ${r.receivedQuantity} ${r.unit} · Unreported: ${Math.max(0,r.receivedQuantity-sum)}`;return sum;};
    form.addEventListener("input",updateTotal);updateTotal();
    form.addEventListener("submit",async e=>{e.preventDefault();
      if(form.elements.quantityUsed.value===""||Number(form.elements.quantityUsed.value)<0){showToast("Quantity used is required and must be 0 or above.","danger");return;}
      ["quantityStillStocked","quantityDisposed"].forEach(n=>{if(form.elements[n].value==="")form.elements[n].value="0";});
      const sum=updateTotal();if(sum>r.receivedQuantity){showToast("Reported quantities cannot exceed the received quantity.","danger");return;}
      const payload={};["quantityUsed","quantityStillStocked","quantityDisposed"].forEach(n=>payload[n]=form.elements[n].value===""?null:Number(form.elements[n].value));payload.monitoringDate=form.elements.monitoringDate.value;payload.remarks=form.elements.remarks.value.trim()||null;
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
    mainContent.innerHTML = `${printDocumentHeader("Supply Management Report", "Assistance supplies and current stock levels")}<div class="view-header"><div><h2>Supply Management</h2><p>Track and manage stock of assistance supplies such as seeds, fertilizers, and tools.</p></div><div class="d-flex gap-2"><div class="dropdown"><button class="btn btn-outline-success dropdown-toggle" type="button" data-bs-toggle="dropdown" aria-expanded="false"><i class="bi bi-file-earmark-excel me-1"></i> Excel</button><ul class="dropdown-menu dropdown-menu-dark"><li><button class="dropdown-item" type="button" data-excel="supplies-export"><i class="bi bi-file-earmark-arrow-down me-2"></i>Export to Excel</button></li></ul></div><button class="btn btn-back" type="button" data-supply-action="print"><i class="bi bi-printer me-1"></i> Print</button><button class="btn btn-success" type="button" data-supply-action="add"><i class="bi bi-plus-lg me-1"></i> Add supply</button></div></div>
        ${errorMessage ? `<div class="alert alert-danger">${escapeHtml(errorMessage)}</div>` : ""}
        <section class="content-panel"><form class="filter-bar" id="supplyFilter">
            <input id="supplySearch" class="form-control" type="search" value="${escapeHtml(search)}" placeholder="Search supply name or category">
            <select id="supplySort" class="form-select">${sortOptions}</select>
            <button class="btn btn-success" type="submit">Filter</button>
        </form><div class="table-responsive"><table class="table align-middle mb-0 supplies-table">
            <colgroup><col class="col-supply-name"><col class="col-supply-category"><col class="tc"><col class="col-supply-unit"><col class="col-supply-expiration"><col class="col-supply-actions"></colgroup>
            <thead><tr><th class="tl">Name</th><th class="tl">Category</th><th class="tc">Stock Quantity</th><th class="tc">Unit</th><th class="tc">Expiration Date</th><th class="tc">Actions</th></tr></thead><tbody id="supplyTableBody"></tbody></table></div></section>
        ${printDocumentFooter()}`;
    document.getElementById("supplyFilter").addEventListener("submit", event => { event.preventDefault(); renderSupplyModule(); });
    document.getElementById("supplySort").addEventListener("change", () => renderSupplies());
    mainContent.querySelectorAll("[data-supply-action]").forEach(button => button.addEventListener("click", () => handleSupplyAction(button)));
    renderSupplies();
}

function renderSupplies(errorMessage = "") {
    const body = document.getElementById("supplyTableBody");
    if (!body) return;
    let visibleSupplyRows = [];
    if (errorMessage) {
        body.innerHTML = `<tr><td colspan="6" class="empty-state">${escapeHtml(errorMessage)}</td></tr>`;
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
    visibleSupplyRows = rows;
    if (!rows.length) {
        body.innerHTML = `<tr><td colspan="6" class="empty-state"><i class="bi bi-box-seam"></i>No supplies match your search</td></tr>`;
        return;
    }
    body.innerHTML = rows.map(supply => {
        const qty = Math.round(Number(supply.stockQuantity || 0));
        const stockCell = supply.isActive && qty <= 0
            ? `<span class="text-danger fw-semibold">Out of stock</span>`
            : `${qty.toLocaleString()}`;
        let expirationCell = "—";
        if (supply.expirationDate) {
            const expiryText = String(supply.expirationDate).slice(0, 10);
            const today = new Date(new Date().toISOString().slice(0, 10) + "T00:00:00");
            const days = Math.ceil((new Date(`${expiryText}T00:00:00`) - today) / 86400000);
            const tone = days < 0 ? "text-danger fw-bold" : days <= 30 ? "text-warning fw-semibold" : "";
            expirationCell = `<span class="${tone}" title="${days < 0 ? "Expired" : `${days} day(s) left`}">${escapeHtml(expiryText)}</span>`;
        }
        return `
        <tr>
            <td class="tl">${escapeHtml(supply.name)}</td>
            <td class="tl col-text-wrap">${escapeHtml(supply.categoryName)}</td>
            <td class="tc col-text-nowrap">${stockCell}</td>
            <td class="tc col-text-nowrap">${escapeHtml(supply.unitOfMeasure)}</td>
            <td class="tc col-text-nowrap">${expirationCell}</td>
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

    window.RISEPrint?.setContext("supplies", () => ({
        title: "Inventory Report",
        subtitle: "Assistance supplies and current stock levels",
        meta: [
            { label: "Report type", value: "Supply inventory" }
        ],
        blocks: [{
            title: "Supply inventory",
            columns: [
                { key: "name", label: "Supply name" },
                { key: "category", label: "Category" },
                { key: "quantity", label: "Available quantity", align: "right" },
                { key: "unit", label: "Unit", align: "center" },
                { key: "status", label: "Stock status", align: "center" }
            ],
            rows: visibleSupplyRows.map(supply => {
                const quantity = Math.round(Number(supply.stockQuantity || 0));
                return {
                    name: supply.name || "—",
                    category: supply.categoryName || "—",
                    quantity: quantity.toLocaleString(),
                    unit: supply.unitOfMeasure || "—",
                    status: quantity <= 0 ? "Out of stock" : "In stock"
                };
            }),
            totals: [{ value: `Total: ${visibleSupplyRows.length}`, colspan: 5 }]
        }]
    }));
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
        supplySummaryStock: supply.isActive ? `${Math.round(Number(supply.stockQuantity || 0)).toLocaleString()} ${escapeHtml(supply.unitOfMeasure)}` : "Archived",
        supplySummaryExpiration: supply.expirationDate ? String(supply.expirationDate).slice(0, 10) : "—"
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
    // Expiration date is fixed at creation so it cannot be manipulated later.
    const expirationAttr = mode === "add" ? "" : "readonly";
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
            <div class="${unitColClass}"><label class="form-label">Expiration date${mode === "add" ? "" : ' <span class="text-muted">(fixed)</span>'}</label><input class="form-control" name="expirationDate" type="date" value="${escapeHtml(record?.expirationDate ? String(record.expirationDate).slice(0, 10) : "")}" ${expirationAttr}></div>
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
            if (mode === "add") payload.expirationDate = form.elements.expirationDate.value || null;
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
    if (action === "print") { window.RISEPrint?.print("supplies"); return; }
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

function seasonDateRange(seasonKey) {
    const [year, kind] = String(seasonKey).split("-");
    const y = Number(year);
    if (!y) return null;
    if (kind === "Wet") return [new Date(y, 5, 1), new Date(y, 10, 30)];
    if (kind === "Dry") return [new Date(y - 1, 11, 1), new Date(y, 4, 31)];
    return null;
}

async function renderRiseReports() {
    const from=document.getElementById("riseReportFrom")?.value||"",to=document.getElementById("riseReportTo")?.value||"",barangay=document.getElementById("riseReportBarangay")?.value.trim()||"",type=document.getElementById("riseReportType")?.value.trim()||"",search=document.getElementById("riseReportSearch")?.value.trim().toLowerCase()||"",season=document.getElementById("riseReportSeason")?.value||"";
    let rows=[],stats=null,error="";
    try{const p=new URLSearchParams();if(from)p.set("from",from);if(to)p.set("to",to);if(barangay)p.set("barangay",barangay);if(type)p.set("assistanceType",type);[rows,stats]=await Promise.all([api.get(`/reports/summary${p.size?`?${p}`:""}`),api.get("/reports/dashboard")]);}catch(e){error=e.message;}
    let activeSeason = "", pastSeasons = [], allSeasons = [], archivedRows = [], utilizationRows = [], seasonStartedAt = null, distCreated = new Map();
    try {
        const info = await api.get("/monitoring-season");
        activeSeason = info?.activeSeason || "";
        allSeasons = (info?.seasons || []).filter(Boolean);
        if (info?.startedAtUtc) seasonStartedAt = new Date(info.startedAtUtc).getTime();
        archivedRows = await api.get("/monitoring-season/history") || [];
        pastSeasons = [...new Set([...archivedRows.map(row => row.seasonKey), ...allSeasons].filter(value => value && value !== activeSeason))].sort();
        if (!allSeasons.length) allSeasons = [activeSeason, ...pastSeasons];
    } catch {}
    const allRows = [...rows];
    if (!season && seasonStartedAt) {
        try {
            const distributions = await api.get("/distributions") || [];
            distCreated = new Map(distributions.map(row => [Number(row.distributionId), row.createdAtUtc]));
            rows = rows.filter(row => {
                const created = distCreated.get(Number(row.distributionId));
                return !created || parseApiDate(created) >= seasonStartedAt;
            });
        } catch {}
    } else if (season) {
        const range = seasonDateRange(season);
        if (range) rows = rows.filter(row => {
            const date = new Date(`${row.distributionDate}T00:00:00`);
            return date >= range[0] && date <= range[1];
        });
    }
    const effectiveSeason = season || activeSeason;
    let reportToMonitor = 0;
    try {
        const [uRows, distributions] = await Promise.all([api.get("/utilization"), api.get("/distributions")]);
        utilizationRows = uRows || [];
        const dates = new Map((distributions || []).map(row => [Number(row.distributionId), row.distributionDate]));
        const range = season ? seasonDateRange(effectiveSeason) : null;
        reportToMonitor = new Set((utilizationRows || [])
            .filter(row => {
                const created = distCreated.get(Number(row.distributionId));
                const date = dates.get(Number(row.distributionId));
                const inSeason = range
                    ? (date && new Date(`${date}T00:00:00`) >= range[0] && new Date(`${date}T00:00:00`) <= range[1])
                    : (!seasonStartedAt || (created && parseApiDate(created) >= seasonStartedAt));
                return inSeason && (row.utilizationMonitoringId === 0 || Number(row.unreportedQuantity) > 0);
            })
            .map(row => Number(row.farmerId))).size;
    } catch {}
    const filtered=rows.filter(r=>!search||[r.farmer,r.item,r.barangay,r.distributionId].some(x=>String(x??"").toLowerCase().includes(search)));
    const aggregate=(key)=>{const map=new Map();filtered.forEach(r=>{const k=r[key]||"Unspecified";const v=map.get(k)||{name:k,records:0,quantity:0};v.records++;v.quantity+=Number(r.quantity||0);map.set(k,v);});return [...map.values()].sort((a,b)=>String(a.name).localeCompare(String(b.name),undefined,{sensitivity:"base"}));};
    const byType=aggregate("item"), byBarangay=aggregate("barangay");
    const usedMonthly=new Map();
    filtered.forEach(row=>{const month=String(row.distributionDate||"").slice(0,7);if(month)usedMonthly.set(month,(usedMonthly.get(month)||0)+Number(row.used||0));});
    const usedMonths=[...usedMonthly.keys()].sort();
    const usedByCategory=new Map();
    rows.forEach(row=>{
        const category=classifyAssistance(row.item);
        const key=`${category} (${row.unit||"unit"})`;
        usedByCategory.set(key,(usedByCategory.get(key)||0)+Number(row.used||0));
    });
    const categoryLabels=[...usedByCategory.keys()].sort(compareCategoriesLast);
    const seasonOrder = [...new Set([...allSeasons, activeSeason].filter(Boolean))].sort((a, b) => {
        const [ya, sa] = String(a).split("-"), [yb, sb] = String(b).split("-");
        return Number(ya) - Number(yb) || String(sa).localeCompare(String(sb));
    });
    const seasonUsed = new Map();
    seasonUsed.set(activeSeason, utilizationRows.filter(row => {
        const created = distCreated.get(Number(row.distributionId));
        return !seasonStartedAt || (created && parseApiDate(created) >= seasonStartedAt);
    }).reduce((sum, row) => sum + Number(row.quantityUsed || 0), 0));
    pastSeasons.forEach(key => seasonUsed.set(key, archivedRows
        .filter(row => row.seasonKey === key).reduce((sum, row) => sum + Number(row.quantityUsed || 0), 0)));
    const lineLabels = seasonOrder.map(key => formatSeasonLabel(key));
    const lineValues = seasonOrder.map(key => seasonUsed.get(key) || 0);
    const reportCategories = [...new Set(rows.map(row => classifyAssistance(row.item)))].sort(compareCategoriesLast);
    const unitTotals = key => formatQuantityByUnit(rows.map(row => [classifyAssistance(row.item), row.unit, row[key]]), reportCategories);
    const reportCardIcons = ["bi-people", "bi-clipboard-check", "bi-box-seam", "bi-check2-circle", "bi-boxes", "bi-trash3"];
    const reportCards = [
        { label: "Total Farmers Beneficiaries", value: stats?.totalFarmers, button: "View Farmers", action: "farmers" },
        { label: "To Monitor", value: reportToMonitor, button: "Continue monitor", action: "utilization" },
        { label: "Total Quantity distributed", display: unitTotals("quantity"), button: "View details", action: "distributed" },
        { label: "Total quantity used", display: unitTotals("used"), button: "View", action: "used" },
        { label: "Still stocked", display: unitTotals("stocked"), button: "View supply", action: "supplies" },
        { label: "Disposed", display: unitTotals("disposed"), button: "View", action: "disposed" }
    ];
    mainContent.innerHTML=`<div class="view-header"><div><h2>Reports and Analytics</h2><p>Gain insights into farm operations, supply distribution, and harvest performance through organized reports and data-driven analytics</p></div><div class="d-flex gap-2 align-items-center"><select id="riseReportSeason" class="form-select" style="width:auto;min-width:170px"><option value="">Present</option>${pastSeasons.map(value=>`<option value="${escapeHtml(value)}"${value===season?" selected":""}>${escapeHtml(formatSeasonLabel(value))}</option>`).join("")}</select><button class="btn btn-outline-success" id="printRiseReport"><i class="bi bi-printer me-1"></i> Print / Save PDF</button></div></div>
    ${error?`<div class="alert alert-danger">${escapeHtml(error)}</div>`:""}
    ${stats?`<section class="stat-grid card-duo">${reportCards.map((card, index) => `<article class="stat-card glass-card"><div class="stat-card-top"><span>${escapeHtml(card.label)}</span><span class="stat-card-icon" aria-hidden="true"><i class="bi ${reportCardIcons[index] || "bi-bar-chart"}"></i></span></div>${card.display ? `<div class="stat-card-value stat-card-value-detail">${card.display}</div>` : `<strong class="stat-card-value stat-card-number">${Number(card.value || 0).toLocaleString()}</strong>`}${card.button ? `<div class="stat-card-foot"><button class="stat-card-action" type="button" data-dashboard-card="${card.action}">${escapeHtml(card.button)}</button></div>` : ""}</article>`).join("")}</section>`:""}
    <section class="dashboard-grid pie-grid">
      <article class="panel glass-panel"><div class="panel-header"><h3>Used supplies</h3></div><div class="chart-wrap"><canvas id="reportUsedPieChart"></canvas></div></article>
      <article class="panel glass-panel"><div class="panel-header"><h3>Still stocked supplies</h3></div><div class="chart-wrap"><canvas id="reportStockedPieChart"></canvas></div></article>
      <article class="panel glass-panel"><div class="panel-header"><h3>Disposed supplies</h3></div><div class="chart-wrap"><canvas id="reportDisposedPieChart"></canvas></div></article>
    </section>
    <section class="dashboard-grid analytics-grid">
      <article class="panel glass-panel"><div class="panel-header"><h3>Supply Utilization Trends</h3></div><div class="chart-wrap"><canvas id="reportUsedLineChart"></canvas></div></article>
      <article class="panel glass-panel"><div class="panel-header"><h3>Used items by category</h3></div><div class="chart-wrap"><canvas id="reportUsedCategoryChart"></canvas></div></article>
    </section>
    <section class="dashboard-grid analytics-grid">
      <article class="panel glass-panel"><div class="panel-header"><h3>Distribution by assistance type</h3></div><div class="table-responsive"><table class="table reports-type-table"><thead><tr><th class="tl">Assistance type</th><th class="tc">Records</th><th class="tc">Total quantity</th></tr></thead><tbody>${byType.length?byType.map(x=>`<tr><td class="tl">${escapeHtml(x.name)}</td><td class="tc">${x.records}</td><td class="tc">${x.quantity.toLocaleString()}</td></tr>`).join(""):`<tr><td colspan="3" class="empty-state">No records</td></tr>`}</tbody></table></div></article>
      <article class="panel glass-panel"><div class="panel-header"><h3>Distribution by barangay</h3></div><div class="table-responsive"><table class="table reports-barangay-table"><thead><tr><th class="tl">Barangay</th><th class="tc">Records</th><th class="tc">Total quantity</th></tr></thead><tbody>${byBarangay.length?byBarangay.map(x=>`<tr><td class="tl">${escapeHtml(x.name)}</td><td class="tc">${x.records}</td><td class="tc">${x.quantity.toLocaleString()}</td></tr>`).join(""):`<tr><td colspan="3" class="empty-state">No records</td></tr>`}</tbody></table></div></article>
    </section>`;
    reportCharts.forEach(chart=>chart.destroy()); reportCharts=[];
    const lineEl=document.getElementById("reportUsedLineChart");
    if(lineEl)reportCharts.push(new Chart(lineEl,{type:"line",data:{labels:lineLabels,datasets:[{label:"Quantity used",data:lineValues,borderColor:"#e5b33d",backgroundColor:"rgba(229,179,61,0.18)",pointBackgroundColor:"#e5b33d",tension:0.3,fill:true,borderWidth:2}]},options:{responsive:true,maintainAspectRatio:false,plugins:{legend:{display:false}}}}));
    const barEl=document.getElementById("reportUsedCategoryChart");
    if(barEl)reportCharts.push(new Chart(barEl,{type:"bar",data:{labels:categoryLabels,datasets:[{label:"Quantity used",data:categoryLabels.map(key=>usedByCategory.get(key)),backgroundColor:"rgba(114,168,86,0.65)",borderColor:"#72a856",borderWidth:1}]},options:{responsive:true,maintainAspectRatio:false,plugins:{legend:{display:false}}}}));
    const piePalette = ["#2f7d55", "#e5b33d", "#6f9e7f", "#b9862f", "#4b6b57", "#9db9a2"];
    const reportPie = (id, key) => {
        const el = document.getElementById(id);
        if (!el) return;
        const map = new Map();
        rows.forEach(row => {
            const quantity = Number(row[key] || 0);
            if (quantity <= 0) return;
            const label = `${classifyAssistance(row.item)} (${row.unit || "unit"})`;
            map.set(label, (map.get(label) || 0) + quantity);
        });
        const entries = [...map.entries()].sort((a, b) => compareCategoriesLast(a[0], b[0]));
        const labels = entries.map(entry => entry[0]);
        const values = entries.map(entry => entry[1]);
        const hasData = values.length > 0;
        reportCharts.push(new Chart(el, {
            type: "doughnut",
            data: {
                labels: hasData ? labels : ["No records"],
                datasets: [{
                    data: hasData ? values : [1],
                    backgroundColor: hasData ? labels.map((_, i) => piePalette[i % piePalette.length]) : ["rgba(160, 160, 160, 0.18)"],
                    borderColor: "rgba(6, 26, 17, 0.55)",
                    borderWidth: 2,
                    hoverOffset: 8
                }]
            },
            options: {
                responsive: true,
                maintainAspectRatio: false,
                cutout: "62%",
                plugins: {
                    legend: { position: "bottom", labels: { color: "#d7e2d8", boxWidth: 10, boxHeight: 10, padding: 12, font: { size: 11 } } },
                    tooltip: { callbacks: { label: context => hasData ? ` ${context.label}: ${Number(context.parsed).toLocaleString(undefined, { maximumFractionDigits: 3 })}` : " No records" } }
                }
            }
        }));
    };
    reportPie("reportUsedPieChart", "used");
    reportPie("reportStockedPieChart", "stocked");
    reportPie("reportDisposedPieChart", "disposed");
    document.getElementById("riseReportSeason")?.addEventListener("change",()=>renderRiseReports());
    document.getElementById("printRiseReport")?.addEventListener("click",()=>window.RISEPrint?.print("reports"));
    window.RISEPrint?.setContext("reports", () => {
        const quantityText = key => {
            const totals = new Map();
            filtered.forEach(row => {
                const unit = row.unit || "unit";
                totals.set(unit, (totals.get(unit) || 0) + Number(row[key] || 0));
            });
            return [...totals.entries()].filter(([, value]) => value > 0)
                .map(([unit, value]) => `${value.toLocaleString(undefined, { maximumFractionDigits: 3 })} ${unit}`).join(", ") || "—";
        };
        const seasonLabel = typeof formatSeasonLabel === "function" ? formatSeasonLabel(effectiveSeason) : effectiveSeason;
        const seasonRange = season ? seasonDateRange(season) : null;
        const period = seasonRange
            ? `${new Date(`${seasonRange[0]}T00:00:00`).toLocaleDateString("en-PH")} to ${new Date(`${seasonRange[1]}T00:00:00`).toLocaleDateString("en-PH")}`
            : "Present season";
        return {
            title: "Statistical Report",
            subtitle: "Assistance distribution and supply utilization",
            landscape: true,
            meta: [
                { label: "Report type", value: "Reports and analytics" },
                { label: "Cropping season", value: seasonLabel || "—" },
                { label: "Reporting period", value: period },
                { label: "Records included", value: filtered.length },
                { label: "Season dropdown", value: document.getElementById("riseReportSeason")?.selectedOptions[0]?.textContent || "Present" }
            ],
            blocks: [
                { title: "Summary", fields: [
                    ["Total Farmers Beneficiaries", stats?.totalFarmers ?? "—"],
                    ["Farmers to monitor", reportToMonitor],
                    ["Distribution items included", filtered.length],
                    ["Quantity distributed", quantityText("quantity")],
                    ["Quantity used", quantityText("used")],
                    ["Quantity still stocked", quantityText("stocked")],
                    ["Quantity disposed", quantityText("disposed")]
                ] },
                { title: "Distribution by assistance type",
                  columns: [{ key: "name", label: "Assistance type" }, { key: "records", label: "Records", align: "right" }, { key: "quantity", label: "Total quantity", align: "right" }],
                  rows: byType,
                  totals: [
                      { value: "TOTAL", colspan: 1 },
                      { value: byType.reduce((sum, item) => sum + item.records, 0), align: "right" },
                      { value: byType.reduce((sum, item) => sum + item.quantity, 0).toLocaleString(), align: "right" }
                  ] },
                { title: "Distribution by barangay",
                  columns: [{ key: "name", label: "Barangay" }, { key: "records", label: "Records", align: "right" }, { key: "quantity", label: "Total quantity", align: "right" }],
                  rows: byBarangay,
                  totals: [
                      { value: "TOTAL", colspan: 1 },
                      { value: byBarangay.reduce((sum, item) => sum + item.records, 0), align: "right" },
                      { value: byBarangay.reduce((sum, item) => sum + item.quantity, 0).toLocaleString(), align: "right" }
                  ] },
                { title: "Supply utilization by category",
                  columns: [{ key: "label", label: "Category and unit" }, { key: "used", label: "Quantity used", align: "right" }],
                  rows: categoryLabels.map(label => ({ label, used: Number(usedByCategory.get(label) || 0).toLocaleString(undefined, { maximumFractionDigits: 3 }) })) },
                { title: "Monthly utilization trend",
                  columns: [{ key: "month", label: "Month" }, { key: "used", label: "Quantity used", align: "right" }],
                  rows: usedMonths.map(month => ({ month, used: Number(usedMonthly.get(month) || 0).toLocaleString(undefined, { maximumFractionDigits: 3 }) })) },
                { title: "Distribution records",
                  columns: [
                      { key: "reference", label: "Reference no." },
                      { key: "farmer", label: "Farmer" },
                      { key: "barangay", label: "Barangay" },
                      { key: "date", label: "Distribution date" },
                      { key: "item", label: "Supply item" },
                      { key: "category", label: "Category" },
                      { key: "quantity", label: "Quantity", align: "right" },
                      { key: "unit", label: "Unit", align: "center" },
                      { key: "used", label: "Used", align: "right" },
                      { key: "stocked", label: "Stocked", align: "right" },
                      { key: "disposed", label: "Disposed", align: "right" }
                  ],
                  rows: filtered.map(row => ({
                      reference: row.distributionId ? `#${row.distributionId}` : "—",
                      farmer: row.farmer || "—",
                      barangay: row.barangay || "—",
                      date: row.distributionDate || "—",
                      item: row.item || "—",
                      category: classifyAssistance(row.item),
                      quantity: Number(row.quantity || 0).toLocaleString(undefined, { maximumFractionDigits: 3 }),
                      unit: row.unit || "—",
                      used: row.used === null || row.used === undefined ? "—" : Number(row.used).toLocaleString(undefined, { maximumFractionDigits: 3 }),
                      stocked: row.stocked === null || row.stocked === undefined ? "—" : Number(row.stocked).toLocaleString(undefined, { maximumFractionDigits: 3 }),
                      disposed: row.disposed === null || row.disposed === undefined ? "—" : Number(row.disposed).toLocaleString(undefined, { maximumFractionDigits: 3 })
                  })),
                  totals: [{ value: `Total: ${filtered.length}`, colspan: 11 }]
                }
            ]
        };
    });
    mainContent.querySelectorAll("[data-dashboard-card]").forEach(button => button.addEventListener("click", () => {
        const action = button.dataset.dashboardCard;
        if (action === "distributed") openDistributedQuantityModal();
        else if (action === "used") openUsedQuantityModal();
        else if (action === "disposed") openDisposedQuantityModal();
        else setView(action);
    }));
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
    if (action === "print") window.RISEPrint?.print("farmers");
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
function parseApiDate(value) {
    if (!value) return 0;
    const text = String(value);
    return new Date(/[zZ]|[+-]\d{2}:\d{2}$/.test(text) ? text : `${text}Z`).getTime();
}

function classifyAssistance(name) {
    const text = String(name || "");
    if (/seed/i.test(text)) return "Seeds";
    if (/fertil|urea|14-14-14|14x14|npk|ammonium/i.test(text)) return "Fertilizer";
    return "Other";
}

function compareCategoriesLast(a, b) {
    const aOther = /^other\b/i.test(String(a || ""));
    const bOther = /^other\b/i.test(String(b || ""));
    if (aOther !== bOther) return aOther ? 1 : -1;
    return String(a || "").localeCompare(String(b || ""), undefined, { sensitivity: "base" });
}

function normalizeUnit(unit) {
    const value = String(unit || "unit").trim().toLowerCase();
    return value === "pcs" || value === "pieces" ? "pc"
        : value === "sacks" ? "sack"
        : value === "bags" ? "bag"
        : value === "kgs" ? "kg" : value;
}

function formatQuantityByUnit(entries, categoryOrder = null) {
    const grouped = new Map();
    entries.forEach(([category, unit, quantity]) => {
        const name = category || "Other";
        if (!grouped.has(name)) grouped.set(name, new Map());
        const units = grouped.get(name);
        const key = normalizeUnit(unit);
        units.set(key, (units.get(key) || 0) + Number(quantity || 0));
    });
    if (!grouped.size) return "0";
    const order = categoryOrder && categoryOrder.length
        ? categoryOrder
        : [...grouped.keys()].sort(compareCategoriesLast);
    return `<div class="qty-panel">${order.map(category => {
        const units = grouped.get(category);
        const measures = units ? [...units.entries()].filter(([, quantity]) => quantity > 0) : [];
        const plainValues = measures
            .map(([unit, quantity]) => `${quantity.toLocaleString(undefined, { maximumFractionDigits: 3 })} ${unit}`).join(", ");
        const values = measures.map(([unit, quantity]) =>
            `<span class="qty-measure"><span class="qty-amount">${quantity.toLocaleString(undefined, { maximumFractionDigits: 3 })}</span><span class="qty-unit">${escapeHtml(unit)}</span></span>`
        ).join('<span class="qty-measure-sep"> · </span>');
        return `<div class="qty-row"><span class="qty-row-label">${escapeHtml(category)}</span><span class="qty-row-value"${plainValues ? ` title="${escapeHtml(plainValues)}"` : ""}>${measures.length ? values : '<span class="qty-amount">0</span>'}</span></div>`;
    }).join("")}</div>`;
}

function showSeasonCreatedDialog(season, monthName, year) {
    document.getElementById("seasonConfirmHost")?.remove();
    const host = document.createElement("div");
    host.id = "seasonConfirmHost";
    host.innerHTML = `<div class="modal fade" tabindex="-1"><div class="modal-dialog modal-dialog-centered"><div class="modal-content season-confirm season-confirm-success">
        <div class="season-confirm-body">
            <span class="season-confirm-icon"><i class="bi bi-check2"></i></span>
            <p class="season-confirm-eyebrow">Season created</p>
            <h5 class="season-confirm-title">Ready for a new monitoring cycle</h5>
            <p class="season-confirm-text">The previous season's records were archived and remain viewable anytime.</p>
            <div class="season-confirm-details">
                <div><span>Season</span><strong>${escapeHtml(formatSeasonLabel(season))}</strong></div>
                <div><span>Monitoring starts</span><strong>${escapeHtml(monthName)} ${escapeHtml(String(year))}</strong></div>
            </div>
        </div>
        <div class="modal-footer"><button class="btn btn-success" type="button" data-bs-dismiss="modal">Done</button></div>
    </div></div></div>`;
    document.body.appendChild(host);
    const modal = new bootstrap.Modal(host.querySelector(".modal"));
    modal.show();
    host.querySelector(".modal").addEventListener("hidden.bs.modal", () => host.remove());
}

function confirmMarkSeasonDone(activeSeason, newSeason, onConfirm) {
    document.getElementById("seasonConfirmAskHost")?.remove();
    const host = document.createElement("div");
    host.id = "seasonConfirmAskHost";
    host.innerHTML = `<div class="modal fade" tabindex="-1"><div class="modal-dialog modal-dialog-centered"><div class="modal-content season-confirm season-confirm-ask">
        <div class="season-confirm-body">
            <span class="season-confirm-icon"><i class="bi bi-check2-square"></i></span>
            <p class="season-confirm-eyebrow">Confirm action</p>
            <h5 class="season-confirm-title">Mark present season as done?</h5>
            <p class="season-confirm-text">The current season <strong>${escapeHtml(formatSeasonLabel(activeSeason) || "—")}</strong> will be marked done. Its monitoring records will be archived and stay viewable. After confirming, you can set up the new season.</p>
        </div>
        <div class="modal-footer"><button class="btn btn-light" type="button" id="seasonDoneNo">Cancel</button><button class="btn btn-success" type="button" id="seasonDoneYes">Yes, mark as done</button></div>
    </div></div></div>`;
    document.body.appendChild(host);
    const modal = new bootstrap.Modal(host.querySelector(".modal"));
    modal.show();
    host.querySelector(".modal").addEventListener("hidden.bs.modal", () => host.remove());
    document.getElementById("seasonDoneNo")?.addEventListener("click", () => modal.hide());
    document.getElementById("seasonDoneYes")?.addEventListener("click", () => { modal.hide(); onConfirm(); });
}

async function openSeasonModal() {
    let seasonInfo = null;
    try { seasonInfo = await api.get("/monitoring-season"); } catch { seasonInfo = null; }
    confirmMarkSeasonDone(seasonInfo?.activeSeason, "new season", () => showSeasonForm(seasonInfo));
}

function showSeasonForm(seasonInfo) {
    const monthNames = ["January", "February", "March", "April", "May", "June", "July", "August", "September", "October", "November", "December"];
    const currentYear = seasonInfo?.startYear || new Date().getFullYear();
    const currentMonth = String(seasonInfo?.startMonth || new Date().getMonth() + 1).padStart(2, "0");
    const monthOptions = monthNames.map((name, index) => `<option value="${String(index + 1).padStart(2, "0")}"${String(index + 1).padStart(2, "0") === currentMonth ? " selected" : ""}>${name}</option>`).join("");
    document.getElementById("seasonModalHost")?.remove();
    const host = document.createElement("div");
    host.id = "seasonModalHost";
    host.innerHTML = `<div class="modal fade" tabindex="-1"><div class="modal-dialog modal-dialog-centered"><div class="modal-content"><form id="seasonForm">
        <div class="modal-header"><h5 class="modal-title">Create new season</h5><button class="btn-close" type="button" data-bs-dismiss="modal" aria-label="Close"></button></div>
        <div class="modal-body"><p class="text-muted small">Current season: <strong>${escapeHtml(seasonInfo?.activeSeason || "—")}</strong>. Creating a new season archives the current monitoring records and starts fresh. Previous data stays saved and viewable.</p>
            <div class="row g-3">
                <div class="col-md-6"><label class="form-label">Cropping season</label><select class="form-select" name="seasonType" required><option value="Wet">Wet season</option><option value="Dry">Dry season</option></select></div>
                <div class="col-md-6"><label class="form-label">Year</label><input class="form-control" name="seasonYear" type="number" min="2000" max="2100" value="${currentYear}" required></div>
                <div class="col-12"><label class="form-label">Start of monitoring (month)</label><select class="form-select" name="startMonth" required>${monthOptions}</select></div>
            </div>
        </div>
        <div class="modal-footer"><button class="btn btn-light" type="button" data-bs-dismiss="modal">Cancel</button><button class="btn btn-success" type="submit">Create season</button></div>
    </form></div></div></div>`;
    document.body.appendChild(host);
    const modal = new bootstrap.Modal(host.querySelector(".modal"));
    modal.show();
    host.querySelector(".modal").addEventListener("hidden.bs.modal", () => host.remove());
    host.querySelector("#seasonForm").addEventListener("submit", async event => {
        event.preventDefault();
        const form = event.currentTarget;
        const season = `${form.elements.seasonYear.value}-${form.elements.seasonType.value}`;
        try {
            const month = Number(form.elements.startMonth.value);
            const year = Number(form.elements.seasonYear.value);
            const result = await api.put("/monitoring-season", {
                season,
                startMonth: month,
                startYear: year
            });
            modal.hide();
            showSeasonCreatedDialog(result?.activeSeason || season, monthNames[month - 1] || "", result?.startYear || year);
            updateTopbarSeason();
            renderRiseDashboard();
        } catch (error) {
            showToast(error.message, "danger");
        }
    });
}

function formatSeasonLabel(value) {
    const [year, kind] = String(value || "").split("-");
    return kind ? `${kind} season - ${year}` : value || "—";
}

function updateTopbarSeason() {
    api.get("/monitoring-season").then(info => {
        const el = document.getElementById("topbarSeason");
        if (el && info?.activeSeason) {
            el.innerHTML = `<i class="bi bi-calendar3 me-1"></i> ${escapeHtml(formatSeasonLabel(info.activeSeason))}`;
            el.title = `Current season: ${formatSeasonLabel(info.activeSeason)}`;
        }
    }).catch(() => {});
}

function syncHeaderAddButton() {
    const button = document.getElementById("addRecordButton");
    const canAdd = currentView === "dashboard";
    button.hidden = !canAdd;
    if (!canAdd) return;
    const label = "Create new season";
    button.setAttribute("aria-label", label);
    button.title = label;
    button.querySelector(".d-none.d-md-inline").textContent = label;
}

async function openHeaderCreateForm() {
    if (currentView === "dashboard") await openSeasonModal();
    else if (currentView === "farmers") openCreateForm();
    else if (currentView === "distribution") await openDistributionModal("add");
    else if (currentView === "supplies") await openSupplyModal("add");
}

document.getElementById("addRecordButton").addEventListener("click", openHeaderCreateForm);
let notificationItems = [];
async function updateNotificationBadge() {
    try {
        notificationItems = await api.get("/notifications") || [];
        const count = notificationItems.filter(item => !item.isRead).length;
        const el = document.getElementById("notificationCount");
        if (el) {
            el.textContent = String(count);
            el.hidden = count === 0;
        }
    } catch {}
}

function toggleNotificationCard() {
    document.getElementById("notificationCard")?.remove();
    if (document.getElementById("notificationCardOpening")) return;
    document.body.dataset.notificationCardOpening = "1";
    if (!notificationItems.length) {
        showToast("No notifications available.", "success");
        delete document.body.dataset.notificationCardOpening;
        return;
    }
    const card = document.createElement("div");
    card.id = "notificationCard";
    card.className = "notification-card";
    card.innerHTML = `<div class="notification-card-header"><strong><i class="bi bi-bell me-2"></i>Notifications</strong><button class="btn btn-sm btn-outline-success" type="button" id="markAllNotificationsRead">Mark all read</button></div>
        <div class="notification-card-list">${notificationItems.map(item => `<div class="notification-item${item.isRead ? "" : " unread"}" role="button" tabindex="0" data-notification-open="${item.notificationId}" data-notification-type="${escapeHtml(item.type)}" data-notification-title="${escapeHtml(item.title)}"><span class="notification-item-icon"><i class="bi ${item.type === "ImportantAlert" ? "bi-exclamation-triangle" : "bi-info-circle"}"></i></span><div class="notification-item-body"><strong>${escapeHtml(item.title)}</strong><p>${escapeHtml(item.message)}</p><small>${escapeHtml(new Date(item.createdAtUtc).toLocaleString("en-PH", { year: "numeric", month: "short", day: "numeric", hour: "2-digit", minute: "2-digit" }))}</small></div>${item.notificationId > 0 ? `<button class="btn btn-sm btn-light" type="button" data-notification-read="${item.notificationId}" title="Mark as read"><i class="bi bi-check2"></i></button>` : ""}</div>`).join("")}</div>
        <div class="notification-card-footer"><a href="settings.html#notifications">Notification settings</a></div>`;
    document.body.appendChild(card);
    delete document.body.dataset.notificationCardOpening;
    const close = event => {
        if (!card.contains(event.target) && event.target.closest("#notificationButton") === null) {
            card.remove();
            document.removeEventListener("click", close);
        }
    };
    setTimeout(() => document.addEventListener("click", close), 0);
    card.querySelector("#markAllNotificationsRead")?.addEventListener("click", async () => {
        try { await api.put("/notifications/read-all", {}); await updateNotificationBadge(); card.remove(); } catch (error) { showToast(error.message, "danger"); }
    });
    card.querySelectorAll("[data-notification-read]").forEach(button => button.addEventListener("click", async event => {
        event.stopPropagation();
        try { await api.put(`/notifications/${button.dataset.notificationRead}/read`, {}); await updateNotificationBadge(); toggleNotificationCard(); } catch (error) { showToast(error.message, "danger"); }
    }));
    card.querySelectorAll("[data-notification-open]").forEach(item => {
        const open = async () => {
            const id = Number(item.dataset.notificationOpen);
            const title = item.dataset.notificationTitle || "";
            const target = title.includes("Low stock") ? "supplies"
                : title.includes("New season") ? "dashboard"
                : title.includes("Overdue") || title.includes("Monitoring due") ? "utilization" : "reports";
            if (id > 0) {
                try { await api.put(`/notifications/${id}/read`, {}); await updateNotificationBadge(); } catch {}
            }
            card.remove();
            setView(target);
        };
        item.addEventListener("click", open);
        item.addEventListener("keydown", event => { if (event.key === "Enter" || event.key === " ") { event.preventDefault(); open(); } });
    });
}

document.getElementById("notificationButton")?.addEventListener("click", event => {
    event.stopPropagation();
    toggleNotificationCard();
});
updateNotificationBadge();

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
document.getElementById("sidebarExpandBtn")?.addEventListener("click", toggleSidebar);

initSidebarState();

syncHeaderAddButton();
updateTopbarSeason();
renderRiseDashboard();
document.getElementById("mainContent").classList.add("view-ready");
