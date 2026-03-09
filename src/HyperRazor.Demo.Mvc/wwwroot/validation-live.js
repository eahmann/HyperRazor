(function () {
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

    function clearServerSlots(input) {
        var slotIds = [input.dataset.hrzServerSlotId].concat(getIds(input.dataset.hrzDependentServerSlotIds));
        slotIds.forEach(function (slotId) {
            var serverSlot = getById(slotId);
            if (serverSlot) {
                serverSlot.innerHTML = '';
            }
        });

        var summarySlot = getById(input.dataset.hrzSummarySlotId);
        if (summarySlot) {
            summarySlot.innerHTML = '';
            summarySlot.classList.add('validation-summary--empty');
        }
    }

    function updateDisplayNameValidation(input) {
        var clientSlot = getById(input.dataset.hrzClientSlotId);
        var value = (input.value || '').trim();
        if (value.length === 0) {
            setMessage(clientSlot, '');
            clearServerSlots(input);
            return true;
        }

        if (value.length < 3) {
            setMessage(clientSlot, 'Display name must be at least 3 characters.');
            clearServerSlots(input);
            return false;
        }

        setMessage(clientSlot, '');
        return true;
    }

    function updateEmailValidation(input) {
        var clientSlot = getById(input.dataset.hrzClientSlotId);
        var value = (input.value || '').trim();
        if (value.length === 0) {
            setMessage(clientSlot, '');
            clearServerSlots(input);
            return false;
        }

        if (input.validity.typeMismatch || input.validity.patternMismatch) {
            setMessage(clientSlot, 'Email must be a valid address.');
            clearServerSlots(input);
            return false;
        }

        setMessage(clientSlot, '');
        return true;
    }

    function handleInput(target) {
        if (target.matches('[data-hrz-local-display-name]')) {
            updateDisplayNameValidation(target);
        }

        if (target.matches('[data-hrz-local-email]')) {
            updateEmailValidation(target);
        }
    }

    document.addEventListener('input', function (event) {
        var target = event.target;
        if (!(target instanceof HTMLInputElement)) {
            return;
        }

        handleInput(target);
    });

    document.body.addEventListener('htmx:configRequest', function (event) {
        var target = event.detail.elt;
        if (!(target instanceof HTMLInputElement)) {
            return;
        }

        if (target.matches('[data-hrz-local-display-name]') && !updateDisplayNameValidation(target)) {
            event.preventDefault();
            return;
        }

        if (target.matches('[data-hrz-local-email]') && !updateEmailValidation(target)) {
            event.preventDefault();
        }
    });
})();
