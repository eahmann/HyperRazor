(function () {
    var carrierEnabledStates = Object.create(null);

    function isFieldElement(target) {
        return target instanceof HTMLInputElement
            || target instanceof HTMLTextAreaElement
            || target instanceof HTMLSelectElement;
    }

    function getById(id) {
        return id ? document.getElementById(id) : null;
    }

    function getIds(value) {
        return (value || '')
            .split(',')
            .map(function (id) { return id.trim(); })
            .filter(Boolean);
    }

    function setMessage(target, message) {
        if (!target) {
            return;
        }

        target.innerHTML = message
            ? '<p class="validation-message validation-message--client">' + message + '</p>'
            : '';
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
        var serverSlot = getById(slotId);
        if (serverSlot) {
            serverSlot.innerHTML = '';
        }
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
    }

    function validateMinLength(input) {
        var clientSlot = getById(input.dataset.hrzClientSlotId);
        var value = (input.value || '').trim();
        var minLength = parseInt(input.dataset.hrzLocalMinLength || '0', 10);
        var message = input.dataset.hrzLocalMinLengthMessage || ('Must be at least ' + minLength + ' characters.');

        if (value.length === 0) {
            setMessage(clientSlot, '');
            clearServerState(input, getPolicyCarrier(input));
            return true;
        }

        if (value.length < minLength) {
            setMessage(clientSlot, message);
            clearServerState(input, getPolicyCarrier(input));
            return false;
        }

        setMessage(clientSlot, '');
        return true;
    }

    function validateEmail(input) {
        var clientSlot = getById(input.dataset.hrzClientSlotId);
        var value = (input.value || '').trim();
        if (value.length === 0) {
            setMessage(clientSlot, '');
            clearServerState(input, getPolicyCarrier(input));
            return false;
        }

        if (input.validity.typeMismatch || input.validity.patternMismatch) {
            setMessage(clientSlot, 'Email must be a valid address.');
            clearServerState(input, getPolicyCarrier(input));
            return false;
        }

        setMessage(clientSlot, '');
        return true;
    }

    function validateLocally(input) {
        var rule = input.dataset.hrzLocalValidation;
        if (!rule) {
            return true;
        }

        if (rule === 'min-length') {
            return validateMinLength(input);
        }

        if (rule === 'email') {
            return validateEmail(input);
        }

        return true;
    }

    function handleInput(target) {
        validateLocally(target);
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

    document.body.addEventListener('htmx:afterSettle', function () {
        syncPolicyCarriers(true);
    });

    syncPolicyCarriers(false);
})();
