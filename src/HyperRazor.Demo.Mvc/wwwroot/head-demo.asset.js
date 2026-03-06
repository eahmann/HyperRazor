(function () {
    var state = window.hrzHeadDemoState;
    if (!state) {
        state = {
            loadCount: 0,
            listenerAttached: false
        };
        window.hrzHeadDemoState = state;
    }

    state.loadCount += 1;

    function syncStatus() {
        var status = document.getElementById("head-script-status");
        if (status) {
            status.textContent = "Script asset active. Load count: " + state.loadCount + ".";
        }

        document.body.setAttribute("data-head-demo-script-loads", String(state.loadCount));
    }

    if (!state.listenerAttached) {
        document.body.addEventListener("htmx:afterSettle", syncStatus);
        state.listenerAttached = true;
    }

    if (document.readyState === "loading") {
        document.addEventListener("DOMContentLoaded", syncStatus, { once: true });
    } else {
        syncStatus();
    }
})();
