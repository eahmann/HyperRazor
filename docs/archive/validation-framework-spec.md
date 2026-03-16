> Historical document
>
> This file is archived design history. It may describe retired package IDs, old `src/` paths, or superseded assumptions.
> Use [`docs/README.md`](../README.md) for the current docs map and [`docs/package-surface.md`](../package-surface.md) for the current package story.

# HyperRazor Validation Framework Spec

> Superseded on 2026-03-06 by `docs/validation-framework-spec-greenfield.md`.
>
> This revision assumed validation-specific glue that is no longer considered a valid starting point. The greenfield spec is the current document.

**Date:** 2026-03-06  
**Status:** Superseded  
**Supersedes:** `docs/validation-framework-plan.md`

## Core Position

HyperRazor is an SSR plus HTMX framework. The browser-facing validation contract should therefore be HTML-first.

The official validation paths are:

1. MVC validates locally and returns HTML.
2. MVC calls a backend API, maps JSON validation into framework validation state, then returns HTML.
3. Minimal API validates locally and returns HTML.
4. Minimal API calls a backend API, maps JSON validation into framework validation state, then returns HTML.
5. Client-side local validation runs in the browser for simple rules.
6. Server live validation runs only for rules the browser cannot know and returns HTML fragments.

Direct JSON-to-browser validation is not an official HyperRazor path in this proposal.

## Goals

- Keep the browser contract HTML-first.
- Preserve MVC ergonomics for form posts.
- Add first-class Minimal API form-post parity.
- Support local client validation for immediate simple rules.
- Support scoped server live validation for server-owned rules.
- Standardize one framework validation state across all server paths.
- Make backend API JSON a server-to-server integration detail, not the primary browser contract.

## Non-Goals

- Do not make raw `ModelStateDictionary` the public framework contract.
- Do not make direct JSON-to-browser validation a first-class framework path.
- Do not require one live-validation endpoint per field.
- Do not require Alpine or any JavaScript library as a framework dependency.
- Do not build a first-party JavaScript validation engine in the first pass.

## Shared Framework Requirements

Every supported path needs the same core pieces.

### 1. Canonical field-path contract

Validation keys must match HTML form field names.

Examples:

- `Email`
- `Address.PostalCode`
- `Items[0].Name`

This must be shared by:

- MVC `ModelState` mapping
- Minimal API validation mapping
- backend API JSON mapping
- live-validation scope keys
- client-side DOM targeting

Candidate helper:

```csharp
var emailField = HrzFieldNames.For(() => Input.Email);
```

### 2. `HrzValidationState`

This is the framework-owned semantic validation model.

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

Add a builder too:

```csharp
var state = new HrzValidationStateBuilder()
    .AddFieldError("Email", "Email is required.")
    .AddSummaryError("Please correct the highlighted fields.")
    .Build();
```

### 3. Render transport

Server render paths need a common way to flow validation into components.

Recommended direction:

- add `HrzContextItemKeys.ValidationState`
- add `HttpContext.SetValidationState(HrzValidationState state)`
- have `HrzComponentViewService` resolve and cascade `HrzValidationState`
- promote a framework `HrzValidationBridge` that hydrates `ValidationMessageStore`

Raw MVC `ModelState` can remain as a transitional internal input, but not as the long-term public contract.

### 4. Backend API JSON mapping

If a backend API performs validation, its JSON error payload should be mapped into `HrzValidationState` at the server edge before returning HTML.

Preferred JSON shape:

- `ValidationProblemDetails`
- or a framework-normalized equivalent that still maps cleanly into field errors plus summary errors

Candidate adapter:

```csharp
var state = problemDetails.ToHrzValidationState();
context.SetValidationState(state);
```

## Path Matrix

