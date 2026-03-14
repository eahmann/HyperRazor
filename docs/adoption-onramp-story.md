# Adoption Onramp Story

## User Story

As a developer evaluating HyperRazor against Rizzy, I want the first working app to feel explicit but small, so I can understand the framework from `Program.cs`, controller actions, and Minimal API routes without needing demo-specific composition helpers or early knowledge of advanced features.

## Problem Statement

HyperRazor's current core APIs are already close to a good onboarding story, but the public story still feels heavier than it should.

Today:

- The canonical setup is split between the real public APIs and demo-specific composition code such as [`DemoServiceCollectionExtensions.cs`](../src/HyperRazor.Demo.Mvc/Composition/DemoServiceCollectionExtensions.cs).
- The demo app's startup shape advertises advanced concerns early, including layout-boundary promotion, custom response handling, replay strategy wiring, and validation infrastructure.
- MVC and Minimal APIs do not present the same first-use vocabulary.
- Minimal API examples still teach `HttpContext`-based lambdas before offering a more direct page/fragment mapping surface.

The result is not that HyperRazor is objectively hard to use. The result is that it can look harder to adopt than Rizzy, especially for the first hour of evaluation.

## Goals

- Keep `AddHyperRazor()` and `AddHtmx()` both required and explicit in the startup contract.
- Avoid hidden service registration or "optional until you need customization" behavior.
- Make the no-options overloads of `AddHyperRazor()` and `AddHtmx()` sufficient for a normal page/fragment app.
- Remove the need for demo-specific service collection extensions from the public onboarding story.
- Reduce the learning gap between MVC controllers and Minimal APIs.
- Keep advanced capabilities available, but move them out of the default adoption path.
- Fail clearly when required registration is missing.

## Non-Goals

- Making `AddHtmx()` optional for the full `HyperRazor` package path.
- Removing advanced configuration points such as layout-boundary promotion, SSE replay, validation policies, or response handling rules.
- Replacing `HrzResults` or `HttpContext.Htmx()` as advanced escape hatches.
- Rewriting the demo app to avoid extension methods internally when they still help demo organization.

## Current Friction Points

- [`DemoServiceCollectionExtensions.cs`](../src/HyperRazor.Demo.Mvc/Composition/DemoServiceCollectionExtensions.cs) compresses a lot of demo-only behavior into something that can be mistaken for the canonical app setup.
- [`Program.cs`](../src/HyperRazor.Demo.Mvc/Program.cs) and the demo endpoint composition show advanced capabilities early, which is good for showcase value but bad for first-impression simplicity.
- [`HrController.cs`](../src/HyperRazor.Mvc/HrController.cs) and [`HrzResults.cs`](../src/HyperRazor.Mvc/HrzResults.cs) should teach the same page/partial vocabulary.
- [`quickstart.md`](quickstart.md) is relatively clean, but the total repository story still points new users toward a more complex mental model than the quickstart intends.

## Proposed Story

### 1. Keep the startup contract explicit

The baseline HyperRazor setup should stay explicit and stable:

```csharp
builder.Services.AddControllersWithViews();
builder.Services.AddAntiforgery();
builder.Services.AddHyperRazor();
builder.Services.AddHtmx();

var app = builder.Build();

app.UseHyperRazor();
```

This is the contract. It should not rely on hidden registration, conditional add-back behavior, or a wrapper extension just to feel tidy.

### 2. Make the no-options path fully usable

`AddHyperRazor()` and `AddHtmx()` should both have production-credible zero-argument defaults for the normal page/fragment path.

That means a user should not need custom options in order to:

- render full pages
- render fragments
- use HTMX request/response helpers
- use antiforgery defaults
- use the standard client assets and head-support behavior for the full-package path

Advanced concerns should remain configurable, but they should not be prerequisites for the first working app.

### 3. Align controller and Minimal API vocabulary

HyperRazor should present the same mental model regardless of endpoint style.

For controllers:

- use `Page<TComponent>()`, `Fragment<TComponent>()`, and `RootSwap<TComponent>()` on `HrController`
- remove `View<TComponent>()` and `PartialView<TComponent>()` from the first-use controller surface

For Minimal APIs:

- add route-builder extensions such as `MapPage<TComponent>(pattern)` and `MapFragment<TComponent>(pattern)`
- keep `HrzResults.Page<TComponent>()`, `HrzResults.Fragment<TComponent>()`, and `HrzResults.RootSwap<TComponent>()` for advanced cases, custom response configuration, and direct composition

The goal is to teach "pages" and "fragments" everywhere, then let deeper APIs exist underneath that surface.

### 4. Demote advanced features out of the onboarding path

The first-time adoption story should not lead with:

- layout-boundary promotion
- custom HTMX response handling rules
- SSE replay strategy overrides
- live validation policies
- demo-specific infrastructure services

Those features should move to clearly labeled advanced docs and showcase samples.

### 5. Keep demo composition internal to the demo

The demo app may still use composition extensions, route-group helpers, and sample infrastructure to stay maintainable.

What should change is the framing:

- demo composition should be presented as demo organization, not framework-required ceremony
- the public quickstart should not depend on `DemoServiceCollectionExtensions`
- the public minimal sample should not depend on `DemoPageEndpoints`

### 6. Validate missing setup early

If an app calls `UseHyperRazor()` or attempts to render a HyperRazor page without both service registrations in place, the failure should be direct and actionable.

The error should tell the developer exactly what is missing:

- `AddHyperRazor()` is required
- `AddHtmx()` is required

This reduces confusion without introducing magic.

## Design Notes

- Internally, `AddHtmx()` should move toward an options-based registration model so it remains the single explicit HTMX configuration seam and does not become sensitive to registration order.
- This is not a proposal to make HTMX registration implicit. It is a proposal to make the explicit two-call contract reliable and easy to customize.
- Route-builder extensions live in `HyperRazor.Mvc` alongside `HrController` and `HrzResults` so the page/partial endpoint surface stays together.
- `HrzResults` and `HttpContext.Htmx()` should remain the advanced surface area for users who need lower-level control.

## Acceptance Criteria

- A basic HyperRazor app can be shown with `AddHyperRazor()`, `AddHtmx()`, `UseHyperRazor()`, and standard ASP.NET registration, without a custom app-specific service extension.
- The canonical docs clearly distinguish the onboarding path from the showcase/advanced path.
- MVC controllers can use `Page<TComponent>()` and `Partial<TComponent>()` with the same meaning as Minimal API examples.
- Minimal APIs can map page and fragment routes through direct endpoint-builder extensions rather than always requiring `HttpContext` lambdas.
- Zero-argument `AddHyperRazor()` and `AddHtmx()` are sufficient for a normal page/fragment app.
- Missing required registration fails with a clear, direct message.
- The demo app may keep its internal composition helpers, but those helpers are no longer presented as required for app startup.

## Suggested Delivery Order

1. Clarify and validate the explicit two-call startup contract.
2. Replace controller `View`/`PartialView` usage with `Page`/`Partial` and add Minimal API route-builder extensions.
3. Rewrite the quickstart and adoption docs around the smaller public story.
4. Reframe demo-specific composition and advanced feature docs as second-stage material.

## Open Questions

- Which advanced features, if any, still deserve mention in the quickstart as "available later" without showing configuration details?
