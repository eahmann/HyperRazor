# HyperRazor Validation Framework Plan

> Superseded on 2026-03-06 by `docs/validation-framework-spec.md`.
>
> This document is retained for history only. The current proposal is HTML-first, removes direct JSON-to-browser validation as an official path, and separates local client validation from server live validation.

**Date:** 2026-03-06  
**Status:** Superseded  
**Audience:** historical reference only

## Intent

HyperRazor needs a real validation story that works across:

- MVC form posts
- Minimal API form posts
- live server validation
- client-side or hybrid validation in an SSR/HTMX app

The current repo proves only one slice: MVC can post a form, MVC `ModelState` can be cascaded into a component tree, and a demo-local bridge can hydrate `EditForm` messages. That is a valid spike, but it is not yet a framework design.

This document proposes the framework direction to get HyperRazor to a stable place without locking the public API to raw MVC `ModelState`.

## Current State

### What already exists

- MVC controllers inherit from `HrController`, which captures `ModelState` into `HttpContext.Items`.
- `HrzComponentViewService` reads that context item and passes it into `HrzComponentHost`.
- `HrzComponentHost` cascades the MVC `ModelStateDictionary` into the component tree.
- `HrzResults.Page<TComponent>` and `HrzResults.Partial<TComponent>` already provide Minimal API render parity for GET/page/fragment rendering.
- `HrzResults.Validation<TComponent>` already applies a status code around a partial render.
- HTMX request parsing, response headers, antiforgery meta/input helpers, and client-side antiforgery header injection already exist.
- The MVC demo now has a demo-local `HrzServerValidationBridge` that copies cascaded `ModelState` errors into a Blazor `ValidationMessageStore`.

### Gaps

- Validation transport is MVC-specific today.
- Minimal API has no form-post validation pipeline and no validation-state transport into components.
- There is no public framework validation abstraction.
- There is no public bridge component for `EditForm`.
- There is no official live-validation contract.
- There is no official client-side or hybrid validation position.
- Field naming is not solved at the framework level for server-posted component forms.

## Goals

- Define one semantic validation model for HyperRazor.
- Preserve MVC ergonomics for normal controller form posts.
- Add a first-class Minimal API form-post path.
- Support live server validation without requiring one endpoint per field.
- Support client-side and hybrid validation without depending on Blazor interactivity.
- Keep HTML fragments as the HTMX response shape.
- Keep invalid-response status semantics configurable, not hard-coded.

## Non-Goals

- Do not make raw `ModelStateDictionary` the public framework contract.
- Do not require a proprietary client-side validation engine in v1.
- Do not require per-field live-validation endpoints.
- Do not require a specific third-party validation library.
- Do not replace ASP.NET Core model binding.

## Design Principles

1. Validation state should be semantic, not transport-shaped.
2. MVC and Minimal API can differ in binding, but should converge on the same validation state.
3. The component layer should consume a HyperRazor validation abstraction, not MVC internals.
4. Live validation should be scoped by field or field-group, not whole-model-by-default.
5. Client-side validation should target stable browser-side DOM hooks and field paths, not require Blazor interactivity.
6. HTML `name` attributes and validation keys must use one canonical field-path contract.

## Recommendation Summary

- Introduce a framework-owned `HrzValidationState`.
- Keep raw MVC `ModelState` as an internal adapter input, not the public app-facing contract.
- Promote the demo bridge into a framework `HrzValidationBridge` only after it depends on `HrzValidationState`, not `ModelStateDictionary`.
- Add a Minimal API bind-and-validate helper for form posts.
- Standardize live validation around one endpoint per form or form section, with a scoped validation request.
- Treat client-side validation as native browser validation first, with a JavaScript adapter path for richer client validation when teams need it.

## Proposed Core Abstractions

### 1. `HrzValidationState`

Add a framework-owned validation payload that can represent:

- summary-level errors
- field-level errors
- validity

Candidate shape:

```csharp
public sealed class HrzValidationState
{
    public static HrzValidationState Empty { get; }
    public bool IsValid { get; }
    public IReadOnlyList<string> SummaryErrors { get; }
    public IReadOnlyDictionary<string, IReadOnlyList<string>> FieldErrors { get; }
}
```

