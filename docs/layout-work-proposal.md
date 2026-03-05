# HyperRazor — Layout Boundary Promotion Plan

**Date:** 2026-03-05  
**Status:** Implementation plan (ready to execute)  

---

## 0) Executive Summary

We will replace “Layout Zones” with **layout-boundary promotion**:

- Each page-level layout belongs to a **layout family** (e.g., `main`, `side`).
- The browser includes its **current** layout family in every HTMX request.
- The server resolves the **target** layout family for the requested route.
- If the family matches: return the normal **fragment** response (current behavior).
- If the family differs: automatically **promote** the response to a “shell update” using either:
  - **ShellSwap:** `HX-Retarget` + `HX-Reswap` + `HX-Reselect` (preferred), or
  - **HardReload:** `HX-Redirect` (fallback / safest).

This achieves “just works” multi-layout navigation without:
- a zone registry,
- per-route controller hacks,
- hash tracking / diff state.

---

## 1) Why This Plan

### 1.1 The HTMX boundary we must respect

HTMX swaps only the configured target subtree. If chrome is outside the target, it does not change unless we do a bigger swap or OOB updates.

We want:
- **fast, stable** within-family navigation (small swaps)
- **correct** cross-family navigation (big swap or reload)
- **no bespoke zone system** for incremental shell sync

### 1.2 Why “Layout Zones” are out
Zones create a second layout composition protocol (registry + fills + conflict rules + hashing). It becomes framework state and adds hidden non-local side effects.

Boundary promotion keeps the system:
- stateless (per request),
- inspectable,
- HTMX-native (uses response headers),
- and easy to reason about.

---

## 2) Terminology

- **Layout family**: a stable id string like `main` or `side` that groups layouts whose shell chrome can remain stable across route swaps.
- **Route frame**: the element you normally swap into (currently `#hrz-main-layout` in `layout.txt`).
- **Shell root**: the element that wraps “chrome that might differ by family” and can be swapped when needed (new: `#hrz-app-shell` as a *div inside body*).

---

## 3) High-Level Algorithm

### 3.1 Request (client → server)
On every HTMX request, client sends:

- `X-Hrz-Layout-Family: <current-family>`

(added via `htmx:configRequest` or `hx-headers`)

### 3.2 Response decision (server)
Resolve:

- `clientFamily` from header
- `routeFamily` by inspecting the route’s resolved layout type (e.g., `MainLayout` → `main`, `SideLayout` → `side`)

Decision:

- If **not HTMX**, return full page HTML (current)
- If **history restore** request (`HX-History-Restore-Request: true`), return full page HTML
- Else, if `clientFamily == routeFamily`: return fragment (current)
- Else (boundary crossed): **promote**
  - `ShellSwap` response OR `HardReload`

---

## 4) Architectural Changes

### 4.1 Change: Shell root must be swappable (avoid swapping `<body>`)
In `layout.txt`, `id="hrz-app-shell"` is on `<body>`. HTMX has a quirk: targeting `body` always uses `innerHTML`, so you cannot change body attributes via an HTMX swap.

**Action:** Move `#hrz-app-shell` onto a *div inside body*:

```razor
<body
    data-hrz-antiforgery-meta="..."
    data-hrz-antiforgery-header="..."
    data-hrz-head-support="true"
    hx-ext="head-support">

    <div id="hrz-app-shell" data-hrz-layout-family="@Shell.LayoutFamily">
        <!-- header + route frame + any family-dependent chrome -->
        <header id="app-shell">...</header>

        <main id="hrz-main-layout" class="layout">
            @Body
        </main>
    </div>

    <!-- Optional: inspector outside shell so it never re-renders on promotions -->
    <section class="panel panel--inspector" hx-preserve="true" id="hrz-inspector">...</section>
</body>
```

Notes:
- Keeping HTMX config / antiforgery data attributes on `<body>` is fine because `<body>` stays stable.
- You can preserve the inspector with `hx-preserve` **or** move it outside the shell root.

### 4.2 Add a shell context (so AppLayout knows the route family)
We need AppLayout to emit `data-hrz-layout-family="..."` on the shell root. AppLayout does not naturally “know” which page layout is in use.

**Plan:** Add a server-provided shell context:

```csharp
public sealed record HrzShellContext(string LayoutFamily);
```

Expose it to components via a cascading parameter from your root wrapper (`HrzApp<T>`).

---

## 5) Client Work (HyperRazor.Client)

### 5.1 Add request header on all HTMX requests
Use `htmx:configRequest` to attach a header (event exposes `detail.headers`).

