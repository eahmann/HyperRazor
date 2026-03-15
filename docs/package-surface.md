# HyperRazor Package Surface

This file is the canonical package-story source and the decision record for Package Story Phase 1. Treat the classifications and wording here as the source for onboarding docs, NuGet readme text, and packable project descriptions.

## Decision

HyperRazor stays on the current two-tier public surface for this phase.

## Which package do I install?

- Full HyperRazor app: install `HyperRazor`.
- Typed HTMX only: install `HyperRazor.Htmx`.
- Advanced composition: install the lower-level packages directly only when you are intentionally composing on those layers.

## Package classification

Primary entry-point packages:

- `HyperRazor`: the default onboarding package for a full HyperRazor app
- `HyperRazor.Htmx`: the default onboarding package for typed HTMX support without the full HyperRazor rendering stack

Advanced but supported composition packages:

- `HyperRazor.Client`
- `HyperRazor.Components`
- `HyperRazor.Htmx.Core`
- `HyperRazor.Htmx.Components`
- `HyperRazor.Mvc`
- `HyperRazor.Rendering`

Internal-only projects:

- `HyperRazor.Demo.Api`
- `HyperRazor.Demo.Mvc`
- `tests/*`

## Rules For Docs And Packaging

- First-stop docs should present the `Which package do I install?` section before they explain advanced composition packages.
- `README.md`, `docs/quickstart.md`, `docs/adopting-hyperrazor.md`, `docs/nuget-readme.md`, and `docs/release-policy.md` should use the same primary/advanced/internal classification language.
- Happy-path examples should use only the primary packages.
- The primary packages should carry the common namespace imports needed for the happy path.
- Advanced packages remain supported and versioned, but they are composition building blocks, not the default onboarding story.
- Demo and test projects are not part of the shipped package surface.

## Advanced Validation Migration

Package Story Phase 2 resolves the old validation ownership mismatch without introducing a new package.

- `HyperRazor.Components` now owns the validation authoring surface.
- `HyperRazor.Components.Validation` is the shared validation contract namespace.
- `HyperRazor.Rendering` remains public for rendering primitives and validation implementations, but no longer owns the shared validation contract surface.
- `HyperRazor.Mvc` now carries a direct dependency on `HyperRazor.Components` because its public validation helpers expose `HyperRazor.Components.Validation` types.

Migration guidance:

```csharp
using HyperRazor.Components;
using HyperRazor.Components.Validation;
```

Replace old validation-type imports from `HyperRazor.Rendering` when a file only needs shared validation contracts such as `HrzValidationRootId`, `HrzFieldPath`, `HrzSubmitValidationState`, `IHrzFieldPathResolver`, or `IHrzLiveValidationPolicyResolver`.
