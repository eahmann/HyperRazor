(function () {
    // Shared DOM and attribute helpers used across the validation runtime.
    var validationDom = (function () {
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

        return {
            isFieldElement: isFieldElement,
            getById: getById,
            toArray: toArray,
            getIds: getIds,
            getPolicyCarrier: getPolicyCarrier,
            getValidationRoot: getValidationRoot,
            getInputForFieldPath: getInputForFieldPath,
            getInputForCarrierId: getInputForCarrierId,
            clearSlotById: clearSlotById,
            slotHasMessage: slotHasMessage
        };
    })();

    // Field-level UI state and stale server-slot cleanup.
    var fieldStateRuntime = (function (dom) {
        function updateFieldState(input) {
            var invalid = input.classList.contains('input-validation-error')
                || dom.slotHasMessage(input.dataset.hrzClientSlotId)
                || dom.slotHasMessage(input.dataset.hrzServerSlotId);
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
                if (dom.isFieldElement(field)) {
                    updateFieldState(field);
                }
            });
        }

        function clearServerSlots(input, policy) {
            var root = dom.getValidationRoot(input);
            var clearFields = policy ? dom.getIds(policy.dataset.hrzLiveClearFields) : [];
            if (clearFields.length > 0) {
                clearFields.forEach(function (fieldPath) {
                    var targetInput = dom.getInputForFieldPath(root, fieldPath);
                    if (targetInput && targetInput.dataset.hrzServerSlotId) {
                        dom.clearSlotById(targetInput.dataset.hrzServerSlotId);
                    }
                });
                return;
            }

            [input.dataset.hrzServerSlotId].concat(dom.getIds(input.dataset.hrzDependentServerSlotIds))
                .forEach(dom.clearSlotById);
        }

        function clearSummary(input, policy) {
            var summarySlotId = (policy && policy.dataset.hrzSummarySlotId) || input.dataset.hrzSummarySlotId;
            var summarySlot = dom.getById(summarySlotId);
            if (summarySlot) {
                summarySlot.innerHTML = '';
                summarySlot.classList.add('validation-summary--empty');
            }
        }

        function clearServerState(input, policy) {
            clearServerSlots(input, policy);

            var shouldClearSummary = !!input.dataset.hrzSummarySlotId
                || (!!policy && policy.dataset.hrzLiveReplaceSummaryWhenDisabled === 'true');
            if (shouldClearSummary) {
                clearSummary(input, policy);
            }

            syncFieldStates(dom.getValidationRoot(input));
        }

        return {
            updateFieldState: updateFieldState,
            syncFieldStates: syncFieldStates,
            clearServerState: clearServerState
        };
    })(validationDom);

    // HTMX form request disable/restore handling.
    var formRequestRuntime = (function (dom) {
        var formDisabledElements = new WeakMap();
        var formPendingRequests = new WeakMap();

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
            }

            return dom.toArray(form.querySelectorAll(selector));
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

        function isHtmxEnhancedForm(form) {
            return form instanceof HTMLFormElement
                && (
                    form.hasAttribute('hx-get')
                    || form.hasAttribute('data-hx-get')
                    || form.hasAttribute('hx-post')
                    || form.hasAttribute('data-hx-post')
                    || form.hasAttribute('hx-put')
                    || form.hasAttribute('data-hx-put')
                    || form.hasAttribute('hx-patch')
                    || form.hasAttribute('data-hx-patch')
                    || form.hasAttribute('hx-delete')
                    || form.hasAttribute('data-hx-delete')
                );
        }

        return {
            disableFormTargets: disableFormTargets,
            restoreFormTargets: restoreFormTargets,
            isHtmxEnhancedForm: isHtmxEnhancedForm
        };
    })(validationDom);

    // Local validation adapter lifecycle and public registration hooks.
    var localValidationRuntime = (function (fieldState, formRequests) {
        var localValidationAdapter = null;
        var localValidationAdapterFactory = null;
        var localValidationProviders = Object.create(null);

        function configureAspNetValidationService(service) {
            var defaultHighlight = service.highlight.bind(service);
            var defaultUnhighlight = service.unhighlight.bind(service);
            var defaultShouldValidate = service.shouldValidate.bind(service);

            service.highlight = function (input, errorClass, validClass) {
                defaultHighlight(input, errorClass, validClass);
                fieldState.updateFieldState(input);
            };

            service.unhighlight = function (input, errorClass, validClass) {
                defaultUnhighlight(input, errorClass, validClass);
                fieldState.updateFieldState(input);
            };

            service.shouldValidate = function (event) {
                if (event && event.type === 'submit' && formRequests.isHtmxEnhancedForm(event.target)) {
                    return false;
                }

                return defaultShouldValidate(event);
            };
        }

        function applyRegisteredLocalValidationProviders(adapter) {
            if (!adapter || typeof adapter.registerProvider !== 'function') {
                return;
            }

            Object.keys(localValidationProviders).forEach(function (name) {
                adapter.registerProvider(name, localValidationProviders[name]);
            });
        }

        function createAspNetLocalValidationAdapter(options) {
            var aspnetValidation = window.aspnetValidation;
            if (!aspnetValidation || typeof aspnetValidation.ValidationService !== 'function') {
                return null;
            }

            var service = new aspnetValidation.ValidationService();
            configureAspNetValidationService(service);
            applyRegisteredLocalValidationProviders({
                registerProvider: function (name, provider) {
                    service.addProvider(name, provider);
                }
            });
            service.bootstrap({
                root: options.root,
                watch: true,
                addNoValidate: true
            });

            return {
                name: 'aspnet-client-validation',
                instance: service,
                validateField: function (input) {
                    return service.isFieldValid(input, true);
                },
                registerProvider: function (name, provider) {
                    service.addProvider(name, provider);
                },
                refresh: function (root) {
                    service.scan(root || options.root);
                },
                destroy: function () {
                    if (typeof service.remove === 'function') {
                        service.remove(options.root);
                    }

                    if (service.observer && typeof service.observer.disconnect === 'function') {
                        service.observer.disconnect();
                    }
                }
            };
        }

        function resolveLocalValidationAdapterFactory() {
            if (localValidationAdapterFactory) {
                return localValidationAdapterFactory;
            }

            var configuredFactory = window.hyperRazorValidationConfig
                && typeof window.hyperRazorValidationConfig.createLocalValidationAdapter === 'function'
                ? window.hyperRazorValidationConfig.createLocalValidationAdapter
                : null;

            localValidationAdapterFactory = configuredFactory || createAspNetLocalValidationAdapter;
            return localValidationAdapterFactory;
        }

        function destroyLocalValidationAdapter() {
            if (!localValidationAdapter) {
                return;
            }

            if (typeof localValidationAdapter.destroy === 'function') {
                localValidationAdapter.destroy();
            }

            localValidationAdapter = null;
            window.hrzValidationService = null;
        }

        function ensureLocalValidationAdapter() {
            if (localValidationAdapter) {
                return localValidationAdapter;
            }

            var factory = resolveLocalValidationAdapterFactory();
            if (typeof factory !== 'function') {
                return null;
            }

            localValidationAdapter = factory({
                root: document.body,
                updateFieldState: fieldState.updateFieldState
            });

            if (!localValidationAdapter) {
                return null;
            }

            applyRegisteredLocalValidationProviders(localValidationAdapter);
            window.hrzValidationService = localValidationAdapter.instance || null;

            return localValidationAdapter;
        }

        function registerLocalValidationProvider(name, provider) {
            if (!name || typeof provider !== 'function') {
                return false;
            }

            localValidationProviders[name] = provider;

            if (localValidationAdapter && typeof localValidationAdapter.registerProvider === 'function') {
                localValidationAdapter.registerProvider(name, provider);
            }

            return true;
        }

        function refreshLocalValidation(root) {
            var adapter = ensureLocalValidationAdapter();
            if (!adapter || typeof adapter.refresh !== 'function') {
                return;
            }

            adapter.refresh(root || document.body);
        }

        function setLocalValidationAdapterFactory(factory) {
            if (factory !== null && typeof factory !== 'function') {
                return false;
            }

            localValidationAdapterFactory = factory || createAspNetLocalValidationAdapter;
            destroyLocalValidationAdapter();
            ensureLocalValidationAdapter();
            fieldState.syncFieldStates();
            return true;
        }

        function validateLocally(input) {
            if (input.getAttribute('data-val') !== 'true') {
                fieldState.updateFieldState(input);
                return true;
            }

            var adapter = ensureLocalValidationAdapter();
            if (!adapter || typeof adapter.validateField !== 'function') {
                fieldState.updateFieldState(input);
                return true;
            }

            var valid = adapter.validateField(input);
            fieldState.updateFieldState(input);
            return valid;
        }

        function getLocalValidationAdapterName() {
            var adapter = ensureLocalValidationAdapter();
            return adapter ? adapter.name : null;
        }

        function getRegisteredClientValidators() {
            return Object.keys(localValidationProviders).sort();
        }

        return {
            ensureLocalValidationAdapter: ensureLocalValidationAdapter,
            registerLocalValidationProvider: registerLocalValidationProvider,
            refreshLocalValidation: refreshLocalValidation,
            setLocalValidationAdapterFactory: setLocalValidationAdapterFactory,
            validateLocally: validateLocally,
            getLocalValidationAdapterName: getLocalValidationAdapterName,
            getRegisteredClientValidators: getRegisteredClientValidators
        };
    })(fieldStateRuntime, formRequestRuntime);

    // Live-policy carrier tracking and immediate recheck transitions.
    var livePolicyRuntime = (function (dom, fieldState, localValidation) {
        var carrierEnabledStates = Object.create(null);

        function handleInput(target) {
            var valid = localValidation.validateLocally(target);
            var policy = dom.getPolicyCarrier(target);

            if (!valid || !target.dataset.hrzLivePolicyId || (policy && policy.dataset.hrzLiveEnabled !== 'true')) {
                fieldState.clearServerState(target, policy);
                return;
            }

            fieldState.updateFieldState(target);
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

            var input = dom.getInputForCarrierId(carrier.id);
            if (!dom.isFieldElement(input)) {
                return;
            }

            if (!currentEnabled) {
                fieldState.clearServerState(input, carrier);
                return;
            }

            if (carrier.dataset.hrzImmediateRecheckWhenEnabled !== 'true') {
                return;
            }

            if ((input.value || '').trim().length === 0) {
                return;
            }

            if (!localValidation.validateLocally(input)) {
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

        return {
            handleInput: handleInput,
            syncPolicyCarriers: syncPolicyCarriers
        };
    })(validationDom, fieldStateRuntime, localValidationRuntime);

    // Event wiring keeps HTMX and local validation responsibilities explicit.
    var validationEvents = (function (dom, fieldState, formRequests, localValidation, livePolicy) {
        function handleFieldEvent(event) {
            var target = event.target;
            if (!dom.isFieldElement(target)) {
                return;
            }

            livePolicy.handleInput(target);
        }

        function handleConfigRequest(event) {
            var target = event.detail.elt;
            if (!dom.isFieldElement(target)) {
                return;
            }

            if (!localValidation.validateLocally(target)) {
                fieldState.clearServerState(target, dom.getPolicyCarrier(target));
                event.preventDefault();
                return;
            }

            var carrierId = target.dataset.hrzLivePolicyId;
            if (!carrierId) {
                return;
            }

            var policy = dom.getPolicyCarrier(target);
            if (!policy) {
                event.preventDefault();
                return;
            }

            if (policy.dataset.hrzLiveEnabled !== 'true') {
                fieldState.clearServerState(target, policy);
                event.preventDefault();
            }
        }

        function handleBeforeRequest(event) {
            var target = event.detail.elt;
            if (!(target instanceof HTMLFormElement)) {
                return;
            }

            formRequests.disableFormTargets(target);
        }

        function handleAfterRequest(event) {
            var target = event.detail.elt;
            if (!(target instanceof HTMLFormElement)) {
                return;
            }

            formRequests.restoreFormTargets(target);
        }

        function handleAfterSettle() {
            livePolicy.syncPolicyCarriers(true);
            fieldState.syncFieldStates();
        }

        function register() {
            document.addEventListener('input', handleFieldEvent);
            document.addEventListener('change', handleFieldEvent);
            document.body.addEventListener('htmx:configRequest', handleConfigRequest);
            document.body.addEventListener('htmx:beforeRequest', handleBeforeRequest);
            document.body.addEventListener('htmx:afterRequest', handleAfterRequest);
            document.body.addEventListener('htmx:afterSettle', handleAfterSettle);
        }

        return {
            register: register
        };
    })(validationDom, fieldStateRuntime, formRequestRuntime, localValidationRuntime, livePolicyRuntime);

    validationEvents.register();

    window.hyperRazorValidation = Object.assign(window.hyperRazorValidation || {}, {
        setLocalValidationAdapterFactory: localValidationRuntime.setLocalValidationAdapterFactory,
        registerClientValidator: localValidationRuntime.registerLocalValidationProvider,
        refreshLocalValidation: localValidationRuntime.refreshLocalValidation,
        getLocalValidationAdapter: localValidationRuntime.ensureLocalValidationAdapter,
        getLocalValidationAdapterName: localValidationRuntime.getLocalValidationAdapterName,
        getRegisteredClientValidators: localValidationRuntime.getRegisteredClientValidators
    });

    localValidationRuntime.ensureLocalValidationAdapter();
    livePolicyRuntime.syncPolicyCarriers(false);
    fieldStateRuntime.syncFieldStates();
})();
