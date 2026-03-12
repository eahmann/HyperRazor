# HyperRazor Phase 2.2 Wrap Plan
**Date:** 2026-03-04  
**Scope:** close Phase 2.x with remaining high-value items, after the validation UX decision to keep demo invalid submits at HTTP `200`.

---

## 1) Decision Record

### Validation status code policy (final for Phase 2.x)
- Demo validation flow remains on `200 OK` for invalid submits.
- We are **not** adding a `/demos/validation-422` route in Phase 2.2.
- HTMX `responseHandling` support for `422` remains available as an optional consumer configuration pattern, not a core demo requirement.

Rationale:
- Cleaner default developer UX (no avoidable red console noise in common demo paths).
- Keeps the baseline flow simple while preserving escape hatches for teams that prefer strict `422` semantics.

---

## 2) Status Snapshot

### Completed
1. `IHrxSwapService.RenderToString(...)` implemented and tested.
2. `IHrxSwapService.ContentItemsUpdated` + `HrxSwapContent` subscription implemented and tested.
3. Demo includes a `RenderToString(clear: true)` execution path (`/fragments/users/create-rendered`).
4. Nested OOB config support (`allowNestedOobSwaps`) added to HTMX config model + tests + demo config.
5. Browser E2E project added (`tests/HyperRazor.E2E`) for OOB, validation, and redirect flows.
6. Docs and demo copy aligned with the no-422-demo decision.

---

## 3) Deliverables

### D2.2.1 — Nested OOB config support (`allowNestedOobSwaps`)
**Goal:** expose an explicit option in HyperRazor HTMX config to control nested OOB behavior.

**Work items**
- Add nullable option (`bool? AllowNestedOobSwaps`) to HTMX config model/builder.
- Emit `allowNestedOobSwaps` in `htmx-config` meta JSON only when explicitly set.
- Document recommended defaults and caveats for reusable fragments.

**Acceptance criteria**
- Config key appears in rendered meta when configured.
- Existing default behavior remains unchanged when option is unset.

**Tests**
- Unit/integration test asserting config JSON emission when set.
- Integration test asserting absence when unset (preserve default behavior).

---

### D2.2.2 — Browser E2E coverage (Playwright)
**Goal:** verify real browser HTMX behavior for the final Phase 2 demo flows.

**Work items**
- Add Playwright .NET test project and deterministic test host setup.
- Add E2E scenarios:
  - `/demos/oob`: main swap + OOB regions updated.
  - `/demos/validation`: invalid submit renders inline errors; valid submit renders success.
  - `/demos/redirects`: `HX-Location` and `HX-Redirect` navigation behavior.

**Acceptance criteria**
- E2E suite runs with one command.
- Tests are deterministic (stable selectors, no arbitrary sleep timing).

---

### D2.2.3 — Docs and demo text alignment
**Goal:** align written docs and demo copy with the finalized 200-based validation approach.

**Work items**
- Update Phase 2 docs to remove any “planned 422 demo route” language.
- Keep guidance for optional `422` handling as an advanced consumer recipe.
- Ensure demo page copy describes current behavior accurately.

**Acceptance criteria**
- No Phase 2.2 docs imply a required `/demos/validation-422` implementation.
- Validation guidance clearly distinguishes:
  - default demo behavior (`200` invalid responses),
  - optional consumer behavior (`422` with explicit HTMX response handling).

---

## 4) Demo Scope (Phase 2.2)
Required demo pages:
1. `/demos/oob`
2. `/demos/validation` (HTTP `200` invalid flow)
3. `/demos/redirects`

Not required for Phase 2.2:
1. `/demos/validation-422`

---

## 5) Definition of Done (Phase 2.x Wrap)
Phase 2.x is wrapped when all are true:
1. `RenderToString` and swap-service eventing remain green in unit/integration tests.
2. `allowNestedOobSwaps` config support is implemented and covered by tests.
3. Playwright E2E covers OOB, validation (`200` flow), and redirects.
4. Docs and demo copy are aligned with the no-422-demo decision.
5. Existing test suites remain green.

---

## 6) Explicit Out-of-Scope
1. Streaming interop module.
2. Full Rizzy parity extras / overload expansion.
3. Building a full form framework beyond current demo + helper patterns.

---

## 7) References
- OOB nesting and `allowNestedOobSwaps`: https://htmx.org/attributes/hx-swap-oob/  
- HTMX config API: https://htmx.org/api/  
- HTMX response handling / non-2xx swap behavior: https://htmx.org/quirks/  
- Playwright .NET docs: https://playwright.dev/dotnet/docs/writing-tests
