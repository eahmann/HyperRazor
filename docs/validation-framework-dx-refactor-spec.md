# HyperRazor Validation DX Refactor Spec

**Date:** 2026-03-09  
**Status:** Draft proposal  
**Audience:** framework design / refactor planning

## 1. Problem Statement

HyperRazor already has most of the validation runtime primitives it needs:

- submit-time validation state
- attempted-value replay
- MVC `ModelState` mapping
- Minimal API bind-and-validate helpers
- backend `ValidationProblemDetails` mapping
- server live-validation patches

The weak point is not the transport model. The weak point is developer experience.

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

HyperRazor validation should feel as close as possible to native ASP.NET:

- validation rules are authored once on the model or validator
- field components bind by expression, not by string path
- the framework emits names, ids, validation metadata, HTMX hooks, and message regions automatically
- MVC and Minimal API both converge on the same invalid-rerender pipeline
- backend API validation remains a server-to-server concern and still rerenders HTML
- local client validation and server live validation compose instead of competing

The desired public feel is:

1. model-first like ASP.NET validation attributes and validators
2. expression-first like `asp-for`, `ValidationMessage`, and `EditForm`
3. HTML-first like HyperRazor and Rizzy
4. transport-agnostic for MVC, Minimal API, and backend-proxy flows

## 3. Non-Negotiable Path Matrix

The refactor must preserve all supported validation paths.

| Path | Required | Browser contract | Notes |
| --- | --- | --- | --- |
| MVC local validation | yes | full HTML or fragment HTML | closest to native ASP.NET controller flow |
| MVC -> backend API -> HTML rerender | yes | full HTML or fragment HTML | backend JSON remains server-to-server only |
| Minimal API local validation | yes | full HTML or fragment HTML | no second-class runtime |
| Minimal API -> backend API -> HTML rerender | yes | full HTML or fragment HTML | same HTML contract as MVC |
| local client validation | yes | DOM updates only | immediate feedback, no network |
| server live validation | yes | targeted field/summary HTML patches | partial patch only, no whole-form replacement |

This proposal does not reduce the path matrix. It standardizes it.

## 4. Core Position

The current greenfield spec is correct about one major thing: the browser-facing contract should stay HTML-first.

However, the public authoring posture should change.

Current posture:

- plain `<form>` is primary
- low-level helpers are primary
- validation authoring is explicit and manual

Proposed posture:

- a framework form/field authoring layer is primary
- plain HTML remains an escape hatch
- low-level transport types remain internal or advanced usage

In short:

`HrzSubmitValidationState` and friends should remain the engine.
They should stop being the app authoring model.

## 5. DX North Star

### 5.1 What “native ASP.NET-like” means

A consumer should be able to:

- put validation rules on a model property or validator
- bind a field with an expression
- drop in a validation message component
- get matching `name`, `id`, and validation metadata automatically
- rerender invalid HTML without manually rebuilding the validation surface

The framework should own the repetitive parts:

- field path generation
- attempted-value replay
- accessibility attributes
- client validation metadata
- live-validation targeting
- server message rendering

### 5.2 What “Rizzy-like” means in practice

Rizzy’s value is not that it invents a new validation system.
Its value is that it keeps the app authoring story close to ASP.NET MVC and Razor Components while still embracing HTMX fragment rendering.

HyperRazor should do the same:

- keep controller and route-handler flows familiar
- keep form authoring expression-based
- keep HTML fragments as the response shape
- make HTMX integration framework-owned instead of app-owned

## 6. Architectural Recommendation

The replacement stack should have four layers.

### 6.1 Layer 1: Validation metadata

This becomes the source of truth.

Inputs:

- `ValidationAttribute`
- `IValidatableObject`
- framework validator adapters
- optional FluentValidation-style adapters later
- optional remote-validation metadata

Responsibilities:

- define server validation rules
- define client-emittable validation metadata
- define cross-field dependencies
- define remote/live-validation metadata

Design rule:

Do not invent a second HyperRazor-only rule DSL for basic validation.

For the default path, HyperRazor should reuse the ASP.NET validation ecosystem and its metadata shape, especially the unobtrusive `data-val-*` contract and `IClientModelValidator` style adapters.

### 6.2 Layer 2: Authoring surface

This is the main refactor target.

Add a framework-owned authoring surface with components similar in spirit to:

- `HrzForm`
- `HrzField`
- `HrzLabel`
- `HrzInputText`
- `HrzInputNumber`
- `HrzTextArea`
- `HrzSelect`
- `HrzCheckbox`
- `HrzValidationMessage`
- `HrzValidationSummary`

