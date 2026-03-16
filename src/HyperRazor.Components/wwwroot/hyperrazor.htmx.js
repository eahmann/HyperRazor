(function () {
    "use strict";

    var CURRENT_LAYOUT_HEADER = "X-Hrz-Current-Layout";
    var CURRENT_LAYOUT_SELECTOR = "template[data-hrz-current-layout]";

    function readBodySetting(name, fallback) {
        var body = document.body;
        if (!body) {
            return fallback;
        }

        var value = body.getAttribute(name);
        return value && value.trim().length > 0 ? value.trim() : fallback;
    }

    function readMetaContent(name) {
        var selector = 'meta[name="' + name + '"]';
        var meta = document.querySelector(selector);
        if (!meta) {
            return null;
        }

        var value = meta.getAttribute("content");
        return value && value.trim().length > 0 ? value.trim() : null;
    }

    function isSafeMethod(method) {
        var normalized = (method || "").toUpperCase();
        return normalized === "GET" || normalized === "HEAD" || normalized === "OPTIONS" || normalized === "TRACE";
    }

    function resolveMethod(detail) {
        if (!detail) {
            return "";
        }

        if (detail.verb) {
            return detail.verb;
        }

        if (detail.requestConfig && detail.requestConfig.verb) {
            return detail.requestConfig.verb;
        }

        if (detail.xhr && detail.xhr.method) {
            return detail.xhr.method;
        }

        return "";
    }

    function resolveHeaders(detail) {
        if (!detail) {
            return null;
        }

        if (detail.headers) {
            return detail.headers;
        }

        if (detail.requestConfig && detail.requestConfig.headers) {
            return detail.requestConfig.headers;
        }

        return null;
    }

    function applyAntiforgery(event) {
        var detail = event && event.detail ? event.detail : null;
        var method = resolveMethod(detail);
        if (isSafeMethod(method)) {
            return;
        }

        var headers = resolveHeaders(detail);
        if (!headers) {
            return;
        }

        var metaName = readBodySetting("data-hrz-antiforgery-meta", "hrz-antiforgery");
        var headerName = readBodySetting("data-hrz-antiforgery-header", "RequestVerificationToken");
        var token = readMetaContent(metaName);
        if (!token) {
            return;
        }

        headers[headerName] = token;
    }

    function readCurrentLayout() {
        var markers = document.querySelectorAll(CURRENT_LAYOUT_SELECTOR);
        if (!markers || markers.length === 0) {
            return null;
        }

        var marker = markers[markers.length - 1];
        var value = marker.getAttribute("data-hrz-current-layout");
        return value && value.trim().length > 0 ? value.trim() : null;
    }

    function applyCurrentLayout(event) {
        var detail = event && event.detail ? event.detail : null;
        var headers = resolveHeaders(detail);
        if (!headers) {
            return;
        }

        var currentLayout = readCurrentLayout();
        if (!currentLayout) {
            return;
        }

        headers[CURRENT_LAYOUT_HEADER] = currentLayout;
    }

    function ensureHeadSupport() {
        var body = document.body;
        if (!body) {
            return;
        }

        var enabled = body.getAttribute("data-hrz-head-support");
        if (enabled !== "true") {
            return;
        }

        var existing = body.getAttribute("hx-ext");
        if (!existing) {
            body.setAttribute("hx-ext", "head-support");
            return;
        }

        if (!existing.split(",").map(function (x) { return x.trim(); }).includes("head-support")) {
            body.setAttribute("hx-ext", existing + ", head-support");
        }
    }

    function bootstrap() {
        var body = document.body;
        if (!body) {
            return;
        }

        body.addEventListener("htmx:configRequest", applyAntiforgery);
        body.addEventListener("htmx:config:request", applyAntiforgery);
        body.addEventListener("htmx:configRequest", applyCurrentLayout);
        body.addEventListener("htmx:config:request", applyCurrentLayout);
        ensureHeadSupport();
    }

    if (document.readyState === "loading") {
        document.addEventListener("DOMContentLoaded", bootstrap);
    } else {
        bootstrap();
    }
})();
