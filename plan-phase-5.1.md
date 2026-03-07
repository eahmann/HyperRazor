# HyperRazor — Phase 5.1 Forms + Validation Plan

**Date:** 2026-03-06  
**Status:** Draft for discussion  
**Naming note:** The current repo code uses the `Hrz*` prefix. Some `Hrx*` names below are placeholder naming from earlier discussion; read them as `Hrz*` unless the team deliberately revives an `Hrx` rename.

---

## 0) Executive Summary

Phase 5.1 should define HyperRazor’s first coherent **server-validated forms** story for MVC / Minimal API apps that render Razor components to HTML.

The key decisions are:

- keep the system **server-authoritative**,
- use **MVC first** because the current rendering pipeline already captures and cascades `ModelState`,
- use **`EditForm` as the preferred authoring surface for the demo**,
- keep the actual transport contract as **plain HTTP/HTMX form post + server rerender**,
- and **borrow Rizzy’s “initial validator bridge” idea without copying Rizzy’s full forms stack**.

Concretely:

- **Do** add a small non-visual bridge component/helper that can take cascaded server validation state and hydrate the current `EditContext` during SSR rendering.
- **Do** prove that inline `ValidationMessage` output can land on the correct fields after an invalid server post.
- **Do not** add a full `HrxForm` / `HrzForm` abstraction in Phase 5.1.
- **Do not** add framework-owned `HrxInputText`, `HrxInputNumber`, etc.
- **Do not** make `[SupplyParameterFromForm]` or internal framework reflection a foundational dependency.

The right target for this phase is **a clean MVC validation loop with `EditForm` and field-level server validation**, not a framework-owned UI/forms subsystem.

---

## 1) Current Repo Context

The public `master` branch already gives us most of the plumbing we need:

- There is **not yet** a dedicated `/demos/validation` route or `ValidationDemoPage` on `master`. The closest existing validation flow is the `/users` page posting to `/fragments/users/invite`, which currently renders `UserCreateValidationResult` into `#validation-result`.
- `HrController` captures `ModelState` before delegating to the render service.
- `HrzComponentViewService` resolves `ModelState` from `HttpContext.Items` and passes it into the render host.
- `HrzResults.Validation<TComponent>()` already exists for partial validation-style responses with a configurable status code.
- the demo app’s HTMX config already allows `[45]..` responses to swap and treats them as non-errors.
- the current demo validation flow is still fragment-level string-list output, not `EditForm` + field-level `ModelState` rendering.

That means the missing piece is mostly **form/validation composition**, not low-level transport.

---

## 2) Phase 5.1 Goals

### G1 — Establish the primary form story

Ship a clear, documented pattern for **server-authoritative forms** in HyperRazor:

- render form markup from Razor components,
- post to MVC endpoints,
- validate on the server,
- rerender with field-level messages,
- and support HTMX-enhanced partial replacement.

### G2 — Prove `EditForm` in this architecture

Use `EditForm` as the preferred authoring surface in the demo, while keeping the real contract server-driven:

- the browser posts the form,
- MVC binds and validates,
- HyperRazor rerenders the component,
- a bridge hydrates the `EditContext`,
- built-in validation message components render inline errors.

### G3 — Upgrade the existing validation demo

Use a dedicated validation route as the proving ground.  
Recommendation: add `/demos/validation` in this phase rather than overloading `/users`, because the current `/users` demo already mixes search, provisioning, and validation concerns.

The demo should show:

- required-field validation,
- at least one business rule that only the server can know,
- field-level messages next to the correct inputs,
- optional validation summary,
- and HTMX-enhanced partial update behavior.

### G4 — Keep the API surface small

Phase 5.1 should prefer:

- native `EditForm`,
- MVC model binding,
- existing HyperRazor rendering entry points,
- and only the smallest reusable helpers that solve a real framework concern.

### G5 — Clarify MVC vs Minimal API support

MVC should be the primary implementation target for Phase 5.1 because it already has the cleanest `ModelState` path.

Minimal API parity should be explored deliberately, not by pretending the MVC path and Minimal API path are identical today.

---

## 3) Non-Goals

Phase 5.1 should explicitly avoid:

- a full `HrxForm` / `HrzForm` abstraction,
- framework-owned input wrappers (`HrxInputText`, `HrxInputSelect`, etc.),
- a custom validation engine,
- client-side unobtrusive validation infrastructure,
- `[SupplyParameterFromForm]` as a foundational dependency,
- a requirement for interactive Blazor runtime behavior,
- and copying Rizzy’s entire form surface area into HyperRazor core.