| Path | Browser receives | Validation source | Notes |
| --- | --- | --- | --- |
| MVC local | full page or partial HTML | MVC model binding + `ModelState` | Most ergonomic local form-post path |
| MVC via backend API | full page or partial HTML | backend API JSON mapped at MVC edge | JSON stays server-to-server |
| Minimal API local | full page or partial HTML | Minimal API bind/validate helper | Needs new helper in framework |
| Minimal API via backend API | full page or partial HTML | backend API JSON mapped at Minimal API edge | Same HTML contract as MVC |
| Client-side local | DOM updates only | browser primitives or JS validation lib | Immediate, no debounce |
| Server live | partial HTML or OOB HTML | scoped server validation | Debounced only when server/network involved |

## Path 1: MVC Validates Locally and Returns HTML

This is the cleanest path for traditional form posts.

### Full-page invalid rerender

```csharp
[HttpPost("/users/create")]
public Task<IResult> Create([FromForm] CreateUserInput input, CancellationToken cancellationToken)
{
    if (!ModelState.IsValid)
    {
        return Page<CreateUserPage>(new
        {
            Input = input
        }, cancellationToken);
    }

    // Save and redirect or rerender success state.
    return Page<CreateUserPage>(new
    {
        Input = new CreateUserInput(),
        SuccessMessage = "User created."
    }, cancellationToken);
}
```

### HTMX partial invalid rerender

```csharp
[HttpPost("/users/create")]
public Task<IResult> Create([FromForm] CreateUserInput input, CancellationToken cancellationToken)
{
    if (!ModelState.IsValid)
    {
        return HttpContext.HtmxRequest().IsPartialRequest
            ? HrzResults.Validation<CreateUserForm>(
                HttpContext,
                new { Input = input },
                statusCode: StatusCodes.Status422UnprocessableEntity,
                cancellationToken: cancellationToken)
            : Page<CreateUserPage>(new { Input = input }, cancellationToken);
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
```

### Form authoring shape

```razor
<EditForm
    EditContext="_editContext"
    action="/users/create"
    hx-post="/users/create"
    hx-target="#create-user-form"
    hx-swap="outerHTML">
    <HrzAntiforgeryInput />
    <HrzValidationBridge />

    <InputText
        id="create-user-email"
        name="@HrzFieldNames.For(() => Input.Email)"
        @bind-Value="Input.Email" />

    <ValidationMessage For="() => Input.Email" />
    <button type="submit">Create</button>
</EditForm>
```

## Path 2: MVC Calls a Backend API, Maps JSON, Then Returns HTML

This path matters when MVC is acting as the HTML edge over another service.

### Backend API contract

Use `ValidationProblemDetails` or a comparable payload:

```json
{
  "type": "https://example.com/problems/validation",
  "title": "One or more validation errors occurred.",
  "status": 422,
  "errors": {
    "Email": ["That email address is already reserved."],
    "Age": ["Age must be at least 18."]
  }
}
```

### MVC mapping example

