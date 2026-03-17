# HyperRazor — Phase 6 Portal Shell + Invite & Provision Plan

**Date:** 2026-03-12  
**Status:** Draft for discussion  
**Scope:** Add a full-shell portal-to-console workflow and turn `/users` into the canonical validated + streamed app story.

---

## 0) Executive Summary

Phase 6 should make HyperRazor’s next story feel like a **real product flow**, not another isolated feature page.

The phase should add a new **Portal** shell family that fully replaces the outer application chrome, then land users in the existing admin console where `/users` becomes the canonical **invite + provision** workflow:

1. start on a minimal portal shell,
2. submit a fake access / workspace entry form,
3. swap the entire app chrome into the console shell,
4. invite a user with server-authoritative validation,
5. on success, replace the form with a provisioning shell,
6. stream deterministic progress over SSE,
7. update multiple regions out-of-band,
8. finish in a stable completed state,
9. allow switching workspace / exiting back to the portal shell.

The point of the phase is not real authentication. The point is to prove, in one short flow, that HyperRazor can:

- rerender validated HTML in place,
- promote across layout families,
- replace the **entire** visible shell,
- stream incremental HTML updates,
- and coordinate OOB updates from the same workflow.

---

## 1) Current Repo Context

The demo revamp already set the right product direction: the app should feel like an “Operations Console,” layout changes should happen inside real workflows, `AppLayout` should stay the outer shell, and the current layout families are `admin`, `workbench`, and `task`. The same revamp also explicitly kept **real auth/identity** out of scope. citeturn1view0

Today, `AppLayout` still presents the demo as one stable Operations Console shell around `@Body`, including the global HX inspector. `AdminLayout`, `WorkbenchLayout`, and `TaskLayout` all sit inside that shell, with workbench and task pages intentionally changing only local chrome while the app shell remains stable. citeturn7view1turn1view4turn1view5turn5view0

The current `/users` page already mixes the right ingredients for a canonical story: directory search, invite validation, provisioning, OOB updates, and `RenderToString()` output on one admin surface. Separate `Validation Paths`, `SSE Live Feed`, and `SSE Replay` pages also exist as supporting labs. citeturn3view2turn5view1turn5view2turn5view3

The validation docs say the runtime and authoring surface are now largely in place and that the next seam is **endpoint/workflow ergonomics**, not another redesign of the validation runtime. The SSE docs likewise push for a deterministic, self-contained HTML-first story with OOB updates, blank-data `done` closure, same-origin baseline behavior, and no background-worker requirement. citeturn5view4turn6view0turn6view2turn6view3

That means Phase 6 should focus on the **app story**: shell switching, endpoint conventions, and one short workflow that combines validation and streaming.

---

## 2) Phase 6 Goals

### G1 — Prove a true full-shell swap
Add a new portal-family shell that is visibly different from the console shell and is swapped wholesale during a realistic workflow.

### G2 — Make `/users` the golden-path demo
Turn `/users` into the first page a person should open after entering the console, and make it the clearest single demonstration of HyperRazor’s value.

### G3 — Join validation and SSE into one copyable workflow
A valid invite should transition directly into a deterministic provisioning stream instead of leaving validation and streaming as separate demos.

### G4 — Keep framework expansion narrow
Only extract the minimum reusable helpers needed for endpoint/workflow ergonomics, shell promotion, and provisioning-stream rendering.

### G5 — Leave behind a strong public story
When Phase 6 is done, the repo should have one obvious answer to “what does HyperRazor feel like in a real app?”

---

## 3) Non-Goals

Phase 6 should explicitly avoid:

- real authentication or identity integration,
- OIDC / cookies / role systems beyond existing demo assumptions,
- durable user/session persistence,
- background-job orchestration,
- a distributed replay or fanout system,
- a WebSocket / SignalR abstraction,
- another validation runtime redesign,
- another big layout-family expansion beyond the portal shell,
- and a component-library / design-system buildout.

The portal should look like a login or workspace gate, but it should remain an honest **demo flow**, not fake “security theater” pretending to be production auth.

---

## 4) Primary Architectural Decision

### 4.1 `AppLayout` becomes a shell switcher
`AppLayout` should stop meaning “always render the Operations Console shell.”

Instead, it should become the outer document host that chooses between two shell families:

- **Portal shell** — minimal, centered, no console nav/sidebar/global inspector
- **Console shell** — the current Operations Console chrome used by `admin`, `workbench`, and `task`

Recommended extraction:

- `AppLayout` = outer shell host / switcher
- `PortalShell` = minimal access/workspace shell
- `ConsoleShell` = current Operations Console shell
- `AdminLayout`, `WorkbenchLayout`, `TaskLayout` = inner console layouts only

Add a stable shell marker for tests and diagnostics, e.g.:

- `data-hrz-demo-shell="portal"`
- `data-hrz-demo-shell="console"`

### 4.2 Add `PortalLayout`
Introduce a new layout family:

- `PortalLayout` with `HrzLayoutFamily("portal")`

This is the only new layout family required for the phase.

### 4.3 The portal is a fake entry flow, not real login
The portal should present a short “access” or “workspace entry” form such as:

