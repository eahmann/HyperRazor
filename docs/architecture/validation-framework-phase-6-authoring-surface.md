# HyperRazor Validation Framework — Phase 6 Authoring Surface

**Date:** 2026-03-07  
**Status:** Proposed  
**Depends on:** `docs/architecture/validation-framework-spec-greenfield.md`, `docs/archive/validation/validation-framework-phase-2-submit-runtime.md`, `docs/archive/validation/validation-framework-phase-4-live-validation.md`

---

## 0) Goal

Replace the current validation-demo attribute soup with a small authoring surface that normal application code would actually use.

Phase 6 starts simple:

- a small set of HTML control wrappers
- one shared abstract base for their validation/HTMX wiring
- keep plain `<form>` as the primary posture
- preserve the current validation contracts and runtime behavior

This phase is about authoring ergonomics, not changing the validation transport model.

---

## 1) Problem

The current form markup on the validation harness proves the runtime, but it is not a good long-term authoring model.

Current pain points:

- attempted-value replay is wired inline
- `aria-invalid` and `aria-describedby` are assembled inline
- client-validation hooks are assembled inline
- live-validation HTMX attributes are assembled inline
- server-slot and summary-slot targeting leaks into each input

Example of the current problem surface:

```razor
<input
    id="@EmailInputId"
    name="email"
    type="email"
    value="@HrzFormRendering.ValueOrAttempted(ValidationState, EmailPath, Form.Input.Email)"
    aria-invalid="@HrzFormRendering.HasErrors(ValidationState, EmailPath)"
    aria-describedby="@($"{EmailClientId} {EmailServerId}")"
    data-hrz-local-email="@(EnableClientValidation ? "true" : null)"
    data-hrz-client-slot-id="@(EnableClientValidation ? EmailClientId : null)"
    data-hrz-server-slot-id="@(HasLiveValidation ? EmailServerId : null)"
    data-hrz-summary-slot-id="@(HasLiveValidation ? SummaryId : null)"
    data-hrz-dependent-server-slot-ids="@(HasLiveValidation ? DisplayNameServerId : null)"
    hx-post="@EmailLiveValidationPath"
    hx-trigger="@LiveValidationTrigger"
    hx-target="@EmailLiveValidationTarget"
    hx-swap="@LiveValidationSwap"
    hx-include="@LiveValidationInclude"
    hx-vals="@EmailLiveValidationValuesJson" />
```

That is acceptable for a spike. It is not acceptable as the framework’s steady-state authoring model.

---

## 2) Core Decision

Phase 6 introduces a small set of wrapper components:

- `HrzInputText`
- `HrzInputTextArea`
- `HrzInputSelect`

Each wrapper owns the validation-related attributes on its control element.
They should share one common base so the validation and HTMX wiring is implemented once.

It will not render:

- the label
- the field wrapper
- the client-owned validation slot
- the server-owned validation slot
- the summary region

Field layout remains entirely caller-owned.

This keeps the surface focused on the common HTML controls while leaving field groups and layout composition to the consumer.

---

## 3) Non-Goals

- do not redesign validation transport
- do not replace plain `<form>` with `EditForm`
- do not add framework-owned field groups in this phase
- do not add per-`type` wrappers such as one component for email and another for number
- do not introduce a JavaScript validation library choice
- do not change the current `HrzSubmitValidationState` or `HrzLiveValidationPatch` contracts

---

## 4) Proposed Public Surface

### 4.1 Shared base class

The wrappers should share one abstract base class.

Suggested shape:

```csharp
internal abstract class HrzControlBase : ComponentBase
{
    [Parameter, EditorRequired] public string Id { get; set; } = string.Empty;
    [Parameter, EditorRequired] public string Name { get; set; } = string.Empty;
    [Parameter, EditorRequired] public HrzFieldPath Path { get; set; } = default!;
    [Parameter, EditorRequired] public HrzValidationRootId RootId { get; set; } = default!;

    [Parameter] public HrzSubmitValidationState? ValidationState { get; set; }
    [Parameter] public string? ClientSlotId { get; set; }
    [Parameter] public string? ServerSlotId { get; set; }
    [Parameter] public string? SummarySlotId { get; set; }

    [Parameter] public bool EnableClientValidation { get; set; }
    [Parameter] public string? LocalValidationKind { get; set; }

    [Parameter] public string? LiveValidationPath { get; set; }
    [Parameter] public IReadOnlyList<string> LiveDependentServerSlotIds { get; set; } = Array.Empty<string>();

    [Parameter(CaptureUnmatchedValues = true)]
    public IReadOnlyDictionary<string, object?> AdditionalAttributes { get; set; } =
        new Dictionary<string, object?>();

    protected string? BuildAriaDescribedBy() { /* ... */ }
    protected IReadOnlyDictionary<string, object?> BuildLiveValidationAttributes() { /* ... */ }
    protected IReadOnlyDictionary<string, object?> BuildClientValidationAttributes() { /* ... */ }
}
```