---

## 4) Primary Architectural Decision

### 4.1 Server state is authoritative

The authoritative validation state for HyperRazor forms should live on the **server request pipeline**, not in a long-lived interactive client component instance.

That means the core loop is:

1. render the form from a component,
2. submit to MVC,
3. validate on the server,
4. return SSR component output with validation state,
5. display errors inline beside the matching fields.

### 4.2 `EditForm` is the authoring surface, not the transport contract

The preferred demo shape for Phase 5.1 is:

- `EditForm`
- built-in input components where practical
- built-in `ValidationMessage` / `ValidationSummary`
- server POST to MVC
- SSR rerender

But the transport contract remains:

- form post,
- server validation,
- server rerender.

HyperRazor should **not** design the phase around assumptions that only hold for interactive Blazor apps, such as:

- `OnValidSubmit` being the main submission path,
- a long-lived in-browser `EditContext`,
- or `ValidationMessageStore` being the primary cross-request transport.

### 4.3 No big `HrxForm`

A large form abstraction would blur the line between HyperRazor core and a UI library.

If Phase 5.1 reveals repeated HTMX / antiforgery / targeting boilerplate, we can consider a **thin** convention helper later. But that should be a follow-up decision based on real duplication, not a starting point.

### 4.4 No custom input controls in core

The core framework should not own:

- labels,
- text boxes,
- selects,
- layout chrome,
- styling,
- or a design system.

Those belong to app code or a UI library.

---

## 5) Rizzy Comparison and Design Implications

### 5.1 What Rizzy proves

Rizzy is useful because it proves that the pattern can work in an MVC + SSR Razor component architecture:

- Rizzy reimplements many of Blazor’s default form inputs and validation components (`RzInput*`, `RzValidationSummary`, etc.) because it wants to handle differences between Blazor interactivity and HTMX/client-side interactivity.
- Rizzy’s MVC story is centered on `RzController` cascading `ModelState` into Razor component views.
- Rizzy’s `RzInitialValidator` then transfers `ModelState` errors into the current `EditContext`, allowing validation messages to render inside an `EditForm` after a postback.
- Rizzy also supports `[SupplyParameterFromForm]`, but its docs explicitly warn that this integration relies on reflection against internal ASP.NET Core services.

So Rizzy demonstrates two things at once:

1. **the bridge idea is sound**, and  
2. **the full forms stack is expensive and framework-heavy**.

### 5.2 What HyperRazor should copy

HyperRazor should copy **the role** of `RzInitialValidator`, not the whole Rizzy forms library.

The right Phase 5.1 extraction is a small non-visual bridge component or helper. Working names:

- `HrxServerValidationBridge`
- `HrxModelStateValidator`
- `HrxInitialValidator` (only if we deliberately want Rizzy-style naming parity)

Its responsibilities should be narrow:

- require a current `EditContext` from `EditForm`,
- read the server validation state already cascaded by HyperRazor,
- clear and repopulate a `ValidationMessageStore` for the current render,
- map server field keys to the same keys used by the form inputs,
- and let built-in `ValidationMessage` / `ValidationSummary` render inline errors.

This is the **right copy** because it solves a real HyperRazor-specific seam:

- MVC owns validation,
- HyperRazor already transports that validation state into the render pipeline,
- but `EditForm` still needs a bridge into `EditContext` if we want native validation authoring.

### 5.3 What HyperRazor should **not** copy

HyperRazor should **not** copy the following parts of Rizzy in Phase 5.1:

#### Do not copy the full `RzInput*` family

Do **not** add:

- `HrxInputText`
- `HrxInputNumber`
- `HrxInputSelect`
- `HrxInputCheckbox`
- etc.

Why:

- this pulls HyperRazor core into UI-library territory,
- it creates a large public surface area immediately,
- and it only becomes necessary if HyperRazor commits to a broader client-side validation/data-val story.

#### Do not copy the full validation component family by default

Do **not** start by adding:

- `HrxValidationMessage`
- `HrxValidationSummary`

Use built-in Blazor validation components first.

Only add HyperRazor-specific validation components if the bridge proves that the built-ins are insufficient in this SSR + HTMX rerender model.

#### Do not copy the `[SupplyParameterFromForm]` integration strategy

Do **not** build Phase 5.1 around form binding that depends on internal ASP.NET Core reflection hooks.

Prefer the explicit MVC path first:

- bind the posted model in the controller,
- validate there,
- pass the model back into the rerendered component as a normal parameter,
- and use the bridge to rehydrate validation messages.

