# HyperRazor Validation Framework Implementation Plan

**Date:** 2026-03-07  
**Status:** Historical implementation roadmap; phases 1-4 implemented on 2026-03-07  
**Source spec:** `docs/validation-framework-spec-greenfield.md`

---

## 0) Executive Summary

This plan is retained as the implementation roadmap that produced the current validation runtime. The sequence was:

1. Freeze the shared validation primitives and registration surface.
2. Wire submit-time MVC and rendering behavior around those primitives.
3. Add Minimal API and backend-proxy validation flows.
4. Add server live validation, demo coverage, and doc cleanup.

The implementation remains HTML-first, keeps plain `<form>` as the primary authoring surface, preserves attempted values for invalid rerenders, and treats legacy MVC `ModelState` transport as additive compatibility during the transition.

---

## 1) Locked Assumptions

- Browser-facing validation responses are HTML, not direct JSON payloads.
- Invalid HTMX submit defaults to `200`; strict `422` remains opt-in.
- `HrzValidationRootId` is explicit in the server contract and is not inferred from rendered HTML.
- Client-owned and server-owned validation DOM slots stay separate.
- `EditForm` submit-time messages are supported, but raw parse-failure replay for typed inputs is not a v1 requirement.

---

## 2) Phase Breakdown

### Phase 1

`docs/validation-framework-phase-1-foundations.md`

Deliver:

- `HrzValidationRootId`
- `HrzFieldPath`
- `IHrzFieldPathResolver`
- attempted-value primitives
- submit/live validation state models
- service registration and compatibility scaffolding

### Phase 2

`docs/validation-framework-phase-2-submit-runtime.md`

Deliver:

- MVC submit mapping
- render-pipeline submit-state transport
- plain-form rendering helpers
- submit-time DOM contract
- `HrzValidationBridge`

### Phase 3

`docs/validation-framework-phase-3-api-and-proxy.md`

Deliver:

- `IHrzModelValidator`
- Minimal API bind/validate helpers
- backend `ValidationProblemDetails` mapping
- MVC and Minimal API backend-proxy flows

### Phase 4

`docs/validation-framework-phase-4-live-validation.md`

Deliver:

- live validation scope binding
- targeted server patch rendering
- dependency-field handling
- demo/live coverage
- documentation cleanup

---

## 3) Shared Test Surfaces

### Rendering and contract tests

Project:

- `tests/HyperRazor.Rendering.Tests/HyperRazor.Rendering.Tests.csproj`

Must cover:

- `HrzFieldPath` equality and canonicalization
- nested and indexed path composition
- attempted-value extraction for single values, repeated values, and files
- render-pipeline submit-state cascading
- legacy `ModelState` compatibility during transition

### HTMX and protocol tests

Projects:

- `tests/HyperRazor.Htmx.Tests/HyperRazor.Htmx.Tests.csproj`
- `tests/HyperRazor.Htmx.Core.Tests/HyperRazor.Htmx.Core.Tests.csproj`

Must cover:

- response status handling assumptions that validation depends on
- HTMX response semantics used by fragment rerenders and OOB updates

### MVC integration tests

Project:

- `tests/HyperRazor.Demo.Mvc.Tests/HyperRazor.Demo.Mvc.Tests.csproj`

Must cover:

- full-page invalid submit rerender
- HTMX invalid submit rerender
- backend `422` mapped back to HTML
- antiforgery behavior on validation posts
- live validation affected-field updates and `204` no-op behavior

### End-to-end tests

Project:

- `tests/HyperRazor.E2E/HyperRazor.E2E.csproj`

Must cover:

- plain submit invalid then valid flow
- HTMX invalid then valid flow
- client slot remains intact when server live validation patches land
- live validation updates only targeted server slots

---

## 4) Suggested Commit Boundaries

1. Contracts + registrations + contract tests
2. MVC submit transport + render integration + plain-form helpers
3. `EditForm` bridge + submit integration coverage
4. Minimal API local validation + backend proxy mapping
5. Live validation patch path + demo + E2E
6. Docs cleanup and adoption notes

---

## 5) Exit Gates

The implementation plan is complete when:

- the public contract in `docs/validation-framework-spec-greenfield.md` is implemented without undocumented API drift
- each phase exit criterion is met
- the demo and test suite cover every supported validation path in the spec
- legacy `ModelState` behavior is either still functional or explicitly marked as deprecated-but-supported
