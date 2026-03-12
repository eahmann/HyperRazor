# HyperRazor Validation Framework — Phase 2 Submit Runtime

**Date:** 2026-03-07  
**Status:** Implemented on 2026-03-07  
**Depends on:** `docs/validation-framework-phase-1-foundations.md`

---

## 0) Goal

Implement the submit-time validation runtime for MVC and SSR rendering. At the end of this phase, invalid MVC submits should rerender through `HrzSubmitValidationState`, plain forms should be able to replay attempted values, and `EditForm` should support submit-time server messages through `HrzValidationBridge`.

---

## 1) In Scope

- MVC `ModelState` -> submit-state mapping
- explicit `validationRootId` on MVC render paths
- submit-state transport through the render pipeline
- plain-form rendering helpers
- submit-time DOM contract
- `HrzValidationBridge`

## 2) Out of Scope

- Minimal API binding helpers
- backend-proxy validation mapping
- live validation endpoints
- client-side validation library selection

---

## 3) Target Files and Modules

### MVC and rendering

- `src/HyperRazor.Mvc/HrController.cs`
- `src/HyperRazor.Mvc/HrzResults.cs`
- `src/HyperRazor.Rendering/HrzComponentViewService.cs`
- `src/HyperRazor.Rendering/HrzContextItemKeys.cs`
- `src/HyperRazor.Components/HrzComponentHost.razor`

### Form and bridge surface

- `src/HyperRazor.Components/`
- `src/HyperRazor.Rendering/`

### First consumer

- `src/HyperRazor.Demo.Mvc/Components/Pages/Admin/UsersPage.razor`
- `src/HyperRazor.Demo.Mvc/Controllers/` or equivalent fragment/controller files that own invite validation

### Primary tests

- `tests/HyperRazor.Rendering.Tests/HyperRazor.Rendering.Tests.csproj`
- `tests/HyperRazor.Demo.Mvc.Tests/HyperRazor.Demo.Mvc.Tests.csproj`

---

## 4) Work Items

### 4.1 Add MVC root-aware render entry points

Extend `HrController` with explicit `validationRootId` support:

- `Page<TComponent>(..., HrzValidationRootId? validationRootId = null, ...)`
- `Partial<TComponent>(..., HrzValidationRootId? validationRootId = null, ...)`
- `Validation<TComponent>(HrzValidationRootId validationRootId, ...)`

The contract is:

- invalid submit paths must supply a root id
- success paths may omit it
- root identity is not inferred from the DOM

### 4.2 Add submit-state transport through rendering

Implement `SetSubmitValidationState()` and `GetSubmitValidationState()` and thread the value through:

- `HrController`
- `HrzResults`
- `HrzComponentViewService`
- `HrzComponentHost`

Keep legacy `ModelState` flowing during the transition, but make the new submit-state transport the preferred path.

### 4.3 Map MVC `ModelState` to `HrzSubmitValidationState`

Add automatic invalid-submit mapping that:

- uses the explicit root id
- normalizes keys through `IHrzFieldPathResolver`
- preserves attempted values from MVC binding
- maps summary vs field errors according to the spec

### 4.4 Add plain-form helpers

Implement:

- `HrzFormRendering.ValueOrAttempted(...)`
- `HrzFormRendering.ValuesOrAttempted(...)`
- `HrzFormRendering.AttemptedValueFor(...)`
- `HrzFormRendering.ErrorsFor(...)`

This is the golden-path authoring surface for v1.

### 4.5 Add `HrzValidationBridge`

Implement submit-time-only `EditForm` support:

- consume `HrzSubmitValidationState`
- populate `ValidationMessageStore`
- clear stale submit-time server messages
- leave live validation out of scope

Keep the v1 limitation explicit:

- typed Blazor inputs do not guarantee raw parse-failure replay

### 4.6 Convert the first demo consumer

Use the existing `/users` invite validation surface as the first integration target so the framework lands against a real form without route churn.

Update the demo so it renders:

- `data-hrz-validation-root`
- `data-hrz-validation-region`
- `data-hrz-client-validation-for`
- `data-hrz-server-validation-for`
- `data-hrz-server-validation-summary`

---

## 5) Test Surface

### Rendering tests to add

In `tests/HyperRazor.Rendering.Tests/`:

- submit state is resolved from `HttpContext`
- `HrzComponentHost` cascades submit state without removing legacy `ModelState`
- `HrzValidationBridge` hydrates messages from submit state only

### MVC integration tests to add

In `tests/HyperRazor.Demo.Mvc.Tests/`:

- invalid normal POST rerenders full HTML with server messages
- invalid HTMX POST rerenders fragment HTML with status `200` by default
- attempted values are replayed for plain inputs after invalid submit
- antiforgery rejection still occurs before validation rendering

### Validation commands

Run:

```bash
dotnet test tests/HyperRazor.Rendering.Tests/HyperRazor.Rendering.Tests.csproj
dotnet test tests/HyperRazor.Demo.Mvc.Tests/HyperRazor.Demo.Mvc.Tests.csproj
```

---

## 6) Exit Criteria

- MVC invalid submit paths use `HrzSubmitValidationState`.
- Root ids are explicit in MVC invalid render calls.
- Plain forms can render attempted values and field errors from submit state.
- `HrzValidationBridge` supports submit-time messages for `EditForm`.
- The `/users` invite flow is running through the new submit-time framework path.

---

## 7) Risks to Watch

- accidentally making `EditForm` the primary surface instead of plain forms
- clearing live or client-side state from submit-time bridge logic
- introducing root inference from rendered HTML instead of explicit server data