That is simpler, more explicit, and less version-fragile.

#### Do not auto-validate untouched forms

HyperRazor should **not** blindly copy Rizzy’s “validate if no `ModelState` errors exist” behavior on first render.

For HyperRazor’s first demo, the safer behavior is:

- initial GET renders a clean form,
- invalid POST rerender hydrates server errors,
- valid POST clears prior errors.

Showing required-field errors on the first untouched GET would be the wrong UX for this phase.

### 5.4 What to do / what not to do

#### Do this

- Keep MVC + `ModelState` as the primary validation transport.
- Use `EditForm` as the authoring surface in the demo.
- Add a **small bridge** that hydrates `EditContext` from server validation state.
- Use built-in `ValidationMessage` / `ValidationSummary` first.
- Pass the model explicitly from the controller on invalid posts.
- Use HTMX to rerender only the form region on invalid submissions.
- Keep the API surface small until the demo proves what is truly repetitive.

#### Do not do this

- Do not introduce a public `HrxForm` yet.
- Do not create a framework-owned `HrxInput*` family.
- Do not depend on `[SupplyParameterFromForm]` internals.
- Do not treat interactive Blazor submit handlers as the primary transport.
- Do not solve client-side validation in Phase 5.1.
- Do not let forms drag HyperRazor core into owning a UI kit.

---

## 6) Recommended Demo Scope

Add a dedicated `/demos/validation` route and `ValidationDemoPage` in this phase.  
Current `master` does not have that route today; the closest existing validation example is the `/users` invite flow.

Use a simple request model with both annotation-style and business-rule validation. Example shape:

```csharp
public sealed class CreateUserInput
{
    [Required]
    public string? Name { get; set; }

    [Required]
    [EmailAddress]
    public string? Email { get; set; }

    [Range(18, 120)]
    public int? Age { get; set; }
}
```

Recommended server-only business rule:

- `Email` must be unique.

The page should show:

- one validation summary,
- one inline message per invalid field,
- invalid CSS / ARIA state on the field wrapper or input,
- and a visible success state after a valid submission.

### Suggested component contract

For Phase 5.1, prefer the **explicit parameter** path:

- the GET action passes a fresh `CreateUserInput` into the page component,
- the POST action binds `CreateUserInput` via MVC,
- invalid POST rerenders the same component and passes the posted model back in,
- the bridge hydrates the `EditContext` from server validation state.

That avoids making `[SupplyParameterFromForm]` part of the foundation.

---

## 7) Proposed Request / Response Flow

### 7.1 GET

```text
GET /demos/validation
  -> new MVC controller action
  -> View<ValidationDemoPage>(new CreateUserInput())
  -> component renders clean EditForm
```

### 7.2 Invalid POST (MVC-first path)

```text
POST /demos/validation
  -> MVC binds CreateUserInput
  -> DataAnnotations + business validation
  -> errors added to ModelState
  -> controller rerenders ValidationDemoPage with posted model
  -> HyperRazor captures ModelState
  -> bridge hydrates EditContext from server errors
  -> ValidationMessage / ValidationSummary render inline errors
```

### 7.3 Valid POST

```text
POST /demos/validation
  -> bind + validate
  -> success
  -> return success fragment/page or redirect
```

### 7.4 HTMX enhancement

For the HTMX-enhanced path, only the form region should be replaced.

Suggested markup shape:

```html
<section id="validation-demo-form">
  <!-- form + messages live here -->
</section>
```

Suggested submit targeting:

```html
<form hx-post="/demos/validation"
      hx-target="#validation-demo-form"
      hx-swap="outerHTML">
```

If `EditForm` is used, the rendered `<form>` needs to preserve the equivalent targeting/submit semantics.

### 7.5 Status code recommendation

For HTMX invalid submissions, prefer **422 Unprocessable Entity** over `200 OK` when the response is a validation failure.

Reasons:

- it is semantically correct,
- it distinguishes validation failure from success,
- and the current demo HTMX config already allows 4xx responses to swap without surfacing them as client-side errors.

For plain non-HTMX full-page posts, returning `200 OK` with the invalid page rerendered is still acceptable.

---

## 8) Rendering Validation Messages on the Fields Themselves

This phase must ensure validation messages land on the actual fields, not only in a summary block.

### 8.1 Preferred path

The preferred path is:

- `EditForm`
- bridge component inside the form
- built-in `ValidationMessage`
- built-in `ValidationSummary`

That gives HyperRazor the cleanest authoring story without owning a full validation UI stack.

