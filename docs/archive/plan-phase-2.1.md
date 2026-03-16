> Historical document
>
> This file is archived design history. It may describe retired package IDs, old `src/` paths, or superseded assumptions.
> Use [`docs/README.md`](../README.md) for the current docs map and [`docs/package-surface.md`](../package-surface.md) for the current package story.

# HyperRazor — Phase 2.1 Implementation Plan (OOB Completion)
**Date:** 2026-03-04

Phase 2 delivered baseline out-of-band swap primitives. Phase 2.1 focuses on finishing OOB so behavior is HTMX-correct, predictable on full vs fragment requests, and proven with one end-to-end multi-region demo flow plus tests.

## Scope
- In:
  - HTMX-compliant `hx-swap-oob` formatting (`swapStyle` or `swapStyle:selector`)
  - HTMX-only rendering guard for swappables
  - ergonomic swap service API for component/fragment/content enqueueing
  - host glue for appending OOB content only on `IsHtmx && !IsHistoryRestore`
  - one demo endpoint that updates multiple regions in one response
  - unit + integration coverage for OOB behavior and cache guardrails
- Out:
  - streaming interop module (`M2.3`)
  - full Rizzy parity extras not required for shipping (advanced overloads, optional browser E2E)
  - form/validation UI features

## Must-Ship Action Items

- [ ] Add `SwapStyle` and formatter utilities for valid HTMX OOB values.
  - Files: `src/HyperRazor.Components/Services/*` (new enum/formatter), [HrxSwappable.razor](/home/eric/repos/HyperRazor/src/HyperRazor.Components/HrxSwappable.razor)

- [ ] Expand `IHrxSwapService` to support typed enqueue APIs and visibility state.
  - Add: `ContentAvailable`, `AddSwappableComponent`, `AddSwappableFragment`, `AddSwappableContent`, `AddRawContent`, `RenderToFragment(bool clear = false)`, `Clear`
  - Files: [IHrxSwapService.cs](/home/eric/repos/HyperRazor/src/HyperRazor.Components/Services/IHrxSwapService.cs), [HrxSwapService.cs](/home/eric/repos/HyperRazor/src/HyperRazor.Components/Services/HrxSwapService.cs)

- [ ] Implement HTMX request guard in OOB rendering path.
  - Swappable items render only on HTMX requests.
  - `AddRawContent` follows the same guard by default, with explicit opt-in option for non-HTMX rendering.
  - Files: `src/HyperRazor.Components/Services/*`, [HrxSwapContent.razor](/home/eric/repos/HyperRazor/src/HyperRazor.Components/HrxSwapContent.razor)

- [ ] Tighten host glue for fragment responses.
  - Append `<HrxSwapContent />` only when `IsHtmx && !IsHistoryRestore`.
  - File: [HrxComponentHost.razor](/home/eric/repos/HyperRazor/src/HyperRazor.Components/HrxComponentHost.razor)

- [ ] Add a dedicated demo flow proving one-request multi-region OOB updates.
  - Scenario: update main users fragment + append toast + replace header counter.
  - Files: `src/HyperRazor.Demo.Mvc/Controllers/*`, `src/HyperRazor.Demo.Mvc/Components/Fragments/*`

- [ ] Add unit tests for swap formatting, queueing, and HTMX guard behavior.
  - Files: add/extend test project under `tests/` for OOB service behavior.

- [ ] Add integration tests for HTMX/non-HTMX response differences and OOB presence.
  - Assert `HX-Request: true` responses include OOB blocks.
  - Assert non-HTMX responses exclude swappable OOB blocks.
  - Assert `Vary: HX-Request` remains present on branching endpoints.
  - Assert `Vary` also includes `HX-History-Restore-Request` when endpoint behavior branches on history restore.
  - File: [DemoMvcIntegrationTests.cs](/home/eric/repos/HyperRazor/tests/HyperRazor.Demo.Mvc.Tests/DemoMvcIntegrationTests.cs)

- [ ] Validate and close Phase 2.1.
  - Command: `dotnet test HyperRazor.slnx -v minimal`
  - Update docs summary: [README.md](/home/eric/repos/HyperRazor/README.md), [phase-2-wrap-oob.md](/home/eric/repos/HyperRazor/phase-2-wrap-oob.md)

## Good-To-Have (Phase 2.1+)
- Add `RenderToString()` to `IHrxSwapService` for non-component composition paths.
- Add content update eventing (`ContentItemsUpdated`) for reactive render scenarios.
- Add Playwright coverage for browser-level OOB behavior.

## Definition of Done
- `HrxSwappable` emits HTMX-valid `hx-swap-oob` values.
- Swap service supports typed enqueue APIs and HTMX-aware rendering behavior.
- Swap service supports both manual `Clear()` and `RenderToFragment(clear: true)`.
- Demo proves one request can update at least two OOB targets plus the main fragment.
- Integration tests verify HTMX vs non-HTMX behavior and `Vary: HX-Request`.
- `dotnet test HyperRazor.slnx` is green.

## Decisions (Locked)
- `AddRawContent` non-HTMX behavior is explicit opt-in only.
  - Default: apply HTMX guard.
  - Opt-in: allow non-HTMX via option (for example `AllowRawContentOnNonHtmx = true`).
- Keep manual `Clear()` and add `RenderToFragment(clear: true)` helper in Phase 2.1.
- Include `HX-History-Restore-Request` in `Vary` when endpoint output branches on history-restore behavior.
