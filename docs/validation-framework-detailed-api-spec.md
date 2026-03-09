# HyperRazor Validation Detailed API Spec

**Date:** 2026-03-09  
**Status:** Draft for implementation  
**Source spec:** `docs/validation-framework-dx-refactor-spec-v4.md`

## 0. Goal

Translate the frozen parent design in `docs/validation-framework-dx-refactor-spec-v4.md` into a concrete API and implementation document.

This document is implementation-facing.
It fixes the public and internal API shapes that should be built next, and it breaks the work into testable slices.

## 1. Locked Assumptions

The following are frozen by the parent spec and are not reopened here:

- browser-facing validation remains HTML-first
- local browser validation uses `data-val-*`
- live validation uses HTMX-triggered HTML patching
- backend validation remains server-to-server and is mapped back into HTML
- `HrzField` plus child controls is the primary authoring shape
- `HrzForm` defaults `Enhance` to `true`
- `Enhance="false"` is the opt-out to plain full-page submit behavior
- the existing runtime primitives remain the transport engine

## 2. Existing Runtime To Preserve

The following existing types remain the runtime contract and are not replaced:

- `HrzValidationRootId`
- `HrzFieldPath`
- `IHrzFieldPathResolver`
- `HrzSubmitValidationState`
- `HrzLiveValidationPatch`
- `HrzValidationScope`
- `HrzFormPostState<TModel>`
- `HrzFormRendering`
- `HrzModelStateExtensions`
- `HrzValidationProblemDetailsExtensions`
- `HrzMinimalApiFormExtensions`

The new authoring layer should sit on top of this runtime.

## 3. New Core Services And Contexts

### 3.1 `IHrzValidationDescriptorProvider`

Add a descriptor provider that converts model metadata into a normalized field/rule graph.

```csharp
public interface IHrzValidationDescriptorProvider
{
    HrzValidationDescriptor GetDescriptor(Type modelType);
}
```

Default implementation requirements:

- populate descriptors from DataAnnotations and ASP.NET metadata
- emit `LocalRules` as the source for `data-val-*`
- attach live-validation metadata only when explicitly configured
- cache descriptors per model type

### 3.2 Descriptor model

The minimum descriptor model from v4 becomes concrete in the implementation:

```csharp
public sealed class HrzValidationDescriptor
{
    public required Type ModelType { get; init; }
    public required IReadOnlyDictionary<HrzFieldPath, HrzFieldDescriptor> Fields { get; init; }
}

public sealed class HrzFieldDescriptor
{
    public required HrzFieldPath Path { get; init; }
    public required string HtmlName { get; init; }
    public string? DisplayName { get; init; }

    public IReadOnlyDictionary<string, string> LocalRules { get; init; } =
        new Dictionary<string, string>();

    public HrzLiveRuleDescriptor? LiveRule { get; init; }
}

public sealed class HrzLiveRuleDescriptor
{
    public required string Endpoint { get; init; }
    public IReadOnlyList<HrzFieldPath> AdditionalFields { get; init; } =
        Array.Empty<HrzFieldPath>();
    public string Trigger { get; init; } = "input changed delay:400ms, blur";
}
```

Rules:

- descriptors own field identity and validation metadata
- descriptors do not own final DOM ids
- final DOM ids are derived from `FormName` plus canonical field identity at render time

### 3.3 `IHrzHtmlIdGenerator`

Add a rendering service for form and field ids.
This is framework infrastructure, not a primary public extension point in v1.

```csharp
public interface IHrzHtmlIdGenerator
{
    string GetFormId(string formName);
    string GetFieldId(string formName, HrzFieldPath path);
    string GetFieldMessageId(string formName, HrzFieldPath path);
    string GetSummaryId(string formName);
}
```

Default formatting rule:

- `users-invite` + `Addresses[0].Street` -> `users-invite-addresses-0-street`

### 3.4 `HrzFormContext`

Add a request-scoped form context cascaded by `HrzForm`.
This is the core non-interactive authoring context.

```csharp
public sealed class HrzFormContext
{
    public required object Model { get; init; }
    public required Type ModelType { get; init; }
    public required string FormName { get; init; }
    public required HrzValidationRootId RootId { get; init; }
    public required string FormId { get; init; }
    public required string SummaryId { get; init; }
    public required bool Enhance { get; init; }
    public required HrzValidationDescriptor Descriptor { get; init; }
    public HrzSubmitValidationState? SubmitValidationState { get; init; }
}
```

### 3.5 `HrzFieldContext`

Add an ambient field context cascaded by `HrzField`.

