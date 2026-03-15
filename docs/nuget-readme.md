# HyperRazor

HyperRazor provides typed HTMX support for ASP.NET with server-rendered Razor components, MVC/Minimal API helpers, out-of-band swap orchestration, and HTMX-aware response utilities.

## Which package do I install?

- Full HyperRazor app: install `HyperRazor`.
- Typed HTMX only: install `HyperRazor.Htmx`.
- Advanced component composition: install `HyperRazor.Components` only when you are intentionally composing on that layer.

Primary entry-point packages:

- `HyperRazor`: the default onboarding package for a full HyperRazor app; it brings in `HyperRazor.Components` and `HyperRazor.Htmx` transitively
- `HyperRazor.Htmx`: the default onboarding package for typed HTMX support without the full HyperRazor rendering stack

Advanced but supported composition package:

- `HyperRazor.Components`

Internal-only projects:

- `samples/HyperRazor.Demo.Api`
- `samples/HyperRazor.Demo.Mvc`
- `tests/*`

The primary packages also provide the common HyperRazor namespace imports used by the first-stop setup path. `HyperRazor.Components` remains supported and versioned, but it is an advanced composition building block rather than the default onboarding path.

Advanced validation authoring stays in `HyperRazor.Components`, shared validation contracts import from `HyperRazor.Components.Validation`, and the `HyperRazor` package continues to expose the `HyperRazor.Mvc` and `HyperRazor.Rendering` namespaces for server/runtime APIs.

Docs:

- `docs/quickstart.md`
- `docs/adopting-hyperrazor.md`
- `docs/package-surface.md`
- `docs/release-policy.md`
