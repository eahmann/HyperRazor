# HyperRazor

HyperRazor provides typed HTMX support for ASP.NET with server-rendered Razor components, MVC/Minimal API helpers, out-of-band swap orchestration, and HTMX-aware response utilities.

Primary entry-point packages:

- `HyperRazor`: the full framework path
- `HyperRazor.Htmx`: typed HTMX support for ASP.NET without the full HyperRazor rendering stack

Advanced but supported composition packages:

- `HyperRazor.Client`
- `HyperRazor.Components`
- `HyperRazor.Htmx.Core`
- `HyperRazor.Htmx.Components`
- `HyperRazor.Mvc`
- `HyperRazor.Rendering`

The primary packages also provide the common HyperRazor namespace imports used by the first-stop setup path. Reference the lower-level packages directly only when you are intentionally composing on those layers.

Docs:

- `docs/quickstart.md`
- `docs/package-surface.md`
- `docs/release-policy.md`
