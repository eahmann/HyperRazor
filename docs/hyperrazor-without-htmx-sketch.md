# HyperRazor Without HTMX Sketch

## Summary

Yes, there is a plausible user shape that wants HyperRazor-style server-rendered components without the HTMX layer.

Examples:

- full-page server rendering only
- standard form posts and redirects
- plain links and full navigations
- no `hx-*` attributes
- no HTMX client script, headers, or response helpers

That shape is not supported by today's full `HyperRazor` package. The current architecture is HTMX-coupled by design.

The clean way to support a no-HTMX scenario is not to make the current `HyperRazor` package ambiguous. The cleaner direction is:

- keep `HyperRazor` as the HTMX-first package
- introduce an SSR-only layer/package underneath it
- let `HyperRazor` compose that SSR layer with `HyperRazor.Htmx`

## Current Coupling Points

Today the full package is tied to HTMX in several places:

- [`src/HyperRazor/HyperRazor.csproj`](../src/HyperRazor/HyperRazor.csproj) references `HyperRazor.Htmx`
- [`src/HyperRazor.Mvc/HyperRazor.Mvc.csproj`](../src/HyperRazor.Mvc/HyperRazor.Mvc.csproj) also references `HyperRazor.Htmx`
- [`src/HyperRazor.Rendering/HrzComponentViewService.cs`](../src/HyperRazor.Rendering/HrzComponentViewService.cs) reads HTMX request state on every render path
- [`src/HyperRazor.Components/Layouts/HrzAppLayout.razor`](../src/HyperRazor.Components/Layouts/HrzAppLayout.razor) injects `HtmxConfig` and emits HTMX client assets
- [`src/HyperRazor.Htmx/HyperRazorHtmxHttpContextExtensions.cs`](../src/HyperRazor.Htmx/HyperRazorHtmxHttpContextExtensions.cs) is part of normal request interpretation
- [`src/HyperRazor/HyperRazorApplicationBuilderExtensions.cs`](../src/HyperRazor/HyperRazorApplicationBuilderExtensions.cs) assumes HTMX-aware middleware is part of the default pipeline

The result is that "not using HTMX interactions" is possible, but "not carrying HTMX infrastructure" is not.

## Product Shapes

There are three realistic directions.

### Option 1: Keep HyperRazor HTMX-first

This is the current product model.

- `HyperRazor` continues to require `AddHyperRazor()` and `AddHtmx()`
- every full-package app carries HTMX config and client assets
- users who do not actively use HTMX still pay the conceptual/runtime packaging cost

This is the simplest model, but it means "HyperRazor without HTMX" remains unsupported.

### Option 2: Make AddHtmx optional inside the current package

This would allow:

```csharp
builder.Services.AddHyperRazor();
```

without:

```csharp
builder.Services.AddHtmx();
```

This looks attractive, but it is the weakest design.

- it blurs the current explicit contract
- it makes behavior depend on hidden fallback defaults
- it complicates docs because the same package now has two operating modes
- it increases branching inside core rendering/layout code

This is not the recommended path.

### Option 3: Introduce an SSR-only layer/package

This is the recommended direction if no-HTMX support matters.

Suggested package story:

- `HyperRazor`: convenience package for SSR + HTMX
- `HyperRazor.Server` or `HyperRazor.Ssr`: server-rendered pages/partials without HTMX
- `HyperRazor.Htmx`: typed HTMX support and HTMX integration

Under that model:

- `HyperRazor.Server` exposes `AddHyperRazor()`, `UseHyperRazor()`, `HrController`, `HrzResults`, `MapPage`, and `MapPartial`
- `HyperRazor.Htmx` adds HTMX request parsing, response helpers, client config, assets, and HTMX-aware middleware behavior
- `HyperRazor` becomes the composition package that references both

This preserves the current golden path while creating a clean non-HTMX story.

## Recommended Architecture

If this work is pursued, the internal architecture should be split into neutral server-rendering concerns and HTMX-specific concerns.