- work email
- access code
- workspace / environment selector

Behavior:

- invalid submit rerenders inline validation inside the portal card
- valid HTMX submit returns `HX-Location` to `/users?workspace=...`
- valid full-page submit returns `303 See Other` to the same destination

The chosen workspace should be visible on the console side as lightweight UI context, not hidden demo state.

### 4.4 `/users` becomes Invite & Provision
The `/users` page should be reshaped so the primary story is:

- search or identify a user,
- fill the invite form,
- validate locally/server-side as needed,
- submit once,
- swap the form panel into a provisioning shell,
- stream progress updates over SSE,
- finish with a final stable user summary.

Directory search stays as useful surrounding context, but it is no longer the star.

### 4.5 Provisioning stays deterministic and self-contained
The provisioning flow should not depend on real queues, workers, or brokers.

Recommended step sequence:

1. directory entry reserved
2. account record created
3. default groups assigned
4. welcome email queued
5. audit record written
6. provisioning complete

Each step should emit visible progress in the main region plus at least three OOB side updates.

---

## 5) Proposed Route Map

### 5.1 New portal routes

- `GET /portal`
  - renders `PortalPage` under `PortalLayout`
  - this is the canonical entry point for the full-shell-swap demo

- `POST /portal/enter`
  - validates the portal form
  - invalid HTMX: rerender portal form region, preferably `422`
  - invalid full-page: rerender portal page
  - valid HTMX: return `HX-Location: /users?workspace={key}`
  - valid full-page: `303 See Other` to `/users?workspace={key}`

### 5.2 Existing console routes kept as the base

Keep these routes in place:

- `GET /`
- `GET /users`
- `GET /settings/branding`
- existing workbench/task routes

Phase 6 should not re-architect the rest of the demo IA.

### 5.3 Users workflow routes

Keep and/or standardize the `/users` workflow around these endpoints:

- `GET /users`
  - renders the admin page and is the landing destination after portal entry

- `GET /fragments/users/search`
  - keep current directory-search behavior

- `POST /fragments/users/invite`
  - canonical submit endpoint for the invite form
  - invalid: rerender the invite form with attempted values and field/summary errors
  - valid: replace the invite panel with a provisioning shell that opens the SSE stream

- `GET /streams/users/provision/{operationId}`
  - deterministic SSE endpoint for the provisioning sequence
  - emits unnamed HTML messages for the main progress region
  - includes OOB content in streamed messages
  - closes with a blank-data `done` event

### 5.4 Console exit / workspace switching

Use a simple boosted navigation action back to:

- `GET /portal`

This should be exposed as one of:

- **Switch workspace**
- **Exit console**
- **Back to portal**

No simulated logout state is required.

### 5.5 Labs retained but demoted

Keep the existing validation and SSE lab pages available during Phase 6, but treat them as supporting harnesses, not the primary demo path.

---

## 6) Deliverables

### D1 — Portal shell family
- `PortalLayout`
- portal-specific shell chrome
- `AppLayout` shell switching between portal and console
- shell root markers for reliable testing

### D2 — Portal entry workflow
- `PortalPage`
- access/workspace form
- invalid submit rerender
- `HX-Location` / `303` success path into `/users`

### D3 — `/users` story refactor
- reframe page copy around Invite & Provision
- keep search as context
- make one invite form the primary action
- success swaps to provisioning shell instead of “instant done” fragment behavior

### D4 — Provisioning SSE flow
- deterministic operation id generation for demo purposes
- SSE endpoint for provisioning steps
- main progress region updates
- OOB updates for badge, latest-user card, activity feed, and inspector / status sidecar
- clean completion with `done`

### D5 — Endpoint/workflow helper extraction
Extract only the helpers that are clearly reusable after the page works, such as:

- portal enter result helper (`HX-Location` vs redirect)
- invite submit result helper (invalid rerender vs provisioning shell)
- provisioning-shell model / result helper
- SSE step rendering helper for HTML + OOB

### D6 — Demo docs and nav cleanup
- promote portal → users as the first suggested demo path
- demote validation/SSE pages to labs/supporting routes
- add one public doc that explains the full story end to end

---

## 7) Milestones

### M1 — Shell Switching Foundation

**Objective:** make the outer chrome truly swappable.

**Work:**
- add `PortalLayout`
- extract `ConsoleShell` from the current `AppLayout`
- implement portal shell rendering in `AppLayout`
- add shell markers for E2E assertions
- add a console action back to `/portal`

**Exit criteria:**
- navigating `portal -> users` changes the full shell, not just page content
- navigating `users -> portal` changes it back
- portal shell no longer shows console nav/sidebar/global inspector

### M2 — Portal Entry Validation

**Objective:** make shell swapping part of a real form workflow.

**Work:**
- build portal page and form
- add server-authoritative validation
- wire invalid HTMX rerender
- wire valid `HX-Location` / full-page redirect
- carry workspace selection into console UI context

**Exit criteria:**
- invalid submit stays on portal and preserves attempted values
- valid submit enters the console with one full-shell transition

### M3 — `/users` Golden Path Refactor

