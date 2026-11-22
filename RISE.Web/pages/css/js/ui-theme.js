/*
 * RISE – chart styling only.
 * Chart.js defaults are tuned for light pages, so on the dark glass theme the axis
 * text, grid lines and bar/doughnut fills are barely visible. This file only sets
 * visual defaults; it does not touch data, API calls or any application logic.
 * Load order: chart.js -> ui-theme.js -> api.js
 */
(function () {
    if (typeof Chart === "undefined") return;

    const palette = ["#d6a83e", "#78a85a", "#7fb2c8", "#e4c59d", "#b9d9b1", "#d9685c", "#a58fd1", "#6fbfb0"];

    Chart.defaults.color = "#d0ddd3";
    Chart.defaults.borderColor = "rgba(255, 255, 255, 0.12)";
    Chart.defaults.font.family = '"DM Sans", "Segoe UI", sans-serif';
    Chart.defaults.font.size = 12;
    Chart.defaults.plugins.legend.position = "bottom";
    Chart.defaults.plugins.legend.labels.usePointStyle = true;
    Chart.defaults.plugins.legend.labels.boxWidth = 8;
    Chart.defaults.plugins.legend.labels.padding = 16;
    Chart.defaults.plugins.tooltip.backgroundColor = "rgba(10, 35, 22, 0.95)";
    Chart.defaults.plugins.tooltip.borderColor = "rgba(255, 255, 255, 0.15)";
    Chart.defaults.plugins.tooltip.borderWidth = 1;
    Chart.defaults.plugins.tooltip.padding = 10;

    // Give datasets a readable colour palette when the page did not specify one.
    Chart.register({
        id: "risePalette",
        beforeUpdate(chart) {
            const type = chart.config.type;
            chart.data.datasets.forEach(dataset => {
                if (dataset.backgroundColor !== undefined) return;
                if (type === "doughnut" || type === "pie") {
                    dataset.backgroundColor = dataset.data.map((_, i) => palette[i % palette.length]);
                    dataset.borderColor = "rgba(16, 47, 35, 0.9)";
                    dataset.borderWidth = 2;
                    dataset.hoverOffset = 6;
                } else {
                    dataset.backgroundColor = palette[0];
                    dataset.borderColor = palette[0];
                    dataset.borderRadius = 6;
                    dataset.maxBarThickness = 44;
                }
            });
        }
    });

    if (Chart.overrides && Chart.overrides.doughnut) Chart.overrides.doughnut.cutout = "62%";

    /* --------------------------------------------------------------
       RISE live header clock
       -------------------------------------------------------------- */
    (function () {
        const clock = document.getElementById("headerClock");
        const options = { weekday: "short", month: "short", day: "numeric", hour: "2-digit", minute: "2-digit" };
        function tick() {
            if (!clock) return;
            clock.textContent = new Date().toLocaleString(undefined, options);
        }
        tick();
        setInterval(tick, 1000);
    })();
})();
