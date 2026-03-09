(function () {
    "use strict";

    var validationApi = window.aspnetValidation;
    if (!validationApi || !validationApi.ValidationService) {
        return;
    }

    function isValidatable(element) {
        return element instanceof HTMLInputElement
            || element instanceof HTMLSelectElement
            || element instanceof HTMLTextAreaElement;
    }

    function isManagedForm(element) {
        return element instanceof HTMLFormElement
            && element.getAttribute("data-hrz-client-validation-form") === "true";
    }

    function isManagedField(element) {
        return isValidatable(element)
            && element.getAttribute("data-val") === "true"
            && element.form instanceof HTMLFormElement
            && isManagedForm(element.form);
    }

    function getById(id) {
        return id ? document.getElementById(id) : null;
    }

    function getIds(value) {
        return (value || "")
            .split(",")
            .map(function (id) { return id.trim(); })
            .filter(Boolean);
    }

    function escapeHtml(value) {
        return (value || "")
            .replace(/&/g, "&amp;")
            .replace(/</g, "&lt;")
            .replace(/>/g, "&gt;")
            .replace(/"/g, "&quot;")
            .replace(/'/g, "&#39;");
    }

    function setClientMessage(element, message) {
        if (!element) {
            return;
        }

        element.innerHTML = message
            ? '<p class="validation-message validation-message--client">' + escapeHtml(message) + "</p>"
            : "";
    }

    function syncInputState(input, invalid) {
        input.setAttribute("aria-invalid", invalid ? "true" : "false");
    }

    function clearServerFeedback(input) {
        var slotIds = [input.dataset.hrzServerSlotId].concat(getIds(input.dataset.hrzDependentServerSlotIds));
        slotIds.forEach(function (slotId) {
            var serverSlot = getById(slotId);
            if (serverSlot) {
                serverSlot.innerHTML = "";
            }
        });

        var summarySlot = getById(input.dataset.hrzSummarySlotId);
        if (summarySlot) {
            summarySlot.innerHTML = "";
            summarySlot.classList.add("validation-summary--empty");
        }
    }

    function clearFormServerFeedback(form) {
        var fields = form.querySelectorAll('[data-val="true"]');
        for (var i = 0; i < fields.length; i++) {
            var field = fields[i];
            if (isValidatable(field)) {
                clearServerFeedback(field);
            }
        }
    }

    function escapeAttributeValue(value) {
        return value.replace(/\\/g, "\\\\").replace(/"/g, '\\"');
    }

    function setValidationMessageState(service, element, invalid) {
        service.swapClasses(
            element,
            invalid ? service.ValidationMessageCssClassName : service.ValidationMessageValidCssClassName,
            invalid ? service.ValidationMessageValidCssClassName : service.ValidationMessageCssClassName);
    }

    function patchValidationService(service) {
        var originalShouldValidate = service.shouldValidate.bind(service);
        service.shouldValidate = function (event) {
            var form = event && event.target;
            if (isManagedForm(form) && (form.hasAttribute("hx-post") || form.hasAttribute("data-hx-post"))) {
                return false;
            }

            return originalShouldValidate(event);
        };

        service.highlight = function (input, errorClass, validClass) {
            this.swapClasses(input, errorClass, validClass);
            syncInputState(input, true);
        };

        service.unhighlight = function (input, errorClass, validClass) {
            this.swapClasses(input, validClass, errorClass);
            syncInputState(input, false);
        };

        service.addError = function (input, message) {
            var spans = this.getMessageFor(input);
            if (spans) {
                for (var i = 0; i < spans.length; i++) {
                    setClientMessage(spans[i], message);
                    setValidationMessageState(this, spans[i], true);
                }
            }

            this.highlight(input, this.ValidationInputCssClassName, this.ValidationInputValidCssClassName);

            if (input.form) {
                var name = escapeAttributeValue(input.name);
                var inputs = input.form.querySelectorAll('[name="' + name + '"]');
                for (var i = 0; i < inputs.length; i++) {
                    var matchingInput = inputs[i];
                    if (!isValidatable(matchingInput)) {
                        continue;
                    }

                    this.swapClasses(matchingInput, this.ValidationInputCssClassName, this.ValidationInputValidCssClassName);
                    syncInputState(matchingInput, true);
                    this.summary[this.getElementUID(matchingInput)] = message;
                }
            }

            this.renderSummary();
        };

        service.removeError = function (input) {
            var spans = this.getMessageFor(input);
            if (spans) {
                for (var i = 0; i < spans.length; i++) {
                    setClientMessage(spans[i], "");
                    setValidationMessageState(this, spans[i], false);
                }
            }

            this.unhighlight(input, this.ValidationInputCssClassName, this.ValidationInputValidCssClassName);

            if (input.form) {
                var name = escapeAttributeValue(input.name);
                var inputs = input.form.querySelectorAll('[name="' + name + '"]');
                for (var i = 0; i < inputs.length; i++) {
                    var matchingInput = inputs[i];
                    if (!isValidatable(matchingInput)) {
                        continue;
                    }

                    this.swapClasses(matchingInput, this.ValidationInputValidCssClassName, this.ValidationInputCssClassName);
                    syncInputState(matchingInput, false);
                    delete this.summary[this.getElementUID(matchingInput)];
                }
            }

            this.renderSummary();
        };
    }

    function scanManagedForms(service, root) {
        var managedForms = [];

        if (isManagedForm(root)) {
            managedForms.push(root);
        }
        else if (root && typeof root.querySelectorAll === "function") {
            managedForms = Array.from(root.querySelectorAll('form[data-hrz-client-validation-form="true"]'));
        }

        managedForms.forEach(function (form) {
            service.scan(form);
        });
    }

    function bootstrap() {
        var service = new validationApi.ValidationService(console);
        patchValidationService(service);
        service.debounce = 0;
        service.bootstrap({
            root: document.createElement("div"),
            watch: false,
            addNoValidate: false
        });

        scanManagedForms(service, document);

        document.addEventListener("input", function (event) {
            var target = event.target;
            if (!isManagedField(target)) {
                return;
            }

            clearServerFeedback(target);
        });

        document.addEventListener("change", function (event) {
            var target = event.target;
            if (!isManagedField(target)) {
                return;
            }

            clearServerFeedback(target);
        });

        document.addEventListener("blur", function (event) {
            var target = event.target;
            if (!isManagedField(target)) {
                return;
            }

            clearServerFeedback(target);
        }, true);

        document.body.addEventListener("htmx:configRequest", function (event) {
            var target = event.detail && event.detail.elt;

            if (isManagedField(target)) {
                if (!service.isFieldValid(target)) {
                    clearServerFeedback(target);
                    event.preventDefault();
                }

                return;
            }

            if (isManagedForm(target)) {
                if (!service.isValid(target)) {
                    clearFormServerFeedback(target);
                    event.preventDefault();
                }
            }
        });

        document.body.addEventListener("htmx:afterSettle", function (event) {
            scanManagedForms(service, event.target || document);
        });
    }

    if (document.readyState === "loading") {
        document.addEventListener("DOMContentLoaded", bootstrap);
    }
    else {
        bootstrap();
    }
})();
