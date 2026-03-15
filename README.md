# HyperRazor

Typed HTMX support for ASP.NET with server-rendered components and MVC endpoint helpers.

## Which package do I install?

- Full HyperRazor app: install `HyperRazor`.
- Typed HTMX only: install `HyperRazor.Htmx`.
- Advanced composition: install the lower-level packages directly only when you are intentionally composing on those layers.

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

Docs:
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
