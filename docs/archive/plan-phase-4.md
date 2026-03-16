> Historical document
>
> This file is archived design history. It may describe retired package IDs, old `src/` paths, or superseded assumptions.
> Use [`docs/README.md`](../README.md) for the current docs map and [`docs/package-surface.md`](../package-surface.md) for the current package story.

# HyperRazor Phase 4 Plan (Verified Baseline)

**Date:** 2026-03-05  
**Scope:** API enrichment, diagnostics hardening, head-management expansion, docs/packaging readiness

---

## 0) Verified current state

This plan is based on the repository state as of today, not assumptions.

- Current prefix baseline is `Hrz*` / `hrz-*` (no `Hrx*` aliases in source).
- Full test suite passes:
  - `HyperRazor.Htmx.Core.Tests`: 11/11
  - `HyperRazor.Htmx.Tests`: 9/9
  - `HyperRazor.Rendering.Tests`: 28/28
  - `HyperRazor.Demo.Mvc.Tests`: 35/35
  - `HyperRazor.E2E`: 7/7
- Core platform already in place:
  - HTMX parsing/writing (`HtmxRequest`, `HtmxResponse`, `HtmxResponseWriter`)
  - hosting/rendering (`AddHyperRazor`, `UseHyperRazor`, `IHrzComponentViewService`)
  - OOB swap queue + flush (`IHrzSwapService`, `HrzSwapContent`, `HrzSwappable`)
  - layout-boundary promotion (ShellSwap/Redirect/Refresh)
  - antiforgery helper components + client header hook
  - demo app with focused pages and E2E coverage

---

## 1) Phase 4 objective

Ship a polished v1 API surface that is easy to adopt, observable in production, and hard to misuse.

Definition of done:

- Clear quickstart docs that match demo code exactly.
- Public API naming and result helpers are coherent for MVC and Minimal API usage.
- Layout promotion decisions are visible in diagnostics.
- Head management supports dedupe/order semantics and script/style handling.
- CI automation exists for unit/integration and scheduled PR-safe E2E.

---

## 2) Already complete (track as closed, do not re-implement)

1. Layout-family request header injection from client script.
2. Layout-boundary promotion logic with configurable mode and boosted-only gating.
3. Vary safety for HTMX and layout-family branching.
4. OOB queue eventing (`ContentItemsUpdated`) and render/clear helpers.
5. Antiforgery token emission + automatic HTMX request header injection.
6. Baseline demo + integration + Playwright E2E flows.

---

## 3) Phase 4 workstreams

### A) API cohesion + onboarding docs

Status:

- complete on 2026-03-06

Deliverables:

- Add `docs/quickstart.md` with a single canonical startup pattern:
  - `AddHyperRazor(...)`
  - `AddHtmx(...)`
  - `UseHyperRazor()`
- Document and standardize result-helper naming:
  - keep current `HrzResults.Page/Partial/...` or rename deliberately
  - avoid having both naming styles without a clear direction
- Add one Minimal API demo endpoint group that renders the same components as MVC examples.

Acceptance:

- Quickstart code compiles and mirrors demo app.
- MVC + Minimal API parity example exists and is tested.

### B) Diagnostics for layout promotion decisions

Status:

- complete on 2026-03-06

Deliverables:

- Extend Development diagnostics scope/logs to include:
  - client layout family
  - route layout family
  - resolved promotion mode
  - whether promotion was applied

Acceptance:

- A single boosted cross-family nav produces an explainable log trail.
- Integration test verifies diagnostics payload shape (or logging behavior via test sink).

### C) OOB API ergonomics (without changing core behavior)

Status:

- complete on 2026-03-06

Deliverables:

- Add ergonomic queue methods (or aliases) on `IHrzSwapService` such as:
  - `QueueComponent<TComponent>(...)`
  - `QueueHtml(...)`
  - `QueueFragment(...)`
- Keep existing semantics: per-request accumulation, HTMX-guarded swappables, optional raw passthrough.

Acceptance:

- Unit tests prove queue ordering, style/selector behavior, and clear behavior unchanged.
- Demo app updates at least one endpoint to use the ergonomic API.

### D) Head-management v2

Status:

- complete on 2026-03-06

Deliverables:

- Expand `IHrzHeadService` with explicit, deterministic operations:
  - title replace semantics
  - keyed dedupe for meta/link/script/style
  - stable ordering rules
- Preserve compatibility with HTMX head-support flow.

Acceptance:

- Unit tests for dedupe + ordering + repeated navigation.
- Demo page proves title/meta/script/style updates without duplication.

### E) CI and release readiness

Status:

- complete on 2026-03-06

Deliverables:

- Add CI workflow(s) to run unit/integration on PRs.
- Add E2E gating strategy (PR or scheduled/nightly depending runtime budget).
- Write package/versioning policy note in docs.

Acceptance:

- Green CI on PR with clear split between fast and slow suites.

---

## 4) Explicit non-goals for Phase 4

- Rebuilding layout boundary from scratch (already shipped).
- Re-introducing old prefixes or compatibility shims.
- Large UI-library buildout (only API shape decisions, not full component catalog).

---

## 5) Proposed execution order

1. Workstream A (quickstart + API naming decision + Minimal API parity)
2. Workstream B (promotion diagnostics)
3. Workstream D (head-management v2)
4. Workstream C (swap API ergonomics cleanup)
5. Workstream E (CI + release policy)

This order front-loads developer adoption and observability before deeper API expansion.