```csharp
public sealed class HrzFieldContext
{
    public required HrzFormContext Form { get; init; }
    public required HrzFieldPath Path { get; init; }
    public required HrzFieldDescriptor Descriptor { get; init; }
    public required string HtmlName { get; init; }
    public required string HtmlId { get; init; }
    public required string MessageId { get; init; }
    public required object? CurrentValue { get; init; }
}
```

Rules:

- `HrzFieldContext` is the default source for `HrzLabel`, `HrzInput`, and `HrzValidationMessage`
- nested and indexed fields must resolve through the same context flow as scalar fields
- `HrzField` does not require `EditContext`

## 4. Public Authoring Components

### 4.1 `HrzForm<TModel>`

```csharp
public sealed partial class HrzForm<TModel> : ComponentBase
{
    [Parameter, EditorRequired] public required TModel Model { get; set; }
    [Parameter, EditorRequired] public required string Action { get; set; }
    [Parameter, EditorRequired] public required string FormName { get; set; }
    [Parameter] public bool Enhance { get; set; } = true;
    [Parameter] public RenderFragment? ChildContent { get; set; }

    [Parameter(CaptureUnmatchedValues = true)]
    public IReadOnlyDictionary<string, object?> AdditionalAttributes { get; set; } =
        new Dictionary<string, object?>();
}
```

Rendering rules:

- renders a normal `<form method="post">`
- always emits antiforgery
- always emits hidden `__hrz_root` with `FormName`
- default `id` is `IHrzHtmlIdGenerator.GetFormId(FormName)`
- when `Enhance == true`, emits `hx-post`, `hx-target`, and `hx-swap` for form-root replacement
- when `Enhance == false`, does not emit enhanced submit attributes

Enhanced submit defaults:

- `hx-post` uses `Action`
- `hx-target` defaults to `#<FormId>`
- `hx-swap` defaults to `outerHTML`
- invalid submit responses replace the form root by default
- field-level live validation does not use the form-root target

`HrzForm` does not:

- own layout grid markup
- choose success retarget behavior automatically
- own client-validation library selection

### 4.2 `HrzField<TValue>`

```csharp
public sealed partial class HrzField<TValue> : ComponentBase
{
    [Parameter, EditorRequired] public required Expression<Func<TValue>> For { get; set; }
    [Parameter] public RenderFragment? ChildContent { get; set; }
}
```

Rules:

- `HrzField` is context-only in v1 and does not emit wrapper markup
- it resolves `HrzFieldContext` from the ambient `HrzFormContext`
- it owns field path resolution, html name, html id, display metadata, message id, attempted-value lookup, and live-validation metadata lookup

### 4.3 `HrzLabel`

```csharp
public sealed partial class HrzLabel : ComponentBase
{
    [Parameter] public string? Text { get; set; }
    [Parameter] public LambdaExpression? For { get; set; }
}
```

Rules:

- inside `HrzField`, `For` is optional and ambient field context is used by default
- outside `HrzField`, `For` is required
- default label text comes from `DisplayName` then from the final member name
- emits `for="<HtmlId>"`

### 4.4 `HrzInput`

```csharp
public sealed partial class HrzInput : ComponentBase
{
    [Parameter] public string Type { get; set; } = "text";

    [Parameter(CaptureUnmatchedValues = true)]
    public IReadOnlyDictionary<string, object?> AdditionalAttributes { get; set; } =
        new Dictionary<string, object?>();
}
```

Supported v1 types:

- `text`
- `email`
- `search`
- `tel`
- `url`
- `password`

Shared rendering rules:

- ambient `HrzFieldContext` is required in v1
- emits `id`, `name`, `aria-invalid`, and `aria-describedby`
- emits local `data-val-*` metadata from `LocalRules`
- emits live-validation `hx-*` and `data-hrz-*` metadata only when the field descriptor has a `LiveRule`

Text-like attempted-value rules:

- non-password text-like inputs use `HrzFormRendering.ValueOrAttempted(...)`
- password inputs never emit a `value` attribute
- password inputs never replay attempted values
- live validation is off by default for password inputs

### 4.5 `HrzTextArea`

```csharp
public sealed partial class HrzTextArea : ComponentBase
{
    [Parameter(CaptureUnmatchedValues = true)]
    public IReadOnlyDictionary<string, object?> AdditionalAttributes { get; set; } =
        new Dictionary<string, object?>();
}
```

Rules:

- ambient `HrzFieldContext` is required in v1
- content uses attempted value first, then model value
- emits `aria-invalid`, `aria-describedby`, `data-val-*`, and live metadata the same way as `HrzInput`

### 4.6 `HrzCheckbox`

```csharp
public sealed partial class HrzCheckbox : ComponentBase
{
    [Parameter(CaptureUnmatchedValues = true)]
    public IReadOnlyDictionary<string, object?> AdditionalAttributes { get; set; } =
        new Dictionary<string, object?>();
}
```

