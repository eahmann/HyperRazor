# HyperRazor v1 Test Cases
*Focus: HTMX request/response foundation, config injection, middleware guardrails, demo app contracts.*

## Test tiers
- **P0** = must-have (release blockers)
- **P1** = strongly recommended
- **P2** = optional / e2e

---

## P0 — Unit tests (library-level)

### 1) Request header parsing
Validate your `HtmxRequest` / extension methods interpret HTMX request headers correctly.

**Cases**
- `IsHtmx`:
  - `HX-Request: true` → `true`
  - missing header → `false`
  - `HX-Request: false` or empty → `false`
- `HxTarget`:
  - returns `HX-Target` value
  - missing header → `null`
- `HxTrigger`:
  - returns `HX-Trigger` value
  - missing header → `null`
- `HxTriggerName`:
  - returns `HX-Trigger-Name` value
  - missing header → `null`
- `HxCurrentUrl`:
  - returns `HX-Current-URL` value
  - missing header → `null`
- `HxBoosted`:
  - `HX-Boosted: true` → `true`
  - missing → `false`
- `HxHistoryRestoreRequest`:
  - `HX-History-Restore-Request: true` → `true`
  - missing → `false`

**Robustness**
- Header name case-insensitivity (e.g., `hx-request`, `HX-REQUEST`)
- Multiple header values (first wins vs join — choose policy and lock it)
- Whitespace trimming rules (choose policy and lock it)

---

### 2) Response header setters (exact strings)
Validate response helper methods set the correct header name/value pair(s) with no surprises.

**Cases**
- `HX-Redirect` sets absolute/relative URL string
- `HX-Location` supports:
  - string form
  - JSON form `{ path, target?, swap?, values?, headers?, select?, push?, replace? }`
- `HX-Push-Url` supports:
  - URL string
  - `false` to prevent push
- `HX-Replace-Url` supports:
  - URL string
  - `false` to prevent replace
- `HX-Refresh`:
  - `true` is written exactly as required by your API
- `HX-Retarget`, `HX-Reswap`, `HX-Reselect` write exact values
- `HX-Trigger`, `HX-Trigger-After-Swap`, `HX-Trigger-After-Settle` support:
  - single event string
  - comma-separated events
  - JSON payload with one event
  - JSON payload with multiple events (top-level object with multiple properties)

**Robustness**
- JSON serialization is stable (ordering doesn’t matter semantically, but string output should be deterministic if you test exact strings)
- Ensure you don’t double-add headers (replace existing value vs append — choose policy and lock it)

---

### 3) Guardrail: 3xx + HX-* headers (do not rely on redirects)
HTMX does **not** process response headers on 3xx responses; lock in behavior to prevent accidental 302s.

**Cases**
- If controller/result sets `HX-Redirect` or `HX-Location`, ensure status is **200** or **204**, never **302**.
- Optional: add a dev-time guard (debug assertion/log) when `StatusCode` is 3xx and HX-* headers are present; test it.

---

### 4) Config injection output is correct
If v1 injects a `<meta name="htmx-config" ...>` or equivalent config emitter, test exact output.

**Cases**
- `historyRestoreAsHxRequest` is set to `false` (foundation default)
- `selfRequestsOnly` is explicitly set to configured value (don’t rely on HTMX version defaults)
- Any other defaults you standardize (e.g., `defaultSwapStyle`) appear exactly once

---

### 5) Cache correctness helper: `Vary: HX-Request`
When your app returns full HTML for non-HTMX requests and partial HTML for HTMX requests, caching must vary by `HX-Request`.

**Cases**
- Middleware (or helper) adds `Vary: HX-Request` when:
  - response is HTML AND
  - endpoint participates in “full vs fragment” switching (however you detect this in v1)
- Ensure `Vary` is appended correctly if an existing `Vary` value is present (e.g., `Vary: Accept-Encoding, HX-Request`)

---

## P1 — Integration tests (Demo.Mvc using WebApplicationFactory)

### 6) Full vs fragment contract (same URL)
Pick at least one endpoint that behaves differently based on `HX-Request`.

**Cases**
- `GET /feature` without `HX-Request`:
  - returns HTML full page
  - includes the HTMX config emitter
- `GET /feature` with `HX-Request: true`:
  - returns fragment HTML
  - does NOT include full shell markers (define a simple sentinel in demo, e.g., `<header id="app-shell">`)

Also assert:
- `Vary: HX-Request` is present on both responses

---

### 7) History restore request behavior
For endpoints that push URLs to history:

**Cases**
- Request with `HX-History-Restore-Request: true`:
  - returns *full page HTML* (not fragment)
  - does not apply HTMX-only partial response shortcut logic

---

### 8) Response header semantics
Integration tests that verify your endpoints produce the correct HX response headers in real pipeline.

**Cases**
- Endpoint sets `HX-Push-Url` (or `HX-Replace-Url`) and the header is present
- Endpoint sets `HX-Trigger` JSON and header is present
- Endpoint sets `HX-Location` JSON and header is present

---

### 9) Security baseline (only if demo has POST)
If Demo.Mvc includes HTMX form POSTs:

**Cases**
- POST without antiforgery token fails (whatever your default policy is)
- POST with antiforgery token succeeds

---

## P2 — Optional E2E (Playwright)

### 10) Back/forward navigation correctness
**Cases**
- Navigate through 2–3 HTMX-driven pages with history support
- Back button restores correctly without “partial page in full document” glitches
- Verify config `historyRestoreAsHxRequest=false` prevents the documented pitfall

---

### 11) URL bar / history behavior
**Cases**
- `HX-Push-Url` actually pushes a new history entry
- `HX-Replace-Url` does not create a new entry (replaces current)

---

## Suggested test project layout
- `HyperRazor.Htmx.Tests` (unit tests: header parsing/writing)
- `HyperRazor.Hosting.Tests` (middleware/config injection tests)
- `HyperRazor.Demo.Mvc.Tests` (integration tests w/ WebApplicationFactory)
- `HyperRazor.E2E` (optional Playwright)