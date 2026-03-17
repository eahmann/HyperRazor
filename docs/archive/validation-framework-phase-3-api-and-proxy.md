> Historical document
>
> This file is archived design history. It may describe retired package IDs, old `src/` paths, or superseded assumptions.
> Use [`docs/README.md`](../README.md) for the current docs map and [`docs/package-surface.md`](../package-surface.md) for the current package story.

# HyperRazor Validation Framework — Phase 3 API and Proxy Paths

**Date:** 2026-03-07  
**Status:** Implemented on 2026-03-07  
**Depends on:** `docs/validation-framework-phase-1-foundations.md`, `docs/validation-framework-phase-2-submit-runtime.md`

---

## 0) Goal

Implement the server-side validation flows that do not come directly from MVC `ModelState`: Minimal API local validation, MVC backend-proxy validation, and Minimal API backend-proxy validation. At the end of this phase, all submit-time server flows in the spec should be able to rerender invalid HTML while preserving attempted values.

---

## 1) In Scope

- `IHrzModelValidator`
- default DataAnnotations-backed validator
- Minimal API bind-and-validate helpers
- backend `ValidationProblemDetails` mapping
- MVC and Minimal API proxy flows

## 2) Out of Scope

- server live validation patches
- client-side validation runtime
- full docs cleanup

---

## 3) Target Files and Modules

### Core framework

- `src/HyperRazor/HyperRazorServiceCollectionExtensions.cs`
- `src/HyperRazor.Mvc/`
- `src/HyperRazor.Rendering/`

### Demo and test consumers

- `src/HyperRazor.Demo.Mvc/`
- `src/HyperRazor.Demo.Api/`
- `tests/HyperRazor.Demo.Mvc.Tests/HyperRazor.Demo.Mvc.Tests.csproj`

---

## 4) Work Items

### 4.1 Add the validator abstraction and default implementation

Implement `IHrzModelValidator` with a default DataAnnotations-backed service that can:

- validate full submit state
- validate live scope later
- return spec-compliant summary and field errors

Register it in `AddHyperRazor()`.

### 4.2 Add Minimal API form helpers

Implement:

- `BindFormAsync<TModel>()`
- `BindFormAndValidateAsync<TModel>()`
- `BindLiveValidationScopeAsync()`

Contract details to preserve:

- attempted values survive even when local validation passes
- local bind/validation failures must short-circuit before backend proxy calls

### 4.3 Add backend problem-details mapping

Implement `ValidationProblemDetails -> HrzSubmitValidationState` mapping that:

- normalizes keys through the resolver
- maps model-level errors to summary only
- preserves caller-supplied attempted values

### 4.4 Add MVC backend-proxy path

Add one MVC-backed validation path that:

- validates local model binding before proxying
- forwards to a backend API only when local validation passes
- maps backend `422` JSON into submit state
- rerenders HTML with the framework validation path

### 4.5 Add Minimal API backend-proxy path

Add the equivalent Minimal API path that:

- uses `BindFormAndValidateAsync<TModel>()`
- preserves attempted values for downstream backend `422` rerenders
- returns HTML fragments or pages rather than raw JSON

### 4.6 Add a focused demo/test harness

Do not make real network calls in tests. Use a fake backend client or test double so integration tests can assert:

- local invalid short-circuits
- backend `422` maps correctly
- success rerenders clean state

---

## 5) Test Surface

### Framework tests to add

In `tests/HyperRazor.Rendering.Tests/` or a new focused test file:

- default validator returns summary vs field errors correctly
- backend problem-details mapping normalizes keys
- attempted values remain present after locally valid binds

### Integration tests to add

In `tests/HyperRazor.Demo.Mvc.Tests/`:

- MVC local invalid does not call backend
- MVC backend `422` returns HTML with mapped server messages
- Minimal API local invalid rerenders HTML
- Minimal API backend `422` rerenders HTML with original attempted values

### Validation commands

Run:

```bash
dotnet test tests/HyperRazor.Rendering.Tests/HyperRazor.Rendering.Tests.csproj
dotnet test tests/HyperRazor.Demo.Mvc.Tests/HyperRazor.Demo.Mvc.Tests.csproj
```

---

## 6) Exit Criteria

- `IHrzModelValidator` is registered and used by Minimal API validation.
- `BindFormAndValidateAsync<TModel>()` is the standard invalid-rerender path for Minimal APIs.
- MVC and Minimal API backend-proxy flows both short-circuit on local invalid input.
- Backend `422` JSON is mapped back into HTML through `HrzSubmitValidationState`.

---

## 7) Risks to Watch

- accidentally treating backend JSON as a browser-facing contract
- losing attempted values after a locally valid bind but backend-invalid response
- duplicating validation logic instead of centralizing it in `IHrzModelValidator`