### 8.2 Field naming rules

For field-level rendering to work reliably:

- input names must match MVC binder keys,
- server validation errors must use those same keys,
- the bridge must add messages using matching field identifiers.

One detail needs to be explicit in the plan: with stock Blazor inputs, the emitted `name` attribute follows the bound expression path. For example, binding `@bind-Value="Model.Email"` produces `name="Model.Email"` in SSR output. If the MVC POST action expects unprefixed keys such as `Email`, Phase 5.1 must either:

- set explicit `name` attributes on the rendered inputs,
- bind the MVC model using a matching prefix,
- or choose a component parameter shape whose generated field names already match the binder contract.

V1 scope should focus on:

- flat fields,
- dotted paths if needed,
- and no collection-index binding complexity yet.

### 8.3 Fallback path if built-ins prove insufficient

If the bridge works but built-in validation components do not render correctly in this SSR rerender model, the next-smallest fallback is:

- a tiny `HrxValidationMessage`
- and, if needed, `HrxValidationSummary`

These should stay:

- non-styled,
- narrowly scoped to server-validation display,
- and only introduced after the bridge spike proves the need.

### 8.4 Field chrome

For invalid fields, the rerendered markup should also apply:

- `aria-invalid="true"`,
- `aria-describedby` pointing at the message element when present,
- and an invalid CSS class on the input or field wrapper.

That is a framework-quality UX improvement even without client-side validation.

---

## 9) API Surface Decisions

### 9.1 Do **not** add input wrappers

Do **not** add:

- `HrxInputText`
- `HrxInputNumber`
- `HrxInputSelect`
- `HrxField`

Those are UI-library concerns, not HyperRazor core concerns.

### 9.2 The only new forms-specific primitive that may be justified

The only clearly justifiable new primitive for Phase 5.1 is the bridge component/helper.

That is acceptable because it is:

- non-visual,
- SSR/MVC integration-specific,
- and directly tied to a real HyperRazor seam.

### 9.3 Do **not** add `HrxForm` yet

A public `HrxForm` / `HrzForm` should be deferred.

Only revisit it if Phase 5.1 proves that apps are repeating the same HyperRazor-specific form plumbing often enough to justify a **thin convention wrapper**.

---

## 10) MVC vs Minimal API Split

### 10.1 MVC is the first-class path for this phase

MVC already has the best current story because:

- `HrController` captures `ModelState`,
- the render pipeline already consumes it,
- and the rerender path is already organized around that flow.

So MVC should be the primary implementation target for the first demo and documentation.

### 10.2 Minimal API is a follow-up investigation inside the same phase

Minimal API support is desirable, but it is not identical to MVC today.

`HrzResults.Validation<TComponent>()` can already return a partial validation response with an explicit status code, but there is no equivalent automatic `ModelState` capture path in the current public branch.

That leaves two realistic options:

#### Option A — MVC-first, Minimal API deferred

Ship the MVC pattern first and document Minimal API as follow-up work.

#### Option B — Introduce a small validation-state transport

Add a framework-owned validation-state abstraction that can be filled by:

- MVC (`ModelStateDictionary` adapter)
- Minimal API (explicit field-error dictionary)

Then cascade that abstraction instead of relying only on raw MVC `ModelState`.

**Recommendation:** do not commit to Option B until the MVC demo is working and the actual friction is clear.

---

## 11) `[SupplyParameterFromForm]` Decision

Do **not** make `[SupplyParameterFromForm]` a foundational part of Phase 5.1.

Reason:

- the HyperRazor architecture is MVC / Minimal API rendering SSR components,
- the primary server-validation path is already cleaner via regular MVC model binding + validation,
- and tying the phase to SSR-specific form-binding quirks introduces unnecessary risk.

That does not mean HyperRazor can never support that pattern. It means it should not be the main form story for this phase.

---

## 12) Concrete Deliverables

### D1 — Refresh the validation demo

Add `/demos/validation` as the canonical example, or explicitly decide to keep `/users` as the canonical validation surface instead.  
Recommendation: add the dedicated route so the example is focused and easier to document.

### D2 — MVC invalid-post loop

Add a POST handler that:

- binds input,
- validates it,
- rerenders the form with inline field errors,
- and supports both full-page and HTMX submissions.

### D3 — Bridge spike

Implement an initial `HrxServerValidationBridge` / `HrxModelStateValidator` spike.

The spike should prove:

- server validation can be transferred into `EditContext`,
- built-in validation components render correctly,
- untouched initial GETs do not show validation noise,
- and valid submits clear prior errors.

