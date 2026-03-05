# HyperRazor

Typed HTMX support for ASP.NET with server-rendered components and MVC endpoint helpers.

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
- first-party head queue/flush support (`IHrxHeadService` + `HrxHeadContent`)
- antiforgery token meta/input helpers and client request wiring
- diagnostics middleware + demo app with feature-isolated pages
- `HyperRazor.Htmx.Components`
- `HyperRazor.Demo.Api`
- `HyperRazor.Demo.Mvc`
