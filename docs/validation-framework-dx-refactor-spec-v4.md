# HyperRazor Validation DX Refactor Spec

**Date:** 2026-03-09  
**Status:** Final parent design spec; detailed API spec still required  
**Supersedes:** `docs/validation-framework-dx-refactor-spec-v3.md`  
**Reviewed input:** `docs/validation-framework-spec-feedback.md`, `docs/validation-framework-dx-refactor-spec-v2-feedback.md`, `docs/validation-framework-dx-refactor-spec-v3-feedback.md`

## 1. Problem Statement

HyperRazor already has most of the validation runtime primitives it needs:

- submit-time validation state
- attempted-value replay
- MVC `ModelState` mapping
- Minimal API bind-and-validate helpers
- backend `ValidationProblemDetails` mapping
- server live-validation patches

The weak point is still developer experience.

Today, application code still hand-wires too much of the validation contract:

- field paths
- `name` values
- attempted-value replay
- `aria-invalid`
- `aria-describedby`
- client-slot IDs
- server-slot IDs
- `hx-*` live-validation attributes
- field-specific browser validation hooks

That is materially worse than native ASP.NET MVC tag helpers and worse than the `EditForm` + `ValidationMessage` mental model that Blazor developers already know.

The refactor should therefore optimize for authoring DX first while preserving the full server-path matrix that HyperRazor already supports.

## 2. Outcome

HyperRazor validation should feel close to native ASP.NET while staying honest to SSR + HTMX:

1. validation rules are authored once on the model or validator
2. field components bind by expression, not by string path
3. the framework emits names, ids, validation metadata, HTMX hooks, and message regions automatically
4. MVC and Minimal API converge on the same invalid-rerender pipeline
5. backend API validation remains a server-to-server concern and still rerenders HTML
6. local client validation and server live validation compose instead of competing

The desired public posture is:

- model-first like ASP.NET validation attributes and validators
- expression-first like `asp-for`, `ValidationMessage`, and `EditForm`
- HTML-first like HyperRazor and Rizzy
- transport-aware for MVC, Minimal API, and backend-proxy flows

## 3. Non-Negotiable Path Matrix

The refactor must preserve all supported validation paths.

| Path | Required | Browser contract | Notes |
| --- | --- | --- | --- |
| MVC local validation | yes | full HTML or fragment HTML | closest to native ASP.NET controller flow |
| MVC -> backend API -> HTML rerender | yes | full HTML or fragment HTML | backend JSON remains server-to-server only |
| Minimal API local validation | yes | full HTML or fragment HTML | no second-class runtime |
| Minimal API -> backend API -> HTML rerender | yes | full HTML or fragment HTML | same HTML contract as MVC |
| local client validation | yes | DOM updates only | immediate feedback, no network |
| server live validation | yes | targeted field and summary HTML patches | partial patch only, no whole-form replacement |

This proposal does not reduce the path matrix. It standardizes it.

## 4. Core Position

The browser-facing contract should remain HTML-first.

The major change is public posture:

- the current runtime primitives remain the engine
- a framework authoring surface becomes the default public path
- plain HTML remains an escape hatch
- low-level validation transport types remain advanced or internal-facing

In short:

`HrzSubmitValidationState`, `HrzLiveValidationPatch`, `HrzFormPostState<TModel>`, `HrzFieldPath`, and `HrzValidationRootId` remain necessary runtime concepts. They should stop being the primary app authoring model.

## 5. Corrected Design Constraints

### 5.1 Normalized HyperRazor metadata is the internal source of truth

ASP.NET validation metadata is the best default source and the best default integration target.

It should not be the literal core model.

HyperRazor should own a normalized internal validation descriptor model that can be populated by:

- DataAnnotations and ASP.NET metadata by default
- HyperRazor live-validation metadata adapters
- backend or application-layer validator adapters
- FluentValidation-style adapters later

That internal model should carry:

- canonical field identity
- display metadata
- local-validation rule metadata
- live-validation semantics
- dependent-field relationships
- message and summary behavior

Design rule:

