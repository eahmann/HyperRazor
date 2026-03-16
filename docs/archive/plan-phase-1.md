> Historical document
>
> This file is archived design history. It may describe retired package IDs, old `src/` paths, or superseded assumptions.
> Use [`docs/README.md`](../README.md) for the current docs map and [`docs/package-surface.md`](../package-surface.md) for the current package story.

# HyperRazor v1 Project Plan (Scaled Scope)
**Date:** 2026-03-04

## 1) What “HyperRazor” is (v1 definition)
**HyperRazor** is a small set of .NET libraries that make **HTMX-in-ASP.NET** boring and consistent—so you can adopt the “MVC + HTMX + SSR Razor Components” pattern incrementally without copy/pasting header strings, config blobs, and caching/security footguns.

**v1 is intentionally NOT the full Rizzy-style SSR component view engine.**  
Instead, v1 focuses on getting the **HTMX layer** correct and reusable first (headers, config, middleware, conventions, tests, demo).

> Why this order? If the HTMX plumbing is sloppy, everything built on top (SSR component rendering, layout switching, validation fragments, etc.) becomes fragile.

---

## 2) v1 Goals (KISS / DRY / “cointerventions”)
### v1 goals
1. **Strongly-typed HTMX request/response handling**
   - Read the standard `HX-*` request headers in one place.
   - Write HTMX response headers via a fluent API (redirects, triggers, push-url, retarget, reswap, etc.).

2. **HTMX configuration management**
   - Server-side options → emitted to the client as a `meta name="htmx-config"` JSON blob (or via a tiny component helper).

3. **Safety + correctness defaults**
   - Prevent “fragment cached as full page” issues (`Vary`).
   - Avoid the back-button/history restore gotchas when you’re using `HX-Request` to decide full-vs-fragment behavior.
   - Make “same origin” defaults the easy path.

4. **Minimal demo apps**
   - `Demo.Api` (JSON)
   - `Demo.Mvc` (controllers + HTMX endpoints returning HTML fragments)
   - **No `.cshtml`** required for the demo (use static `wwwroot/index.html` + endpoints returning HTML fragments).

### v1 non-goals
- No SSR Razor Components view engine (that’s v2).
- No form/validation component framework.
- No streaming/out-of-band swapping service abstractions (possible v2+).

---

## 3) Key HTMX facts we are building around (the “sorted out” list)

### 3.1 Response headers you should treat as first-class
We’ll wrap these into a typed response API:

- `HX-Redirect` — redirects with a full page reload (useful when you need the browser to reload head/scripts)  
- `HX-Location` — client-side redirect without a full reload (acts like following an `hx-boost` link)  
- `HX-Push-Url` — push a URL into browser history  
- `HX-Trigger` / `HX-Trigger-After-Swap` / `HX-Trigger-After-Settle` — trigger client-side events from the server

These are all core HTMX behaviors that map cleanly to response header setters in ASP.NET.

### 3.2 Cache correctness: `Vary: HX-Request`
If your server can return different HTML for the same URL depending on `HX-Request` (full page vs fragment), you must vary caching by that header or you risk caching the wrong representation.

v1 will include **middleware** (and/or endpoint opt-in helpers) to set this consistently.

### 3.3 History restore gotcha (HTMX 2.x config default)
HTMX has a config option:

- `htmx.config.historyRestoreAsHxRequest` defaults to `true`
- and HTMX docs explicitly warn it should be disabled when you’re using `HX-Request` to optionally return partial responses.

v1 will set a safe default for this (and expose it as an option).

### 3.4 Same-origin defaults
HTMX has:

- `htmx.config.selfRequestsOnly` defaults to `true` (same-origin AJAX only)

v1 will make it easy to keep the default. If you choose cross-origin in the future, we’ll document the constraints clearly.

---

