# HyperRazor Validation Framework Spec

**Date:** 2026-03-07  
**Status:** Current proposal  
**Supersedes:** `docs/validation-framework-spec.md`

## Core Position

HyperRazor is SSR plus HTMX. The browser-facing validation contract should therefore be HTML-first.

The official validation paths are:

1. MVC local validation -> full-page or fragment rerender, based on request mode
2. MVC -> backend API JSON -> mapped to submit validation state -> full-page or fragment rerender, based on request mode
3. Minimal API local validation -> full-page or fragment rerender, based on request mode
4. Minimal API -> backend API JSON -> mapped to submit validation state -> full-page or fragment rerender, based on request mode
5. Client-side local validation -> immediate browser-side DOM updates, no network
6. Server live validation -> targeted validation-region patches for server-owned rules

Direct JSON-to-browser validation is not part of the framework proposal.

## Decisions Frozen In This Draft

- Plain `<form>` remains the primary authoring surface.
- `EditForm` is supported, but not the primary framework posture.
- MVC submit validation mapping is automatic in `HrController`.
- Minimal API validation is extensible through a framework validator abstraction.
- Submit-time validation and live validation use different public models.
- Root scoping is part of the public API now, not a later add-on.
- Attempted-value preservation is part of the public API now, not a later add-on.
- Default invalid HTMX submit status is `200`; strict `422` is opt-in.

## Public Model

### 1. `HrzValidationRootId`

Validation is always scoped to one logical form root.

```csharp
public sealed record HrzValidationRootId(string Value);
```

This exists because pages can host multiple forms and repeated field names.

### 2. `HrzFieldPath`

Field keys are not raw strings in the public model. They are canonicalized field paths.

```csharp
public sealed class HrzFieldPath : IEquatable<HrzFieldPath>
{
    public string Value { get; }
    internal HrzFieldPath(string value) => Value = value;

    public bool Equals(HrzFieldPath? other) =>
        other is not null
        && StringComparer.Ordinal.Equals(Value, other.Value);

    public override bool Equals(object? obj) =>
        obj is HrzFieldPath other && Equals(other);

    public override int GetHashCode() =>
        StringComparer.Ordinal.GetHashCode(Value);
}
```

The formatted string contract still uses HTML field-name syntax:

- `Email`
- `Address.PostalCode`
- `Items[0].Name`

### 3. `IHrzFieldPathResolver`

Field names and bridge resolution must use the same path system.

```csharp
public interface IHrzFieldPathResolver
{
    HrzFieldPath FromExpression<TValue>(Expression<Func<TValue>> accessor);
    HrzFieldPath FromFieldName(string value);
    HrzFieldPath Append(HrzFieldPath parent, string propertyName);
    HrzFieldPath Index(HrzFieldPath collection, int index);
    string Format(HrzFieldPath path);
    FieldIdentifier Resolve(object model, HrzFieldPath path);
}
```

This is the critical contract that prevents the field-name helper and the bridge from drifting apart.

`HrzFieldPath` instances must be created through the resolver or framework adapters that delegate to the resolver. The public API should not expose a separate unconstrained `Parse()` path.

Canonicalization rules:

- field paths are always relative to `HrzValidationRootId`
- canonical paths do not include wrapper prefixes such as `Model.` or `Input.`
- nested properties use dot notation, for example `Address.PostalCode`
- collections use numeric bracket indices, for example `Items[0].Name`
- all adapters that receive raw keys from `ModelState`, `ValidationProblemDetails`, or request payloads must normalize through `IHrzFieldPathResolver.FromFieldName()`

Equality rule:

- equality is ordinal over the resolver-normalized canonical `Value`
- callers should not depend on alternate casing or alternate formatting being equivalent

### 4. `HrzAttemptedValue`

Attempted values must preserve repeated keys and file-input presence.

```csharp
public sealed record HrzAttemptedFile(
    string Name,
    string? FileName,
    string? ContentType,
    long? Length);

public sealed record HrzAttemptedValue(
    StringValues Values,
    IReadOnlyList<HrzAttemptedFile> Files);
```

### 5. `HrzSubmitValidationState`

Submit-time validation is full-root state, not a patch.