The critical rule is that fields bind by expression, not by string field name.

Example target shape:

```razor
<HrzForm Model="Invite" Action="/users/invite" Enhance>
    <HrzValidationSummary />

    <HrzField For="() => Invite.DisplayName">
        <HrzLabel />
        <HrzInputText />
        <HrzValidationMessage />
    </HrzField>

    <HrzField For="() => Invite.Email">
        <HrzLabel />
        <HrzInputText Type="email" />
        <HrzValidationMessage />
    </HrzField>
</HrzForm>
```

The framework, not application code, should determine:

- canonical field path
- `name`
- stable `id`
- attempted value
- invalid CSS class
- `aria-invalid`
- `aria-describedby`
- message region ids
- client validation metadata
- live-validation HTMX hooks

Plain HTML inputs should still be supported, but they should be the escape hatch for unusual cases, not the recommended golden path.

### 6.3 Layer 3: Request and render runtime

Keep the existing runtime split:

- full-root submit validation state
- targeted live-validation patch state

Recommended internal types to preserve:

- `HrzSubmitValidationState`
- `HrzLiveValidationPatch`
- `HrzFormPostState<TModel>`
- attempted-value primitives
- canonical field-path resolver

These types are still necessary for MVC, Minimal API, and backend proxy flows.
The change is that application authors should touch them rarely.

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

Recommendation:

- use a stable `FormName` concept publicly
- map it internally to `HrzValidationRootId`
- allow explicit override
- auto-generate only when stability is guaranteed

Why:

- ASP.NET and Blazor already lean on named forms
- multiple forms on one page are normal
- root identity must remain explicit somewhere for server rerender paths

### 7.2 Field binding

Every field component should accept a `For` expression.

That expression should drive:

- path resolution
- label text defaulting
- id generation
- validation message lookup
- client metadata emission
- dependency resolution

This is the single biggest DX gain.

### 7.3 Message components

Add first-class message primitives:

- `HrzValidationMessage For="..."`
- `HrzValidationSummary`

These should render both:

- submit-time server errors
- live-validation server errors
- client-side local errors

The implementation may still keep separate internal slots for client vs server ownership, but the public API should not force application code to manage that split manually.

### 7.4 Layout ownership

Do not make the framework own grid/layout markup.

Recommended split:

- framework owns field semantics
- app owns layout and styling

That preserves flexibility without leaving validation plumbing in app code.

## 8. Client Validation Strategy

### 8.1 Recommendation

Replace the current field-specific demo script with a generic client-validation harness that consumes standard validation metadata.

Preferred contract:

- standard `data-val-*` emission for local rules
- framework-specific attributes only for HTMX/live coordination

Why:

- it aligns with native ASP.NET MVC
- it avoids inventing rule names like `data-hrz-local-email`
- it allows custom validators to plug in through familiar server-side metadata
- it makes browser behavior more portable across MVC and component rendering

### 8.2 Default browser library

The best default direction is:

1. emit ASP.NET-style unobtrusive validation attributes
2. use a no-jQuery client adapter by default
3. let teams swap the client adapter later if needed

The practical reference point here is `aspnet-client-validation`:

- compatible with the `data-val-*` ecosystem
- no jQuery dependency
- supports custom providers
- supports async validation
- supports configurable events and debounce

That makes it a better fit than continuing the current bespoke field-specific script.

### 8.3 Client/server split

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
- cross-field rules that the browser cannot evaluate reliably

If local validation fails, server live validation should be blocked automatically.

## 9. Live Validation Strategy

### 9.1 Recommendation

Keep live validation as targeted HTML patching, not JSON-to-browser validation.

That part of the current design is correct and should remain.

### 9.2 Metadata source

Live validation should be declared from validation metadata, not inline per-input HTMX wiring.

The design should support:

- ASP.NET `RemoteAttribute` style semantics
- additional dependent fields
- remote endpoint selection
- debounce and trigger options

HyperRazor may need a custom attribute or adapter for SSR/HTMX-specific behavior, but it should feel like an extension of ASP.NET remote validation rather than a separate subsystem.

### 9.3 DOM behavior

Live validation must only patch:

- the affected field message region
- dependent field message regions when necessary
- the summary region when necessary

It must not rerender the whole form during typing.

## 10. MVC and Minimal API Shape

### 10.1 MVC

MVC should feel nearly native:

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

The important part is not the exact method name.
The important part is that invalid rerendering should be one obvious helper call, not manual validation-state plumbing.

### 10.2 Minimal API

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

But the public goal is clear:

Minimal API should not feel like a second validation framework.

### 10.3 Backend API proxy flows

Both MVC and Minimal API must continue supporting:

1. local bind and validation first
2. backend call only when local validation passes
3. backend `ValidationProblemDetails` mapped back into HTML
4. attempted values preserved

This flow is mandatory because it is the cleanest way to keep backend JSON off the browser contract without losing the ability to proxy validation from the HTML edge.

## 11. Internal Contracts to Keep

The following concepts remain correct and should survive the refactor:

- canonical field paths
- root-scoped validation
- attempted-value preservation
- full-root submit state vs targeted live patch split
- backend problem-details mapping
- HTML-first browser contract

The refactor is not a rejection of those ideas.
It is a rejection of exposing those ideas too directly in app markup.

## 12. Stack Replacement Recommendation

If the current stack can be replaced completely, the best target stack is:

1. ASP.NET-compatible validation metadata as the primary rule source
2. HyperRazor field/form components as the primary authoring surface
3. shared HyperRazor submit/live runtime as the transport layer
4. a generic client harness built on `data-val-*` plus HTMX coordination
5. MVC and Minimal API adapter helpers that feel symmetrical

In practical terms, replace:

- manual field-path authoring
- manual slot wiring
- the current low-level `hyperrazor.validation.js` harness
- per-controller validation-state glue

Retain or evolve:

- submit validation state
- live patch state
- attempted-value capture
- `ValidationProblemDetails` mapping
- field-path canonicalization

## 13. Migration Plan

### Phase A: authoring layer first

Build the new field/form components on top of the current runtime.

Goal:

- prove the new DX without rewriting the entire engine first

### Phase B: adapter simplification

Add first-class MVC and Minimal API invalid-rerender helpers.

Goal:

- remove manual `SetSubmitValidationState(...)` style controller code

### Phase C: metadata-driven client validation

Replace the demo-specific browser script with a generic `data-val-*` harness plus HTMX/live coordination.

Goal:

- remove field-specific JavaScript

### Phase D: deprecate low-level authoring

Mark the current attribute-heavy examples and helper-first docs as advanced or legacy.

Goal:

- make the new field/form surface the documented default

## 14. Explicit Rejections

The refactor should not do any of the following:

- make JSON validation a browser-facing primary contract
- force interactive Blazor just to get client validation
- invent a second HyperRazor-only rule DSL for common rules
- keep field-specific client validation hooks
- require application code to wire server/client slots by hand
- make Minimal API validation materially worse than MVC

## 15. Open Questions For The Next Spec Pass

These should be answered in the detailed API spec with code snippets.

1. Should the primary public root concept be `FormName`, `RootId`, or both?
2. Should the primary surface be `HrzField` + child controls, standalone `HrzInput* For=...`, or both?
3. How much of ASP.NET `IClientModelValidator` infrastructure can be reused directly inside component rendering?
4. Should remote/live validation use pure `RemoteAttribute`, a HyperRazor-specific derivative, or a separate adapter registration model?
5. How should file inputs and repeated-value controls surface attempted-value replay ergonomically?
6. What is the exact invalid-render helper shape for MVC and Minimal API?
7. How much compatibility do we want with FluentValidation client adapters in v1 of the refactor?

## 16. Recommendation Summary

The right refactor is not “add a few wrappers around the current validation demo.”

The right refactor is:

- preserve the runtime primitives
- replace the public authoring model
- standardize on ASP.NET-style validation metadata
- adopt a generic no-jQuery client harness
- make MVC and Minimal API feel like the same framework

That gets HyperRazor closest to native ASP.NET DX while still staying true to SSR + HTMX and still preserving every required validation path.

## 17. References

- ASP.NET Core Blazor forms overview: https://learn.microsoft.com/en-us/aspnet/core/blazor/forms/?view=aspnetcore-9.0
- ASP.NET Core Blazor forms binding: https://learn.microsoft.com/en-us/aspnet/core/blazor/forms/binding?view=aspnetcore-10.0
- Microsoft `RemoteAttribute` API: https://learn.microsoft.com/en-us/dotnet/api/microsoft.aspnetcore.mvc.remoteattribute?view=aspnetcore-10.0
- Microsoft `RemoteAttributeBase.AdditionalFields`: https://learn.microsoft.com/en-us/dotnet/api/microsoft.aspnetcore.mvc.remoteattributebase.additionalfields?view=aspnetcore-10.0
- `aspnet-client-validation` README: https://github.com/haacked/aspnet-client-validation
- Rizzy docs overview: https://jalexsocial.github.io/rizzy.docs/
