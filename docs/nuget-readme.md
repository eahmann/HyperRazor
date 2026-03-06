# HyperRazor

HyperRazor provides typed HTMX support for ASP.NET with server-rendered Razor components, MVC/Minimal API helpers, out-of-band swap orchestration, and HTMX-aware response utilities.

Core areas:

- `HyperRazor.Htmx`: request/response primitives and header constants
- `HyperRazor.Htmx.AspNetCore`: ASP.NET Core integrations, diagnostics, and HTMX helpers
- `HyperRazor.Client`: browser assets for antiforgery, HTMX config, and client-side integration glue
- `HyperRazor.Components`: render-time primitives such as head and swap content hosts
- `HyperRazor.Mvc`: MVC controller base classes and result helpers
- `HyperRazor.Hosting`: app startup helpers
- `HyperRazor.Rendering`: server-side rendering pipeline and layout-boundary handling

Docs:

- `docs/quickstart.md`
- `docs/release-policy.md`
