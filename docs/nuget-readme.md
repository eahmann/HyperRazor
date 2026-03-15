# HyperRazor

HyperRazor provides typed HTMX support for ASP.NET with server-rendered Razor components, MVC/Minimal API helpers, out-of-band swap orchestration, and HTMX-aware response utilities.

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

The primary packages also provide the common HyperRazor namespace imports used by the first-stop setup path. The lower-level packages remain supported and versioned, but they are advanced composition building blocks rather than the default onboarding path.

Docs:

- `docs/quickstart.md`
- `docs/adopting-hyperrazor.md`
- `docs/package-surface.md`
- `docs/release-policy.md`
