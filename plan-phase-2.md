# HyperRazor — Phase 2 Implementation Plan (Component Views + Layout Glue)
**Date:** 2026-03-04

> Phase 1 got HTMX "sorted out" (typed headers, config injection, middleware guardrails, demos + tests).
> **Phase 2** makes MVC controllers return **SSR Razor Components as HTML** (full pages + fragments), with Rizzy-style layout handling via `HtmxApp<T>` + `HtmxLayout<T>`.
> Reference behaviors: Rizzy's MVC controller integration (`RzController.View<T>()`, `PartialView<T>()`) and layout wrappers.

---

## 1) Phase 2 goals (what we ship)

### G1 — Controllers return Razor Component HTML (no `.cshtml`, no `<component>` tag helper)
- Add an opt-in controller base (`HrController`) that exposes:
  - `View<TComponent>(...)` to render a full component "page"
  - `PartialView<TComponent>(...)` to render a fragment (no outer layout)
  - dictionary overloads and `RenderFragment[]` overload for composable fragments
- Mirrors Rizzy's controller API and multi-fragment support.

### G2 — Layout glue: prevent shell duplication on HTMX requests
- Implement `HtmxApp<TLayout>` and `HtmxLayout<TLayout>` so:
  - non-HTMX request -> full layout renders
  - HTMX request -> minimal/empty layout renders, returning only what's needed
- Mirrors Rizzy's `HtmxLayout` behavior with `EmptyLayout` / `MinimalLayout`.

### G3 — Rendering engine service (HtmlRenderer) + results
- Add a scoped renderer service that:
  - builds a component tree (`RzView`-like wrapper is optional but recommended)
  - uses Blazor `HtmlRenderer` to render to HTML (string or stream)
  - supports "Page mode" and "Partial mode"
  - can pass parameters, cascade a request context + ModelState
- Rizzy architecture describes `HtmlRenderer` rendering a component tree and using `RzViewMode.Page/Partial`.

### G4 — Keep v1 HTMX helpers, add MVC filters (optional)
- Provide action filters:
  - `[HtmxRequest]` (optionally restricted to a target)
  - `[HtmxResponse(...)]` to declaratively apply common `HX-*` response headers

### G5 (optional) — Out-of-band swap infrastructure
- Provide:
  - `HtmxSwappable` component (writes `hx-swap-oob` markup)
  - `IHtmxSwapService` + `HtmxSwapContent` placeholder component
- This enables "one request updates multiple regions" without sprinkling raw OOB HTML in controllers.

### Deferred (Phase 3+)
- Streaming interop (`hx-ext="...streaming"`) unless a real page needs it.
- Form/validation components.

---

## 2) Deliverables: packages and responsibilities

### 2.1 `HyperRazor.Components` (RCL)
**Purpose:** Layout + infrastructure components.

Deliver:
- `AppLayout.razor` (root app shell)
- `MainLayout.razor` (default content layout)
- `HtmxApp<TLayout>`: wraps app layout inside `HtmxLayout<TLayout>`
- `HtmxLayout<TLayout>`: conditional layout (full vs minimal/empty for HTMX)
  - internal `EmptyLayout`
  - internal `MinimalLayout`
- (Optional) `HtmxSwapContent`, `HtmxSwappable`

### 2.2 `HyperRazor.Rendering` (class library)
**Purpose:** SSR component rendering to HTML.

Deliver:
- `HyperRazorOptions`
  - `RootComponent` (typically `typeof(HtmxApp<AppLayout>)`)
  - `DefaultLayout` (typically `typeof(HtmxLayout<MainLayout>)`)
  - `AntiforgeryStrategy` (optional; leave minimal in phase 2)
- `IComponentViewService`
  - `Task<IResult> View<TComponent>(object? data = null)`
  - `Task<IResult> View<TComponent>(Dictionary<string, object?> data)`
  - `Task<IResult> PartialView<TComponent>(object? data = null)`
  - `Task<IResult> PartialView<TComponent>(Dictionary<string, object?> data)`
  - `Task<IResult> PartialView(params RenderFragment[] fragments)` (optional)
