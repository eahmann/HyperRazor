# HyperRazor Validation Framework — Phase 1 Foundations

**Date:** 2026-03-07  
**Status:** Implemented on 2026-03-07  
**Depends on:** `docs/architecture/validation-framework-spec-greenfield.md`

---

## 0) Goal

Create the shared validation primitives and service seams that every later phase depends on. At the end of this phase, the repo should have stable contracts for root scoping, field paths, attempted values, submit state, live patches, and validation service registration.

---

## 1) In Scope

- Public validation primitives
- Field-path canonicalization and composition
- Attempted-value capture primitives
- Validation service registration
- Compatibility scaffolding for the current `ModelState` transport

## 2) Out of Scope

- MVC submit rerender behavior
- Minimal API endpoint helpers
- Demo routes and forms
- Live validation endpoints

---

## 3) Target Files and Modules

### Primary framework code

- `src/HyperRazor.Rendering/`
- `src/HyperRazor.Mvc/`
- `src/HyperRazor/HyperRazorServiceCollectionExtensions.cs`

### Existing compatibility surface to preserve

- `src/HyperRazor.Rendering/HrzContextItemKeys.cs`
- `src/HyperRazor.Rendering/HrzComponentViewService.cs`
- `src/HyperRazor.Components/HrzComponentHost.razor`
- `src/HyperRazor.Mvc/HrController.cs`

### Primary tests

- `tests/HyperRazor.Rendering.Tests/HyperRazor.Rendering.Tests.csproj`

---

## 4) Work Items

### 4.1 Add validation primitives

Add the frozen public types from the spec:

- `HrzValidationRootId`
- `HrzFieldPath`
- `HrzAttemptedFile`
- `HrzAttemptedValue`
- `HrzSubmitValidationState`
- `HrzLiveValidationPatch`
- `HrzValidationScope`
- `HrzFormPostState<TModel>`

Suggested organization:

- create a `Validation/` folder under `src/HyperRazor.Rendering/` for shared transport types

### 4.2 Add the field-path resolver

Implement `IHrzFieldPathResolver` and the public helper accessors:

- `FromExpression(...)`
- `FromFieldName(...)`
- `Append(...)`
- `Index(...)`
- `Format(...)`
- `Resolve(...)`

The resolver is responsible for:

- canonicalization
- equality-safe path creation
- translating field paths back into `FieldIdentifier`

### 4.3 Add attempted-value extraction

Implement `HrzAttemptedValues.FromRequest(HttpRequest)` and make it preserve:

- single values
- repeated values
- checkbox and multi-select values in order
- uploaded file metadata

### 4.4 Add base registration seams

Register the validation services in `AddHyperRazor()`:

- field-path resolver
- default validator placeholder or interface registration
- any helper services needed by later phases

Keep the registration additive so the current non-validation scenarios continue working unchanged.

### 4.5 Preserve transition compatibility

Leave the current `ModelState` surface functional while preparing for the new submit-state transport:

- do not remove `HrzContextItemKeys.ModelState`
- do not break `HrzComponentHost.ModelState`
- do not change existing rendering behavior yet

---

## 5) Test Surface

### Unit tests to add

In `tests/HyperRazor.Rendering.Tests/`:

- `HrzFieldPath` uses ordinal value equality
- `FromFieldName("Input.Email")` normalizes to the canonical path
- `Append(...)` and `Index(...)` build `Items[0].Name` correctly
- repeated form values are preserved in posted order
- file metadata is captured without attempting to rehydrate files
- `HrzSubmitValidationState.IsValid` is derived from summary and field errors only

### Validation command

Run:

```bash
dotnet test tests/HyperRazor.Rendering.Tests/HyperRazor.Rendering.Tests.csproj
```

---

## 6) Exit Criteria

- All public validation primitives exist and compile.
- Field-path composition supports nested and indexed editors.
- Attempted-value extraction preserves repeated values and files.
- `AddHyperRazor()` registers the new validation seams.
- Rendering tests cover the contract behavior above.

---

## 7) Risks to Watch

- putting the shared primitives in the wrong package layer and forcing later moves
- allowing any public path-construction route to bypass canonicalization
- widening attempted-value shape after shipping v1