Do not couple the framework's long-term authoring surface to MVC metadata internals or to the exact unobtrusive validation implementation details.

### 5.2 Expression-first does not imply interactive Blazor semantics

`For="..."` exists for metadata inference.

It does not imply:

- interactive Blazor events
- `OnValidSubmit` as the primary submission model
- `EditContext` as the source of truth
- websocket or circuit-based validation lifecycle

It should be used to derive:

- canonical field path
- `name`
- `id`
- display metadata
- validation metadata
- live-validation targeting

This needs to be explicit so the authoring model stays aligned with HyperRazor's actual runtime.

### 5.3 `FormName` is the primary public root concept

The authoring surface should expose a stable `FormName` concept publicly.

Recommendation:

- use `FormName` on `HrzForm` and related authoring components
- map it internally to `HrzValidationRootId`
- allow explicit override when needed
- auto-generate only when stability is guaranteed

This is a naming and authoring decision, not a commitment to Blazor `_handler` semantics.

Low-level MVC and Minimal API helpers may continue to accept `HrzValidationRootId` directly where that is the clearer transport-facing contract.

## 6. Architecture

The replacement stack should have four layers.

### 6.1 Layer 1: Validation descriptors

This becomes the normalized internal source of truth for field identity, validation metadata, and DOM emission.

Inputs:

- `ValidationAttribute`
- `IValidatableObject`
- ASP.NET metadata adapters
- HyperRazor live-validation metadata adapters
- optional FluentValidation-style adapters later

Responsibilities:

- normalize validation rules across metadata sources
- define client-emittable metadata
- define live-validation semantics and dependencies
- define display and field identity metadata
- define enough field metadata for authoring components to resolve names, ids, and attempted-value access without transport-specific glue

Default posture:

- DataAnnotations and ASP.NET metadata are the primary adapter path
- `data-val-*` is the preferred emitted local-validation contract
- HyperRazor-specific metadata is limited to live-validation coordination and HTML patch targeting

Minimum descriptor shape for the next API pass:

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

The concrete API names can still change.
What should not stay vague is the minimum information the authoring layer needs.

Final DOM ids should be derived by the form and field rendering context from `FormName` plus canonical field identity.
They should not be treated as descriptor-owned state.

### 6.2 Layer 2: Authoring surface

This is the main refactor target.

Add a framework-owned authoring surface whose documented default is `HrzField` plus child controls.

Primary public shape:

- `HrzForm`
- `HrzField`
- `HrzLabel`
- `HrzValidationMessage`
- `HrzValidationSummary`

Standalone `HrzInput* For="..."` controls should not be first-class in v1.
They can be added later as a convenience or advanced path after the field-context model is proven.

The first-pass control family should stay intentionally small:

- `HrzInput`
- `HrzTextArea`
- `HrzCheckbox`

`HrzInput` is the first-pass control for text-like input types such as:

- `text`
- `email`
- `search`
- `tel`
- `url`
- `password`

Defer until the form and field model is proven:

- `HrzSelect`
- `HrzInputNumber`
- radio groups
- file inputs
- date and time inputs
- other specialized controls

Example target shape:

```razor
<HrzForm Model="Invite" Action="/users/invite" FormName="users-invite">
    <HrzValidationSummary />

    <HrzField For="() => Invite.DisplayName">
        <HrzLabel />
        <HrzInput />
        <HrzValidationMessage />
    </HrzField>

    <HrzField For="() => Invite.Email">
        <HrzLabel />
        <HrzInput Type="email" />
        <HrzValidationMessage />
    </HrzField>
</HrzForm>
```

Non-interactive runtime explanation:

- `HrzForm` provides a request-scoped form context containing the model instance, `FormName`, submit validation state, attempted values, and validation descriptor graph.
- `HrzField For="..."` resolves field metadata and value access against that context.
- the framework uses that resolution to derive canonical path, HTML name, stable id, display metadata, attempted-value lookup, descriptor lookup, and live-validation targeting.
- this does not require interactive Blazor lifecycle or `EditContext` ownership.
- nested and indexed field identities must resolve through the same context rules as scalar fields.

The framework, not application code, should determine:

