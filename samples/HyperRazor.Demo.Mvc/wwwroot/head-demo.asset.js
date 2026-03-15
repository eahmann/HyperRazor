(function () {
    var assetKey = "head-demo-script";
    var state = window.hrzHeadDemoState;
    if (!state) {
        state = {
            assetKeys: {},
            listenerAttached: false
        };
        window.hrzHeadDemoState = state;
    }

    state.assetKeys[assetKey] = true;

    function syncStatus() {
        var assetCount = Object.keys(state.assetKeys).length;
        var status = document.getElementById("head-script-status");
        if (status) {
            status.textContent = "Script asset active. Keyed asset count: " + assetCount + ".";
        }

        document.body.setAttribute("data-head-demo-script-assets", String(assetCount));
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
