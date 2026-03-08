# HyperRazor Validation Framework — Phase 4 Live Validation, Demo, and Docs

**Date:** 2026-03-07  
**Status:** Ready to execute  
**Depends on:** `docs/validation-framework-phase-2-submit-runtime.md`, `docs/validation-framework-phase-3-api-and-proxy.md`

---

## 0) Goal

Implement the server live-validation path and finish the documentation/demo story around the framework. At the end of this phase, the repo should demonstrate targeted server-owned live validation patches that preserve client-side state and document the supported validation model clearly.

---

## 1) In Scope

- live validation request binding
- server patch rendering
- dependency-field handling
- client/server validation slot preservation
- demo coverage for live validation
- cleanup of superseded validation docs

## 2) Out of Scope

- a production JS validation library choice
- full typed-input attempted replay for `EditForm`
- direct JSON-to-browser validation

---

## 3) Target Files and Modules

### Framework

- `src/HyperRazor.Mvc/`
- `src/HyperRazor.Rendering/`
- `src/HyperRazor.Components/`

### Demo surface

- `src/HyperRazor.Demo.Mvc/Components/Pages/Admin/UsersPage.razor`
- `src/HyperRazor.Demo.Mvc/Components/Fragments/`
- `src/HyperRazor.Demo.Mvc/Controllers/` or fragment endpoints
- `src/HyperRazor.Demo.Mvc/wwwroot/`

### Documentation

- `docs/validation-framework-spec-greenfield.md`
- `docs/live-email-feedback-spec.md`
- `docs/validation-framework-spec.md`
- `docs/validation-framework-plan.md`

### Tests

- `tests/HyperRazor.Demo.Mvc.Tests/HyperRazor.Demo.Mvc.Tests.csproj`
- `tests/HyperRazor.E2E/HyperRazor.E2E.csproj`

---

## 4) Work Items

### 4.1 Add live validation scope binding

Implement the request binding path for:

- `RootId`
- `Fields`
- `ValidateAll`

The binding must normalize field names and reject or no-op when required dependency values are missing.

### 4.2 Add targeted server patch rendering

Implement live validation so it patches:

- server-owned field slots only
- server-owned summary slots only when requested

Do not rerender the whole form for live validation.

### 4.3 Add dependency-field behavior

Support affected-field updates when one field depends on another:

- include dependency values through the request
- return OOB updates for affected server slots
- return `204` or neutral output when dependency data is missing

### 4.4 Add a concrete demo flow

Use the existing `/users` surface for the first live field if it is enough to demonstrate the patch model. If dependent-field scenarios outgrow that surface, add a dedicated validation demo page after the framework behavior is already proven in tests.

The demo must show:

- client-owned local message slot
- server-owned live message slot
- targeted HTMX swaps
- preserved input focus and local state

### 4.5 Add E2E and integration coverage

Cover:

- field-only server patching
- no whole-form rerender during live validation
- client slot remains intact while server slot changes
- dependent-field OOB update behavior

### 4.6 Clean up docs

When the implementation is stable:

- mark old validation docs as historical or superseded
- align the docs with the routes and tests that actually exist
- add an adoption-oriented doc that explains the golden path and the v1 limitations

---

## 5) Test Surface

### MVC integration tests to add

In `tests/HyperRazor.Demo.Mvc.Tests/`:

- live validation returns only targeted server-owned regions
- dependent-field live validation returns OOB updates for affected fields
- missing dependency input returns `204` or neutral output
- submit-time invalid state still works after a live-validation round-trip

### End-to-end tests to add

In `tests/HyperRazor.E2E/`:

- typing in a locally valid field triggers server live validation without replacing the full form
- client-side local error display remains intact while server slot updates
- valid submit after prior live validation still succeeds cleanly

### Validation commands

Run:

```bash
dotnet test tests/HyperRazor.Demo.Mvc.Tests/HyperRazor.Demo.Mvc.Tests.csproj
dotnet test tests/HyperRazor.E2E/HyperRazor.E2E.csproj
```

---

## 6) Exit Criteria

- live validation uses `HrzLiveValidationPatch`, not submit-state transport
- only server-owned validation slots are patched during live validation
- dependency-field updates work through targeted/OOB HTML updates
- the demo and docs reflect the implemented framework behavior without stale route references

---

## 7) Risks to Watch

- replacing whole forms during live validation and wiping client state
- mixing client-owned and server-owned validation content in the same DOM slot
- leaving old docs in place that describe removed routes or pre-framework spikes