```js
// hyperrazor.htmx.js
document.body.addEventListener('htmx:configRequest', function (evt) {
  const shell = document.querySelector('#hrz-app-shell');
  const family = shell?.dataset?.hrzLayoutFamily || '';
  evt.detail.headers['X-Hrz-Layout-Family'] = family;
});
```

### 5.2 Optional: alternative using `hx-headers`
If you prefer declarative inheritance, `hx-headers` can add headers to all descendant requests.

Example concept (verify exact expression style you want):
```html
<body hx-headers='{"X-Hrz-Layout-Family":"js:document.querySelector(\"#hrz-app-shell\")?.dataset?.hrzLayoutFamily"}'>
```

We’ll ship the `htmx:configRequest` approach as default because it’s explicit and debuggable.

---

## 6) Server Work (HyperRazor.*)

### 6.1 Add a layout family resolver
Create a small, testable service:

```csharp
public interface IHrzLayoutFamilyResolver
{
    string ResolveForPageComponent(Type pageComponentType);
    string ResolveForLayoutType(Type layoutType);
}
```

Resolution rules (KISS):
1. If layout type has `[HrzLayoutFamily("main")]` attribute → use it.
2. Else derive from naming convention:
   - `MainLayout` → `main`
   - `SideLayout` → `side`
3. Else fallback to configured default family (e.g., `main`).

Also add:
- caching (ConcurrentDictionary) because reflection is hot on navigation paths.

### 6.2 Add options for boundary promotion

```csharp
public enum HrzLayoutBoundaryPromotionMode
{
    Off = 0,
    ShellSwap = 1,
    Redirect = 2,
    Refresh = 3
}

public sealed class HrzLayoutBoundaryOptions
{
    public bool Enabled { get; set; } = true;
    public string RequestHeaderName { get; set; } = "X-Hrz-Layout-Family";
    public bool OnlyBoostedRequests { get; set; } = true;  // default safer
    public HrzLayoutBoundaryPromotionMode Mode { get; set; } = HrzLayoutBoundaryPromotionMode.ShellSwap;

    public string ShellTargetSelector { get; set; } = "#hrz-app-shell";
    public string ShellSwapStyle { get; set; } = "outerHTML";
    public string ShellReselectSelector { get; set; } = "#hrz-app-shell";

    public bool AddVaryHeader { get; set; } = true;
}
```

### 6.3 Promotion decision point
Implement promotion in the same layer that currently decides “full vs partial” rendering (likely your component view/result pipeline, not inside the Razor component host, because response headers must be set there).

Pseudo:

```csharp
if (!request.IsHtmx || request.IsHistoryRestore)
  return FullPage();

if (options.OnlyBoostedRequests && !request.IsBoosted)
  return Fragment();

var clientFamily = request.Headers[options.RequestHeaderName].ToString();
var routeFamily = layoutFamilyResolver.ResolveForPageComponent(pageComponentType);

if (string.Equals(clientFamily, routeFamily, OrdinalIgnoreCase))
  return Fragment();

return Promote(routeFamily);
```

### 6.4 Promotion responses

#### Mode A: ShellSwap (default)
Return **full page HTML** (same as non-HTMX render), but instruct HTMX to:
- retarget to the shell root
- swap outerHTML
- select only the shell root from the response

Headers:
- `HX-Retarget: #hrz-app-shell`
- `HX-Reswap: outerHTML`
- `HX-Reselect: #hrz-app-shell`

Optionally:
- `HX-Push-Url: <requested-url>` (extra safety)

Body:
- full HTML document render is acceptable; `HX-Reselect` picks the shell.

#### Mode B: Redirect
Headers:
- `HX-Redirect: <requested-url>`

Body:
- empty or minimal content.

Use when:
- head/scripts/styles meaningfully differ by family
- you want “always correct” behavior over SPA feel

#### Mode C: Refresh
Headers:
- `HX-Refresh: true`

Body:
- empty or minimal content

(rare; redirect is generally clearer)

### 6.5 Caching correctness
If you use caching, HTMX docs recommend:

- `Vary: HX-Request` when rendering fragments for HTMX requests but full HTML otherwise
- disable `historyRestoreAsHxRequest` when using the fragment/full split

With boundary promotion enabled, responses also vary by `X-Hrz-Layout-Family` (because mismatch vs match changes response shape), so include it in Vary as well.

Implementation:
- Always add: `Vary: HX-Request, X-Hrz-Layout-Family` when boundary promotion is enabled (and the response is HTML).

---

## 7) Demo Work (HyperRazor.Demo.Mvc)

### 7.1 Update AppLayout markup
- Move `#hrz-app-shell` to a div inside body
- Add `data-hrz-layout-family` to the shell root
- Move inspector outside shell root OR add `hx-preserve` + stable id

