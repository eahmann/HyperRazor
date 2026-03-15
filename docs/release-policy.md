# HyperRazor Release Policy

This is the release and versioning policy for the repository as Phase 4 wraps.

## Which package do I install?

- Full HyperRazor app: install `HyperRazor`.
- Typed HTMX only: install `HyperRazor.Htmx`.
- Advanced composition: install the lower-level packages directly only when you are intentionally composing on those layers.

## Package scope

Primary entry-point packages:

- `HyperRazor`: the default onboarding package for a full HyperRazor app (`src/HyperRazor`)
- `HyperRazor.Htmx`: the default onboarding package for typed HTMX support without the full HyperRazor rendering stack (`src/HyperRazor.Htmx`)

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

## Versioning

Use one shared semantic version across all published HyperRazor packages.

- `MAJOR`: breaking API changes, breaking behavior changes, or default flips that require consumer intervention
- `MINOR`: additive public API, new features, or opt-in behavior that remains backward compatible
- `PATCH`: bug fixes, diagnostics, test-only changes, doc fixes, and internal refactors with no intended public surface change

Use prerelease suffixes for staged releases:

- `-preview.N` for early public validation
- `-rc.N` for release candidates

## Packaging guidance

Keep version stamping centralized at pack/publish time so package versions cannot drift between projects.

The primary packages own the onboarding story. The advanced packages remain versioned and published because the current architecture ships multiple assemblies, but they are advanced composition building blocks rather than the default docs path.

Example:

```bash
dotnet pack HyperRazor.slnx -c Release /p:Version=1.0.0-preview.1
```

Because demo and test projects are non-packable, the solution-level pack command remains safe once version metadata is provided by CI or the release command.

## CI gates

HyperRazor uses a fast/slow split.

- `CI` workflow runs the fast suites on pushes and pull requests:
  - `HyperRazor.Htmx.Tests`
  - `HyperRazor.Htmx.Core.Tests`
  - `HyperRazor.Rendering.Tests`
  - `HyperRazor.Demo.Mvc.Tests`
- `E2E` workflow runs the Playwright suite outside pull requests:
  - manual dispatch
  - pushes to protected branches
  - nightly schedule

The release bar is:

1. fast CI green
2. latest E2E run green
3. publish only from an intentional version/tag decision

## Branch and tag policy

Use annotated tags in `vX.Y.Z` or `vX.Y.Z-suffix.N` form for release points.

Examples:

- `v1.0.0`
- `v1.1.0`
- `v1.2.0-preview.1`
- `v1.2.0-rc.1`

Publish from the tagged commit, not from an arbitrary working branch head.
