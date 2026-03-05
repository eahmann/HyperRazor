# HyperRazor — Phase 3 Implementation Plan
**Date:** 2026-03-05  
**Theme:** Production hardening + navigation semantics + security + HTMX 4 readiness (without turning HyperRazor into a UI kit).

Phase 2.2 completed:
- OOB swap stack + richer swap API, `RenderToString()`, eventing, nested-OOB tuning, validation flow aligned to 200-default behavior, and Playwright E2E.
- Component rendering pipeline + layout suppression.

Phase 3 makes the pattern “real app ready”:
- Predictable navigation/history behavior (hx-boost/hx-push-url contracts)
- Head/title handling
- Error/status semantics that don’t devolve into ad-hoc JS
- First-class antiforgery for HTMX requests
- Minimal client glue with compatibility for HTMX 2.x and HTMX 4.x

---

## 0) Design Principles (carry-forward)
- **KISS:** prefer conventions + a few primitives over a framework.
- **DRY:** one canonical way to:
  - decide full vs partial responses
  - append OOB payloads
  - attach antiforgery to HTMX requests
  - handle status-code routing
- **Opt-in:** nothing here should force an app-wide style system or form component suite.
- **Version-aware:** HTMX 4 introduces breaking changes (event names, inheritance, headers, history semantics) and is being rolled out gradually. HyperRazor needs a deliberate stance.  
  - HTMX 4 is a fetch()-based rewrite with new event naming and history behavior.  
    It is planned to be rolled out over a multi-year period with HTMX 2.x supported long-term.  
    See “The fetch()ening” and HTMX 4 changes/migration docs.  
    (Refs: htmx.org essay + four.htmx.org change logs)

References used for Phase 3 decisions:
- HTMX 4 rationale/timeline & changes: https://htmx.org/essays/the-fetchening/  
- HTMX 4 change list: https://four.htmx.org/htmx-4/  
- HTMX 2→4 migration guide: https://four.htmx.org/migration-guide-htmx-4/  
- hx-boost: https://htmx.org/attributes/hx-boost/  
- hx-push-url: https://htmx.org/attributes/hx-push-url/ and HX-Push-Url header: https://htmx.org/headers/hx-push-url/  
- hx-history/hx-history-elt (HTMX 2.x): https://htmx.org/attributes/hx-history/ and https://htmx.org/attributes/hx-history-elt/  
- OOB nesting config: https://htmx.org/attributes/hx-swap-oob/  
- Head support extension: https://htmx.org/extensions/head-support/  
- Response targets extension: https://htmx.org/extensions/response-targets/  
- htmx:configRequest (2.x): https://htmx.org/docs/  
- ASP.NET Core antiforgery: https://learn.microsoft.com/en-us/aspnet/core/security/anti-request-forgery?view=aspnetcore-10.0  
- ASP.NET Core integration tests + antiforgery parsing: https://learn.microsoft.com/en-us/aspnet/core/test/integration-tests?view=aspnetcore-10.0  

---

## 1) Phase 3 Goals
By the end of Phase 3:

1) **Navigation feels coherent**
- “Boosted” navigation swaps the correct region, updates URL history correctly, and preserves shell.
- Back/forward works under the chosen HTMX version policy.

2) **Errors and validation are not ad-hoc**
- There is a recommended, test-covered way to:
  - show validation errors (200 default; 422 optional)
  - handle 401/403/404/500 with predictable UI behavior
  - avoid “random DOM swaps to body” surprises

3) **Security is turnkey**
- HTMX requests carry antiforgery automatically.
- Tests prove correct behavior.

4) **HyperRazor is HTMX 4 aware**
- HyperRazor’s own client glue supports both HTMX 2.x and HTMX 4.x event naming + header differences (where relevant).
- Demos can run against HTMX 2.x *and* HTMX 4.x (at least in compatibility mode).

---

## 2) Non-Goals (explicit)
- Streaming module and SSE/WebSockets modules (separate future phase).
- A form component/validation component framework.
- A full design system, CSS framework, or “Rizzy parity for everything”.

---

## 3) New Projects / Packages (Phase 3)
Phase 3 introduces two focused deliverables:

### 3.1 `HyperRazor.Client` (RCL)
Purpose: ship a *tiny* JS payload + optional CSS (spinners/disabled) as **static web assets**.

- `wwwroot/hyperrazor.htmx.js`
- `wwwroot/hyperrazor.css` (optional; can be minimal)
- Optional: `wwwroot/vendor/` for extension scripts if you choose to vendor them (or leave to app)

