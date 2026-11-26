(function () {
    const SECTION_ID = "distributionScheduleSection";
    const state = { year: "", month: "", season: "", barangay: "", type: "", newestFirst: true };
    let rows = [];

    function statusMeta(status) {
        if (status === "Distributed") return { label: "Received", cssClass: "status-received" };
        if (status === "Cancelled") return { label: "Cancelled", cssClass: "status-cancelled" };
        if (status === "Approved") return { label: "Approved", cssClass: "status-pending" };
        return { label: "Pending", cssClass: "status-pending" };
    }

    async function load() {
        const body = document.getElementById("scheduleTableBody");
        if (body) body.innerHTML = `<tr><td colspan="10" class="empty-state">Loading distribution schedule...</td></tr>`;
        const params = new URLSearchParams();
        if (state.year) params.set("year", state.year);
        if (state.season) params.set("season", state.season);
        if (state.barangay) params.set("barangay", state.barangay);
        if (state.type) params.set("type", state.type);
        try {
            rows = await api.get(`/reports/distribution-schedule${params.size ? `?${params}` : ""}`) || [];
            if (state.month) rows = rows.filter(row => String(row.distributionDate || "").slice(5, 7) === state.month);
            rows = [...rows].sort((a, b) => {
                const diff = String(a.createdAtUtc || "").localeCompare(String(b.createdAtUtc || ""));
                return state.newestFirst ? -diff : diff;
            });
        } catch (error) {
            if (body) body.innerHTML = `<tr><td colspan="10" class="empty-state">${escapeHtml(error.message)}</td></tr>`;
            return;
        }
        renderTable();
    }

    function renderTable() {
        const body = document.getElementById("scheduleTableBody");
        if (!body) return;
        if (!rows.length) {
            body.innerHTML = `<tr><td colspan="10" class="empty-state"><i class="bi bi-inbox"></i>No distribution records match these filters.</td></tr>`;
            return;
        }
        body.innerHTML = rows.map(row => {
            const meta = statusMeta(row.distributionStatus);
            return `<tr>
                <td class="tl col-text-nowrap">${escapeHtml(row.distributionDate || "—")}</td>
                <td class="tc"><span class="distribution-status-badge ${row.croppingSeason === "Wet" ? "status-pending" : "status-received"}"><span class="status-dot"></span>${escapeHtml(row.croppingSeason || "—")}</span></td>
                <td class="tl">${escapeHtml(row.farmerName || "—")}</td>
                <td class="tl">${escapeHtml(row.assistanceType || "Other")}</td>
                <td class="tl col-text-wrap">${escapeHtml(row.itemName || "—")}</td>
                <td class="tc">${escapeHtml(row.quantityDistributed ?? 0)}</td>
                <td class="tc">${escapeHtml(row.farmerBeneficiaries ?? 0)}</td>
                <td class="tl col-text-wrap">${escapeHtml(row.barangayCovered || "—")}</td>
                <td class="tc"><span class="distribution-status-badge ${meta.cssClass}"><span class="status-dot"></span>${escapeHtml(meta.label)}</span></td>
                <td class="tc action-buttons"><button class="btn btn-sm btn-outline-secondary" type="button" data-schedule-view="${row.distributionId}" title="View distribution details"><i class="bi bi-eye"></i></button></td>
            </tr>`;
        }).join("");
        body.querySelectorAll("[data-schedule-view]").forEach(button => button.addEventListener("click", () => {
            if (typeof openDistributionModal === "function") openDistributionModal("view", Number(button.dataset.scheduleView));
        }));
    }

    function render() {
        if (document.getElementById(SECTION_ID)) return;
        const mainContent = document.getElementById("mainContent");
        if (!mainContent) return;
        const host = document.createElement("section");
        host.id = SECTION_ID;
        host.innerHTML = `
            <div class="view-header"><div><p class="eyebrow">HISTORY</p><h2><i class="bi bi-clock-history me-2"></i>History</h2></div></div>
            <section class="content-panel">
                <form class="filter-bar" id="scheduleFilter">
                    <input class="form-control" id="scheduleYear" type="number" min="2000" max="2100" placeholder="Year">
                    <select class="form-select" id="scheduleMonth"><option value="">All months</option>${["January","February","March","April","May","June","July","August","September","October","November","December"].map((name,index)=>`<option value="${String(index+1).padStart(2,"0")}">${name}</option>`).join("")}</select>
                    <select class="form-select" id="scheduleSeason"><option value="">All seasons</option><option value="Wet">Wet</option><option value="Dry">Dry</option></select>
                    <input class="form-control" id="scheduleBarangay" type="text" placeholder="Barangay">
                    <select class="form-select" id="scheduleType"><option value="">All assistance types</option><option value="Seeds">Seeds</option><option value="Fertilizer">Fertilizer</option><option value="Other">Other</option></select>
                    <button class="btn btn-success" type="submit"><i class="bi bi-search me-1"></i> Filter</button>
                    <button class="btn btn-light" type="button" id="scheduleSortBtn"><i class="bi bi-arrow-down-up me-1"></i> Sort: ${state.newestFirst ? "Newest first" : "Oldest first"}</button>
                    <button class="btn btn-light" type="button" id="scheduleClear">Clear</button>
                </form>
                <div class="table-responsive"><table class="table align-middle mb-0">
                    <thead><tr><th class="tl">Distribution date</th><th class="tc">Cropping season</th><th class="tl">Farmer name</th><th class="tl">Assistance type</th><th class="tl">Item</th><th class="tc">Quantity distributed</th><th class="tc">Farmer beneficiaries</th><th class="tl">Barangay covered</th><th class="tc">Status</th><th class="tc">Actions</th></tr></thead>
                    <tbody id="scheduleTableBody"><tr><td colspan="10" class="empty-state">Loading distribution schedule...</td></tr></tbody>
                </table></div>
            </section>`;
        mainContent.appendChild(host);

        document.getElementById("scheduleFilter").addEventListener("submit", event => {
            event.preventDefault();
            state.year = document.getElementById("scheduleYear").value.trim();
            state.season = document.getElementById("scheduleSeason").value;
            state.barangay = document.getElementById("scheduleBarangay").value.trim();
            state.type = document.getElementById("scheduleType").value;
            load();
        });
        document.getElementById("scheduleSortBtn")?.addEventListener("click", event => {
            state.newestFirst = !state.newestFirst;
            event.currentTarget.innerHTML = `<i class="bi bi-arrow-down-up me-1"></i> Sort: ${state.newestFirst ? "Newest first" : "Oldest first"}`;
            rows = [...rows].sort((a, b) => {
                const diff = String(a.createdAtUtc || "").localeCompare(String(b.createdAtUtc || ""));
                return state.newestFirst ? -diff : diff;
            });
            renderTable();
        });
        document.getElementById("scheduleClear").addEventListener("click", () => {
            document.getElementById("scheduleYear").value = "";
            document.getElementById("scheduleSeason").value = "";
            document.getElementById("scheduleBarangay").value = "";
            document.getElementById("scheduleType").value = "";
            state.year = ""; state.season = ""; state.barangay = ""; state.type = "";
            load();
        });
        load();
    }

    window.RISEDistributionSchedule = { render };
})();
