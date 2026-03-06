# HyperRazor

Typed HTMX support for ASP.NET with server-rendered components and MVC endpoint helpers.

Docs:
- [Quickstart](/home/eric/repos/HyperRazor/docs/quickstart.md)
- [Release Policy](/home/eric/repos/HyperRazor/docs/release-policy.md)

Current stack:
- `HyperRazor.Htmx`
- `HyperRazor.Htmx.AspNetCore`
- `HyperRazor.Client`
- `HyperRazor.Components`
- `HyperRazor.Mvc`
- `HyperRazor.Hosting`
- `HyperRazor.Rendering`
- request context/profile parsing for HTMX 2 and HTMX 4-style headers
- OOB swap queue + `RenderToString()` support
- first-party head queue/flush support (`IHrzHeadService` + `HrzHeadContent`)
- antiforgery token meta/input helpers and client request wiring
- diagnostics middleware + operations-console demo app
- `HyperRazor.Htmx.Components`
- `HyperRazor.Demo.Api`
- `HyperRazor.Demo.Mvc`
