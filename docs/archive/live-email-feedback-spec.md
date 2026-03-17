> Historical document
>
> This file is archived design history. It may describe retired package IDs, old `src/` paths, or superseded assumptions.
> Use [`docs/README.md`](../README.md) for the current docs map and [`docs/package-surface.md`](../package-surface.md) for the current package story.

# Live Email Feedback Design Spec

**Date:** 2026-03-06  
**Status:** Superseded by the implemented validation framework and `/validation` harness  
**Scope:** Historical demo-only note retained for discussion context

> Historical note: the current implementation uses `/validation` and `/validation/live` with `ValidationPage`, `ValidationController`, `UserInviteValidationForm`, and `HrzValidationBridge`. Use `docs/validation-framework-spec-greenfield.md` and the phase docs for the current framework direction.

## Summary

Add server-backed live feedback for the email field on `/demos/validation`.

The interaction should stay MVC-first and HTMX-first:

- `EditForm` remains the authoring surface.
- MVC remains the source of truth for submit-time binding and validation.
- HTMX provides debounced, field-scoped email checks while the user types.
- Whole-form validation continues to happen only on submit.

This feature is a demo enhancement, not a framework-level validation abstraction.

## Problem Statement

The current validation demo only reports email problems after form submit. That is enough to prove the server-validation bridge, but it does not show the more interactive case where a server-only rule can provide useful feedback before submit.

The specific server-only rule already present in the demo is email reservation:

- `alex@hyperrazor.dev`
- `ops@hyperrazor.dev`

We want to surface that rule as the user types without turning the whole form into a live full-model validator.

## Goals

- Give the email field live feedback with a debounce.
- Reuse the existing server-only email reservation rule.
- Keep the main `/demos/validation` submit endpoint authoritative.
- Avoid validating unrelated fields on every keystroke.
- Avoid introducing a generic framework validation abstraction in this task.
- Keep the response HTML-based, not JSON-based.

## Non-Goals

- Do not promote `HrzServerValidationBridge` as part of this task.
- Do not invent a cross-framework validation contract.
- Do not add Minimal API parity here.
- Do not add client-side validation libraries.
- Do not make the live endpoint responsible for full-form success/failure state.

## Baseline

Current implementation:

- GET `/demos/validation` renders `ValidationDemoPage`.
- POST `/demos/validation` performs normal MVC model binding and submit-time validation.
- Invalid HTMX submit returns `422` and rerenders `ValidationDemoForm`.
- `HrzServerValidationBridge` hydrates Blazor validation UI from MVC `ModelState`.
- The email reservation rule is hard-coded inside `ValidationDemoController`.

Relevant files:

- `src/HyperRazor.Demo.Mvc/Controllers/ValidationDemoController.cs`
- `src/HyperRazor.Demo.Mvc/Components/Fragments/ValidationDemoForm.razor`
- `src/HyperRazor.Demo.Mvc/Models/CreateUserInput.cs`
- `src/HyperRazor.Demo.Mvc/Components/HrzServerValidationBridge.razor`

## Key Design Decisions

### D1. Use a dedicated live-email endpoint

Add a separate HTMX endpoint for live email feedback instead of reusing the submit endpoint.

Recommended route:

- `POST /demos/validation/email-live`

Reason:

- keeps submit-time validation authoritative
- keeps live requests clearly field-scoped
- avoids mixing live advisory feedback with submit failure semantics

### D2. Do not run whole-model validation on each live request

The live endpoint should not call `ModelState.IsValid` as if the user submitted the full form.

Live requests should evaluate only email-specific rules:

- empty / whitespace
- incomplete or invalid email format
- reserved email rule

Reason:

- avoids noise from untouched required fields
- avoids treating a field check like a fake submit
- keeps the mental model simple

### D3. Return HTML fragments, not JSON

The live endpoint should return a rerendered `ValidationDemoForm` fragment with a new `LiveEmailStatus` parameter.

Reason:

