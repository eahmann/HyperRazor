# HyperRazor Validation Authoring Cookbook

**Status:** v1 companion guide  
**Related:** `docs/validation-framework-detailed-api-spec.md`

This document is the practical companion to the validation API spec. It focuses on the common authoring shapes that should be copy-pasteable in real forms.

## 1. Local Validation Form

Use this when the edge app can validate locally and rerender the form on invalid submit.

```razor
<HrzForm Model="Input" Action="/users/invite" FormName="users-invite">
    <HrzValidationSummary />

    <div class="field-shell">
        <HrzField For="() => Input.DisplayName">
            <HrzLabel />
            <HrzInput />
            <HrzValidationMessage />
        </HrzField>
    </div>

    <div class="field-shell">
        <HrzField For="() => Input.Email">
            <HrzLabel />
            <HrzInput Type="email" />
            <HrzValidationMessage />
        </HrzField>
    </div>

    <button type="submit">Invite User</button>
</HrzForm>
```

Notes:

- `HrzForm` enhancement is on by default
- `HrzField` owns field context only; wrapper layout stays app-owned
- `HrzValidationMessage` and `HrzValidationSummary` render both client and submit/server validation state

## 2. Password Fields

Use the normal `HrzInput` component with `Type="password"`.

```razor
<HrzField For="() => Input.Password">
    <HrzLabel />
    <HrzInput Type="password" />
    <HrzValidationMessage />
</HrzField>
```

Password rules in v1:

- password inputs do not replay attempted values
- password inputs do not emit a `value` attribute
- local validation metadata still emits normally
- live validation is off by default

## 3. Numeric Fields

Numeric fields use `HrzInput Type="number"`.

```razor
<HrzField For="() => Input.Age">
    <HrzLabel />
    <HrzInput Type="number" />
    <HrzValidationMessage />
</HrzField>
```

Example model:

```csharp
public sealed class InviteInput
{
    [Range(18, 120, ErrorMessage = "Age must be between 18 and 120.")]
    public int Age { get; set; }
}
```

The rendered control replays attempted numeric values and emits `data-val-range`, `data-val-range-min`, and `data-val-range-max`.

## 4. Minimal API Backend Proxy

Use `HrzPosted<TModel>` when the API or backend policy is authoritative and the HTML edge needs to rerender invalid state cleanly.

```csharp
app.MapPost("/validation/minimal/proxy", async (
    HrzPosted<InviteInput> posted,
    CancellationToken cancellationToken) =>
{
    if (!posted.IsValid)
    {
        return await posted.Invalid<InviteForm>(new { Form = InvitePage.Invalid(posted.Model) }, cancellationToken);
    }

    var backendState = await ValidateBackendAsync(posted.Model, cancellationToken);
    if (!backendState.IsValid)
    {
        return await posted.Invalid<InviteForm>(backendState, new { Form = InvitePage.Invalid(posted.Model) }, cancellationToken);
    }

    return await posted.Valid<InviteForm>(new { Form = InvitePage.Success(posted.Model) }, cancellationToken);
});
```

Notes:

- `HrzPosted<TModel>` normalizes local submit validation before the form component rerenders
- backend invalid state should be normalized to `HrzSubmitValidationState` before render-time components consume it

## 5. Live Validation With Dependencies

In v1, live validation transport is still configured per form, but the request payload should stay scoped to:

- the triggering field
- reserved HyperRazor live fields
- only the declared dependent fields needed for the rule

Do not use `hx-include="closest form"` for field-level live validation.

Example:

```razor
@inject IHrzHtmlIdGenerator HtmlIdGenerator

<HrzField For="() => Input.Email">
    <HrzLabel />
    <HrzInput Type="email" AdditionalAttributes="@EmailLiveAttributes" />
    <HrzValidationMessage />
</HrzField>

@code {
    private IReadOnlyDictionary<string, object?> EmailLiveAttributes => new Dictionary<string, object?>
    {
        ["hx-post"] = "/validation/live",
        ["hx-trigger"] = "input changed delay:400ms, blur",
        ["hx-target"] = $"#{HtmlIdGenerator.GetFieldMessageId(\"users-invite\", HrzFieldPaths.FromFieldName(nameof(InviteInput.Email)))}--server",
        ["hx-swap"] = "outerHTML",
        ["hx-include"] = $"#{HtmlIdGenerator.GetFieldId(\"users-invite\", HrzFieldPaths.FromFieldName(nameof(InviteInput.DisplayName)))}",
        ["hx-vals"] = "{\"__hrz_root\":\"users-invite\",\"__hrz_fields\":\"Email\"}"
    };
}
```

Notes:

- dependency-scoped inclusion is a framework concern; app code should not reinvent whole-form transport rules
- smarter dependency-activated live validation is deferred to `docs/validation-framework-future-dependency-activated-live-validation.md`

## 6. Checkbox Fields

Checkboxes use `HrzCheckbox`.

```razor
<HrzField For="() => Input.AcceptTerms">
    <HrzCheckbox />
    <HrzLabel Text="Accept terms" />
    <HrzValidationMessage />
</HrzField>
```

Notes:

- v1 supports non-nullable `bool` only
- the component emits the hidden false companion input automatically
- attempted values win over the model on rerender
