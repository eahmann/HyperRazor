# HyperRazor

Typed HTMX support for ASP.NET with server-rendered components and MVC endpoint helpers.

Docs:
- [Quickstart](/home/eric/repos/HyperRazor/docs/quickstart.md)
- [Release Policy](/home/eric/repos/HyperRazor/docs/release-policy.md)

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
- request context/profile parsing for HTMX 2 and HTMX 4-style headers
- OOB swap queue + `RenderToString()` support
- first-party head queue/flush support (`IHrzHeadService` + `HrzHeadContent`)
- antiforgery token meta/input helpers and client request wiring
- diagnostics middleware + operations-console demo app
- `HyperRazor.Demo.Api`
- `HyperRazor.Demo.Mvc`