- canonical field path
- `name`
- stable `id`
- attempted value
- invalid CSS class hooks
- `aria-invalid`
- `aria-describedby`
- validation message region ids
- local-validation metadata
- live-validation HTMX hooks

Plain HTML inputs should still be supported, but they should be the escape hatch for unusual cases, not the recommended golden path.

### 6.3 Layer 3: Request and render runtime

Keep the existing runtime split:

- full-root submit validation state
- targeted live-validation patch state

Recommended runtime concepts to preserve:

- `HrzSubmitValidationState`
- `HrzLiveValidationPatch`
- `HrzFormPostState<TModel>`
- attempted-value primitives
- canonical field-path resolution
- backend `ValidationProblemDetails` mapping

The runtime continues to serve MVC, Minimal API, and backend proxy flows.
The public change is that application authors should touch these concepts rarely.

### 6.4 Layer 4: Endpoint adapters

Keep one semantic invalid-rendering pipeline, then expose it through:

- MVC controller helpers
- Minimal API route-handler helpers
- backend problem-details mappers

The adapter layer should hide transport differences instead of leaking them to application code.

## 7. Public API Direction

### 7.1 Form root

Introduce a form root component that owns:

- form identity
- antiforgery
- HTMX enhancement
- validation summary region
- root-level DOM metadata
- invalid submit rerender integration

The public parameter should be `FormName`.

Enhancement defaults:

- `HrzForm` defaults `Enhance` to `true`.
- when enhanced, it emits normal `<form>` markup and antiforgery plus HTMX submit enhancement for HTML fragment postbacks.
- enhancement preserves plain-form fallback when HTMX or JavaScript is unavailable.
- enhancement controls submit transport behavior only; it does not by itself enable or disable local validation or live-validation metadata emission.
- `Enhance="false"` opts out to plain full-page form submission while keeping the same authoring surface.

Enhanced submit target defaults:

- when `HrzForm` is enhanced, submit requests target the form root by default
- invalid submit responses replace the form root with rerendered HTML for that same form
- successful submit responses may retarget elsewhere through explicit response behavior, but the default contract is form-root replacement
- field-level live validation does not reuse the form-root target; it continues to target field and summary regions only

### 7.2 Field binding

Every field component should accept a `For` expression.

That expression should drive:

- path resolution
- label text defaulting
- id generation
- validation message lookup
- client metadata emission
- live-validation dependency resolution

This is the single biggest DX gain in the refactor.

`HrzField` is the default field-context owner.

Inside `HrzField`, child components should bind to ambient field context by default:

- `<HrzLabel />` binds to the current field
- `<HrzInput />` binds to the current field
- `<HrzValidationMessage />` binds to the current field

Out-of-context field targeting such as `<HrzLabel For="..." />` or `<HrzValidationMessage For="..." />` remains an escape hatch, not the documented primary posture.

### 7.3 Message components

Add first-class message primitives:

- `HrzValidationMessage For="..."`
- `HrzValidationSummary`

These should render both:

- submit-time server errors
- live-validation server errors
- client-side local errors

The implementation may still keep separate internal slots for client versus server ownership, but the public API should not force application code to manage that split manually.

Summary merge rules for v1:

- `HrzValidationSummary` renders submit-time server errors by default.
- `HrzValidationSummary` may render live-validation server errors when a live patch explicitly marks summary replacement through `ReplaceSummary`.
- full invalid submit rerenders replace summary contents entirely from `HrzSubmitValidationState`.
- client-only local field errors do not appear in summary by default in v1; they remain field-local unless a later opt-in summary mode is added.
- when local client validation fails for a field, live validation for that field is blocked and no summary patch should be requested for that field interaction.

### 7.4 Layout ownership

Do not make the framework own grid or layout markup.

Recommended split:

- framework owns field semantics
- app owns layout and styling

### 7.5 Escape hatch

Plain `<form>` and plain HTML controls remain supported.

They should continue to work with:

- `HrzFieldPath` and `HrzFormRendering` helpers
- current MVC and Minimal API runtime contracts
- explicit root and path wiring for unusual cases