- matches repo HTMX conventions
- keeps all display logic server-rendered
- avoids adding client rendering logic for a demo feature

### D4. Use `200` for live checks

The live endpoint should always return `200 OK`.

Do not use `422` for debounced field checks.

Reason:

- a live field check is advisory, not a failed submit
- `422` remains reserved for actual submit-time invalid HTMX posts

### D5. Do not use the server-validation bridge for live checks

`HrzServerValidationBridge` stays submit-only.

The live endpoint should not populate MVC `ModelState` for display through `ValidationMessage` and `ValidationSummary`.

Reason:

- the bridge is tied to submit-time MVC validation state
- live feedback is a distinct UX surface
- keeping these flows separate avoids stale summary and stale submit-error behavior

## Proposed Interaction Model

### User Flow

1. User loads `/demos/validation`.
2. User types into the email field.
3. HTMX sends a debounced POST to `/demos/validation/email-live`.
4. Server evaluates email-only rules.
5. Server rerenders the full `ValidationDemoForm` fragment with:
   - current form values preserved
   - live email status rendered
   - no submit-time validation summary or submit-time field errors
6. If the user submits the form, normal `/demos/validation` submit-time validation still runs.

### Why rerender the full form fragment

The earlier idea of swapping only a tiny email-status region is appealing, but it leaves stale submit-time UI behind:

- stale `ValidationMessage` content for email
- stale `ValidationSummary`
- stale invalid field styling

Rerendering the full `ValidationDemoForm` fragment avoids that state drift without inventing a second client-side state system.

This is the recommended design unless browser testing shows unacceptable caret/focus churn while typing.

### Fallback if full-form swaps cause typing jitter

If swapping the whole form proves unreliable while typing, the fallback design is:

- keep the main live target as a dedicated `#validation-demo-email-live` region
- use OOB swaps to clear `#validation-demo-email-error` and `#validation-demo-summary`
- keep the live status advisory-only

That fallback is lower priority and should only be used if the primary design fails real browser verification.

## Endpoint Contract

### Route

- `POST /demos/validation/email-live`

### Request

Bind from form data using the existing model:

```csharp
[FromForm] CreateUserInput input
```

The request should include the entire current form snapshot so the server can rerender the form with all current values intact.

### Response

For HTMX requests:

- return `Partial<ValidationDemoForm>(...)`
- status code `200`

For non-HTMX requests:

- return `400 Bad Request`

The live endpoint is an HTMX-only fragment contract.

## Live Email State Model

Add a small demo-local status model.

Recommended shape:

```csharp
public enum LiveEmailState
{
    Idle,
    Incomplete,
    Available,
    Unavailable
}

public sealed record LiveEmailStatus(LiveEmailState State, string Message);
```

Recommended file:

- `src/HyperRazor.Demo.Mvc/Models/LiveEmailStatus.cs`

This type is demo-local. Do not place it in a framework package.

## Validation Rules

The live endpoint should apply these rules in order:

1. Trim the incoming email.
2. If empty or whitespace:
   - state: `Idle`
   - message: `Enter an email address to check availability.`
3. If not a valid email format:
   - state: `Incomplete`
   - message: `Enter a full email address to check availability.`
4. If the normalized email is reserved:
   - state: `Unavailable`
   - message: `That email address is already reserved by the demo directory.`
5. Otherwise:
   - state: `Available`
   - message: `Email looks available.`

Important:

- `Incomplete` is advisory, not a submit-time error.
- Only the submit endpoint should surface full validation errors.

## Reuse Strategy

Extract the reserved-email rule from `ValidationDemoController` into a small reusable demo-local service or helper.

Recommended responsibilities:

- normalize email input
- determine whether an email is reserved
- produce the live availability result
- allow the submit endpoint to reuse the same reserved-email rule

Suggested locations:

- `src/HyperRazor.Demo.Mvc/Infrastructure/ValidationDemoEmailRules.cs`
- or `src/HyperRazor.Demo.Mvc/Models/ValidationDemoEmailRules.cs`

