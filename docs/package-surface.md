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

## Explicit Follow-Up

The current validation/component ownership mismatch remains follow-up work after the package story is stable.

- No file moves out of `Rendering`
- No namespace renames
- No attempt to resolve the `HyperRazor.Components`-from-`Rendering` mismatch in this phase

The validation component files currently live under `src/HyperRazor.Rendering/Validation/Components`, even though they publish `HyperRazor.Components` types. Moving them into `src/HyperRazor.Components` would currently introduce a project-cycle problem because `HyperRazor.Rendering` already depends on `HyperRazor.Components`.