They are the escape hatch, not the documentation default.

### 7.6 Checkbox semantics

`HrzCheckbox` remains a special-case first-pass control only if its transport semantics are defined explicitly.

V1 checkbox rules:

- `HrzCheckbox` targets non-nullable `bool` fields in the first pass.
- it emits a visible checkbox plus a hidden false companion input so unchecked submits still post a value in normal HTML form posts.
- on submit rerender, attempted values win over the model value when determining checked state.
- when both the hidden false companion value and the checked value are posted, the checked value takes precedence.
- nullable `bool?`, tri-state behavior, and more advanced checkbox variants are deferred until after the first-pass authoring model is stable.

### 7.7 Password input semantics

`HrzInput Type="password"` is a special-case text-like input and must not inherit normal attempted-value replay behavior.

V1 password rules:

- password inputs do not replay attempted values on rerender
- password inputs do not emit a `value` attribute from either the model or `HrzSubmitValidationState`
- local client validation metadata may still be emitted for rules such as `required`, `minlength`, and `maxlength`
- live validation is off by default for password inputs
- password confirmation and other cross-field password rules may still participate through local validation metadata or submit-time and server validation

## 8. Client Validation Strategy

### 8.1 Browser contract

The browser-facing local-validation contract should be:

- standard `data-val-*` metadata for local validation
- HyperRazor metadata only for live-validation coordination and DOM targeting

Why:

- it aligns with native ASP.NET MVC
- it avoids inventing HyperRazor-only rule names for common rules
- it keeps custom validator integration familiar
- it works across MVC and component rendering

### 8.2 Browser library posture

Do not lock the framework contract to one browser validation library yet.

The browser library should be treated as:

- the default adapter
- the reference implementation
- or an official companion package

It should not be the hardcoded framework contract.

`aspnet-client-validation` is a good default reference point because it fits the `data-val-*` ecosystem without a jQuery dependency, but the spec should not require wire-level or package-level exclusivity.

### 8.3 Local versus live split

Client validation should handle:

- required
- format
- length
- range
- simple cross-field rules that can be expressed locally

Server live validation should handle:

- uniqueness
- policy checks
- backend-owned rules
- rules needing database or service access
- cross-field rules the browser cannot evaluate reliably

If local validation fails, server live validation should be blocked automatically.

### 8.4 Guaranteed local-client support

V1 local client validation is only guaranteed for rules that can be emitted into the chosen browser metadata contract.

That means:

- DataAnnotations-backed rules are the default local-client path.
- other validator sources may participate in submit-time and live server validation immediately.
- those other validator sources only participate in local browser validation when an adapter emits compatible client metadata.
- FluentValidation or other future adapters should not imply automatic browser parity unless they also supply local metadata emission.

## 9. Live Validation Strategy

### 9.1 Transport

Keep live validation as targeted HTML patching, not JSON-to-browser validation.

That is part of HyperRazor's architecture and should remain.

### 9.2 Metadata semantics

Live validation should be declared from validation metadata, not inline per-input HTMX wiring.

The design should support `RemoteAttribute`-like semantics:

- endpoint selection
- dependent fields
- validation timing
- debounce and trigger options

But it should not imply classic ASP.NET remote-validation transport compatibility.

The correct framing is:

HyperRazor live validation borrows remote-validation semantics while retaining an HTML-patch transport contract.

HyperRazor may need a custom descriptor, attribute, or adapter registration model for SSR and HTMX-specific behavior.

### 9.3 DOM behavior

Live validation must only patch:

- the affected field message region
- dependent field message regions when necessary
- the summary region when necessary

It must not rerender the whole form during typing.

## 10. MVC and Minimal API Shape

### 10.1 Helper contract requirement

The invalid-render helper surface must support all three invalid sources without awkward branching:

- locally-derived submit validation state
- backend-mapped validation state
- merged validation state containing both local and backend errors

The eventual public helpers may expose convenience overloads for `ModelState` or `ValidationProblemDetails`, but the semantic contract must support a fully normalized `HrzSubmitValidationState` as the common denominator.
Normalization to `HrzSubmitValidationState` should happen before render-time authoring components consume the result.