This base should be framework implementation detail in the first pass, not a primary public extension point.

### 4.2 `HrzInputText`

Initial shape:

```csharp
public sealed partial class HrzInputText : HrzControlBase
{
    [Parameter] public string Type { get; set; } = "text";
    [Parameter] public string? Value { get; set; }
}
```

Supported first-pass use:

- `type="text"`
- `type="email"`
- `type="search"`
- `type="password"`
- `type="tel"`
- `type="url"`
- `type="number"`
- `type="checkbox"`

### 4.3 Responsibilities

`HrzInputText` owns:

- `value="@HrzFormRendering.ValueOrAttempted(...)"`
- `aria-invalid`
- `aria-describedby`
- local validation `data-*` attributes
- live-validation `data-*` attributes
- `hx-post`
- `hx-trigger`
- `hx-target`
- `hx-swap`
- `hx-include`
- `hx-vals`

Callers own:

- layout
- label placement
- wrapper classes
- client/server validation slot markup
- slot IDs
- model value
- canonical field path
- root ID
- whether client validation is enabled
- whether live validation is enabled

### 4.4 `HrzInputTextArea`

`HrzInputTextArea` inherits the shared base and renders a `<textarea>`.

It should own:

- attempted-value replay for text-area content
- `aria-invalid`
- `aria-describedby`
- local validation `data-*` attributes
- live-validation `data-*` attributes
- `hx-*` live-validation attributes when configured

### 4.5 `HrzInputSelect`

`HrzInputSelect` inherits the shared base and renders a `<select>`.

It should own:

- attempted-value replay for selected value(s)
- `aria-invalid`
- `aria-describedby`
- local validation `data-*` attributes
- live-validation `data-*` attributes
- `hx-*` live-validation attributes when configured

---

## 5) Authoring Shape

### 5.1 Before

```razor
<div class="validation-field @(HrzFormRendering.HasErrors(ValidationState, EmailPath) ? "validation-field--invalid" : null)">
    <label for="@EmailInputId">Email</label>
    <input
        id="@EmailInputId"
        name="email"
        type="email"
        value="@HrzFormRendering.ValueOrAttempted(ValidationState, EmailPath, Form.Input.Email)"
        aria-invalid="@HrzFormRendering.HasErrors(ValidationState, EmailPath)"
        aria-describedby="@($"{EmailClientId} {EmailServerId}")"
        data-hrz-local-email="@(EnableClientValidation ? "true" : null)"
        data-hrz-client-slot-id="@(EnableClientValidation ? EmailClientId : null)"
        data-hrz-server-slot-id="@(HasLiveValidation ? EmailServerId : null)"
        data-hrz-summary-slot-id="@(HasLiveValidation ? SummaryId : null)"
        data-hrz-dependent-server-slot-ids="@(HasLiveValidation ? DisplayNameServerId : null)"
        hx-post="@EmailLiveValidationPath"
        hx-trigger="@LiveValidationTrigger"
        hx-target="@EmailLiveValidationTarget"
        hx-swap="@LiveValidationSwap"
        hx-include="@LiveValidationInclude"
        hx-vals="@EmailLiveValidationValuesJson" />
    <div id="@EmailClientId" data-hrz-client-validation-for="@EmailPath.Value"></div>
    <ValidationServerFieldSlot
        Id="@EmailServerId"
        FieldPath="@EmailPath.Value"
        Errors="@HrzFormRendering.ErrorsFor(ValidationState, EmailPath)" />
</div>
```

### 5.2 After

