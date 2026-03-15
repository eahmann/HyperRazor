(function () {
    "use strict";

    function updateThemeButtons(theme) {
        var buttons = document.querySelectorAll(".chrome-theme-form button[data-theme-option]");
        buttons.forEach(function (button) {
            var option = button.getAttribute("data-theme-option");
            var isActive = option === theme;
            button.setAttribute("aria-pressed", isActive ? "true" : "false");
            button.classList.add("btn", "btn-sm");
            button.classList.toggle("btn-primary", isActive);
            button.classList.toggle("btn-outline-secondary", !isActive);
        });
    }

    function updateThemeStylesheet(href) {
        if (!href) {
            return;
        }

        var themeLink = document.getElementById("hrz-theme-css");
        if (!themeLink) {
            return;
        }

        var currentHref = themeLink.getAttribute("href");
        if (currentHref === href) {
            return;
        }

        themeLink.setAttribute("href", href);
    }

    function applyThemeUpdate(event) {
        var detail = event && event.detail ? event.detail : null;
        if (!detail || !detail.theme) {
            return;
        }

        document.documentElement.setAttribute("data-bs-theme", detail.theme);
        updateThemeStylesheet(detail.href);
        updateThemeButtons(detail.theme);
    }

    function bootstrap() {
        var body = document.body;
        if (!body) {
            return;
        }

        body.addEventListener("chrome:theme-updated", applyThemeUpdate);
    }

    if (document.readyState === "loading") {
        document.addEventListener("DOMContentLoaded", bootstrap);
    } else {
        bootstrap();
    }
})();
