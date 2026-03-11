(function () {
    var carrierEnabledStates = Object.create(null);
    var formDisabledElements = new WeakMap();
    var formPendingRequests = new WeakMap();
    var validationService = null;

    function isFieldElement(target) {
        return target instanceof HTMLInputElement
            || target instanceof HTMLTextAreaElement
            || target instanceof HTMLSelectElement;
    }

    function getById(id) {
        return id ? document.getElementById(id) : null;
    }

    function toArray(list) {
        return Array.prototype.slice.call(list || []);
    }

    function getIds(value) {
        return (value || '')
            .split(',')
            .map(function (id) { return id.trim(); })
            .filter(Boolean);
    }

    function getPolicyCarrier(input) {
        return getById(input.dataset.hrzLivePolicyId);
    }

    function getValidationRoot(element) {
        return element.closest('[data-hrz-validation-root]');
    }

    function getInputForFieldPath(root, fieldPath) {
        if (!root || !fieldPath) {
            return null;
        }

        return root.querySelector('[data-hrz-field-path="' + fieldPath + '"]');
    }

    function getInputForCarrierId(carrierId) {
        return document.querySelector('[data-hrz-live-policy-id="' + carrierId + '"]');
    }

    function clearSlotById(slotId) {
        var slot = getById(slotId);
        if (slot) {
            slot.innerHTML = '';
        }
    }

    function slotHasMessage(slotId) {
        var slot = getById(slotId);
        return !!slot && !!slot.textContent && slot.textContent.trim().length > 0;
    }

    function updateFieldState(input) {
        var invalid = input.classList.contains('input-validation-error')
            || slotHasMessage(input.dataset.hrzClientSlotId)
            || slotHasMessage(input.dataset.hrzServerSlotId);
        var field = input.closest('.validation-field');

        input.setAttribute('aria-invalid', invalid ? 'true' : 'false');
        if (field) {
            field.classList.toggle('validation-field--invalid', invalid);
        }
    }

    function syncFieldStates(root) {
        var scope = root || document;
        var fields = scope.querySelectorAll('[data-hrz-field-path]');

        Array.prototype.forEach.call(fields, function (field) {
            if (isFieldElement(field)) {
                updateFieldState(field);
            }
        });
    }

    function clearServerSlots(input, policy) {
        var root = getValidationRoot(input);
        var clearFields = policy ? getIds(policy.dataset.hrzLiveClearFields) : [];
        if (clearFields.length > 0) {
            clearFields.forEach(function (fieldPath) {
                var targetInput = getInputForFieldPath(root, fieldPath);
                if (targetInput && targetInput.dataset.hrzServerSlotId) {
                    clearSlotById(targetInput.dataset.hrzServerSlotId);
                }
            });
            return;
        }

        [input.dataset.hrzServerSlotId].concat(getIds(input.dataset.hrzDependentServerSlotIds))
            .forEach(clearSlotById);
    }

    function clearSummary(input, policy) {
        var summarySlotId = (policy && policy.dataset.hrzSummarySlotId) || input.dataset.hrzSummarySlotId;
        var summarySlot = getById(summarySlotId);
        if (summarySlot) {
            summarySlot.innerHTML = '';
            summarySlot.classList.add('validation-summary--empty');
        }
    }

    function clearServerState(input, policy) {
        clearServerSlots(input, policy);

        var shouldClearSummary = !policy
            || policy.dataset.hrzLiveReplaceSummaryWhenDisabled === 'true'
            || !!input.dataset.hrzSummarySlotId;
        if (shouldClearSummary) {
            clearSummary(input, policy);
        }

        syncFieldStates(getValidationRoot(input));
    }

    function getFormDisabledElementsSelector(form) {
        return form.dataset.hrzDisabledElt || '';
    }

    function getFormDisabledElementsTargets(form) {
        var selector = getFormDisabledElementsSelector(form).trim();
        if (!selector) {
            return [];
        }

        if (selector === 'this') {
            return [form];
        }

        if (selector.indexOf('find ') === 0) {
            selector = selector.slice(5).trim();
            if (!selector) {
                return [];
            }

            return toArray(form.querySelectorAll(selector));
        }

        return toArray(form.querySelectorAll(selector));
    }

    function disableFormTargets(form) {
        var pendingCount = formPendingRequests.get(form) || 0;
        formPendingRequests.set(form, pendingCount + 1);
        if (pendingCount > 0) {
            return;
        }

        var targets = getFormDisabledElementsTargets(form).map(function (element) {
            return {
                element: element,
                disabled: !!element.disabled
            };
        });

        formDisabledElements.set(form, targets);
        targets.forEach(function (target) {
            target.element.disabled = true;
        });
    }

    function restoreFormTargets(form) {
        var pendingCount = formPendingRequests.get(form) || 0;
        if (pendingCount <= 1) {
            formPendingRequests.delete(form);
        } else {
            formPendingRequests.set(form, pendingCount - 1);
            return;
        }

        var targets = formDisabledElements.get(form);
        if (!targets) {
            return;
        }

        targets.forEach(function (target) {
            if (target.element && 'disabled' in target.element) {
                target.element.disabled = target.disabled;
            }
        });

        formDisabledElements.delete(form);
    }

    function configureValidationService(service) {
        var defaultHighlight = service.highlight.bind(service);
        var defaultUnhighlight = service.unhighlight.bind(service);

        service.highlight = function (input, errorClass, validClass) {
            defaultHighlight(input, errorClass, validClass);
            updateFieldState(input);
        };

        service.unhighlight = function (input, errorClass, validClass) {
            defaultUnhighlight(input, errorClass, validClass);
            updateFieldState(input);
        };
    }

    function ensureValidationService() {
        if (validationService) {
            return validationService;
        }

        var aspnetValidation = window.aspnetValidation;
        if (!aspnetValidation || typeof aspnetValidation.ValidationService !== 'function') {
            return null;
        }

        validationService = new aspnetValidation.ValidationService();
        configureValidationService(validationService);
        validationService.bootstrap({
            root: document.body,
            watch: true,
            addNoValidate: true
        });
        window.hrzValidationService = validationService;

        return validationService;
    }

    function validateLocally(input) {
        if (input.getAttribute('data-val') !== 'true') {
            updateFieldState(input);
            return true;
        }

        var service = ensureValidationService();
        if (!service) {
            updateFieldState(input);
            return true;
        }

        var valid = service.isFieldValid(input, true);
        updateFieldState(input);
        return valid;
    }

    function handleInput(target) {
        var valid = validateLocally(target);
        var policy = getPolicyCarrier(target);

        if (!valid || !target.dataset.hrzLivePolicyId || (policy && policy.dataset.hrzLiveEnabled !== 'true')) {
            clearServerState(target, policy);
            return;
        }

        updateFieldState(target);
    }

    function isCarrierEnabled(carrier) {
        return !!carrier && carrier.dataset.hrzLiveEnabled === 'true';
    }

    function triggerImmediateRecheck(input) {
        window.setTimeout(function () {
            if (!document.body.contains(input)) {
                return;
            }

            if (window.htmx && typeof window.htmx.trigger === 'function') {
                window.htmx.trigger(input, 'blur');
                return;
            }

            input.dispatchEvent(new Event('blur', { bubbles: true }));
        }, 0);
    }

    function handleCarrierTransition(carrier, previousEnabled) {
        var currentEnabled = isCarrierEnabled(carrier);
        if (previousEnabled === currentEnabled) {
            return;
        }

        var input = getInputForCarrierId(carrier.id);
        if (!isFieldElement(input)) {
            return;
        }

        if (!currentEnabled) {
            clearServerState(input, carrier);
            return;
        }

        if (carrier.dataset.hrzImmediateRecheckWhenEnabled !== 'true') {
            return;
        }

        if ((input.value || '').trim().length === 0) {
            return;
        }

        if (!validateLocally(input)) {
            return;
        }

        triggerImmediateRecheck(input);
    }

    function syncPolicyCarriers(runTransitions) {
        var carriers = document.querySelectorAll('[data-hrz-live-policy-region] [id][data-hrz-live-enabled]');
        var seenIds = Object.create(null);

        Array.prototype.forEach.call(carriers, function (carrier) {
            seenIds[carrier.id] = true;

            var previousEnabled = carrierEnabledStates[carrier.id];
            if (runTransitions && typeof previousEnabled === 'boolean') {
                handleCarrierTransition(carrier, previousEnabled);
            }

            carrierEnabledStates[carrier.id] = isCarrierEnabled(carrier);
        });

        Object.keys(carrierEnabledStates).forEach(function (carrierId) {
            if (!seenIds[carrierId]) {
                delete carrierEnabledStates[carrierId];
            }
        });
    }

    document.addEventListener('input', function (event) {
        var target = event.target;
        if (!isFieldElement(target)) {
            return;
        }

        handleInput(target);
    });

    document.addEventListener('change', function (event) {
        var target = event.target;
        if (!isFieldElement(target)) {
            return;
        }

        handleInput(target);
    });

    document.body.addEventListener('htmx:configRequest', function (event) {
        var target = event.detail.elt;
        if (!isFieldElement(target)) {
            return;
        }

        if (!validateLocally(target)) {
            clearServerState(target, getPolicyCarrier(target));
            event.preventDefault();
            return;
        }

        var carrierId = target.dataset.hrzLivePolicyId;
        if (!carrierId) {
            return;
        }

        var policy = getPolicyCarrier(target);
        if (!policy) {
            event.preventDefault();
            return;
        }

        if (policy.dataset.hrzLiveEnabled !== 'true') {
            clearServerState(target, policy);
            event.preventDefault();
        }
    });

    document.body.addEventListener('htmx:beforeRequest', function (event) {
        var target = event.detail.elt;
        if (!(target instanceof HTMLFormElement)) {
            return;
        }

        disableFormTargets(target);
    });

    document.body.addEventListener('htmx:afterRequest', function (event) {
        var target = event.detail.elt;
        if (!(target instanceof HTMLFormElement)) {
            return;
        }

        restoreFormTargets(target);
    });

    document.body.addEventListener('htmx:afterSettle', function () {
        syncPolicyCarriers(true);
        syncFieldStates();
    });

    ensureValidationService();
    syncPolicyCarriers(false);
    syncFieldStates();
})();