- `HtmlRendererAdapter`
  - creates/uses `HtmlRenderer`
  - renders a component and awaits quiescence where appropriate

### 2.3 `HyperRazor.Mvc` (class library)
**Purpose:** MVC integration and opt-in controller experience.

Deliver:
- `HrController` (opt-in base controller)
  - `View<T>()` / `PartialView<T>()` delegating to `IComponentViewService`
  - cascades `ModelState` and other context via renderer wrapper
- (Optional) `HrControllerWithViews` for mixed apps (not used in demos)
- MVC action filters:
  - `[HtmxRequest]`, `[HtmxResponse]` (thin wrappers around v1 HTMX helpers)

### 2.4 `HyperRazor.Hosting` (class library)
**Purpose:** Dependency injection + middleware glue.

Deliver:
- `IServiceCollection.AddHyperRazor(Action<HyperRazorOptions> configure)`
  - registers `AddRazorComponents()` prerequisites
  - registers renderer + options
  - registers MVC pieces (`HrController` uses services)
- `IApplicationBuilder.UseHyperRazor()` (optional)
  - appends `Vary: HX-Request` (carry over from v1)
  - other cross-cutting bits as needed

### 2.5 Demos (update/extend)
- `HyperRazor.Demo.Mvc`
  - **no `.cshtml`**
  - controllers return component views/fragments
  - includes layouts + HTMX config injection from v1
- `HyperRazor.Demo.Api`
  - stays mostly as-is; just provides data/validation endpoints

---

## 3) Public API design (keep it small, Rizzy-shaped)

### 3.1 Startup configuration (mirrors the shape you posted)
```csharp
builder.Services.AddControllers();
builder.Services.AddRazorComponents();

// HyperRazor config (Rizzy-shaped)
builder.Services.AddHyperRazor(cfg =>
{
    cfg.RootComponent = typeof(HtmxApp<AppLayout>);
    cfg.DefaultLayout = typeof(HtmxLayout<MainLayout>);
    // cfg.AntiforgeryStrategy = AntiforgeryStrategy.GenerateTokensPerPage;
});

// v1 HTMX config stays
builder.Services.AddHtmx(cfg =>
{
    cfg.SelfRequestsOnly = true;
    cfg.HistoryRestoreAsHxRequest = false; // strongly recommended for partial/full switching
});
```

### 3.2 Opt-in controller usage
```csharp
public sealed class UsersController : HrController
{
    [HttpGet("/users")]
    public Task<IResult> Index()
        => View<UsersPage>();

    [HttpGet("/users/search")]
    [HtmxRequest] // optional
    public Task<IResult> Search(string q)
        => PartialView<UserSearchResults>(new { Query = q });
}
```

### 3.3 Parameter passing options (phase 2)
Ship the two simplest overloads first:
- anonymous object: `new { Foo = 123 }`
- dictionary: `new Dictionary<string, object?> { ["Foo"] = 123 }`