```csharp
public sealed record HrzSubmitValidationState(
    HrzValidationRootId RootId,
    IReadOnlyList<string> SummaryErrors,
    IReadOnlyDictionary<HrzFieldPath, IReadOnlyList<string>> FieldErrors,
    IReadOnlyDictionary<HrzFieldPath, HrzAttemptedValue> AttemptedValues)
{
    public bool IsValid =>
        SummaryErrors.Count == 0
        && FieldErrors.Values.All(messages => messages.Count == 0);
}
```

`AttemptedValues` is required so invalid rerenders can preserve raw user input that failed type conversion, repeated keys, and file-input context.

Submit-state invariants:

- `IsValid` is derived from `SummaryErrors` and `FieldErrors`; adapters must not invent a conflicting value
- model-level, empty-key, or root-level errors map to `SummaryErrors` only
- field-scoped errors map to `FieldErrors` only
- the same error should not be duplicated into both `SummaryErrors` and `FieldErrors`
- `AttemptedValues` is captured for submit requests even when local validation passes, because downstream backend validation may still need to rerender the original post
- `AttemptedValues` keys are canonical `HrzFieldPath` instances scoped to `RootId`
- `HrzAttemptedValue.Values` preserves posted order and repeated values
- `HrzAttemptedValue.Files` preserves submitted file metadata only; file inputs are not repopulated on rerender

### 6. `HrzLiveValidationPatch`

Live validation is a targeted patch model, not a full-root submit state.

```csharp
public sealed record HrzLiveValidationPatch(
    HrzValidationRootId RootId,
    IReadOnlyList<HrzFieldPath> AffectedFields,
    IReadOnlyDictionary<HrzFieldPath, IReadOnlyList<string>> FieldErrors,
    bool ReplaceSummary,
    IReadOnlyList<string> SummaryErrors);
```

This model is intentionally patch-shaped so live validation does not accidentally clear or replace unrelated state.

### 7. `HrzValidationScope`

This is the incoming request model for server live validation.

```csharp
public sealed record HrzValidationScope(
    HrzValidationRootId RootId,
    bool ValidateAll,
    IReadOnlyList<HrzFieldPath> Fields);
```

### 8. `HrzFormPostState<TModel>`

Minimal API bind-and-validate needs a form-post result that preserves more than a typed model.

```csharp
public sealed record HrzFormPostState<TModel>(
    TModel Model,
    HrzSubmitValidationState ValidationState);
```

This replaces the too-small `HrzBoundForm<TModel>` idea.

## Extensibility

### `IHrzModelValidator`

Minimal API and optional MVC customization need an extensibility seam now.

```csharp
public interface IHrzModelValidator
{
    Task<HrzSubmitValidationState> ValidateSubmitAsync<TModel>(
        TModel model,
        HrzValidationRootId rootId,
        IReadOnlyDictionary<HrzFieldPath, HrzAttemptedValue> attemptedValues,
        CancellationToken cancellationToken = default);

    Task<HrzLiveValidationPatch> ValidateLiveAsync<TModel>(
        TModel model,
        HrzValidationScope scope,
        IReadOnlyDictionary<HrzFieldPath, HrzAttemptedValue> attemptedValues,
        CancellationToken cancellationToken = default);
}
```

V1 should ship with a default DataAnnotations-backed implementation, but the public seam must not hard-code DataAnnotations forever.

## Framework Surface

### `IHrzFieldPathResolver` helper access

For convenience, the framework can also expose:

```csharp
public static class HrzFieldPaths
{
    public static HrzFieldPath For<TValue>(Expression<Func<TValue>> accessor);
    public static HrzFieldPath FromFieldName(string value);
    public static HrzFieldPath Append(HrzFieldPath parent, string propertyName);
    public static HrzFieldPath Index(HrzFieldPath collection, int index);
}
```

This must delegate to the same resolver used by the bridge.

`HrzFieldPaths.For(...)` is for root-bound or directly-addressable members. Nested and repeated child editors should receive an explicit parent path and compose on top of it through `Append(...)` and `Index(...)`.

Example:

```razor
@{
    var itemsPath = HrzFieldPaths.For(() => Input.Items);
    var itemPath = HrzFieldPaths.Index(itemsPath, index);
}

<LineItemEditor Item="Input.Items[index]" FieldPrefix="itemPath" />
```