Rules:

- ambient `HrzFieldContext` is required in v1
- v1 supports non-nullable `bool` only
- emits a hidden false companion input plus the checkbox input
- posted checked value wins over the hidden false value
- attempted values win over the model when determining checked state on rerender

### 4.7 `HrzValidationMessage`

```csharp
public sealed partial class HrzValidationMessage : ComponentBase
{
    [Parameter] public LambdaExpression? For { get; set; }
}
```

Rules:

- inside `HrzField`, `For` is optional and ambient field context is used by default
- outside `HrzField`, `For` is required
- emits one public message root for the field
- may keep separate internal client and server subslots

Default DOM shape:

```html
<div id="{MessageId}" data-hrz-validation-for="{Path}">
  <div id="{MessageId}--client" data-hrz-client-validation-for="{Path}"></div>
  <div id="{MessageId}--server" data-hrz-server-validation-for="{Path}"></div>
</div>
```

### 4.8 `HrzValidationSummary`

```csharp
public sealed partial class HrzValidationSummary : ComponentBase
{
}
```

Rules:

- renders submit-time server errors by default
- may render live-validation server errors only when `ReplaceSummary == true`
- client-only local errors stay field-local by default in v1
- default `id` uses `IHrzHtmlIdGenerator.GetSummaryId(FormName)`

## 5. DOM Contract

### 5.1 Reserved fields

Reserved form and live-validation fields:

- `__hrz_root`
- `__hrz_fields`
- `__hrz_validate_all`

`HrzForm` always emits `__hrz_root`.

### 5.2 Local validation metadata

Rules in `HrzFieldDescriptor.LocalRules` emit to `data-val-*`.

Examples:

- `required` -> `data-val-required`
- `email` -> `data-val-email`
- `length` -> `data-val-length`, `data-val-length-min`, `data-val-length-max`

### 5.3 Live validation metadata

When `HrzFieldDescriptor.LiveRule` is present, field controls emit:

- `hx-post="<Endpoint>"`
- `hx-trigger="<Trigger>"`
- `hx-target="#<MessageId>--server"`
- `hx-swap="outerHTML"`
- `hx-include` for declared dependent fields only, derived from `LiveRule.AdditionalFields`
- `hx-vals` carrying `__hrz_root` and `__hrz_fields`
- `data-hrz-summary-slot-id="<SummaryId>"`

Transport rules:

- the triggering control posts its own value normally through HTMX
- `hx-include` adds only the declared dependent field controls needed for the live rule
- unrelated form fields are not included by default
- reserved HyperRazor live-validation fields still travel through `hx-vals`

Password fields do not emit live-validation attributes by default.

## 6. MVC And Minimal API Helper Surface

### 6.1 MVC helpers on `HrController`

Add semantic helpers on `HrController`:

```csharp
protected Task<IResult> HrzInvalid<TComponent>(
    object? data = null,
    CancellationToken cancellationToken = default,
    HrzValidationRootId? validationRootId = null)
    where TComponent : IComponent;

protected Task<IResult> HrzInvalid<TComponent>(
    HrzSubmitValidationState validationState,
    object? data = null,
    CancellationToken cancellationToken = default)
    where TComponent : IComponent;

protected Task<IResult> HrzValid<TComponent>(
    object? data = null,
    CancellationToken cancellationToken = default)
    where TComponent : IComponent;
```

Rules:

- helpers normalize to `HrzSubmitValidationState` before rendering components consume the result
- no-explicit-state overload uses current `ModelState` plus attempted values
- explicit-state overload is the backend-proxy and merged-state path
- request mode decides full-page `View<TComponent>()` versus fragment `PartialView<TComponent>()`

### 6.2 Minimal API public wrapper

Keep `HrzFormPostState<TModel>` as the low-level runtime type.
Add `HrzPosted<TModel>` as the author-facing Minimal API type.

```csharp
public sealed class HrzPosted<TModel>
{
    public required HttpContext HttpContext { get; init; }
    public required HrzValidationRootId RootId { get; init; }
    public required TModel Model { get; init; }
    public required HrzSubmitValidationState ValidationState { get; init; }

    public bool IsValid => ValidationState.IsValid;

    public Task<IResult> Invalid<TComponent>(
        object? data = null,
        CancellationToken cancellationToken = default)
        where TComponent : IComponent;

    public Task<IResult> Invalid<TComponent>(
        HrzSubmitValidationState validationState,
        object? data = null,
        CancellationToken cancellationToken = default)
        where TComponent : IComponent;

    public Task<IResult> Valid<TComponent>(
        object? data = null,
        CancellationToken cancellationToken = default)
        where TComponent : IComponent;
}
```

Binding rules:

