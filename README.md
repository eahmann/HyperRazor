# HyperRazor

Typed HTMX support for ASP.NET with server-rendered components and MVC endpoint helpers.

Docs:
- [Docs Index](docs/README.md)
- [Quickstart](docs/quickstart.md)
- [Adopting HyperRazor](docs/adopting-hyperrazor.md)
- [Release Policy](docs/release-policy.md)

Public package setups:
- `HyperRazor`: the golden-path package for a full HyperRazor app
- `HyperRazor.Htmx`: typed HTMX support for ASP.NET without opting into full HyperRazor rendering

Advanced composition packages:
- `HyperRazor.Client`
- `HyperRazor.Components`
- `HyperRazor.Htmx.Core`
- `HyperRazor.Htmx.Components`
- `HyperRazor.Mvc`
- `HyperRazor.Rendering`

Includes:
- request context/profile parsing for HTMX 2 and HTMX 4-style headers
- OOB swap queue + `RenderToString()` support
- first-party head queue/flush support (`IHrzHeadService` + `HrzHeadContent`)
- antiforgery token meta/input helpers and client request wiring
- diagnostics middleware + operations-console demo app

Non-packable demo applications:
- `HyperRazor.Demo.Api`
- `HyperRazor.Demo.Mvc`
