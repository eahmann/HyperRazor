(function () {
    "use strict";

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

        var metaName = readBodySetting("data-hrx-antiforgery-meta", "hrx-antiforgery");
        var headerName = readBodySetting("data-hrx-antiforgery-header", "RequestVerificationToken");
        var token = readMetaContent(metaName);
        if (!token) {
            return;
        }

        headers[headerName] = token;
    }

    function applyLayoutFamilyHeader(event) {
        var detail = event && event.detail ? event.detail : null;
        var headers = resolveHeaders(detail);
        if (!headers) {
            return;
        }

        var shell = document.querySelector("#hrx-app-shell");
        if (!shell) {
            return;
        }

        var family = shell.getAttribute("data-hrx-layout-family");
        if (!family || family.trim().length === 0) {
            return;
        }

        headers["X-Hrx-Layout-Family"] = family.trim();
    }

    function ensureHeadSupport() {
        var body = document.body;
        if (!body) {
            return;
        }

        var enabled = body.getAttribute("data-hrx-head-support");
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
        body.addEventListener("htmx:configRequest", applyLayoutFamilyHeader);
        body.addEventListener("htmx:config:request", applyLayoutFamilyHeader);
        ensureHeadSupport();
    }

    if (document.readyState === "loading") {
        document.addEventListener("DOMContentLoaded", bootstrap);
    } else {
        bootstrap();
    }
})();