Do not extract full-form validation into a general validator service in this task. Only lift the reusable email rule.

## Razor Changes

Update `ValidationDemoForm.razor` to accept a live status parameter:

```csharp
[Parameter]
public LiveEmailStatus? LiveEmailStatus { get; set; }
```

Render a dedicated live-status region under the email input:

- stable id: `validation-demo-email-live`
- `aria-live="polite"`
- `aria-atomic="true"`
- omit the region entirely when there is no status

Recommended email input HTMX shape:

```razor
<InputText
    id="validation-demo-email"
    name="Email"
    class="validation-input"
    autocomplete="email"
    hx-post="/demos/validation/email-live"
    hx-trigger="input changed delay:400ms, blur"
    hx-target="#validation-demo-form"
    hx-swap="outerHTML"
    hx-include="closest form"
    hx-indicator="#validation-demo-email-indicator"
    hx-sync="closest form:abort"
    @bind-Value="Input.Email" />
```

Notes:

- Keep `name="Email"` explicit for MVC binding consistency.
- `hx-include="closest form"` is for preserving the current form snapshot, not for antiforgery.
- HyperRazor already attaches antiforgery headers automatically for unsafe HTMX methods.

## Form Rendering Rules

### On initial GET

- render no live email status
- render no validation summary
- render no field errors

### On live email HTMX POST

- rerender the whole `ValidationDemoForm`
- preserve current input values
- render `LiveEmailStatus`
- do not render `SuccessMessage`
- do not render submit-time `ValidationSummary`
- do not render submit-time `ValidationMessage` errors
- do not populate the server-validation bridge

### On submit POST

- preserve current behavior
- invalid full-page submit returns page with summary and field errors
- invalid HTMX submit returns `422` with summary and field errors
- valid submit returns success state and clears live email status

## Controller Changes

Update `ValidationDemoController` in three places.

### C1. Extract reserved-email logic

Move the current hard-coded reservation rule out of the controller body into a small helper or service.

### C2. Add the live-email endpoint

Recommended shape:

```csharp
[HttpPost("/demos/validation/email-live")]
public Task<IResult> EmailLive([FromForm] CreateUserInput input, CancellationToken cancellationToken)
{
    if (!HttpContext.HtmxRequest().IsPartialRequest)
    {
        return Results.BadRequest();
    }

    var liveEmailStatus = _emailRules.GetLiveStatus(input.Email);

    return Partial<ValidationDemoForm>(
        new
        {
            Input = input,
            LiveEmailStatus = liveEmailStatus
        },
        cancellationToken);
}
```

### C3. Keep submit behavior authoritative

The existing `/demos/validation` submit action should keep:

- MVC model binding
- DataAnnotations validation
- reserved email rule applied to `ModelState`
- full-page invalid `200`
- HTMX invalid `422`

The only behavioral change to the submit action should be reuse of the extracted email rule helper.

## UI States

Recommended visual states for `#validation-demo-email-live`:

| State | Tone | Message |
| --- | --- | --- |
| `Idle` | neutral | Enter an email address to check availability. |
| `Incomplete` | muted/advisory | Enter a full email address to check availability. |
| `Available` | success | Email looks available. |
| `Unavailable` | danger | That email address is already reserved by the demo directory. |

Implementation notes:

- Do not present `Incomplete` as a full error banner.
- Only `Unavailable` should read as a negative server result.
- The submit endpoint remains the only authoritative source of form validity.

## Accessibility

- Keep the email input `id` stable: `validation-demo-email`.
- Include the live-status region in `aria-describedby` when it is rendered.
- Use `aria-live="polite"` to avoid over-announcing while typing.
- Keep server submit errors in the existing `ValidationMessage` path on real submit.
- The live endpoint should not steal focus.

## CSS Expectations

Add styles for:

- `.field-live-status`
- `.field-live-status--idle`
- `.field-live-status--incomplete`
- `.field-live-status--available`
- `.field-live-status--unavailable`