This should live in a framework assembly that both MVC and Minimal API callers can use, preferably under `HyperRazor.Components` or a dedicated validation namespace within the main `HyperRazor` package surface.

### 2. `HrzValidationStateBuilder`

Add a small builder for app code and adapters:

- `AddFieldError(string fieldPath, string message)`
- `AddSummaryError(string message)`
- `Build()`

This keeps custom server-side rules straightforward in both MVC and Minimal API paths.

### 3. `HrzValidationScope`

Add a scoped-validation model for live validation:

```csharp
public sealed record HrzValidationScope(
    bool ValidateAll,
    IReadOnlyList<string> Fields);
```

Purpose:

- identify which field changed
- let validators expand into dependent fields
- avoid whole-form validation on every keystroke

HyperRazor should standardize the concept, not force a single route pattern.

## Field Path Contract

The canonical key for validation must be the HTML form field path, which is also the server binding path.

Examples:

- `Email`
- `Address.PostalCode`
- `Items[0].Name`

This contract must be shared by:

- MVC adapters
- Minimal API adapters
- live validation payloads
- component bridge mapping
- client-side validation integration

### Why this matters

The current MVC demo works only because the form explicitly emits `name="Name"`, `name="Email"`, and `name="Age"`. Stock Blazor binding can produce names like `Model.Email`, which is not a good long-term contract for server-posted component forms.

### Framework requirement

HyperRazor needs a first-class field-name story.

Recommended first step:

- add a helper that computes canonical field paths from a field expression

Possible shape:

```csharp
HrzFieldNames.For(() => Input.Email)
```

Optional later step if ergonomics require it:

- add `HrzInputText`, `HrzInputNumber`, and similar wrappers that automatically emit canonical `name` values

Do not leave this as a docs-only convention.

## Render Transport Design

### Current transport

Today the renderer only knows about `HrzContextItemKeys.ModelState`, which is MVC-specific.

### Target transport

Add a new context item:

- `HrzContextItemKeys.ValidationState`

Then update rendering to:

1. resolve `HrzValidationState` first
2. continue resolving raw `ModelState` during a transition period if needed
3. cascade `HrzValidationState` from `HrzComponentHost`

Recommended transition:

- phase 1: cascade both `ModelState` and `HrzValidationState`
- phase 2: framework bridge consumes `HrzValidationState`
- phase 3: raw `ModelState` becomes internal/back-compat only

## Public Component Surface

### `HrzValidationBridge`

Promote a framework component that:

- reads cascaded `HrzValidationState`
- maps field paths to `FieldIdentifier`
- writes errors into a `ValidationMessageStore`
- calls `EditContext.NotifyValidationStateChanged()`

This is the real reusable primitive, not raw `ModelState`.

### Support requirements for the bridge

The bridge must handle:

- summary errors
- flat properties
- nested object paths
- indexed collection paths
- repeated rerenders with a stable `EditContext`

The current demo bridge is not enough for promotion because its field-path mapping is heuristic and incomplete.

## MVC Path

### Recommendation

Keep MVC as the most ergonomic path for traditional form posts.

MVC should:

- continue using ASP.NET Core model binding
- continue using `ModelState`
- adapt `ModelState` into `HrzValidationState` automatically inside `HrController`

### MVC responsibilities

- `HrController.View<TComponent>` and `PartialView<TComponent>` should capture both raw `ModelState` and adapted `HrzValidationState`
- MVC form posts should keep normal DataAnnotations behavior
- custom server-only rules should be addable through either `ModelState.AddModelError(...)` or a builder helper that feeds the same transport

### Result behavior

For full-page invalid posts:

- return `200` with the page rerender

For HTMX partial invalid posts:

- default framework docs should show `200`
- strict `422` should remain opt-in

The framework helper should stay status-code-flexible.

## Minimal API Path

### Problem

Minimal API has render parity today, but not form-validation parity.

### Recommendation

Add a Minimal API helper that binds form data and produces `HrzValidationState`.

Candidate responsibility:

- bind `TModel` from `Request.Form`
- run DataAnnotations validation
- let app code add custom errors
- return `(model, validationState)`