```razor
@code {
    [Parameter, EditorRequired] public required LineItemInput Item { get; set; }
    [Parameter, EditorRequired] public required HrzFieldPath FieldPrefix { get; set; }
}

@{
    var namePath = HrzFieldPaths.Append(FieldPrefix, nameof(LineItemInput.Name));
}

<input name="@namePath.Value" value="@Item.Name" />
```

### Attempted-value helper

```csharp
public static class HrzAttemptedValues
{
    public static IReadOnlyDictionary<HrzFieldPath, HrzAttemptedValue> FromRequest(HttpRequest request);
}
```

Required behavior:

- normalize every incoming key through `IHrzFieldPathResolver.FromFieldName()`
- preserve repeated scalar values in posted order
- preserve uploaded file metadata for the matching field path

### Plain-form rendering helpers

Because plain `<form>` is primary, the framework should provide helpers for rendering attempted values and field errors without `EditForm`.

```csharp
public static class HrzFormRendering
{
    public static HrzAttemptedValue? AttemptedValueFor(
        HrzSubmitValidationState? state,
        HrzFieldPath path);

    public static StringValues ValuesOrAttempted(
        HrzSubmitValidationState? state,
        HrzFieldPath path,
        IEnumerable<string>? modelValues);

    public static string? ValueOrAttempted(
        HrzSubmitValidationState? state,
        HrzFieldPath path,
        string? modelValue);

    public static IReadOnlyList<string> ErrorsFor(
        HrzSubmitValidationState? state,
        HrzFieldPath path);
}
```

Use `ValueOrAttempted()` for single-value inputs, `ValuesOrAttempted()` for multi-selects and checkbox groups, and `AttemptedValueFor()` when the UI needs file-input context or repeated-value inspection.

### Submit validation transport

Submit validation needs transport helpers through the render pipeline.

```csharp
public static class HrzValidationHttpContextExtensions
{
    public static void SetSubmitValidationState(this HttpContext context, HrzSubmitValidationState state);
    public static HrzSubmitValidationState? GetSubmitValidationState(this HttpContext context);
}
```

There is intentionally no `SetLiveValidationPatch()` equivalent for whole-tree rendering. Live patches should stay local to the targeted fragment response.

### Backend API mapping helper

```csharp
public static class HrzValidationProblemDetailsExtensions
{
    public static HrzSubmitValidationState ToHrzSubmitValidationState(
        this ValidationProblemDetails problem,
        HrzValidationRootId rootId,
        IReadOnlyDictionary<HrzFieldPath, HrzAttemptedValue> attemptedValues);
}
```

Required behavior:

- normalize backend error keys through `IHrzFieldPathResolver.FromFieldName()`
- map model-level or empty keys to `SummaryErrors`
- preserve caller-supplied `AttemptedValues`

### MVC controller integration

`HrController` should own automatic MVC mapping, including attempted values.

```csharp
public abstract class HrController : ControllerBase
{
    protected Task<IResult> Page<TComponent>(
        object? data = null,
        HrzValidationRootId? validationRootId = null,
        CancellationToken cancellationToken = default);
    protected Task<IResult> Partial<TComponent>(
        object? data = null,
        HrzValidationRootId? validationRootId = null,
        CancellationToken cancellationToken = default);
    protected Task<IResult> Validation<TComponent>(
        HrzValidationRootId validationRootId,
        object? data = null,
        int statusCode = StatusCodes.Status200OK,
        CancellationToken cancellationToken = default);
}
```

Required behavior:

- adapt `ModelState` to `HrzSubmitValidationState`
- preserve attempted values from MVC binding
- normalize `ModelState` keys through `IHrzFieldPathResolver.FromFieldName()`
- use the explicit `validationRootId` supplied by the caller; do not infer root identity from rendered HTML
- set submit validation transport automatically before rendering

### Minimal API helpers