```csharp
[HttpPost("/users/create")]
public async Task<IResult> Create([FromForm] CreateUserInput input, CancellationToken cancellationToken)
{
    var response = await _backend.PostAsJsonAsync("/api/users", input, cancellationToken);

    if (response.StatusCode == HttpStatusCode.UnprocessableEntity)
    {
        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>(cancellationToken);
        HttpContext.SetValidationState(problem!.ToHrzValidationState());

        return HttpContext.HtmxRequest().IsPartialRequest
            ? await HrzResults.Validation<CreateUserForm>(
                HttpContext,
                new { Input = input },
                statusCode: StatusCodes.Status422UnprocessableEntity,
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

### Framework point

The browser still receives HTML. JSON exists only between servers.

## Path 3: Minimal API Validates Locally and Returns HTML

This is the Minimal API parity path HyperRazor is missing today.

### Candidate helper

```csharp
var bound = await context.BindFormAndValidateAsync<CreateUserInput>(cancellationToken);
```

Where `bound` contains:

- `Model`
- `ValidationState`

### Minimal API full page or partial example

```csharp
app.MapPost("/users/create", async (HttpContext context, CancellationToken cancellationToken) =>
{
    var bound = await context.BindFormAndValidateAsync<CreateUserInput>(cancellationToken);

    if (!bound.ValidationState.IsValid)
    {
        context.SetValidationState(bound.ValidationState);

        return context.HtmxRequest().IsPartialRequest
            ? await HrzResults.Validation<CreateUserForm>(
                context,
                new { Input = bound.Model },
                statusCode: StatusCodes.Status422UnprocessableEntity,
                cancellationToken: cancellationToken)
            : await HrzResults.Page<CreateUserPage>(
                context,
                new { Input = bound.Model },
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

### Framework point

Minimal API should not invent fake MVC `ModelState`. It should produce `HrzValidationState` directly.

## Path 4: Minimal API Calls a Backend API, Maps JSON, Then Returns HTML

This is the Minimal API equivalent of the MVC backend-proxy path.

```csharp
app.MapPost("/users/create", async (HttpContext context, HttpClient backend, CancellationToken cancellationToken) =>
{
    var bound = await context.BindFormAsync<CreateUserInput>(cancellationToken);
    var response = await backend.PostAsJsonAsync("/api/users", bound.Model, cancellationToken);

    if (response.StatusCode == HttpStatusCode.UnprocessableEntity)
    {
        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>(cancellationToken);
        context.SetValidationState(problem!.ToHrzValidationState());

        return context.HtmxRequest().IsPartialRequest
            ? await HrzResults.Validation<CreateUserForm>(
                context,
                new { Input = bound.Model },
                statusCode: StatusCodes.Status422UnprocessableEntity,
                cancellationToken: cancellationToken)
            : await HrzResults.Page<CreateUserPage>(
                context,
                new { Input = bound.Model },
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

### Framework point

Again, JSON is server-to-server. The browser still receives HTML.

## Path 5: Client-Side Local Validation

This is the path for simple immediate rules. It should not debounce because there is no network involved.

Examples of rules that belong here:

- required
- type/format
- length
- min/max
- pattern
- simple cross-field comparisons that only use current form state

### 5A. Native browser validation

```html
<form action="/users/create" method="post">
  <input type="email" name="Email" required />
  <input type="number" name="Age" min="18" max="120" />
  <button type="submit">Create</button>
</form>
```

This is the zero-dependency path.

### 5B. JavaScript-library validation

If the app wants richer client-side validation, use a browser-side validation library and update the DOM directly.

Candidate DOM contract:

- field inputs use canonical `name`
- field message regions expose `data-hrz-validation-for`
- summary region exposes `data-hrz-validation-summary`

Example markup:

```html
<form x-data="createUserValidation()" novalidate>
  <input x-model="form.email" name="Email" type="email" @input="validate('Email')" />
  <p data-hrz-validation-for="Email" x-text="errors.Email?.[0] ?? ''"></p>

  <input x-model="form.age" name="Age" type="number" @input="validate('Age')" />
  <p data-hrz-validation-for="Age" x-text="errors.Age?.[0] ?? ''"></p>

  <div data-hrz-validation-summary x-show="summary.length > 0">
    <template x-for="message in summary">
      <p x-text="message"></p>
    </template>
  </div>
</form>
```

Example Alpine-oriented glue:

```html
<script type="module">
  import Ajv from "ajv";

  const schema = {
    type: "object",
    properties: {
      Email: { type: "string", minLength: 1, format: "email" },
      Age: { type: "integer", minimum: 18, maximum: 120 }
    },
    required: ["Email"]
  };

  window.createUserValidation = function () {
    const ajv = new Ajv({ allErrors: true });
    const validateModel = ajv.compile(schema);

    return {
      form: { Email: "", Age: null },
      errors: {},
      summary: [],

      validate() {
        const valid = validateModel(this.form);
        this.errors = {};
        this.summary = [];

        if (valid) {
          return;
        }

        for (const error of validateModel.errors ?? []) {
          const field = (error.instancePath || "").replace(/^\//, "") || error.params.missingProperty;
          if (!field) {
            this.summary.push(error.message ?? "Invalid input.");
            continue;
          }

          this.errors[field] ??= [];
          this.errors[field].push(error.message ?? "Invalid input.");
        }
      }
    };
  };
</script>
```

### Alpine's role

Alpine is useful here as orchestration glue:

- track touched and dirty state
- call a validator on `input` or `blur`
- show or hide local error regions
- gate server live validation until local rules pass

Alpine is not the validation engine by itself. It is the browser-side state layer around whatever validation approach the app chooses.

## Path 6: Server Live Validation

This is only for rules the browser cannot know.

Examples:

- uniqueness
- permission checks
- server-owned policy rules
- lookups against external systems

### Core rules

- use one endpoint per form or meaningful form section, not per field
- include the current form snapshot
- include a scoped validation request
- debounce only because network is involved
- return HTML fragments, not JSON

Candidate live-validation scope:

```csharp
public sealed record HrzValidationScope(
    bool ValidateAll,
    IReadOnlyList<string> Fields);
```

### HTMX input shape

```razor
<InputText
    id="create-user-email"
    name="@HrzFieldNames.For(() => Input.Email)"
    @bind-Value="Input.Email"
    hx-post="/users/live-validate"
    hx-trigger="input changed delay:400ms, blur"
    hx-include="closest form"
    hx-target="#create-user-email-validation"
    hx-swap="outerHTML" />
```

### MVC live-validation example

```csharp
[HttpPost("/users/live-validate")]
public Task<IResult> LiveValidate(
    [FromForm] CreateUserInput input,
    [FromForm] string[] fields,
    CancellationToken cancellationToken)
{
    var scope = new HrzValidationScope(ValidateAll: false, Fields: fields);
    var state = _validator.ValidateLive(input, scope);

    HttpContext.SetValidationState(state);

    return Partial<CreateUserValidationRegions>(new
    {
        Input = input
    }, cancellationToken);
}
```

### Minimal API live-validation example

```csharp
app.MapPost("/users/live-validate", async (HttpContext context, CancellationToken cancellationToken) =>
{
    var bound = await context.BindFormAsync<CreateUserInput>(cancellationToken);
    var scope = await context.BindLiveValidationScopeAsync(cancellationToken);
    var state = _validator.ValidateLive(bound.Model, scope);

    context.SetValidationState(state);

    return await HrzResults.Partial<CreateUserValidationRegions>(
        context,
        new { Input = bound.Model },
        cancellationToken: cancellationToken);
});
```

### Important split

Client-side local validation and server live validation are different paths:

- local validation is immediate and not debounced
- server live validation is async and usually debounced

Do not force simple local rules through the server.

## Explicitly Excluded Path

The following is not an official path in this spec:

- browser submits to JSON endpoint and a client-side library becomes the primary rendering contract

That may exist in an application, but it should not be the design center for HyperRazor.

## Framework Work Implied by This Spec

### Core

- add `HrzValidationState`
- add `HrzValidationStateBuilder`
- add `HrzContextItemKeys.ValidationState`
- add `HttpContext.SetValidationState(...)`
- add backend JSON to `HrzValidationState` adapters

### Components

- promote `HrzValidationBridge`
- add field-name helper
- ensure stable DOM targeting for client-side validation regions

### MVC

- adapt `ModelState` into `HrzValidationState` automatically

### Minimal API

- add `BindFormAsync<T>()`
- add `BindFormAndValidateAsync<T>()`
- add live-scope binding helper

### Docs and demos

- MVC local validation demo
- MVC backend-proxy validation demo
- Minimal API local validation demo
- Minimal API backend-proxy validation demo
- client-side local validation demo
- scoped server live validation demo

## Recommended Implementation Order

1. Build `HrzValidationState` and render transport.
2. Promote `HrzValidationBridge`.
3. Add the Minimal API bind-and-validate helper.
4. Add backend JSON mapping helpers.
5. Add the client-side DOM contract and example.
6. Add scoped server live validation on top of the shared state model.

## Final Recommendation

HyperRazor should standardize around this hierarchy:

- HTML-first submit flows are the primary path.
- Backend JSON validation is an internal server integration detail.
- Client-side local validation is immediate and optional.
- Server live validation is reserved for server-owned rules.

That keeps the framework aligned with SSR plus HTMX instead of drifting toward a JSON-first browser architecture.