### 1. Introduce a transport-neutral request model

`HrzComponentViewService` should not read `HtmxRequest` directly.

Instead, introduce a neutral request abstraction, for example:

- `IHrzRequestContext`
- `IHrzRequestContextAccessor`
- `IHrzClientCapabilities`

That abstraction should answer framework-level questions such as:

- is this request asking for a full page or a fragment
- is this a history restoration request
- does the client support OOB/head merging
- should response branching add vary headers

HTMX can be one implementation of that abstraction. An SSR-only implementation can always return:

- full-page by default for `Page<TComponent>()`
- fragment-only for `Partial<TComponent>()`
- no client-side enhancement capabilities

### 2. Split response-writing from rendering

Today `HrzResults` mixes page/partial rendering with HTMX response configuration seams.

That should become:

- transport-neutral rendering in the core server package
- HTMX response helpers layered on top

Concretely:

- keep `HrzResults.Page<TComponent>()` and `HrzResults.Partial<TComponent>()` in the server layer
- move HTMX-specific response configuration overloads or helper types behind the HTMX package

If direct overload removal is too disruptive, the HTMX-aware overloads can remain in the composition package while the SSR-only package exposes the simpler forms.

### 3. Remove mandatory HtmxConfig from the root layout

The current app layout assumes HTMX assets and config are always present.

That should be reworked into:

- core shell assets/options that apply to all HyperRazor apps
- optional HTMX asset/config emission contributed only when the HTMX package is present

Practical effect:

- SSR-only apps do not emit `htmx-config`
- SSR-only apps do not load `htmx`, `head-support`, or HTMX SSE scripts
- SSR-only apps can still render pages and forms normally

### 4. Isolate HTMX-aware middleware

`UseHyperRazor()` currently assumes HTMX-aware diagnostics and `Vary` behavior.

A cleaner split is:

- `UseHyperRazor()` in the SSR-only layer enables only server-rendering requirements
- `UseHyperRazorHtmx()` or equivalent adds HTMX-aware diagnostics and branching behavior
- the composed `HyperRazor` package can keep a convenience `UseHyperRazor()` that calls both

This avoids forcing HTMX middleware semantics into apps that do not want HTMX.

### 5. Track feature capability explicitly

Some HyperRazor features are intrinsically HTMX-oriented:

- response header helpers and triggers
- OOB swap workflows
- head merge behavior on fragment responses
- layout-boundary promotion tied to HTMX navigation semantics

Those should either:

- live only in the HTMX package
- or degrade cleanly behind capability checks

The better default is to treat them as HTMX-package features, not server-core features.

## Expected SSR-Only Behavior

If a user installs the SSR-only package and not `HyperRazor.Htmx`, the expected behavior should be:

- `Page<TComponent>()` renders full pages
- `Partial<TComponent>()` renders fragment HTML
- standard forms and redirects work
- antiforgery helpers still work for normal MVC form posts
- no HTMX request parsing is required
- no HTMX scripts or config meta tags are emitted
- no HTMX `Vary` headers are added
- HTMX-specific helpers are unavailable by design

That is a coherent framework mode.

## Migration Strategy

The least disruptive sequence is:

1. Introduce transport-neutral internal abstractions without changing the public package story.
2. Move `HrzComponentViewService` and layout composition off direct `HtmxConfig` assumptions.
3. Isolate HTMX middleware and HTMX response helpers behind package-level registrations.
4. Add the SSR-only public package.
5. Keep `HyperRazor` as the composed package for today's users.

This preserves backward compatibility for existing HTMX-first adopters.

## Recommendation

If the goal is only to improve onboarding, do not do this yet. The current HTMX-first package story is simpler.

If the goal is to support a genuinely different product shape, pursue the SSR-only layer/package approach.

The key point is: do not make the existing `HyperRazor` package half-optional around HTMX. That creates a muddled contract. A separate server-rendering layer is cleaner technically and clearer publicly.
