# HyperRazor Package Surface

This file is the canonical package-story source and decision record for the current public package layout. Treat the classifications and wording here as the source for onboarding docs, NuGet readme text, and packable project descriptions.

## Decision

HyperRazor ships three library projects under `src` and keeps the current two-package primary onboarding story.

## Which package do I install?

- Full HyperRazor app: install `HyperRazor`.
- Typed HTMX only: install `HyperRazor.Htmx`.
- Advanced component composition: install `HyperRazor.Components` only when you are intentionally composing on that layer.

## Package classification

Primary entry-point packages:

- `HyperRazor`: the default onboarding package for a full HyperRazor app; it brings in `HyperRazor.Components` and `HyperRazor.Htmx` transitively
- `HyperRazor.Htmx`: the default onboarding package for typed HTMX support without the full HyperRazor rendering stack

Advanced but supported composition package:

- `HyperRazor.Components`

Internal-only projects:

- `samples/HyperRazor.Demo.Api`
- `samples/HyperRazor.Demo.Mvc`
- `tests/*`

## Rules For Docs And Packaging

- First-stop docs should present the `Which package do I install?` section before they explain advanced composition packages.
- `README.md`, `docs/quickstart.md`, `docs/adopting-hyperrazor.md`, `docs/nuget-readme.md`, and `docs/release-policy.md` should use the same primary/advanced/internal classification language.
- Happy-path examples should use only the primary packages.
- The primary packages should carry the common namespace imports needed for the happy path.
- `HyperRazor.Components` remains supported and versioned, but it is a composition building block, not the default onboarding story.
- Demo and test projects are not part of the shipped package surface.

## Source Project Layout

- `src/HyperRazor` owns the full-stack onboarding package and now contains the runtime, `HyperRazor.Mvc`, and `HyperRazor.Rendering` source.
- `src/HyperRazor.Components` owns the component authoring surface, validation contracts/authoring, and client assets.
- `src/HyperRazor.Htmx` owns the HTMX primitives, services, and HTMX-related components.

## Migration

Package Story Phase 3 retires the old advanced package IDs without introducing a new package:

- `HyperRazor.Client` -> `HyperRazor.Components`
- `HyperRazor.Mvc` -> `HyperRazor`
- `HyperRazor.Rendering` -> `HyperRazor`
- `HyperRazor.Htmx.Core` -> `HyperRazor.Htmx`
- `HyperRazor.Htmx.Components` -> `HyperRazor.Htmx`

Validation ownership from Phase 2 remains in place:

- `HyperRazor.Components` owns the validation authoring surface.
- `HyperRazor.Components.Validation` is the shared validation contract namespace.
- The implementation APIs remain public under the `HyperRazor.Rendering` namespace, but they now ship from the `HyperRazor` package.

Migration guidance:

```csharp
using HyperRazor.Components;
using HyperRazor.Components.Validation;
```

Replace old validation-type imports from `HyperRazor.Rendering` when a file only needs shared validation contracts such as `HrzValidationRootId`, `HrzFieldPath`, `HrzSubmitValidationState`, `IHrzFieldPathResolver`, or `IHrzLiveValidationPolicyResolver`. Switch retired package references to the package mapping above.