### 3.2 `HyperRazor.Conventions` (class library)
Purpose: centralize “rules of the road” and keep them consistent:
- request context parsing
- response policy selection (full vs partial)
- navigation contract configuration

(If you prefer fewer projects, this can live in `HyperRazor.Hosting`.)

---

## 4) Deliverables (detailed)

# D3.0 — HTMX Version Strategy + Request Context Unification
**Problem:** HTMX 4 changes request headers and event naming, and removes some attributes (hx-history/hx-history-elt). HyperRazor needs a stable internal model for “what kind of request is this?” and “what semantics should we rely on?”

HTMX 4 notes that:
- attribute inheritance is explicit by default (`:inherited`)
- history no longer uses local storage snapshots and does full page refresh on history navigation
- request headers include `HX-Request-Type` ("full"/"partial") and `HX-Source` replaces `HX-Trigger`
- some request/response headers and attributes change names
(See HTMX 4 changes/migration docs.)

## Work items
1) Add `HrxHtmxVersion` + `HrxHtmxClientProfile`
- `HrxHtmxVersion`: `Htmx2`, `Htmx4`
- `HrxHtmxClientProfile`:
  - `Htmx2Defaults` (current baseline)
  - `Htmx4Compat` (HTMX 4 configured to behave closer to 2.x where possible)
  - `Htmx4Native` (default 4.x semantics)

2) Add a single request context model:
- `IHrxRequestContext` with:
  - `IsHtmx`
  - `IsBoosted` (via headers)
  - `RequestType` (`Partial`, `Full`, `Unknown`)
  - `SourceElement` (2.x: `HX-Trigger`; 4.x: `HX-Source`)
  - `TargetElement` (parse format changes in 4.x)
  - `CurrentUrl` (when present)
- Parser rules:
  - Prefer `HX-Request-Type` when present (HTMX 4)
  - Fall back to existing HX-Request + target heuristics (HTMX 2)

3) Update full vs partial selection logic:
- `partial` response only when `RequestType == Partial`
- if `RequestType == Full`, return full page (shell + body)
- this also positions you to handle HTMX 4 history behavior correctly.

## Acceptance criteria
- HyperRazor can decide “full vs partial” without relying on a single header that changed.
- Demo app runs with either HTMX 2 or HTMX 4 (compat profile), with correct behavior.

## Tests
- Unit tests for header parsing in both HTMX 2 and HTMX 4 formats.
- Integration tests that assert:
  - partial request headers yield fragment responses
  - full request headers yield full-page responses

---

# D3.1 — Navigation & History Contract (hx-boost + push-url + shell stability)
**Problem:** hx-boost defaults can swap `body` and use `innerHTML`. HyperRazor needs a consistent “app root” and “main content” region so navigation updates don’t blow away shell, scripts, or layout invariants.

## Work items
1) Establish a standard app structure (document it + demo it)
- `AppLayout` includes a stable outer shell:
  - nav/header/footer regions
  - a single “main swap region” (e.g., `<main id="hrx-main">...</main>`)
- Add a single “opt-in” enablement point for hx-boost:
  - Example: `<main id="hrx-main" hx-boost="true" hx-target="#hrx-main" hx-swap="outerHTML">...</main>`
  - (For HTMX 4 native semantics you may choose `hx-boost:inherited="true"` patterns.)

2) URL synchronization rules
- Use:
  - `hx-push-url="true"` on navigation elements **or**
  - `HX-Push-Url` response header for server-driven pushes
- Establish one convention: “If it changes what the user is looking at, it pushes a URL.”

3) History & privacy rules
- HTMX 2 has `hx-history`/`hx-history-elt` for snapshotting and sensitive pages.
- HTMX 4 removes these and does full refresh requests for history.
HyperRazor should:
- keep hx-history usage strictly in HTMX 2 profile
- in HTMX 4 profile, remove it from docs/demos and rely on full-page history requests

4) Head/title policy (ship like Rizzy)
- Ship first-class Head handling in HyperRazor (Rizzy-style) instead of docs-only guidance.
- Provide opt-in wiring for the official `head-support` extension in `HyperRazor.Client`.
- Define a clear contract for title/meta/link updates during boosted navigation so behavior is consistent across demos and real apps.

## Acceptance criteria
- Boosted navigation updates the main content region without re-rendering the shell.
- URL changes are consistent and back/forward behaves as specified by the profile.
- Title/head behavior is predictable, documented, and supported by a first-party feature when enabled.

## Tests
- Playwright E2E:
  - click boosted link → main region swaps, nav stays stable
  - URL updates (push)
  - back button returns to previous page state