```csharp
public static class HrzMinimalApiFormExtensions
{
    public static Task<TModel> BindFormAsync<TModel>(
        this HttpContext context,
        CancellationToken cancellationToken = default);

    public static Task<HrzFormPostState<TModel>> BindFormAndValidateAsync<TModel>(
        this HttpContext context,
        HrzValidationRootId rootId,
        CancellationToken cancellationToken = default);

    public static Task<HrzValidationScope> BindLiveValidationScopeAsync(
        this HttpContext context,
        CancellationToken cancellationToken = default);
}
```

Required behavior:

- bind typed model
- preserve attempted values
- validate through `IHrzModelValidator`
- keep attempted values in the returned `HrzFormPostState<TModel>` even when `ValidationState.IsValid` is `true`

`BindFormAsync<TModel>()` is the raw binding helper. Submit flows that need to rerender invalid input should prefer `BindFormAndValidateAsync<TModel>()` so local bind or validation failures are handled before any backend proxy call.

### Render pipeline integration

The render pipeline should:

- resolve `HrzSubmitValidationState` from `HttpContext`
- cascade it from `HrzComponentHost`
- keep MVC `ModelState` internal if still needed during transition

### MVC `ModelState` transition and compatibility

The current codebase already exposes MVC `ModelState` through the render path, including `HrzContextItemKeys.ModelState` and `HrzComponentHost.ModelState`.

Required compatibility behavior:

- `HrController` continues capturing `ModelState`
- `HrzComponentViewService` and `HrzComponentHost` may continue flowing raw `ModelState` during the transition
- the new submit-state transport is additive in the release that introduces it
- new framework code should prefer `HrzSubmitValidationState`
- existing public `ModelState` surface is treated as legacy, documented as such, and marked obsolete only after the replacement API ships
- removal of the legacy `ModelState` surface happens no earlier than the next major version

### `HrzValidationBridge`

This bridge is for submit-time full-root state only.

```csharp
public sealed partial class HrzValidationBridge : ComponentBase
{
    [CascadingParameter] public EditContext? EditContext { get; set; }
    [CascadingParameter] public HrzSubmitValidationState? SubmitValidationState { get; set; }
}
```

Responsibilities:

- resolve `HrzFieldPath` to `FieldIdentifier`
- populate `ValidationMessageStore`
- clear stale submit-time server messages on rerender
- raise `NotifyValidationStateChanged()`

Non-goal:

- do not use this bridge for server live validation patches

## Authoring Surfaces

### Plain `<form>` is primary

This is the primary HyperRazor authoring surface.

Why:

- aligns with current repo usage
- preserves normal browser POST fallback naturally
- handles attempted-value rendering more directly
- avoids making `EditForm` the framework center of gravity

### `EditForm` is supported

`EditForm` remains a supported SSR authoring surface when teams want Blazor validation components on submit-time rerenders.

It should sit on top of the same root, path, and submit-validation contracts as plain forms.

### `EditForm` v1 limitation

`HrzValidationBridge` hydrates submit-time validation messages. It does not, by itself, guarantee replay of raw unparseable text for typed Blazor inputs such as `<InputNumber>`.

V1 contract:

- plain forms plus `HrzFormRendering` are the only path that guarantees raw attempted-value replay on invalid submit rerenders
- `EditForm` supports submit-time server messages and summary rendering
- `EditForm` with typed Blazor inputs does not promise redisplay of raw invalid text such as `"abc"` for a numeric field
- if a form requires raw parse-failure replay, use plain inputs in v1

## DOM Contract

Both server and browser-side validation need stable DOM targets.

Required contract:

- every logical form root has `data-hrz-validation-root="<root id>"`
- inputs use canonical HTML `name`
- field wrappers use `data-hrz-validation-region="<field path>"`
- client-owned field messages use `data-hrz-client-validation-for="<field path>"`
- server-owned field messages use `data-hrz-server-validation-for="<field path>"`
- server summary region uses `data-hrz-server-validation-summary`
- optional client summary region uses `data-hrz-client-validation-summary`
- invalid wrappers use `data-hrz-validation-invalid="true"`

Example:

```html
<form data-hrz-validation-root="create-user">
  <div class="field" data-hrz-validation-region="Email" data-hrz-validation-invalid="true">
    <input name="Email" type="email" />
    <p data-hrz-client-validation-for="Email"></p>
    <p data-hrz-server-validation-for="Email">Email is required.</p>
  </div>

  <div data-hrz-client-validation-summary></div>
  <div data-hrz-server-validation-summary>
    <p>Please correct the highlighted fields.</p>
  </div>
</form>
```

