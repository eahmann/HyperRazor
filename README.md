# HyperRazor

Typed HTMX support for ASP.NET with server-rendered components and MVC endpoint helpers.

## Which package do I install?

- Full HyperRazor app: install `HyperRazor`.
- Typed HTMX only: install `HyperRazor.Htmx`.
- Advanced component composition: install `HyperRazor.Components` only when you are intentionally composing on that layer.

Primary entry-point packages:

- `HyperRazor`: the default onboarding package for a full HyperRazor app; it brings in `HyperRazor.Components` and `HyperRazor.Htmx` transitively
- `HyperRazor.Htmx`: the default onboarding package for typed HTMX support without the full HyperRazor rendering stack

Advanced but supported composition package:

- `HyperRazor.Components`
  This package includes the stock validation components plus the advanced validation builder/scope API (`IHrzForms`, `HrzFormScope`, `HrzFieldScope`).

Internal-only projects:

- `samples/HyperRazor.Demo.Api`
- `samples/HyperRazor.Demo.Mvc`
- `tests/*`

Advanced validation authoring stays in `HyperRazor.Components`. Use `HrzForm` / `HrzField` / `HrzInput*` for the default path, or `IHrzForms`, `HrzFormScope`, and `HrzFieldScope` for custom markup and custom input components. Shared validation contracts import from `HyperRazor.Components.Validation`, and the `HyperRazor` package continues to expose the `HyperRazor.Mvc` and `HyperRazor.Rendering` namespaces for server/runtime APIs.

Docs:
- [Docs Index](docs/README.md)
- [Quickstart](docs/quickstart.md)
- [Adopting HyperRazor](docs/adopting-hyperrazor.md)
- [Package Surface](docs/package-surface.md)
- [Release Policy](docs/release-policy.md)

Includes:
- request context/profile parsing for HTMX 2 and HTMX 4-style headers
- OOB swap queue + `RenderToString()` support
- first-party head queue/flush support (`IHrzHeadService` + `HrzHeadContent`)
- antiforgery token meta/input helpers and client request wiring
- diagnostics middleware + operations-console demo app
