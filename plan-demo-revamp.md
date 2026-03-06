# HyperRazor Demo App Revamp Plan

**Date:** 2026-03-05  
**Scope:** Replace the feature-isolated demo app with a coherent backoffice-style product demo that exercises HyperRazor in realistic workflows.

---

## 0) Locked decisions

- Use a single product-like demo theme: `Operations Console`.
- Keep two domains:
  - `Operations`
  - `Incidents`
- Use **Minimal API** for `AdminLayout` pages:
  - `/`
  - `/users`
  - `/settings/branding`
- Use **MVC page routes + fragment endpoints** for `WorkbenchLayout` and `TaskLayout`.
- Use `HX-Location` as the default success path when a focused task flow returns to a full shell page.
- Reserve `HX-Redirect` for cases that truly require a hard navigation.

---

## 1) Demo app objective

Ship a demo app that feels like a real internal tool, not a list of isolated HTMX tricks.

Definition of done:

- Navigation feels like a small, believable product.
- Layout changes happen as part of real workflows.
- OOB swaps, head updates, validation, redirects, and status handling all show up in realistic actions.
- The demo still proves HyperRazor primitives clearly enough for tests and docs.

---

## 2) Proposed information architecture

### Layout families

1. `AppLayout`
   - Stable shell only
   - App title/branding
   - Global inspector region

2. `AdminLayout`
   - Main product navigation
   - Summary/dashboard shell
   - Used for broader “backoffice” pages

3. `WorkbenchLayout`
   - Denser queue/list-detail shell
   - Used for operational screens with frequent HTMX interaction

4. `TaskLayout`
   - Focused form/review shell
   - Used for approve/reject/create/triage flows

### Route map

#### AdminLayout via Minimal API

- `/`
  - `DashboardPage`
  - Proves boosted navigation, stable shell, summary panels, inspector persistence

- `/users`
  - `UsersPage`
  - Proves live search, sort, pagination, OOB count/feed updates, `RenderToString()`

- `/settings/branding`
  - `BrandingSettingsPage`
  - Proves head handling with realistic title/meta/style/script updates

#### WorkbenchLayout via MVC

- `/access-requests`
  - `AccessRequestsPage`
  - Proves list/detail workflow, dense HTMX interaction, OOB updates after queue actions

- `/incidents`
  - `IncidentsPage`
  - Proves workbench interactions, status handling, and richer operational state

#### TaskLayout via MVC

- `/access-requests/{id}/review`
  - `ReviewAccessRequestPage`
  - Proves focused task flow, validation, `HX-Location` success navigation back out

- `/incidents/{id}/triage`
  - `IncidentTriagePage`
  - Proves focused task flow, error/status handling, and task completion back to workbench/admin

---

## 3) Feature mapping

- **Boosted navigation**
  - Dashboard, Users, Settings, Access Requests, Incidents

- **Layout-boundary promotion**
  - `AdminLayout -> WorkbenchLayout`
  - `WorkbenchLayout -> TaskLayout`
  - `TaskLayout -> AdminLayout` via `HX-Location`

- **OOB swaps**
  - Toasts
  - Badge/count updates
  - Activity feed entries
  - Queue counters / summary cards

- **Head handling**
  - Branding settings page
  - Keyed title/meta/style/script updates

- **Validation**
  - Access request review
  - Incident triage
  - Settings forms where appropriate

- **Status handling**
  - Incident actions
  - Forbidden/unauthorized/not-found/error demo paths embedded in realistic flows

- **RenderToString**
  - Users flow or incident/export preview flow
  - Keep at least one visible example in the app

---

## 4) Active workstreams

### A) Reframe shell, nav, and branding

- [x] Rename the demo in UI copy from feature playground language to `Operations Console`.
- [x] Replace feature-first nav in `src/HyperRazor.Demo.Mvc/Components/DemoNavLinks.razor` with product routes.
- [x] Keep the inspector global, but rewrite its helper copy so it fits the new app framing.

### B) Reorganize layouts and page folders

- [x] Replace the current `MainLayout` / `SideLayout` demo framing with `AdminLayout`, `WorkbenchLayout`, and `TaskLayout`.
- [x] Use folder-based `_Imports.razor` so page groups inherit their layout structurally.
- [x] Keep `AppLayout` as the outer shell and preserve current layout-family promotion behavior.

### C) Build the AdminLayout experience

- [x] Replace `HomePage` with `DashboardPage`.
- [x] Replace the current feature-isolated users/search/OOB examples with a single richer `UsersPage`.
- [x] Move the realistic head demo into `BrandingSettingsPage`.
- [x] Serve these top-level pages from Minimal API routes.

### D) Build the WorkbenchLayout experience

- [x] Add `AccessRequestsPage` with queue + detail behavior.
- [x] Add `IncidentsPage` with queue/list-detail behavior and richer state transitions.
- [x] Use these pages to demonstrate within-family swaps and multi-region updates.

### E) Build the TaskLayout flows

- [x] Add `ReviewAccessRequestPage`.
- [x] Add `IncidentTriagePage`.
- [x] Use `HX-Location` on successful completion to return users to the appropriate shell page.
- [x] Keep failure/validation paths fragment-first so field errors remain in place.

### F) Refactor controller and endpoint surface

- [x] Replace `FeatureController` route responsibilities with the new page model.
- [x] Rework `FragmentsController` around product actions instead of feature-labeled demos.
- [x] Keep fragment endpoints explicit and inspectable; avoid hiding interesting transport behavior behind too much indirection.

### G) Retire old routes and demo pages

- [x] Remove or replace the current feature-named pages:
  - `BasicDemoPage`
  - `SearchDemoPage`
  - `RedirectDemoPage`
  - `ErrorsDemoPage`
  - `ValidationDemoPage`
  - `FeaturePage`
  - `HeadDemoPage`
  - `LayoutSwapDemoPage`
  - `LayoutSwapDetailsPage`
- [x] Remove old nav links and route paths once the new IA fully covers those behaviors.

### H) Update tests to match the new product demo

- [x] Rewrite MVC integration tests around the new route map.
- [x] Rewrite E2E flows around realistic user journeys:
  - dashboard -> users
  - users action with OOB updates
  - admin -> access requests -> review task -> return via `HX-Location`
  - branding settings head update repeat submit
  - incidents task/error flow
- [x] Keep explicit assertions for layout promotion, response headers, OOB payloads, and head dedupe behavior.

---

## 5) Acceptance criteria

- The demo app reads as a coherent product, not a feature checklist.
- Every major HyperRazor behavior is still demonstrated by at least one clear workflow.
- At least one `AdminLayout` page is served by Minimal API and is indistinguishable in UX from MVC-served pages.
- `TaskLayout` completion demonstrates the intended `HX-Location` return pattern clearly.
- Old feature-demo routes are removed once replacement coverage exists.
- Full test suite remains green.

---

## 6) Explicit non-goals

- Real auth/identity
- Real persistence/database integration
- A full component library buildout
- More than two demo domains in this revamp

---

## 7) Suggested implementation order

1. Rework layouts and nav shell
2. Build `DashboardPage`, `UsersPage`, and `BrandingSettingsPage` on `AdminLayout`
3. Build `AccessRequestsPage` and `ReviewAccessRequestPage`
4. Build `IncidentsPage` and `IncidentTriagePage`
5. Remove old feature demo pages and routes
6. Rewrite integration and E2E coverage