## Path 1: MVC Local Validation -> Full-Page or Fragment Rerender

Request mode, not success vs. error, decides full page vs. fragment:

- normal browser POST -> full page
- HTMX POST -> fragment

### MVC endpoint

```csharp
public sealed class UsersController : HrController
{
    [HttpPost("/users/create")]
    public Task<IResult> Create([FromForm] CreateUserInput input, CancellationToken cancellationToken)
    {
        var rootId = new HrzValidationRootId("create-user");

        if (!ModelState.IsValid)
        {
            return HttpContext.HtmxRequest().IsPartialRequest
                ? Validation<CreateUserForm>(rootId, new { Input = input }, cancellationToken: cancellationToken)
                : Page<CreateUserPage>(new { Input = input }, validationRootId: rootId, cancellationToken);
        }

        return HttpContext.HtmxRequest().IsPartialRequest
            ? Partial<CreateUserForm>(new
            {
                Input = new CreateUserInput(),
                SuccessMessage = "User created."
            }, cancellationToken)
            : Page<CreateUserPage>(new
            {
                Input = new CreateUserInput(),
                SuccessMessage = "User created."
            }, cancellationToken);
    }
}
```

### Plain form example

```razor
<form
    id="create-user-form"
    data-hrz-validation-root="create-user"
    action="/users/create"
    hx-post="/users/create"
    hx-target="#create-user-form"
    hx-swap="outerHTML">
    <HrzAntiforgeryInput />

    <label for="email">Email</label>
    <input
        id="email"
        name="@HrzFieldPaths.For(() => Input.Email).Value"
        value="@HrzFormRendering.ValueOrAttempted(
            SubmitValidationState,
            HrzFieldPaths.For(() => Input.Email),
            Input.Email)" />

    <div data-hrz-validation-region="Email">
        <p data-hrz-client-validation-for="Email"></p>
        @foreach (var message in HrzFormRendering.ErrorsFor(
            SubmitValidationState,
            HrzFieldPaths.For(() => Input.Email)))
        {
            <p data-hrz-server-validation-for="Email">@message</p>
        }
    </div>

    <button type="submit">Create</button>
</form>
```

## Path 2: MVC -> Backend API JSON -> Full-Page or Fragment Rerender

This is the BFF or edge-controller path.

### Backend API JSON contract

Preferred payload:

```json
{
  "title": "One or more validation errors occurred.",
  "status": 422,
  "errors": {
    "Email": ["That email address is already reserved."],
    "Age": ["Age must be between 18 and 120."]
  }
}
```

The `422` shown here is the backend API contract between servers. The browser-facing MVC response still defaults to HTML with status `200` for invalid HTMX submits unless strict mode is chosen explicitly.

### MVC endpoint

```csharp
[HttpPost("/users/create")]
public async Task<IResult> Create([FromForm] CreateUserInput input, CancellationToken cancellationToken)
{
    var rootId = new HrzValidationRootId("create-user");

    if (!ModelState.IsValid)
    {
        return HttpContext.HtmxRequest().IsPartialRequest
            ? await Validation<CreateUserForm>(rootId, new { Input = input }, cancellationToken: cancellationToken)
            : await Page<CreateUserPage>(new { Input = input }, validationRootId: rootId, cancellationToken);
    }

    var attemptedValues = HrzAttemptedValues.FromRequest(Request);
    var response = await _backend.PostAsJsonAsync("/api/users", input, cancellationToken);

    if (response.StatusCode == HttpStatusCode.UnprocessableEntity)
    {
        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>(cancellationToken);
        HttpContext.SetSubmitValidationState(
            problem!.ToHrzSubmitValidationState(
                rootId,
                attemptedValues));

        return HttpContext.HtmxRequest().IsPartialRequest
            ? await HrzResults.Validation<CreateUserForm>(
                HttpContext,
                new { Input = input },
                cancellationToken: cancellationToken)
            : await Page<CreateUserPage>(new { Input = input }, cancellationToken);
    }

    response.EnsureSuccessStatusCode();

    return HttpContext.HtmxRequest().IsPartialRequest
        ? await Partial<CreateUserForm>(new
        {
            Input = new CreateUserInput(),
            SuccessMessage = "User created."
        }, cancellationToken)
        : await Page<CreateUserPage>(new
        {
            Input = new CreateUserInput(),
            SuccessMessage = "User created."
        }, cancellationToken);
}
```

