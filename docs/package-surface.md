# HyperRazor Package Surface

This note defines the intended package story for the repository so docs, package metadata, and namespaces can be evaluated against the same model.

## Decision

HyperRazor uses a two-tier public surface:

- Primary entry-point packages:
  - `HyperRazor`: the default package for a full HyperRazor app
  - `HyperRazor.Htmx`: the default package for typed HTMX support without the full HyperRazor rendering stack
- Advanced but supported composition packages:
  - `HyperRazor.Client`
  - `HyperRazor.Components`
  - `HyperRazor.Htmx.Core`
  - `HyperRazor.Htmx.Components`
  - `HyperRazor.Mvc`
  - `HyperRazor.Rendering`
- Internal-only projects:
  - `HyperRazor.Demo.Api`
  - `HyperRazor.Demo.Mvc`
  - `tests/*`

## Rules For Docs And Packaging

- First-stop docs should start with `HyperRazor` or `HyperRazor.Htmx`, not the lower-level packages.
- The primary packages should carry the common namespace imports needed for the happy path.
- Advanced packages remain supported and versioned, but they are composition building blocks, not the default onboarding story.
- Demo and test projects are not part of the shipped package surface.

## Consequences

Because the current architecture ships multiple assemblies, the advanced packages still publish separately and share the same versioning policy. That does not make them the primary entry points.

This slice does not move the validation component files that currently live under `src/HyperRazor.Rendering/Validation/Components`. Those files publish `HyperRazor.Components` types, but moving them into `src/HyperRazor.Components` would currently introduce a project-cycle problem because `HyperRazor.Rendering` already depends on `HyperRazor.Components`.

That file-layout mismatch remains follow-up work. The immediate goal here is to make the package story and onboarding story explicit and consistent.