## 4) Solution layout (v1)
```
/src
  HyperRazor.Htmx/                 (class library)  // pure request/response + config objects
  HyperRazor.Htmx.AspNetCore/      (class library)  // DI + middleware + MVC conveniences
  HyperRazor.Htmx.Components/      (Razor Class Library, RCL) // OPTIONAL v1 helper components (tiny)
  HyperRazor.Demo.Api/             (web api)         // JSON-only demo
  HyperRazor.Demo.Mvc/             (mvc)             // HTMX endpoints returning HTML fragments
/docs
  adopting-hyperrazor.md
  htmx-conventions.md
```

### Project naming (no org required)
- NuGet IDs and namespaces:
  - `HyperRazor.Htmx`
  - `HyperRazor.Htmx.AspNetCore`
  - `HyperRazor.Htmx.Components`

---

## 5) v1 Public API shape

### 5.1 Server-side registration (Rizzy-ish shape, smaller scope)
```csharp
builder.Services.AddControllers();

// v1 focuses on HTMX. Razor Components are optional in v1.
builder.Services.AddHyperRazorHtmx(htmx =>
{
    // Default client config that we emit via meta tag or helper component:
    htmx.SelfRequestsOnly = true;
    htmx.HistoryRestoreAsHxRequest = false; // safer for “full vs fragment” patterns
    htmx.DefaultSwapStyle = "outerHTML";    // optional opinion (keep configurable)
});

var app = builder.Build();

app.UseStaticFiles();
app.UseRouting();

// Ensures caching correctness when endpoints vary by HX-Request
app.UseHyperRazorHtmxVary();

// Optional: emits security headers or centralizes “HX-*” response header exposure
// app.UseHyperRazorHtmxSecurity();

app.MapControllers();
app.Run();
```

### 5.2 Request object (pure)
- `HtmxRequest`
  - `bool IsHtmx`
  - `string? Target`
  - `string? Trigger`
  - `string? TriggerName`
  - `Uri? CurrentUrl`
  - `bool IsBoosted`
  - `bool IsHistoryRestoreRequest` (header-based)

### 5.3 Response builder (pure)
- `HtmxResponse`
  - `.Redirect(url)` → `HX-Redirect`
  - `.Location(url)` → `HX-Location`
  - `.PushUrl(url | false)` → `HX-Push-Url`
  - `.ReplaceUrl(url | false)` → `HX-Replace-Url`
  - `.Retarget(cssSelector)` → `HX-Retarget`
  - `.Reswap(value)` → `HX-Reswap`
  - `.Trigger(name, payload?)` → `HX-Trigger`
  - `.TriggerAfterSwap(...)`, `.TriggerAfterSettle(...)`

### 5.4 ASP.NET conveniences
- `HttpRequest` / `HttpContext` extensions:
  - `HttpContext.Htmx()` returning `(HtmxRequest req, HtmxResponse res)` or two separate methods.
- MVC attribute helpers (optional, tiny):
  - `[RequireHtmx]` to restrict an endpoint to HTMX requests (or vice versa)
  - `[VaryByHtmx]` to add `Vary: HX-Request` on selected endpoints (if you don’t want global middleware)

---

## 6) v1 “cointerventions” (conventions you adopt together)

### CI‑H1: Every HTMX endpoint returns HTML + uses response headers (not JSON + client JS)
- HTMX swaps fragments; avoid pushing logic to JS.
- Prefer server-driven `HX-Trigger` events to coordinate updates.

### CI‑H2: Always address caching when behavior varies
- Default: add `Vary: HX-Request` for HTML responses.

### CI‑H3: Opt into safe history behavior
- Default `historyRestoreAsHxRequest = false` so back/restore requests don’t accidentally get fragments.
- If an app needs a different behavior, it must choose intentionally.

### CI‑H4: Stable IDs for swap targets
- Decide a small convention for root IDs:
  - `#main`, `#panel`, `#search-results`, etc.
- Helps keep fragments interchangeable.

---

## 7) Demo applications (v1)

### 7.1 `HyperRazor.Demo.Api` (JSON)
Purpose: show how MVC app can call a backend API server-side (no CORS) and render fragments.