The browser still receives HTML. JSON exists only between servers.

## Path 3: Minimal API Local Validation -> Full-Page or Fragment Rerender

### Minimal API endpoint

```csharp
app.MapPost("/users/create", async (HttpContext context, CancellationToken cancellationToken) =>
{
    var posted = await context.BindFormAndValidateAsync<CreateUserInput>(
        new HrzValidationRootId("create-user"),
        cancellationToken);

    if (!posted.ValidationState.IsValid)
    {
        context.SetSubmitValidationState(posted.ValidationState);

        return context.HtmxRequest().IsPartialRequest
            ? await HrzResults.Validation<CreateUserForm>(
                context,
                new { Input = posted.Model },
                cancellationToken: cancellationToken)
            : await HrzResults.Page<CreateUserPage>(
                context,
                new { Input = posted.Model },
                cancellationToken: cancellationToken);
    }

    return context.HtmxRequest().IsPartialRequest
        ? await HrzResults.Partial<CreateUserForm>(
            context,
            new
            {
                Input = new CreateUserInput(),
                SuccessMessage = "User created."
            },
            cancellationToken: cancellationToken)
        : await HrzResults.Page<CreateUserPage>(
            context,
            new
            {
                Input = new CreateUserInput(),
                SuccessMessage = "User created."
            },
            cancellationToken: cancellationToken);
});
```

This path preserves attempted values through `HrzFormPostState<TModel>`.

## Path 4: Minimal API -> Backend API JSON -> Full-Page or Fragment Rerender

```csharp
app.MapPost("/users/create", async (HttpContext context, HttpClient backend, CancellationToken cancellationToken) =>
{
    var rootId = new HrzValidationRootId("create-user");
    var posted = await context.BindFormAndValidateAsync<CreateUserInput>(rootId, cancellationToken);

    if (!posted.ValidationState.IsValid)
    {
        context.SetSubmitValidationState(posted.ValidationState);

        return context.HtmxRequest().IsPartialRequest
            ? await HrzResults.Validation<CreateUserForm>(
                context,
                new { Input = posted.Model },
                cancellationToken: cancellationToken)
            : await HrzResults.Page<CreateUserPage>(
                context,
                new { Input = posted.Model },
                cancellationToken: cancellationToken);
    }

    var response = await backend.PostAsJsonAsync("/api/users", posted.Model, cancellationToken);

    if (response.StatusCode == HttpStatusCode.UnprocessableEntity)
    {
        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>(cancellationToken);
        context.SetSubmitValidationState(
            problem!.ToHrzSubmitValidationState(
                rootId,
                posted.ValidationState.AttemptedValues));

        return context.HtmxRequest().IsPartialRequest
            ? await HrzResults.Validation<CreateUserForm>(
                context,
                new { Input = posted.Model },
                cancellationToken: cancellationToken)
            : await HrzResults.Page<CreateUserPage>(
                context,
                new { Input = posted.Model },
                cancellationToken: cancellationToken);
    }

    response.EnsureSuccessStatusCode();

    return context.HtmxRequest().IsPartialRequest
        ? await HrzResults.Partial<CreateUserForm>(
            context,
            new
            {
                Input = new CreateUserInput(),
                SuccessMessage = "User created."
            },
            cancellationToken: cancellationToken)
        : await HrzResults.Page<CreateUserPage>(
            context,
            new
            {
                Input = new CreateUserInput(),
                SuccessMessage = "User created."
            },
            cancellationToken: cancellationToken);
});
```

## Path 5: Client-Side Local Validation -> Immediate DOM Updates

This path is for simple rules the browser can know immediately:

- required
- type or format
- min and max
- regex
- length
- simple cross-field comparisons using only current form state

This path should not debounce because it does not involve a network call.

### 5A. Native browser validation

