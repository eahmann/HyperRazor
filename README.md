# HyperRazor

Typed HTMX support for ASP.NET with server-rendered components and MVC endpoint helpers.

Docs:
- [Quickstart](docs/quickstart.md)
- [Package Surface](docs/package-surface.md)
- [Release Policy](docs/release-policy.md)

Primary entry-point packages:
- `HyperRazor`: the golden-path package for a full HyperRazor app
- `HyperRazor.Htmx`: typed HTMX support for ASP.NET without opting into full HyperRazor rendering

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

Includes:
- request context/profile parsing for HTMX 2 and HTMX 4-style headers
- OOB swap queue + `RenderToString()` support
- first-party head queue/flush support (`IHrzHeadService` + `HrzHeadContent`)
- antiforgery token meta/input helpers and client request wiring
- diagnostics middleware + operations-console demo app