- Integration tests:
  - `HX-Push-Url` header present when controller chooses to push URL

---

# D3.2 — Status-Code & Error Handling (response targets / hx-status) + Server Results
**Problem:** apps need a consistent approach for non-happy-path flows without sprinkling custom JS.

HTMX 2:
- by default does not swap for 4xx/5xx; can be configured via `responseHandling` or via extensions/events.

HTMX 4:
- swaps most responses by default (4xx/5xx included) unless configured otherwise, and introduces the `hx-status:XXX` pattern and pushes the “response-targets” concept.

## Work items
1) Add `HrxStatusHandling` conventions
- Provide a simple “status policy” that maps:
  - 401 → retarget to login modal/region (or HX-Redirect)
  - 403 → show forbidden fragment
  - 404 → show not-found fragment (retarget or full page)
  - invalid form submits → swap validation fragment into form region with **200 by default**
  - optional 422 mode for consumers that explicitly opt in
  - 500 → error region + optional toast OOB

2) Adopt **one** of these strategies per HTMX version profile:
- HTMX 2 profile:
  - standardize on `responseHandling` meta config as the canonical approach in docs/demos
- HTMX 4 profile:
  - follow native HTMX 4 behavior by default; use status-targeting patterns only where needed

3) Add server-side results helpers (no UI kit, just mechanics)
Create `HrxResults` (or `HrxResult<TComponent>`) with helpers like:
- `HrxResults.Page<TComponent>(...)`
- `HrxResults.Partial<TComponent>(...)`
- `HrxResults.Validation<TFormComponent>(statusCode: 200|422, ...)` (default 200)
- `HrxResults.NotFound<TComponent>(...)`
- `HrxResults.Forbidden<TComponent>(...)`
- Each helper:
  - respects the full/partial policy
  - can attach HTMX headers (`HX-Redirect`, `HX-Location`, `HX-Push-Url`, `HX-Trigger`) where appropriate

4) Add demo pages
- `/demos/errors`
  - simulate 401/403/404/500
  - show status-based targeting behavior
- Keep `/demos/validation` as the canonical invalid-form demo (200 path).
- Optionally keep a non-default 422 integration-test path for consumers who opt in.

## Acceptance criteria
- There is a tested, documented story for each status class.
- Apps can choose “semantic codes” without losing swap behavior.

## Tests
- Integration: endpoints return expected status codes + fragments.
- Playwright E2E:
  - 404 swaps error region (not whole page)
  - invalid form swaps validation errors into the form region without console-error noise in default profile
  - toast OOB shows on server errors

---

# D3.3 — Antiforgery / CSRF for HTMX Requests (turnkey)
**Problem:** ASP.NET Core antiforgery is critical for POST/PUT/PATCH/DELETE. HTMX requests must carry tokens. We want this to “just work” with minimal app code.

ASP.NET Core antiforgery is part of the platform and is commonly required for unsafe HTTP methods. (See MS antiforgery docs.)

## Work items
1) Server helper to emit antiforgery token in a predictable place
Options:
- meta tag: `<meta name="hrx-antiforgery" content="...">`
- hidden input in a shared form component
- both, if you want redundancy

2) Client glue in `hyperrazor.htmx.js`
- Read token from meta (or DOM)
- Attach it to every HTMX request via:
  - HTMX 2: `htmx:configRequest` event
  - HTMX 4: `htmx:config:request` event (renamed)
- Add header:
  - `RequestVerificationToken` (or whatever you standardize on)
- Ensure it does not attach to GETs unless you want it to.

3) Test coverage
- Integration tests following Microsoft’s antiforgery testing guidance:
  - GET page → parse cookies/tokens
  - POST with token succeeds
  - POST without token fails

## Acceptance criteria
- Demo app can enable antiforgery globally and HTMX POSTs still succeed.
- Missing token fails as expected and the error handling policy covers the UX.

---

# D3.4 — UX Co-interventions: indicators, disabling, focus/scroll preservation
**Problem:** even with correct server responses, HTMX apps feel “janky” without small UX conventions.

## Work items
1) Standard loading indicator strategy
- Document + demo:
  - `hx-indicator` usage
  - `.htmx-request` CSS class styling
- Provide an optional default spinner element in layouts.

2) Disable interactions during requests
- HTMX 2: `hx-disabled-elt`
- HTMX 4: attribute renames (`hx-disabled-elt` → `hx-disable`, and `hx-disable` → `hx-ignore`)
HyperRazor should:
- avoid hardcoding old attribute names in core components
- provide docs for both profiles
- optionally provide a tiny helper component:
  - `<HrxDisableDuringRequest Selector="..." />` that emits correct attribute name per profile