```html
<form action="/users/create" method="post">
  <label for="email">Email</label>
  <input id="email" name="Email" type="email" required />

  <label for="age">Age</label>
  <input id="age" name="Age" type="number" min="18" max="120" />

  <button type="submit">Create</button>
</form>
```

### 5B. Alpine plus a validation library

Alpine is optional. It is useful for:

- touched and dirty state
- showing and clearing local errors
- gating server live validation until local validation passes

Example:

```html
<form x-data="createUserValidation()" data-hrz-validation-root="create-user" novalidate>
  <label for="email">Email</label>
  <input
    id="email"
    name="Email"
    type="email"
    x-model="form.Email"
    @input="validateField('Email')"
    @blur="touch('Email'); validateField('Email')" />
  <div data-hrz-validation-region="Email">
    <p data-hrz-client-validation-for="Email" x-text="errors.Email?.[0] ?? ''"></p>
    <div id="email-server-validation" data-hrz-server-validation-for="Email"></div>
  </div>

  <label for="age">Age</label>
  <input
    id="age"
    name="Age"
    type="number"
    x-model="form.Age"
    @input="validateField('Age')"
    @blur="touch('Age'); validateField('Age')" />
  <div data-hrz-validation-region="Age">
    <p data-hrz-client-validation-for="Age" x-text="errors.Age?.[0] ?? ''"></p>
    <div data-hrz-server-validation-for="Age"></div>
  </div>
</form>
```

## Path 6: Server Live Validation -> Targeted Validation-Region Patches

This path is only for rules the browser cannot know:

- uniqueness
- reserved values
- permission checks
- server-owned policy rules
- external lookups

### Boundary

Live validation is not submit validation.

- submit-time flows use `HrzSubmitValidationState`
- live flows use `HrzLiveValidationPatch`
- `HrzValidationBridge` is for submit-time rerenders only
- live responses patch server-owned validation slots only and must not clear unrelated client or submit-time state

### Dependency model

Each live rule must define:

- trigger fields
- required fields
- affected fields
- summary impact

Examples:

| Rule | Trigger fields | Required fields | Affected fields | Update summary |
| --- | --- | --- | --- | --- |
| Email uniqueness | `Email` | `Email` | `Email` | no |
| State required when Country is US | `Country`, `State` | `Country`, `State` | `State` | optional |
| Postal code format depends on Country | `Country`, `PostalCode` | `Country`, `PostalCode` | `PostalCode` | no |
| Plan availability depends on TenantId elsewhere on page | `PlanId`, `TenantId` | `PlanId`, `TenantId` | `PlanId` | optional |

### Root rule

Live validation is scoped to one logical validation root.

That means:

- dependent fields may live in other sections of the page
- dependent fields may live outside the current HTMX target region
- dependent fields must still belong to the same root
- the request must include those dependency values explicitly

### State-preservation rule

Live server validation must preserve browser-side state:

- input value
- cursor position
- focus
- client-side validation state
- Alpine state
- touched and dirty flags

That is why live validation returns targeted region patches instead of whole-form rerenders.

### HTMX request shape

```razor
<div id="create-user-root" data-hrz-validation-root="create-user">
    <InputSelect
        id="tenant"
        name="@HrzFieldPaths.For(() => Input.TenantId).Value"
        @bind-Value="Input.TenantId" />

    <InputText
        id="create-user-email"
        name="@HrzFieldPaths.For(() => Input.Email).Value"
        @bind-Value="Input.Email"
        hx-post="/users/live-validate"
        hx-trigger="input changed delay:400ms, blur"
        hx-include="#create-user-root [name='TenantId'], closest form"
        hx-target="#email-server-validation"
        hx-swap="outerHTML"
        hx-sync="closest form:abort" />

    <input type="hidden" name="RootId" value="create-user" />
    <input type="hidden" name="Fields" value="Email" />
    <input type="hidden" name="ValidateAll" value="false" />

    <div data-hrz-validation-region="Email">
        <p data-hrz-client-validation-for="Email"></p>
        <div id="email-server-validation" data-hrz-server-validation-for="Email"></div>
    </div>
</div>
```

If changing one field affects others, the response can include OOB updates for the dependent regions:

```html
<div id="state-server-validation" data-hrz-server-validation-for="State" hx-swap-oob="outerHTML">
  <p>State is required when Country is United States.</p>
</div>
```

### MVC endpoint

```csharp
[HttpPost("/users/live-validate")]
public async Task<IResult> LiveValidate(
    [FromForm] CreateUserInput input,
    CancellationToken cancellationToken)
{
    var attemptedValues = HrzAttemptedValues.FromRequest(Request);
    var scope = await HttpContext.BindLiveValidationScopeAsync(cancellationToken);
    var patch = await _validator.ValidateLiveAsync(input, scope, attemptedValues, cancellationToken);

    return Partial<CreateUserLiveValidationUpdate>(new { Patch = patch }, cancellationToken);
}
```

### Minimal API endpoint

```csharp
app.MapPost("/users/live-validate", async (HttpContext context, CancellationToken cancellationToken) =>
{
    var model = await context.BindFormAsync<CreateUserInput>(cancellationToken);
    var attemptedValues = HrzAttemptedValues.FromRequest(context.Request);
    var scope = await context.BindLiveValidationScopeAsync(cancellationToken);
    var patch = await _validator.ValidateLiveAsync(model, scope, attemptedValues, cancellationToken);

    return await HrzResults.Partial<CreateUserLiveValidationUpdate>(
        context,
        new { Patch = patch },
        cancellationToken: cancellationToken);
});
```

### Fragment shape

The live-validation fragment should render only the regions named in `AffectedFields`.

```razor
@{
    var emailPath = HrzFieldPaths.FromFieldName("Email");
}

@if (Patch.AffectedFields.Contains(emailPath))
{
    <div id="email-server-validation" data-hrz-server-validation-for="Email">
        @foreach (var message in Patch.FieldErrors.GetValueOrDefault(emailPath) ?? [])
        {
            <p>@message</p>
        }
    </div>
}

@if (Patch.ReplaceSummary)
{
    <div data-hrz-server-validation-summary hx-swap-oob="outerHTML">
        @foreach (var message in Patch.SummaryErrors)
        {
            <p>@message</p>
        }
    </div>
}
```

### Missing dependency values

If the live-validation request does not contain the required dependency values for the requested scope, the server should not guess.

Recommended behavior:

- return `204 No Content`
- or return a neutral field region with no server-owned message

## Status Codes

Framework defaults:

- full-page invalid submit: `200`
- HTMX invalid submit: `200`
- strict invalid HTMX submit: `422` as explicit opt-in
- server live validation: `200`
- no-op live validation: `204`

Examples above use the framework default `200` flow for invalid HTMX submits.

Strict variant example:

```csharp
return Validation<CreateUserForm>(
    new { Input = input },
    statusCode: StatusCodes.Status422UnprocessableEntity,
    cancellationToken: cancellationToken);
```

## Documentation Cleanup

Before implementation begins:

- mark old validation notes as historical or superseded
- remove dead route references
- align demo docs with the routes and tests that actually exist

## Build Order

1. Implement `HrzValidationRootId`, `HrzFieldPath`, and `IHrzFieldPathResolver`.
2. Implement `HrzSubmitValidationState` and `HrzLiveValidationPatch`.
3. Implement submit validation transport helpers and render integration.
4. Implement automatic MVC mapping in `HrController`, including attempted values.
5. Implement `IHrzModelValidator` with a default DataAnnotations-backed service.
6. Implement `BindFormAndValidateAsync()` and live-scope binding for Minimal API.
7. Implement backend JSON mapping helpers.
8. Implement `HrzValidationBridge` for submit-time `EditForm` support.
9. Add client-side local validation examples.
10. Add server live validation on top of the root and patch model.

## Final Recommendation

Freeze the public API around:

- `HrzValidationRootId`
- `HrzFieldPath`
- `HrzSubmitValidationState`
- `HrzLiveValidationPatch`
- `IHrzModelValidator`

That separation makes the framework honest about:

- full submit rerenders
- targeted live patches
- multi-form pages
- attempted-value preservation
- extensible validation backends

It also keeps HyperRazor aligned with SSR plus HTMX instead of drifting toward a JSON-first browser architecture.
