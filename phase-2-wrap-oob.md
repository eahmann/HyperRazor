# Phase 2 Wrap: OOB Stack

## Behaviors We Must Support (HTMX Contract)

- Emit out-of-band swaps using `hx-swap-oob`, including:
  - `true`
  - any valid `hx-swap` value
  - `swapStyle:cssSelector` (swap strategy + explicit target selector)
- Support multi-region updates in a single HTMX response by appending multiple OOB blocks.
- Decide up-front how to handle nested OOB swaps:
  - HTMX processes nested OOB swaps by default.
  - You can configure HTMX to process only OOB swaps adjacent to the main response element via `htmx.config.allowNestedOobSwaps = false`.

## API Parity Target (Rizzy Model)

Rizzy's core value here is: enqueue OOB updates in server code, and a host component appends them automatically.

Rizzy's swap service exposes:

- `AddSwappableComponent<T>()`
- `AddSwappableFragment(...)`
- `AddSwappableContent(...)`
- `AddRawContent(...)`
- `RenderToFragment()`
- `RenderToString()`
- `Clear()`
- `ContentAvailable`
- an update event

It also guards OOB rendering so swappables only render when the request is HTMX.

## Phase A: Lock the Public API (1-2 short PRs)

### A1) Define `IHrxSwapService` to match Rizzy ergonomics

Create an interface equivalent in spirit to Rizzy's `IHtmxSwapService`.

Recommended surface:

```csharp
event EventHandler? ContentItemsUpdated;

bool ContentAvailable { get; }

void AddSwappableComponent<TComponent>(
    string targetId,
    Dictionary<string, object?>? parameters = null,
    SwapStyle swapStyle = SwapStyle.outerHTML,
    string? selector = null);

void AddSwappableComponent<TComponent>(
    string targetId,
    object? parameters = null,
    SwapStyle swapStyle = SwapStyle.outerHTML,
    string? selector = null);

void AddSwappableFragment(
    string targetId,
    RenderFragment fragment,
    SwapStyle swapStyle = SwapStyle.outerHTML,
    string? selector = null);

void AddSwappableContent(
    string targetId,
    string html,
    SwapStyle swapStyle = SwapStyle.outerHTML,
    string? selector = null);

void AddRawContent(string html);

RenderFragment RenderToFragment();

Task<string> RenderToString();

void Clear();
```

Why this matters: it cleanly fills the gap (`Queue(target, html, swap)` only) while preserving a simple mental model.

### A2) Define `SwapStyle` + `ToHtmxString()`

Mirror Rizzy's approach where `SwapStyle` is typed and converted into HTMX swap strings.

Acceptance criteria:

- `SwapStyle.outerHTML` -> `"outerHTML"` (or `"true"`; HTMX treats these equivalently for OOB)
- `SwapStyle.beforeend` + selector `"#toasts"` -> `"beforeend:#toasts"`

## Phase B: Implement the Swap Engine Correctly (Core Gaps)

### B1) Implement `HrxSwapService` as scoped per-request queue

Model it on Rizzy's internal structure:

- store a list of content item records:
  - type: `Swappable` vs `RawHtml`
  - target id, swap style, selector, and a `RenderFragment` payload
- fire `ContentItemsUpdated` whenever items are added (so `HrxSwapContent` can call `StateHasChanged()`)

### B2) Add missing HTMX request guard

In `RenderToFragment()`:

- detect HTMX requests and only render swappable items when HTMX is true
- always allow raw HTML items (useful for non-HTMX full loads)

This resolves:

- no explicit render-only-when-HTMX guard
- accidental OOB blocks in non-HTMX full renders

### B3) Detect HTMX + History Restore properly

Implement a small `HrxRequest` helper:

- `IsHtmx` via `HX-Request: true`
- `IsHistoryRestore` via `HX-History-Restore-Request: true`

Rule of thumb:

- Partial = `IsHtmx && !IsHistoryRestore`
- Full otherwise (history misses should get full pages)

### B4) Add caching guardrails

If responses vary by `HX-Request`, HTMX guidance requires `Vary: HX-Request`, and typically setting `historyRestoreAsHxRequest = false`.

Deliverables:

- middleware or result filter to append `Vary: HX-Request` (and optionally `HX-History-Restore-Request` if branching on it)
- small JS bootstrap that sets:
  - `htmx.config.historyRestoreAsHxRequest = false`
  - optionally `htmx.config.allowNestedOobSwaps = false`