**Objective:** make `/users` the canonical app story.

**Work:**
- rewrite page copy and panel hierarchy around Invite & Provision
- keep search as supporting context
- make invite submit the main CTA
- replace “provision instantly” behavior with “start provisioning” shell transition

**Exit criteria:**
- `/users` is understandable as a single app workflow without visiting any other page
- invalid invite submit does not start the provisioning stream

### M4 — Provisioning Stream

**Objective:** combine validation success with streamed completion.

**Work:**
- add deterministic provisioning operation state
- implement SSE stream endpoint
- stream 4–6 progress steps
- append OOB updates during the same sequence
- emit blank-data `done` close event
- leave behind a stable final completed state

**Exit criteria:**
- the main progress region updates incrementally without polling
- at least three secondary regions update out-of-band during the stream
- the connection closes cleanly when complete

### M5 — Docs, Cleanup, and Hardening

**Objective:** make the new story the default way to understand the repo.

**Work:**
- update demo nav emphasis
- add docs for portal → users → provisioning
- keep labs accessible but secondary
- retire or demote superseded `/users` endpoints if they are no longer useful
- finish integration and E2E coverage

**Exit criteria:**
- a new contributor can discover the portal flow first
- the test suite proves shell swap, validation, stream, and OOB behavior end to end

---

## 8) Acceptance Criteria

Phase 6 is done when all of the following are true:

1. The demo has a **portal shell** that is visibly distinct from the console shell.
2. A realistic submit flow causes the app to swap the **entire outer chrome**.
3. `/users` is the clearest single demo of validation + OOB + streaming.
4. Invalid portal submits rerender inline and do not leave the portal shell.
5. Invalid invite submits rerender inline and do not open the provisioning stream.
6. Valid invite submits replace the form panel with a provisioning shell.
7. The provisioning stream updates the main region incrementally.
8. At least three secondary regions are updated out-of-band during the stream.
9. The stream closes with a working blank-data `done` event.
10. The final completed state is stable and readable after the connection closes.
11. Existing lab pages remain available, but the main story no longer depends on them.
12. No real auth, background worker, or transport-abstraction work was introduced just to make the demo function.

---

## 9) Acceptance Tests

### 9.1 Unit tests

- `AppLayout` chooses portal shell for `portal` family and console shell for `admin` / `workbench` / `task`
- portal enter helper returns `HX-Location` on HTMX success
- portal enter helper returns `303` on non-HTMX success
- invite submit helper distinguishes invalid rerender from provisioning-shell success
- provisioning SSE helper emits blank-data `done`
- OOB payloads are cleared between streamed messages

### 9.2 Integration tests

- `GET /portal` renders portal shell markers and does **not** render console-shell chrome
- invalid `POST /portal/enter` returns portal validation markup with attempted values preserved
- valid HTMX `POST /portal/enter` returns `HX-Location: /users?...`
- valid non-HTMX `POST /portal/enter` returns redirect to `/users?...`
- `GET /users` renders console shell markers and users-page content
- invalid `POST /fragments/users/invite` returns the invite region with field and summary errors
- valid `POST /fragments/users/invite` returns provisioning-shell HTML instead of the invite form
- `GET /streams/users/provision/{operationId}` returns `text/event-stream`
- streamed payloads include primary HTML updates and OOB markup
- stream ends with `event: done` plus blank `data:`

### 9.3 Browser E2E tests

**Portal to console shell swap**
- open `/portal`
- assert portal shell is visible
- submit invalid form
- assert inline errors appear and shell does not change
- correct the form and submit
- assert full shell swap into console
- assert `/users` content is visible

**Invite invalid path**
- on `/users`, submit an invalid invite
- assert field errors render in place
- assert provisioning stream does not start
- assert current shell remains the console

**Invite + provisioning happy path**
- submit a valid invite
- assert invite panel swaps into provisioning shell
- assert progress updates arrive incrementally without polling
- assert badge, latest-user card, and activity feed all change during the stream
- assert final `done` closes the connection
- assert final state remains stable after closure

**Console exit**
- trigger “Switch workspace” / “Exit console”
- assert full shell swap back to portal
- assert console-only chrome is gone

---

## 10) Suggested Implementation Order

1. Implement `PortalLayout` and `AppLayout` shell switching.
2. Build `PortalPage` and `/portal/enter`.
3. Add the console exit / switch-workspace action.
4. Refactor `/users` copy and panel structure around Invite & Provision.
5. Replace the current invite success path with provisioning-shell swap behavior.
6. Add the deterministic SSE provisioning endpoint.
7. Wire OOB side updates into the streamed sequence.
8. Demote lab pages in nav and docs.
9. Finish integration and Playwright coverage.

---

## 11) Bottom Line

Phase 6 should make HyperRazor’s next leap **visible**.

Not “more primitives.” Not “more labs.”

The win is a short, believable flow where a user:

- enters through one shell,
- validates into another,
- completes real work,
- watches that work stream live,
- and can return back out again.

That is the first moment where the demo will prove that HyperRazor owns the whole HTML-over-the-wire application experience — not just fragments inside a stable frame.
