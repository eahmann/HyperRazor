# Phase 2.1 Optional Items (Deferred)
**Date:** 2026-03-04

This document lists optional items that were intentionally not completed during Phase 2.1, plus rationale and suggested follow-up scope.

## Deferred Optional Items

### 1) `IHrxSwapService.RenderToString()`
- Status: Deferred
- Why deferred:
  - Current implementation path composes OOB content through component rendering (`RenderToFragment`) and `HrxSwapContent`.
  - No active production use case currently requires string-based composition from swap service.
- Follow-up target:
  - Add `RenderToString()` to interface + implementation.
  - Add tests proving parity between fragment and string output.

### 2) `ContentItemsUpdated` Eventing on Swap Service
- Status: Deferred
- Why deferred:
  - Current flow is request-scoped and synchronous enough for demo and server rendering paths.
  - No reactive host currently depends on event-driven queue updates.
- Follow-up target:
  - Add `event EventHandler? ContentItemsUpdated` to `IHrxSwapService`.
  - Fire event on all enqueue operations and `Clear()`.
  - Add unit tests for event ordering/count.

### 3) Browser E2E Coverage (Playwright)
- Status: Deferred
- Why deferred:
  - Unit + integration coverage is in place and currently green for OOB and HTMX branching behaviors.
  - Browser automation adds setup/runtime overhead and was optional for Phase 2.1.
- Follow-up target:
  - Add Playwright tests for:
    - OOB multi-region updates (`/demos/oob`)
    - validation UX (`/demos/validation`)
    - redirect patterns (`/demos/redirects`)

### 4) Nested OOB Behavior Tuning (`allowNestedOobSwaps`)
- Status: Deferred
- Why deferred:
  - Current demo payload structure is safe without explicit nested-OOB config tuning.
  - Not required for Phase 2.1 DoD.
- Follow-up target:
  - Add explicit client config support for `allowNestedOobSwaps`.
  - Add demo/test showing behavior difference when enabled vs disabled.

### 5) Strict Validation Semantics Variant (`422` demo endpoint)
- Status: Deferred
- Why deferred:
  - Validation demo switched invalid responses to `200` for cleaner developer UX (no red console noise).
  - Current endpoint still demonstrates server-side validation and HTMX fragment updates.
- Follow-up target:
  - Add a second endpoint/page variant returning `422` for invalid form submits.
  - Document tradeoffs (`semantic HTTP` vs `clean console UX`).Upda

## Out-of-Scope Items (Not Phase 2.1 Optional)

### 1) Streaming interop module (`M2.3`)
- Status: Out of scope for Phase 2.1

### 2) Full Rizzy parity extras / advanced overloads
- Status: Out of scope for Phase 2.1

## Notes
- Phase 2.1 must-ship deliverables are implemented and covered by current automated tests.
- This file tracks only deferred optional or future-scope work.