## Phase C: Components + Host Glue

### C1) Upgrade `HrxSwappable` to match HTMX semantics

Rizzy's `HtmxSwappable`:

- renders a wrapper element with `id=TargetId`
- sets `hx-swap-oob` to either:
  - `"swapStyle"`
  - `"swapStyle:selector"`

Implement the same so output matches HTMX documented values.

Fix current limitation (`swap:target` always). Generate:

- `outerHTML` (or `true`) when no selector
- `swapStyle:selector` only when selector is provided

### C2) Upgrade `HrxSwapContent` to reactive flush pattern

Rizzy's `HtmxSwapContent`:

- injects swap service
- subscribes to `ContentItemsUpdated`
- renders `RenderToFragment()` when `ContentAvailable`

Do the same.

Optional improvement:

- decide whether render should clear automatically to avoid duplicate output on multiple renders
- parity recommendation: keep explicit `Clear()`, optionally add `RenderToFragment(clear: true)` later if needed

### C3) Ensure host glue appends OOB next to main fragment

Keep `HrxComponentHost` appending `<HrxSwapContent />` on fragment path, with tighter conditions:

- append only when `IsHtmx && !IsHistoryRestore`
- ensure OOB blocks are siblings adjacent to the main fragment

This keeps behavior compatible with `allowNestedOobSwaps = false`.

## Phase D: Dedicated Full Demo (End-to-End Proof)

Create a demo proving real multi-region OOB from one request.

Demo scenario: Users + Toasts + Header Badge

One HTMX `POST` does all of this:

- main fragment swap
- update `#users-list`
- OOB swap: append toast to `#toast-stack` via `beforeend:#toast-stack`
- OOB swap: replace `#user-count` via `outerHTML` (or `true`)

Server handler pseudoflow:

```csharp
create user;

swap.AddSwappableComponent<Toast>(
    targetId: "toast-stack",
    swapStyle: beforeend,
    selector: "#toast-stack",
    parameters: ...);

swap.AddSwappableContent(
    targetId: "user-count",
    html: "<span id='user-count'>42</span>");

return main fragment component for the list;
```

Why this demo matters:

- proves multi-region updates piggybacked on one response
- proves host glue appends OOB content correctly

Demo must include history/caching config:

- JS sets `htmx.config.historyRestoreAsHxRequest = false`
- server returns `Vary: HX-Request` for branching endpoints

## Phase E: Tests (Unit + Integration + Optional Browser)

### E1) Unit tests for swap service

Test suite for `HrxSwapService`:

- queue behavior
  - add swappable content/fragment/component adds items and toggles `ContentAvailable`
  - `Clear()` resets state
- HTMX guard
  - without `HX-Request`, `RenderToFragment()` excludes swappables
  - with `HX-Request: true`, includes swappables
- swap formatting
  - no selector -> `"outerHTML"`, `"beforeend"`, etc.
  - selector -> `"beforeend:#toast-stack"`

### E2) Integration tests (`WebApplicationFactory`)

Spin up demo app and assert real HTTP responses:

- HTMX request returns OOB blocks
  - `POST` with `HX-Request: true`
  - response contains main fragment plus at least two `hx-swap-oob="..."` blocks
- non-HTMX request does not include OOB blocks
  - same `POST` without `HX-Request`
  - no `hx-swap-oob` (or swappables absent)
- history restore produces full response
  - `GET` with `HX-History-Restore-Request: true`
  - full-page output contract (not fragment-only)
- caching header
  - endpoints varying on `HX-Request` include `Vary: HX-Request`

### E3) Optional Playwright browser test

If desired, add an end-to-end browser check:

- load `/users`
- submit HTMX form
- assert users list changes, toast appears, and count updates

Optional for v1 OOB, but high value for regression safety.

## Deliverables Checklist

When complete, Phase 2 OOB wrap should provide:

- `IHrxSwapService` API comparable to Rizzy's `IHtmxSwapService` (typed helpers, raw helper, render helpers)
- correct `hx-swap-oob` output formats per HTMX docs
- correct HTMX-only rendering guard
- dedicated demo proving multi-region updates from one request
- tests locking behavior, including `Vary: HX-Request` + history restore rules

If implemented in Phase A->E order, this closes all currently identified OOB gaps and gives a stable framework primitive to build on.