Endpoints:
- `GET /api/health`
- `GET /api/users?query=...`
- `POST /api/users/validate` (optional: username checks)

Implementation:
- In-memory data is fine.

### 7.2 `HyperRazor.Demo.Mvc` (MVC + HTMX, no cshtml)
Purpose: prove the HTMX foundation layer is pleasant to use.

Approach:
- Static shell in `wwwroot/index.html`
- Controllers provide fragment endpoints:
  - `/fragments/users/search?query=...` → returns `<ul>...</ul>`
  - `/fragments/toast/success` → returns a toast fragment and sets `HX-Trigger`

Demonstrations:
1. **Live search** using `hx-trigger="keyup changed delay:200ms"` (or faster) and `hx-target="#search-results"`.
2. **Server-driven event** using `HX-Trigger` to refresh multiple regions.
3. **Redirect patterns**:
   - after POST, return `HX-Location` for soft navigation
   - or `HX-Redirect` for full reload when needed

---

## 8) Milestones (v1)

### M1 — `HyperRazor.Htmx` (core types)
Deliverables:
- `HtmxRequest` parsing
- `HtmxResponse` header setters
- `HtmxConfig` (serializable to JSON for `<meta name="htmx-config">`)

Quality gate:
- Unit tests: header parsing & header writing
- No stringly-typed `HX-*` scattered in the demo

### M2 — `HyperRazor.Htmx.AspNetCore` (middleware + DI)
Deliverables:
- `AddHyperRazorHtmx(Action<HtmxConfig>)`
- `UseHyperRazorHtmxVary()` middleware
- `HttpContext` extension helpers

Quality gate:
- Demo.Mvc uses only the typed API to read/write HTMX headers.

### M3 — Demo apps
Deliverables:
- Demo.Api (JSON)
- Demo.Mvc (static shell + fragment endpoints + HTMX conventions doc)

Quality gate:
- Clear copy/paste examples for:
  - `HX-Trigger`
  - redirects (`HX-Location` vs `HX-Redirect`)
  - `Vary: HX-Request`

### M4 — Docs
Deliverables:
- `/docs/htmx-conventions.md`
- `/docs/adopting-hyperrazor.md`

Quality gate:
- A new project can adopt the HTMX layer in under ~10 minutes.

---

## 9) v2 Preview (intentionally short)
After v1 is stable, v2 adds the “Rizzy-style” SSR Razor Components layer:

- `HtmxApp<TLayout>` + `HtmxLayout<TLayout>` wrappers (layout suppression based on `HX-Request`)
- Controller base like Rizzy’s `RzController`:
  - `View<TComponent>()` and `PartialView<TComponent>()`
- Leverage .NET’s built-in component rendering results where possible to avoid reinventing the renderer.

v2 is where `MainLayout.razor` + `AppLayout.razor` become “the way” for pages/fragments—**without** any `.cshtml` and **without** `<component>` tag helpers.

---

## 10) Appendix: reference links (for implementers)
- Rizzy’s architecture + why it uses components + HTMX: https://jalexsocial.github.io/rizzy.docs/docs/introduction/architecture-overview/
- Rizzy’s layout wrappers (`HtmxApp<T>` / `HtmxLayout<T>`): https://jalexsocial.github.io/rizzy.docs/docs/components/layout/
- Rizzy’s MVC controller base (`View<T>` / `PartialView<T>` pattern): https://jalexsocial.github.io/rizzy.docs/docs/framework/mvc/setup-with-mvc-controllers/
- HTMX header reference:
  - `HX-Redirect`: https://htmx.org/headers/hx-redirect/
  - `HX-Location`: https://htmx.org/headers/hx-location/
  - `HX-Trigger`: https://htmx.org/headers/hx-trigger/
  - `HX-Push-Url`: https://htmx.org/headers/hx-push-url/
- HTMX config reference (notably `selfRequestsOnly` and `historyRestoreAsHxRequest`): https://htmx.org/reference/