3) Focus and scroll conventions
- Use stable `id` attributes for inputs to preserve focus.
- For long requests where focus scroll is desired, demonstrate `hx-swap="... focus-scroll:true"`.

## Acceptance criteria
- Demos show:
  - spinner on request
  - submit button disabled during request
  - focus preserved and optionally scrolled into view

## Tests
- Playwright E2E:
  - indicator shows on a deliberate delay endpoint
  - button becomes disabled during request
  - focus remains on same input after field validation swap

---

# D3.5 — Observability: HTMX-aware logging + minimal diagnostics
**Problem:** debugging hypermedia apps without good logs is painful, especially across HTMX 2/4 header changes.

## Work items
1) Add `UseHyperRazorDiagnostics()` middleware
- Add structured log scope containing:
  - IsHtmx
  - RequestType (partial/full)
  - Source/Target (parsed)
  - CurrentUrl (if present)
- Keep this behind an option flag in production.

2) Add a debug endpoint or “developer panel” demo (optional)
- Show parsed HTMX headers + HyperRazor interpretation.

## Acceptance criteria
- A single log line answers: “what did HTMX ask for and why did we return what we returned?”
- Works on both HTMX 2 and HTMX 4 header formats.

---

# D3.6 — Docs, Upgrade Notes, and Templates
**Problem:** without docs and a repeatable starting point, the pattern won’t be adopted consistently.

## Work items
1) Document the contracts
- “Navigation contract”
- “Full vs partial selection”
- “OOB swaps and multi-region updates”
- “Status handling”
- “Antiforgery + HTMX”
- “HTMX 2 vs HTMX 4 profile differences”
  - include the **minimum** differences that affect HyperRazor users (attributes, event names, headers, history)

2) Templates (optional but high leverage)
- `dotnet new hyperrazor-demo`
- `dotnet new hyperrazor-app`
- Ensure templates demonstrate:
  - layout structure
  - hx-boost navigation
  - error/status page
  - antiforgery + HTMX posts

## Acceptance criteria
- A developer can start from template and have “working navigation + OOB + antiforgery” in minutes.

---

## 5) Test Matrix (Phase 3)
Maintain previous unit/integration/E2E baseline and add:

### Unit
- header parsing: HTMX 2 + HTMX 4 formats
- request type determination
- server results attach expected headers

### Integration
- full vs partial response correctness across request types
- antiforgery enforced and HTMX requests succeed when configured
- status endpoints return expected fragments/status codes

### Playwright E2E
- boosted navigation + back/forward
- errors demo (401/403/404/500)
- antiforgery (submit works; missing token shows expected UX)
- indicators/disabled/focus retention

---

## 6) Definition of Done (Phase 3)
Phase 3 is complete when:
- HyperRazor has a documented, test-covered navigation contract.
- A unified request context exists and supports both HTMX 2 and HTMX 4 header semantics.
- Antiforgery works for HTMX posts with a turnkey path and tests.
- Status/error handling is standardized and demoed with 200-default validation behavior.
- Head/title strategy is documented and shipped as a first-party feature (Rizzy-style, opt-in).
- Playwright E2E covers navigation, errors, antiforgery, and UX co-interventions.
- All previous automated tests remain green.

---

## Decisions Locked for Phase 3 Kickoff
1) **HTMX version stance**
- HTMX 2 remains the default.
- `Htmx4Compat` is an explicit opt-in profile.

2) **Status handling strategy**
- HTMX 2 canonical strategy is `responseHandling` meta config.
- Default validation path uses 200 responses (no default 422 demo route).
- 422 behavior remains an optional, consumer-enabled variant.

3) **Multi-target updates**
- OOB swaps remain the first-class mechanism.
- `<hx-partial>` is deferred and can be evaluated later.

4) **Head handling**
- HyperRazor will ship a first-party Head handling feature (Rizzy-style).
- `head-support` extension wiring is included as opt-in support in `HyperRazor.Client`.

5) **Antiforgery transport**
- Recommended path: `RequestVerificationToken` header from a `<meta>` token via configRequest hook.
- Alternative transports can be documented, but not primary.

6) **Attribute rename compatibility**
- Docs-first for renamed HTMX attributes; helper components are optional follow-up if adoption pressure appears.

7) **Diagnostics defaults**
- HTMX-aware diagnostics enabled by default in Development, opt-out via options.

8) **Template scope**
- `dotnet new` templates are deferred until Phase 3.x after API conventions stabilize.