Possible surface:

```csharp
var bound = await HrzFormBinder.BindAndValidateAsync<CreateUserInput>(context, cancellationToken);
```

or:

```csharp
var form = await context.BindFormAsync<CreateUserInput>(cancellationToken);
```

The exact API can vary. The important part is that Minimal API callers should not need to manually reimplement form binding plus validation-state creation every time.

### Minimal API invalid flow

For invalid form posts:

1. bind model
2. produce `HrzValidationState`
3. store it in the render context
4. return `HrzResults.Page<TComponent>` or `HrzResults.Validation<TComponent>` as appropriate

This gives Minimal API parity without inventing fake `ModelState`.

## Live Server Validation Path

### Recommendation

Support live validation as a scoped form-validation request, not a per-field endpoint model.

The recommended shape is:

- one live-validation endpoint per form or per meaningful form section
- request includes the current form snapshot
- request includes a `HrzValidationScope`
- server validates only the changed field and any dependent fields
- response returns HTML for the affected validation UI

### Why not one endpoint per field

- it does not scale for complex forms
- dependencies become awkward
- routing and controller surface area explode

### Why not whole-model validation on every keystroke

- unrelated required fields create noise
- server work scales badly
- the UX stops matching user intent

### Shared semantics, host-specific registration

MVC and Minimal API should share:

- the same request concept
- the same `HrzValidationScope`
- the same `HrzValidationState`
- the same fragment response model

They do not need to share the same registration style:

- MVC can use controllers
- Minimal API can use `MapPost`

### Response semantics

For live validation:

- default to `200`
- allow `204` for “nothing changed”
- do not use `422` for debounced field checks

### Response shape

HyperRazor should stay HTML-first:

- return fragments, not JSON
- update only the affected validation UI where practical
- allow OOB swaps when one changed field affects multiple validation regions

## Client-Side Path

### Recommendation

Do not make Blazor interactivity part of the validation architecture.

HyperRazor is an SSR plus HTMX project. The client-side story should therefore be:

1. Native browser validation
2. JavaScript adapter path for a client validation library or a future first-party engine

### Native browser validation

For plain SSR forms:

- standard HTML validation attributes remain valid
- apps may keep browser validation enabled when they want it
- apps may use `novalidate` when they want server-authoritative UX

HyperRazor should document this clearly, not override it.

### JavaScript adapter path

For richer client-side validation, HyperRazor should define a small DOM contract that a JavaScript library or future first-party engine can target.

That contract should include:

- canonical field paths based on HTML `name`
- stable field-level validation targets
- a stable summary target
- shared CSS classes or data attributes for valid/invalid state

The important point is that client-side validation in HyperRazor should be DOM-driven, not `EditContext`-driven in the browser.

### Hybrid behavior

Server-side and client-side validation should share:

- the same canonical field paths
- the same DOM targets where practical
- the same visual state conventions

They do not need to share the same runtime mechanism:

- server validation flows through `HrzValidationState` and server rendering
- client validation flows through browser APIs or JavaScript acting on DOM targets

### Future first-party JavaScript engine

If HyperRazor eventually ships its own JavaScript validation layer, it should build on the same DOM contract and field-path rules instead of introducing a separate naming or targeting model.

## Package and File Direction

Likely framework touch points:

- `src/HyperRazor.Components`
  - add core validation types
  - add `HrzValidationBridge`
  - add field-name helper and possibly future input wrappers
- `src/HyperRazor.Rendering`
  - add validation-state context item
  - cascade `HrzValidationState` from `HrzComponentHost`
  - resolve validation state in `HrzComponentViewService`
- `src/HyperRazor.Mvc`
  - add MVC adapter from `ModelStateDictionary`
  - optionally add result overloads or context helper methods
- `src/HyperRazor`
  - expose the main registration/docs surface if new services are required

Demo and docs touch points:

- `src/HyperRazor.Demo.Mvc`
  - replace the demo-local bridge with framework APIs
  - add a Minimal API form-post example
  - add a live-validation example based on scoped validation
- `docs/`
  - update quickstart and adoption docs
  - add validation docs as a first-class topic

## Phased Plan