Optional phase 2.1:
- a fluent builder (like Rizzy's `RizzyComponentParameterBuilder`)
- a source-generator `Params(...)` pattern

---

## 4) Rendering architecture (how the sausage gets made)

### 4.1 Use Blazor `HtmlRenderer`
The renderer must:
- begin rendering the component with a `ParameterView`
- await quiescence for async completion when needed
- produce the HTML output

This is the supported approach for rendering Razor components to HTML as a string/stream.

### 4.2 `RzView`-like wrapper (recommended)
Rizzy's architecture describes a wrapper component (`RzView`) that controls mode:
- **Page mode:** component is rendered under `RootComponent + HtmxLayout`, which chooses full vs minimal layout based on `HX-Request`
- **Partial mode:** component is rendered under `EmptyLayout` and bypasses root wrappers

HyperRazor should implement a similar internal wrapper to keep the controller API tiny and avoid duplicating rendering logic.

### 4.3 Cascading context
At minimum, cascade:
- `HttpContext` (or a safe wrapper)
- `ModelStateDictionary`
- any per-request "view context" object used for diagnostics

No opinionated validation UI; just make the data available.

---

## 5) Milestones (Phase 2 breakdown)

### M2.0 — Layout glue + Page/Partial rendering (core)
Deliver:
- `HyperRazor.Components`: `HtmxApp<T>`, `HtmxLayout<T>`, `AppLayout`, `MainLayout`
- `HyperRazor.Rendering`: `HtmlRenderer` adapter + `IComponentViewService`
- `HyperRazor.Mvc`: `HrController` with `View<T>` / `PartialView<T>`
- `Demo.Mvc` migrated to "components only"

Acceptance:
- normal request to `/users` returns full shell
- HTMX request to `/users` returns content without duplicating shell (layout suppression works)
- `/users/search` returns fragment (no layout)

### M2.1 — MVC filters + ergonomics
Deliver:
- `[HtmxRequest]` filter (optional target restriction)
- `[HtmxResponse]` filter (declarative HX headers)
- nicer parameter passing helpers (optional builder)

Acceptance:
- demo uses filters in at least one endpoint
- v1 HTMX response header helpers remain the single source of `HX-*` header truth

### M2.2 — Out-of-band swaps (optional but valuable)
Deliver:
- `IHtmxSwapService`
- `HtmxSwapContent` placeholder component
- `HtmxSwappable` component for OOB swaps
- demo page showing one request updates:
  - a toast/alert region
  - a main region

Acceptance:
- demo shows OOB swap behavior without hand-written `hx-swap-oob` strings in controller responses

### M2.3 — Streaming interop module (defer unless needed)
Deliver:
- a tiny `hyperrazor-streaming` extension (or reuse Rizzy's approach conceptually)
- demo streaming component using `[StreamRendering]`
- guidance: enable extension via `hx-ext="...streaming"` on `<body>`

Acceptance:
- streaming component loads safely even when placed inside an HTMX swap target

---

## 6) Test plan additions (Phase 2)
Add to the v1 test suite.

### Unit tests
- `HtmxLayout` chooses full vs minimal/empty based on `HX-Request`
- renderer in Partial mode never emits layout chrome markers
- parameter binding: anonymous object and dictionary both populate `[Parameter]` values
- quiescence: async component (delayed load) completes and final HTML contains expected content

### Integration tests (`WebApplicationFactory`)
- same URL, full request vs HTMX request:
  - verify different payloads
  - verify `Vary: HX-Request`
- controller `View<T>` and `PartialView<T>` endpoints return `text/html` with expected fragments
- optional: OOB swap endpoint returns HTML containing `hx-swap-oob` payloads when swap service is used

### Optional E2E (Playwright)
- click-through + `hx-boost` + history navigation works with chosen HTMX config (`historyRestoreAsHxRequest=false` strongly recommended for partial/full switching)

---

## 7) `Demo.Mvc` migration checklist (no `.cshtml`)
- create `Components/Layouts/AppLayout.razor` and `MainLayout.razor`
- add HTMX config meta emitter (v1) into `AppLayout` head
- ensure `<body>` includes:
  - `hx-ext="..."` only if/when needed
  - optional `<HtmxSwapContent />` if OOB swaps are enabled
- convert controllers to inherit `HrController` and return component results
- move pages/fragments into `Components/Pages/*` and `Components/Fragments/*`

---

## 8) Keep v1 intact (do not regress)
Phase 2 must not break:
- HTMX config defaults (especially history-restore behavior for partial/full switching)
- `Vary` header / caching guardrails
- typed `HX-*` header setters and parsing helpers

---

## 9) Definition of done (Phase 2)
- controllers can render components as full pages and fragments with a tiny API surface
- layout glue prevents full shell duplication on HTMX requests
- `Demo.Mvc` runs with no `.cshtml` and demonstrates:
  - full-page navigation
  - fragment updates
  - (optional) OOB swap pattern
- tests cover layout selection, rendering modes, and basic integration