- `HrzPosted<TModel>` binds from form posts only
- it reads `__hrz_root`, then delegates to existing `BindFormAndValidateAsync<TModel>()`
- it preserves attempted values whether local validation passes or fails
- render-time helpers use the same normalized `HrzSubmitValidationState` contract as MVC

### 6.3 Merged-state rule

The merged invalid path is first-class and must work the same way in MVC and Minimal API:

```csharp
var merged = localState.Merge(backendState);
return await this.HrzInvalid<MyFormComponent>(merged, data, cancellationToken);
```

No render-time component should consume raw `ModelState` or raw `ValidationProblemDetails`.

## 7. Implementation Slices

### Slice 1: Descriptor provider and id generation

Deliver:

- `IHrzValidationDescriptorProvider`
- default descriptor provider
- `IHrzHtmlIdGenerator`
- nested and indexed path coverage

Tests:

- descriptor generation for required, email, length, regex
- descriptor path lookup for nested and indexed members
- stable id generation from `FormName` plus field path

### Slice 2: Form and field context

Deliver:

- `HrzForm<TModel>`
- `HrzField<TValue>`
- `HrzFormContext`
- `HrzFieldContext`

Tests:

- `HrzForm` emits antiforgery and `__hrz_root`
- enhanced submit defaults target the form root
- `Enhance="false"` removes enhanced submit attributes
- `HrzField` resolves path, name, id, display name, and attempted-value access

### Slice 3: Message and summary components

Deliver:

- `HrzValidationMessage`
- `HrzValidationSummary`

Tests:

- field message ids align with input `aria-describedby`
- submit rerender replaces summary state entirely
- live patch summary updates only when `ReplaceSummary == true`
- client-only local field errors stay out of summary by default

### Slice 4: `HrzInput` and `HrzTextArea`

Deliver:

- `HrzInput`
- `HrzTextArea`
- password special-casing

Tests:

- non-password inputs replay attempted values
- password inputs never emit `value`
- password inputs never replay attempted values
- text-like input types emit `data-val-*` and live metadata correctly

### Slice 5: `HrzCheckbox`

Deliver:

- `HrzCheckbox`
- hidden false companion input behavior

Tests:

- unchecked submit posts false
- checked submit wins over hidden false companion
- attempted values determine checked state on rerender
- `bool?` remains unsupported in v1

### Slice 6: MVC helpers and Minimal API wrapper

Deliver:

- `HrController.HrzInvalid<TComponent>()`
- `HrController.HrzValid<TComponent>()`
- `HrzPosted<TModel>`

Tests:

- MVC local invalid uses normalized submit state
- MVC backend-mapped invalid uses explicit state overload
- Minimal API local invalid uses `HrzPosted<TModel>`
- merged invalid state works in both MVC and Minimal API

### Slice 7: Browser metadata and live validation integration

Deliver:

- `data-val-*` emission from descriptors
- field-level live-validation `hx-*` emission
- server-slot and summary-slot targeting contract

Tests:

- local metadata emitted for supported DataAnnotations rules
- live field target is the field server slot, not the form root
- password fields do not emit live-validation metadata by default
- nested and indexed field live-target metadata remains stable

## 8. Test Projects And Commands

Primary test projects:

- `tests/HyperRazor.Rendering.Tests/HyperRazor.Rendering.Tests.csproj`
- `tests/HyperRazor.Demo.Mvc.Tests/HyperRazor.Demo.Mvc.Tests.csproj`
- `tests/HyperRazor.E2E/HyperRazor.E2E.csproj`

Expected validation commands:

```bash
dotnet test tests/HyperRazor.Rendering.Tests/HyperRazor.Rendering.Tests.csproj
dotnet test tests/HyperRazor.Demo.Mvc.Tests/HyperRazor.Demo.Mvc.Tests.csproj
dotnet test tests/HyperRazor.E2E/HyperRazor.E2E.csproj
```

## 9. Exit Criteria

This detailed API spec is implemented when:

- `HrzForm` and `HrzField` provide the default authoring path without manual field-path wiring
- form enhancement defaults behave as specified, with `Enhance="false"` as opt-out
- `HrzInput`, `HrzTextArea`, and `HrzCheckbox` satisfy the replay and transport rules defined here
- password inputs never replay attempted values
- MVC and Minimal API both render from normalized `HrzSubmitValidationState`
- local browser validation uses emitted `data-val-*`
- live validation remains field-targeted and does not reuse form-root targeting
- nested and indexed fields pass the same render and validation contracts as scalar fields

## 10. Non-Goals For This Pass

Do not expand this detailed API pass to include:

- `HrzSelect`
- `HrzInputNumber`
- radio-group abstractions
- file-input authoring
- `bool?` and tri-state checkbox authoring
- browser-library lock-in
- JSON-to-browser validation