### D4 — Extraction decision

After the spike:

- keep the bridge demo-local if the API is still unclear,
- or promote it into shared framework code if the shape is stable.

### D5 — Documentation

Document:

- the authoritative validation flow,
- the explicit-parameter MVC pattern,
- the role of the bridge,
- why HyperRazor is not shipping a full form/input stack in 5.1,
- and the current MVC-first recommendation.

### D6 — Tests

Add coverage for:

- invalid POST returns inline field messages,
- the correct field gets the correct message,
- HTMX invalid submit swaps the form region,
- valid submit clears prior errors,
- antiforgery still works,
- and 422 swaps behave as expected.

---

## 13) Suggested Implementation Order

### Step 1 — Prove MVC end-to-end with explicit model passing

- add `ValidationDemoPage`
- add GET/POST controller flow
- bind the posted model in MVC
- rerender the component with the posted model on invalid submit

### Step 2 — Add the bridge inside `EditForm`

- add `EditForm`
- add the bridge component
- verify built-in `ValidationMessage` / `ValidationSummary`
- verify invalid field chrome and ARIA wiring

### Step 3 — Add HTMX partial rerender + 422 behavior

- target only the form region
- confirm invalid 422 responses swap cleanly
- confirm full-page fallback still works

### Step 4 — Decide whether any helper deserves promotion

- if the bridge is clean, consider promoting it
- if validation display is still awkward, consider tiny display helpers
- do **not** promote a full forms stack

### Step 5 — Investigate Minimal API parity

After MVC is solid, decide whether Minimal API needs:

- docs only,
- or a small validation-state transport abstraction.

---

## 14) Resolved Questions

1. What should the bridge be called publicly, if it becomes public?  
   **Answer:** prefer `HrzServerValidationBridge`. It describes the actual job without overcommitting HyperRazor to raw MVC `ModelState` forever. Keep the first implementation demo-local until the shape settles; promote it later if the API still looks clean.

2. Do built-in `ValidationMessage` / `ValidationSummary` work cleanly once the bridge hydrates the `EditContext`?  
   **Answer:** yes. A local render probe against .NET 10 SSR confirms that once a `ValidationMessageStore` is populated before render, the built-in validation components render inline messages correctly, and stock inputs also emit invalid field chrome such as `aria-invalid="true"` and the `invalid` CSS class. Phase 5.1 does not need custom HyperRazor validation display components by default.

3. Does `EditForm` need `FormName` in this external-post MVC pattern, or can it stay purely as an authoring surface?  
   **Answer:** it can stay purely as an authoring surface for the MVC/HTMX postback path. In SSR output, `FormName` only adds the hidden `_handler` field. That matters for Blazor form mapping and `[SupplyParameterFromForm]`, not for a plain browser post to MVC. Treat `FormName` as optional unless HyperRazor later adopts Blazor form-mapping semantics.

4. Should invalid HTMX responses use `200` or `422`?  
   **Answer:** use `422` for HTMX invalid submits and `200` for full-page invalid rerenders. The current demo HTMX config already allows 4xx swaps, so this gives better semantics without needing a client-side workaround.

5. Should Minimal API parity be in-scope for the same phase?  
   **Answer:** only after the MVC path is proven. The current repo has `HrzResults.Validation<TComponent>()`, but no automatic Minimal API equivalent to the MVC `ModelState` capture path. MVC is the primary path; Minimal API parity is follow-up work unless the MVC spike shows a tiny shared validation-state abstraction is obviously worth extracting.

---

## 15) Exit Criteria

Phase 5.1 is done when:

- the repo has a polished validation demo at `/demos/validation`,
- invalid submissions show messages on the correct fields,
- HTMX invalid submissions rerender cleanly,
- the `EditForm` + bridge path is proven,
- the server-driven validation story is documented,
- no unnecessary form/input abstraction has been introduced,
- and the team can clearly explain why HyperRazor copied Rizzy’s bridge idea but not Rizzy’s full forms stack.

---

## 16) Bottom Line

The correct Phase 5.1 move is not “build a form framework.”

It is:

- prove the **server-authoritative** form/validation loop,
- use **`EditForm` as the authoring surface** where it improves clarity,
- bridge server validation into `EditContext` with the smallest possible helper,
- keep MVC first because the current pipeline already supports `ModelState`,
- and extract only the smallest reusable pieces after the pattern is proven.

Borrow the **bridge idea** from Rizzy.

Do **not** borrow the whole forms stack.