### 7.2 Make family-dependent shell chrome obvious
Add a small, visible element in the AppLayout header that differs by family:

- main family: top nav / “Main Family Header”
- side family: “Side Family Header” / alternate links / different accent

This is crucial: it proves shell changes only when crossing families.

### 7.3 Add demo routes
- `/demos/layout-family/main/*` (uses `MainLayout`)
- `/demos/layout-family/side/*` (uses `SideLayout`)

Inside each, include enough navigation links to:
- navigate within family (should stay fragment swaps)
- navigate across families (should promote)

---

## 8) Tests

### 8.1 Unit tests
- `LayoutFamilyResolverTests`
  - attribute-based resolution
  - naming convention fallback
  - caching behavior (same layout returns same string instance or cached)
- `BoundaryPromotionDecisionTests`
  - match → fragment
  - mismatch → promote
  - history restore → full
  - OnlyBoostedRequests respects `HX-Boosted`

### 8.2 Integration tests (WebApplicationFactory)
- HTMX boosted request with `X-Hrz-Layout-Family=main` to a side-family route:
  - asserts response includes: `HX-Retarget`, `HX-Reswap`, `HX-Reselect`
  - asserts response contains `id="hrz-app-shell"`
- Same-family request:
  - asserts promotion headers are absent
  - asserts response is fragment-shaped (no doctype, depending on your current fragment output)
- History restore request:
  - send `HX-History-Restore-Request: true`
  - assert full HTML returned

### 8.3 Playwright (E2E)
Scenarios:
1) **Within-family**: click 3 links inside main family → ensure the header chrome does not change and shell root DOM node stays stable.
2) **Cross-family**: click link to side family → ensure header chrome changes (shell promotion).
3) **Back/forward**: navigate main → side → back → forward; assert correct shell each time.

---

## 9) Documentation

Create a short doc page:

- “Layout Families & Boundary Promotion”
  - how to tag layouts (attribute or naming convention)
  - how the client header works
  - how promotion works (ShellSwap vs Redirect)
  - known constraints:
    - keep “family-dependent chrome” inside shell root
    - keep route content inside route frame
  - debugging tips:
    - show `data-hrz-layout-family` in inspector
    - log promotion decisions in Development

---

## 10) Migration / Cleanup

- Remove (or keep strictly experimental) any “Layout Zone” prototypes.
- Keep OOB swap plumbing for small multi-region updates (toasts, inspector, badges), but do not use it for layout-level chrome synchronization.

---

## 11) Definition of Done

- Cross-family navigation updates shell chrome automatically without controller code.
- Within-family navigation remains fragment-based and fast.
- Works with:
  - HTMX boosted navigation
  - history restore cache misses
  - existing OOB inspector updates
- Test suite includes:
  - unit coverage for family resolution
  - integration coverage for promotion headers
  - Playwright proof of real DOM behavior

---

## 12) Resolved Decisions

1. **Default promotion mode = `ShellSwap`**
   - Rationale: preserves SPA-like feel across family boundaries and demonstrates the feature directly.
   - Safety valve: support per-route/per-layout opt-in hard navigation when needed (see decision 4).

2. **Promotion scope = boosted navigation only (default)**
   - Apply boundary promotion only when `HX-Boosted: true`.
   - Non-boosted HTMX requests (forms/actions/fragments) remain fragment-first and do not trigger layout promotion logic by default.
   - For non-boosted flows that need a full-layout transition (for example minimal form layout -> full page), use explicit `HX-Redirect` or `HX-Location`.
   - Keep this configurable for advanced consumers.

3. **Missing client family header behavior = fail safe, with fallback inference**
   - Primary source: `X-Hrz-Layout-Family`.
   - Fallback: derive current family from `HX-Current-URL` route mapping when available.
   - If neither can be resolved reliably, treat as boundary mismatch and promote (safe default).

4. **Head/script-sensitive routes = explicit hard-navigation marker**
   - Add an explicit route/layout marker (attribute or metadata), e.g. `[HrzRequireHardNavigation]`.
   - When marker is present, bypass `ShellSwap` and return `HX-Redirect` for correctness.
   - Keep default unmarked routes on `ShellSwap`.

5. **Inspector persistence = outside shell root**
   - Keep inspector outside `#hrz-app-shell` as the default structure.
   - This avoids extra preserve semantics and removes one moving part during shell promotions.

6. **Scope control for v1**
   - Do not introduce layout zones.
   - Deliver boundary promotion first, then evaluate whether additional abstraction is actually needed.