Add a small inline indicator near the email field:

- id: `validation-demo-email-indicator`
- text: `Checking email...`

The live indicator should be scoped to the email field and should not replace the main form submit indicator.

## HTMX Expectations

- Use HTML fragment responses only.
- Keep the target id stable: `#validation-demo-form`.
- Debounce with `delay:400ms`.
- Include `blur` so keyboard users get feedback when tabbing away.
- Use a sync strategy that prevents stale in-flight requests from winning.

Recommended initial configuration:

```text
hx-trigger="input changed delay:400ms, blur"
hx-sync="closest form:abort"
```

Another agent should verify this in-browser and adjust only if actual request races appear.

## Testing Requirements

Add integration coverage in `tests/HyperRazor.Demo.Mvc.Tests/DemoMvcIntegrationTests.cs` for:

1. GET `/demos/validation` still renders without a live status region.
2. POST `/demos/validation/email-live` without antiforgery token returns `400`.
3. HTMX POST `/demos/validation/email-live` with blank email returns `200` and `Idle` message.
4. HTMX POST `/demos/validation/email-live` with malformed email returns `200` and `Incomplete` message.
5. HTMX POST `/demos/validation/email-live` with reserved email returns `200` and `Unavailable` message.
6. HTMX POST `/demos/validation/email-live` with non-reserved email returns `200` and `Available` message.
7. Live email responses preserve current `Name` and `Age` values in the rerendered form.
8. Submit-time invalid HTMX POST to `/demos/validation` still returns `422`.
9. Submit-time valid HTMX POST to `/demos/validation` still succeeds and clears any live email status.

If Playwright coverage is added later, include one browser test that proves:

- typing `alex@hyperrazor.dev` surfaces live unavailable feedback
- typing a new available address replaces it
- final submit still uses normal server validation

## File Map

Expected files to touch:

- `src/HyperRazor.Demo.Mvc/Controllers/ValidationDemoController.cs`
- `src/HyperRazor.Demo.Mvc/Components/Fragments/ValidationDemoForm.razor`
- `src/HyperRazor.Demo.Mvc/wwwroot/app.css`
- `tests/HyperRazor.Demo.Mvc.Tests/DemoMvcIntegrationTests.cs`

Expected new files:

- `src/HyperRazor.Demo.Mvc/Models/LiveEmailStatus.cs`
- one demo-local helper or service for reserved-email logic

## Implementation Sequence

1. Extract the reserved-email rule into a reusable demo-local helper.
2. Add `LiveEmailStatus` model.
3. Add the `/demos/validation/email-live` controller action.
4. Extend `ValidationDemoForm.razor` with live status rendering and email-field HTMX wiring.
5. Add CSS for the live status and field indicator.
6. Add integration tests.
7. Verify in a browser that full-form rerender does not break typing flow.

## Risks

### R1. Caret or focus instability during full-form swaps

This is the main implementation risk.

Mitigation:

- keep the email input id stable
- verify in a real browser
- only fall back to the status-region-plus-OOB design if the full-form swap is visibly unreliable

### R2. Rule duplication

If the reserved-email rule remains in both submit and live endpoints, the demo will drift.

Mitigation:

- extract the rule before wiring the live endpoint

### R3. Scope creep into generic validation infrastructure

This feature can easily turn into a framework-abstraction discussion.

Mitigation:

- keep everything demo-local
- keep `HrzServerValidationBridge` untouched
- do not generalize beyond the MVC demo

## Final Recommendation

Implement live email feedback as a separate HTMX POST endpoint that rerenders the full `ValidationDemoForm` fragment with a demo-local `LiveEmailStatus`.

That gives the cleanest state model:

- live email feedback while typing
- no whole-model validation on every keystroke
- no stale submit-time summary or field errors
- no new framework abstraction

If the full-form swap causes real typing problems in browser verification, use the smaller email-status target with OOB clearing as the fallback design.