### 10.2 MVC

MVC should feel close to native:

```csharp
[HttpPost("/users/invite")]
public async Task<IResult> Invite([FromForm] InviteUserInput input, CancellationToken cancellationToken)
{
    if (!ModelState.IsValid)
    {
        return await this.HrzInvalid<UserInviteForm>(input, cancellationToken);
    }

    // optional backend call here
    return await this.HrzValid<UserInviteForm>(new InviteUserInput(), cancellationToken);
}
```

The important part is not the exact helper name.
The important part is that invalid rerendering should be one obvious helper call, not manual validation-state plumbing.

### 10.3 Minimal API

Minimal APIs need equivalent ergonomics.

Target posture:

```csharp
app.MapPost("/users/invite", async (
    HrzPosted<InviteUserInput> post,
    CancellationToken cancellationToken) =>
{
    if (!post.IsValid)
    {
        return await post.Invalid<UserInviteForm>(cancellationToken);
    }

    return await post.Valid<UserInviteForm>(cancellationToken);
});
```

That may be implemented with:

- a custom binder type
- endpoint filters
- helper extensions

The public goal is simple:

Minimal API should not feel like a second validation framework.

### 10.4 Backend API proxy flows

Both MVC and Minimal API must continue supporting:

1. local bind and validation first
2. backend call only when local validation passes
3. backend `ValidationProblemDetails` mapped back into HTML
4. attempted values preserved

This flow is mandatory because it keeps backend JSON off the browser contract without losing the ability to proxy validation from the HTML edge.

Merged invalid state should be a first-class case, for example when local validation finds field-format issues and a downstream API returns additional policy errors for other fields.

## 11. First-Pass Scope and Deferrals

The first authoring-surface pass should prove the form, field, and message model.

Ship in the first pass:

- normalized validation descriptors with an ASP.NET default adapter
- `HrzForm`
- `HrzField`
- `HrzLabel`
- `HrzValidationMessage`
- `HrzValidationSummary`
- `HrzInput`
- `HrzTextArea`
- `HrzCheckbox`
- MVC and Minimal API invalid-rerender helpers
- generic `data-val-*` plus live-coordination browser harness

`HrzCheckbox` is the only special-case control allowed into first pass, and only with the explicit transport semantics defined earlier in this document.

Defer:

- `HrzSelect`
- `HrzInputNumber`
- radio groups
- file inputs
- date and time controls
- large specialized control families
- any browser-library lock-in as a framework guarantee

The goal of the first pass is to prove the authoring model, not to solve every input quirk.

## 12. Migration Plan

### Phase A: Normalize metadata internally

Introduce the internal HyperRazor validation descriptor model and populate it from the current DataAnnotations and ASP.NET pipeline first.

Goal:

- keep the public authoring surface stable while leaving room for alternate validator sources later

### Phase B: Build the authoring surface on top of the current runtime

Add `HrzForm`, `HrzField`, the message primitives, and the first-pass controls on top of the existing submit and live runtime.

Goal:

- prove the new DX without rewriting the transport engine first

### Phase C: Add first-class MVC and Minimal API helpers

Add the obvious invalid-rerender and valid-rerender helpers so application code stops hand-plumbing validation state.

Goal:

- remove manual `SetSubmitValidationState(...)` style controller code

### Phase D: Replace the bespoke client harness

Replace the current demo-specific script and rule names with a generic `data-val-*` based local harness plus HyperRazor live-validation coordination.

Goal:

- remove field-specific JavaScript and align the browser metadata contract with ASP.NET conventions

### Phase E: Deprecate low-level authoring as the default path

Mark the current attribute-heavy examples and helper-first docs as advanced or legacy.

Goal:

- make the new form and field surface the documented default

### Phase F: Add deferred controls after the model is proven

Expand the control family only after the metadata, runtime emission, and patch behavior are stable.

Goal:

- avoid locking in input-specific quirks before the core authoring model is validated

## 13. Explicit Rejections

The refactor should not do any of the following:

- make JSON validation a browser-facing primary contract
- force interactive Blazor just to get client validation
- make MVC metadata the literal internal source of truth
- imply classic `RemoteAttribute` JSON transport compatibility
- invent a second HyperRazor-only rule DSL for common rules
- keep field-specific client validation hooks
- require application code to wire server or client slots by hand
- hard-bind the framework to one browser validation library
- make Minimal API validation materially worse than MVC
- rerender the whole form during live typing

## 14. Open Questions For The Detailed API Pass

These should be answered in the detailed API spec with code snippets.

1. Should `FormName` always be explicit on `HrzForm`, or can it be inferred safely in some cases?
2. What is the exact public shape of the request-scoped `HrzForm` and `HrzField` context objects?
3. Which concrete descriptor type names should ship for the minimum descriptor model defined in this document?
4. Should live-validation semantics surface as custom attributes, adapter registration, or both?
5. When should standalone `HrzInput* For=...` controls be added, if at all?
6. What is the exact invalid-render helper shape for MVC and Minimal API when the caller already has a normalized or merged `HrzSubmitValidationState`?
7. Which nested and indexed field scenarios must be validated in the detailed API pass before the authoring model is considered complete?

## 15. Feedback Disposition

This final parent spec carries forward the earlier feedback already adopted in v2 and v3 and additionally reviews every major point in `docs/validation-framework-dx-refactor-spec-v3-feedback.md`.

1. Make `HrzField` the true owner of field context. Adopted.
   Child components now bind to ambient field context by default inside `HrzField`, with explicit `For` targeting kept as an escape hatch.
2. Move final DOM-id assumptions out of the descriptor layer. Adopted.
   The minimum descriptor shape no longer carries `HtmlIdPrefix`; form and field rendering context derive final ids from `FormName` plus field identity.
3. Resolve the `HrzInputText` naming smell. Adopted.
   The first-pass text-like input control is now `HrzInput`.
4. Define `Enhance` precisely. Adopted and finalized.
   `HrzForm` now defaults `Enhance` to `true`, and `Enhance="false"` is the opt-out to plain full-page submission.
5. Call out nested and indexed field identity explicitly. Adopted.
   The spec now makes this a required detailed API-pass validation area.
6. Reinforce normalized submit state as the render-time common denominator. Adopted.
   Helper overloads may accept transport-shaped inputs, but normalization to `HrzSubmitValidationState` happens before render-time authoring components consume the result.

## 16. Recommendation Summary

The right refactor is not "add a few wrappers around the current validation demo."

The right refactor is:

- preserve the runtime primitives
- replace the public authoring model
- normalize validation metadata internally
- make `HrzField` plus child controls the single primary authoring shape
- let `HrzField` own ambient field context so child components are repetition-free by default
- standardize on `data-val-*` for local browser metadata
- borrow remote-validation semantics without borrowing JSON transport assumptions
- make enhanced HTML submission the default form posture, with explicit opt-out
- keep the initial control family small
- define summary and helper behavior tightly enough to guide implementation
- make MVC and Minimal API feel like the same framework

That gets HyperRazor closer to native ASP.NET DX while staying true to SSR + HTMX and preserving every required validation path.

## 17. References

- ASP.NET Core Blazor forms overview: https://learn.microsoft.com/en-us/aspnet/core/blazor/forms/?view=aspnetcore-9.0
- ASP.NET Core Blazor forms binding: https://learn.microsoft.com/en-us/aspnet/core/blazor/forms/binding?view=aspnetcore-10.0
- Microsoft `RemoteAttribute` API: https://learn.microsoft.com/en-us/dotnet/api/microsoft.aspnetcore.mvc.remoteattribute?view=aspnetcore-10.0
- Microsoft `RemoteAttributeBase.AdditionalFields`: https://learn.microsoft.com/en-us/dotnet/api/microsoft.aspnetcore.mvc.remoteattributebase.additionalfields?view=aspnetcore-10.0
- `aspnet-client-validation` README: https://github.com/haacked/aspnet-client-validation
- Rizzy docs overview: https://jalexsocial.github.io/rizzy.docs/