```razor
<div class="validation-field @(HrzFormRendering.HasErrors(ValidationState, EmailPath) ? "validation-field--invalid" : null)">
    <label for="@EmailInputId">Email</label>
    <HrzInputText
        Id="@EmailInputId"
        Name="email"
        Type="email"
        Path="EmailPath"
        RootId="Form.RootId"
        Value="Form.Input.Email"
        ValidationState="ValidationState"
        ClientSlotId="@EmailClientId"
        ServerSlotId="@EmailServerId"
        SummarySlotId="@SummaryId"
        EnableClientValidation="EnableClientValidation"
        LocalValidationKind="email"
        LiveValidationPath="Form.LiveValidationPath"
        LiveDependentServerSlotIds="@(new[] { DisplayNameServerId })" />
    <div id="@EmailClientId" data-hrz-client-validation-for="@EmailPath.Value"></div>
    <ValidationServerFieldSlot
        Id="@EmailServerId"
        FieldPath="@EmailPath.Value"
        Errors="@HrzFormRendering.ErrorsFor(ValidationState, EmailPath)" />
</div>
```

This is the target outcome for phase 6: the caller still controls layout, but no longer hand-assembles validation transport plumbing on the control element.

---

## 6) Rendering Rules

### 6.1 Submit-time rules

`HrzInputText` must preserve the existing submit-time behavior:

- attempted values come from `HrzFormRendering.ValueOrAttempted(...)`
- `aria-invalid` comes from `HrzFormRendering.HasErrors(...)`
- `aria-describedby` is built from the supplied slot IDs

Wrapper styling and message rendering remain caller-owned.

### 6.2 Live-validation rules

When `LiveValidationPath` is provided:

- `hx-post` targets that path
- `hx-target` points to the server-owned field slot for the current field
- `hx-vals` carries `__hrz_root` and `__hrz_fields`
- `LiveDependentFields` are translated into dependent server-slot IDs
- the component never targets the full form shell for live validation

When `LiveValidationPath` is not provided:

- no live-validation HTMX attributes are rendered

### 6.3 Client-validation rules

When `EnableClientValidation` is `true`:

- local validation data attributes are rendered
- supplied client/server/summary slot IDs are emitted where needed

When `EnableClientValidation` is `false`:

- local validation data attributes are omitted

---

## 7) Implementation Plan

### 7.1 First pass

Add `HrzInputText`, `HrzInputTextArea`, and `HrzInputSelect` in `src/HyperRazor.Demo.Mvc/Components/Fragments/` and migrate the validation harness first.

Initial targets:

- [UserInviteValidationForm.razor](../../src/HyperRazor.Demo.Mvc/Components/Fragments/UserInviteValidationForm.razor)
- `/users` invite form
- `/validation` MVC proxy form
- `/validation` Minimal API local form
- `/validation` Minimal API proxy form

### 7.2 Second pass

If the demo migration is clean, promote the wrappers into framework-owned surface area.

Likely homes:

- `src/HyperRazor.Rendering/`
- or `src/HyperRazor.Components/`

Promotion is only appropriate after:

- the component shape survives multiple form flows
- tests prove it does not regress attempted-value replay
- the live-validation wiring remains path-based, not demo-specific
- the single-wrapper shape still feels coherent after real usage
- the shared base is actually reducing duplication instead of hiding awkward divergence

---

## 8) Test Surface

### 8.1 Rendering tests

Add tests for:

- attempted value wins over model value on invalid submit
- `aria-invalid` reflects submit-time server errors
- `aria-describedby` includes the supplied slot IDs
- local validation attrs render only when enabled
- live-validation attrs render only when configured
- dependent server-slot IDs render only when present

Minimum first-pass coverage:

- `type="text"`
- `type="email"`
- one non-text input type, likely `checkbox` or `number`
- `HrzInputTextArea`
- `HrzInputSelect`

Likely home:

- `tests/HyperRazor.Rendering.Tests`

### 8.2 Demo integration tests

Verify the migrated `/validation` harness still behaves the same:

- invalid submit rerenders with server errors
- backend proxy invalid rerenders with mapped errors
- live validation still returns targeted/OOB fragments
- inspector updates still appear

Likely home:

- `tests/HyperRazor.Demo.Mvc.Tests`

### 8.3 E2E tests

Existing live-validation E2E coverage should continue to pass unchanged after the migration.

Likely home:

- `tests/HyperRazor.E2E`

---

## 9) Exit Criteria

- the validation harness no longer hand-assembles HTMX validation attrs inline
- field layout remains caller-controlled
- submit-time attempted-value replay still works
- live server validation still patches only server-owned regions
- the resulting markup is materially smaller and easier to read

---

## 10) Open Questions

- Should the wrappers stay demo-local for one phase before promotion, or should they land directly in framework code?
- Should the first pass require explicit slot IDs from the caller, or should the components accept a smaller naming seed and derive them internally?
- Which `input type=` values are truly phase-6 minimum scope beyond text/email?