### Phase 1: Core validation state

- Add `HrzValidationState`, builder, and context transport.
- Update `HrzComponentHost` and `HrzComponentViewService` to cascade the new state.
- Keep raw `ModelState` transport in parallel for transition safety.

### Phase 2: Promote the bridge

- Move the validation bridge out of the demo.
- Rewrite it against `HrzValidationState`.
- Add proper field-path mapping tests, including nested objects and indexed collections.

### Phase 3: MVC formalization

- Add the MVC adapter from `ModelStateDictionary` to `HrzValidationState`.
- Update `HrController` to capture adapted validation automatically.
- Replace demo-specific MVC validation plumbing with framework APIs.

### Phase 4: Minimal API form-post parity

- Add a Minimal API bind-and-validate helper.
- Support full-page and HTMX invalid rerender flows through the same validation transport.
- Add a Minimal API demo form-post example in `HyperRazor.Demo.Mvc`.

### Phase 5: Live validation contract

- Add `HrzValidationScope`.
- Document one-endpoint-per-form live validation.
- Add a live-validation example that works on top of the shared validation state instead of a demo-only special case.

### Phase 6: Client-side guidance

- Document native browser validation as the zero-dependency client-side mode.
- Define the DOM contract for JavaScript-library integration.
- Verify that server-rendered validation markup exposes stable hooks for browser-side updates.

### Phase 7: Docs and examples

- Add a dedicated validation doc set.
- Update quickstart/adoption docs to show MVC, Minimal API, and live-validation examples.
- Reframe the current demo page as one example, not the architecture.

## Testing Plan

### Unit coverage

Add or expand tests for:

- validation-state builders
- MVC `ModelState` adapter
- field-path mapping logic
- transport resolution in `HrzComponentViewService`

Likely home:

- `tests/HyperRazor.Rendering.Tests`
- a new test project only if the existing projects become awkward

### Integration coverage

Add integration tests for:

- MVC full-page invalid form post
- MVC HTMX invalid form post with `200`
- MVC HTMX invalid form post with `422`
- Minimal API full-page invalid form post
- Minimal API HTMX invalid form post
- shared live-validation flow

Likely home:

- `tests/HyperRazor.Demo.Mvc.Tests`

### E2E coverage

Add browser tests for:

- submit-time validation
- live validation with debounce
- native browser validation behavior
- client-side plus server-side hybrid behavior when a JavaScript-driven sample exists

Likely home:

- `tests/HyperRazor.E2E`

## Risks

### R1. Public API hardens around the wrong transport

If HyperRazor promotes raw `ModelState`, Minimal API and browser-side client validation parity will stay awkward.

Mitigation:

- promote `HrzValidationState`, not `ModelStateDictionary`

### R2. Field-path mapping remains incomplete

If collections and nested objects are not solved, the framework bridge will fail on real forms.

Mitigation:

- make field-path mapping a first-class requirement with dedicated tests

### R3. Live validation scope is under-specified

If scope is hand-wavy, apps will fall back to per-field endpoints or whole-model validation.

Mitigation:

- ship `HrzValidationScope` and document one-endpoint-per-form semantics

### R4. Client-side story becomes fragmented

If HyperRazor invents a separate client validation model, app authors will end up maintaining two systems.

Mitigation:

- anchor client-side validation on browser primitives and a small DOM adapter contract

## Recommended Immediate Next Steps

1. Build `HrzValidationState` and transport it through rendering without removing existing MVC `ModelState` flow.
2. Promote a framework `HrzValidationBridge` that consumes the new state.
3. Add a Minimal API bind-and-validate helper before adding more demo validation flows.
4. Replace the current live-email discussion with a scoped live-validation example built on the shared framework primitives.

## Open Questions

- Should the first field-naming solution be a helper (`HrzFieldNames.For(...)`) or input wrappers (`HrzInputText`, `HrzInputNumber`, and friends`)?
- Should HyperRazor ship explicit `HrzResults` overloads for validation state, or rely on a lower-level context setter plus existing result helpers?
- Do we keep raw `ModelState` cascaded for one release as a transition aid, or mark it internal immediately once `HrzValidationState` exists?
