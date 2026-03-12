# Live Validation Toggle Proposals

**Date:** 2026-03-10  
**Status:** Proposal  
**Audience:** DX and validation planning

> Detailed Proposal B runtime follow-up: `docs/validation-framework-live-policy-spec.md`
> 
> Note: the detailed spec refines the early Proposal B sketch in this memo. Hidden live-policy carriers, not field-shell swaps, are now the default policy update unit.

## 1. Current State

HyperRazor already has a credible validation runtime:

- submit-time state via `HrzSubmitValidationState`
- live patch state via `HrzLiveValidationPatch`
- scoped live requests via `HrzValidationScope`
- MVC `ModelState` mapping
- Minimal API bind-and-validate helpers
- backend `ValidationProblemDetails` mapping back into HTML

The `/validation` harness in `HyperRazor.Demo.Mvc` proves that stack end-to-end. In particular:

- `UserInviteValidationForm.razor` shows the current authoring model for local validation, server live validation, and submit rerendering
- `hyperrazor.validation.js` gates live HTMX requests when local rules fail
- `/validation/live` in `Program.cs` computes a server patch or returns a no-op

That means the runtime is not the weak point anymore. DX is.

Today, application code still hand-wires too much:

- field paths
- input names and ids
- attempted-value replay
- `aria-invalid`
- `aria-describedby`
- local validation hooks
- server slot ids
- summary slot ids
- live `hx-*` attributes
- dependency-field OOB behavior

The live-toggle problem sits inside that gap.

## 2. The Actual Gap

The current stack has first-class models for:

- submit validation
- live validation patches
- live validation request scope

It does **not** have a first-class model for:

- whether live validation is currently active for a field
- what outside information controls that activation
- how activation changes should clear stale server-owned validation UI
- how MVC and Minimal API should describe that policy consistently

Right now that decision is split across ad hoc layers:

- local client rules decide whether a request is blocked before HTMX sends it
- field markup decides whether `hx-post` is present at all
- the server endpoint decides whether to return a patch or a no-op

The Demo.Mvc example works because the field is effectively always armed once `LiveValidationPath` is rendered. The server then decides whether the current request matters. That is a valid demo. It is not yet a clean framework story for conditional live validation.

## 3. Design Goals

Any next-step design should preserve these constraints:

- HTML-first responses remain the browser contract
- server rules remain authoritative
- submit validation and live validation stay separate
- local client validation still blocks obviously bad live requests
- stale server-owned messages clear predictably when a live rule turns off
- MVC and Minimal API share the same semantics
- the authoring surface gets materially simpler than the current attribute soup

## 4. Proposal A: Formalize the Current Server-Authoritative Pattern

### Summary

Keep live validation always armed at the field level. The field always has live HTMX wiring. The server decides on every request whether the live rule is active and either:

- returns a targeted patch
- returns a clearing patch
- returns `204 No Content`

### What changes

Add a framework concept for server-side live rule evaluation, for example:

```csharp
public sealed record HrzLiveRuleDecision(
    bool IsActive,
    bool ClearServerStateWhenInactive);

public interface IHrzLiveValidationRule<TModel>
{
    HrzLiveRuleDecision Decide(TModel model, HrzValidationScope scope);
    HrzLiveValidationPatch Validate(TModel model, HrzValidationScope scope);
}
```

The authoring surface stays close to what the demo uses today. The main improvement is that the server behavior becomes explicit and reusable instead of living inside a route handler.

### Pros

- smallest change from the current implementation
- server stays fully authoritative
- fits naturally with backend-driven or tenant-driven toggles
- no dynamic client enable/disable contract required

### Cons

- request volume does not improve when the rule is inactive
- field wiring is still mostly manual
- "live validation is always on" remains true at the transport layer
- stale-state clearing still needs careful server discipline

### Best fit

Use this when:

- the live rule is rare
- the backend check is cheap
- you want the minimum framework change

## 5. Proposal B: Add First-Class Live Policy Metadata and a Thin Client Gate

### Summary

Introduce a first-class live-validation policy model. The server renders policy metadata into the field. The client does not invent business rules; it only checks whether the current field is enabled for live validation before letting HTMX send the request.

This is the best "middle" option.

### What changes

Add a policy model, for example:

```csharp
public sealed record HrzLiveValidationPolicy(
    bool Enabled,
    string? Path,
    string Trigger,
    IReadOnlyList<HrzFieldPath> DependsOn,
    IReadOnlyList<HrzFieldPath> ClearsWhenDisabled);
```

Render generic attributes instead of demo-specific ones:

```html
data-hrz-local-validation="email"
data-hrz-live-enabled="true"
data-hrz-live-path="/validation/live"
data-hrz-live-depends-on="Email,DirectoryType"
data-hrz-live-clear-fields="Email,DisplayName"
```

Replace the demo-only client harness with a generic one that:

- runs local validation first
- blocks live requests when local validation fails
- blocks live requests when `data-hrz-live-enabled="false"`
- clears server-owned slots when policy says the rule is disabled

The server still owns the policy decision. The client only enforces the already-rendered policy.

### Required refactor

- add a policy abstraction beside `HrzValidationScope` and `HrzLiveValidationPatch`
- replace `hyperrazor.validation.js` special-casing with a generic registry-based harness
- add helper APIs or small wrapper controls so app code does not assemble live metadata manually

### Proposed DOM contract

Proposal B needs one concept that the current demo does not have: a stable field shell that owns the input plus its live-policy metadata.

Suggested shape:

```html
<div id="invite-email-field" data-hrz-field-shell data-hrz-field="Email">
  <label for="invite-email">Email</label>
  <input
    id="invite-email"
    name="email"
    type="email"
    data-hrz-local-validation="email"
    data-hrz-client-slot-id="invite-email-client"
    data-hrz-server-slot-id="invite-email-server"
    data-hrz-summary-slot-id="invite-summary"
    data-hrz-live-enabled="false"
    data-hrz-live-path="/validation/live"
    data-hrz-live-trigger="input changed delay:400ms, blur"
    data-hrz-live-swap="outerHTML"
    data-hrz-live-target="#invite-email-server"
    data-hrz-live-policy-version="3"
    data-hrz-live-clear-fields="Email,DisplayName"
    data-hrz-live-affects="Email,DisplayName" />
  <div id="invite-email-client" data-hrz-client-validation-for="Email"></div>
  <div id="invite-email-server" data-hrz-server-validation-for="Email"></div>
</div>
```

Meaning:

- field shell id
  the smallest safe rerender target when policy metadata changes
- `data-hrz-live-enabled`
  whether the thin gate should allow a live request
- `data-hrz-live-clear-fields`
  fields whose server-owned slots should clear when the rule turns off
- `data-hrz-live-affects`
  fields whose shells or server slots may be updated from this field's request
- `data-hrz-live-policy-version`
  optional monotonic version to help ignore stale async responses

### Thin client gate behavior

The gate should stay narrow:

1. run local validation
2. if local validation fails, clear server-owned slots and cancel the request
3. if `data-hrz-live-enabled` is not `true`, clear server-owned slots named by policy and cancel the request
4. otherwise allow HTMX to send the request unchanged

Suggested interception point:

- keep using `htmx:configRequest`, which is already where the demo blocks invalid live requests

Suggested logic shape:

```js
document.body.addEventListener('htmx:configRequest', function (event) {
  const input = event.detail.elt;
  if (!(input instanceof HTMLInputElement)) return;

  const local = validateLocally(input);
  if (!local.valid) {
    clearServerStateFromPolicy(input);
    event.preventDefault();
    return;
  }

  if (input.dataset.hrzLiveEnabled !== 'true') {
    clearServerStateFromPolicy(input);
    event.preventDefault();
    return;
  }
});
```

The client still does not decide the business rule. It only enforces the policy the server already rendered.

### How fields toggle on and off

The toggle itself should happen by rerendering field shells, not by mutating arbitrary input attributes in place.

There are two cases:

1. the field toggles itself
2. another field toggles it

#### Case 1: field toggles itself

The triggering field can receive a direct swap for its own field shell or its own server slot, depending on what changed.

Use direct swap when:

- policy metadata on the same field changed
- its own client/server ids remain stable
- replacing the shell will not break focus restoration in practice

#### Case 2: another field toggles it

Return an OOB swap for the dependent field shell.

This is the main Proposal B path for conditional live validation.

Example:

- `DirectoryType` changes
- the server decides email live validation should now be disabled
- the response includes:
  - the normal response for `DirectoryType`
  - an OOB `outerHTML` swap for `#invite-email-field`
  - optional OOB clears for `#invite-email-server` and `#invite-summary`

Example response shape:

```html
<div id="invite-directory-type-server">...</div>
<div id="invite-email-field" hx-swap-oob="outerHTML">
  ...
  <input data-hrz-live-enabled="false" ... />
  ...
</div>
<div id="invite-email-server" hx-swap-oob="outerHTML"></div>
<div id="invite-summary" hx-swap-oob="outerHTML"></div>
```

### Why field-shell swaps are needed

The current demo only has stable targets for server-owned message slots and the summary slot.

That is enough for:

- field error patches
- summary patches
- dependency-field server message updates

It is not enough for Proposal B because policy metadata lives on the field/input itself. That means Proposal B needs a stable shell target for each field whose live policy can change.

### Immediate recheck rule

When a field changes from disabled to enabled and the current value is:

- non-empty
- locally valid

the client should schedule one immediate live request for that field.

This avoids the awkward state where a rule turns on but the user must type again before server feedback appears.

### Pros

- better network behavior than Proposal A
- preserves server authority
- explicit model for on/off live validation
- cleanest bridge from current runtime to a better DX story
- works for MVC and Minimal API without changing the response model

### Cons

- requires client and server policy state to stay in sync
- still needs a dependency-update story when outside information changes
- more framework surface than Proposal A

### Best fit

Use this when:

- conditional live validation is a real product requirement
- DX matters now
- you want a framework default, not just a demo technique

## 6. Proposal C: Add a Live Policy Coordinator Endpoint

### Summary

Split live activation from live validation.

Instead of assuming the live field is already armed, introduce a dedicated policy/coordinator pass that asks the server which live rules are currently active for the form snapshot. The response updates field policy and clears stale server state as needed.

### What changes

Add a coordinator model, for example:

```csharp
public sealed record HrzLiveValidationPlan(
    HrzValidationRootId RootId,
    IReadOnlyDictionary<HrzFieldPath, HrzLiveValidationPolicy> Policies);
```

Suggested flow:

1. a dependency field changes
2. the client posts the current form snapshot to `/validation/live-policy`
3. the server evaluates outside information and dependency state
4. the server returns OOB updates that enable, disable, or retarget live validation on affected fields
5. fields only send actual live-validation requests when the current policy says they are active

### Pros

- strongest server-authoritative model for complex forms
- good fit when outside information comes from backend state, feature flags, tenant rules, or expensive lookups
- can enable or disable multiple live rules in one pass
- stale UI clearing can be centralized

### Cons

- adds a second live-validation roundtrip and a second concept to teach
- more moving parts than most forms need
- more framework work before the DX payoff is visible

### Best fit

Use this when:

- the toggle depends on more than one field or more than one server data source
- activation needs to change independently of the live-validated field itself
- the app has dense enterprise forms with real dependency graphs

## 7. Proposal D: Commit to the DX Refactor and Put Live Policy Behind an Authoring Surface

### Summary

Treat the live-toggle problem as a symptom of a broader DX issue and solve it at the authoring layer.

This proposal builds directly on the direction already described in:

- `docs/validation-framework-dx-refactor-spec.md`
- `docs/validation-framework-phase-6-authoring-surface.md`
- `docs/validation-framework-phase-6-client-harness.md`

### What changes

Introduce a framework authoring surface such as:

- `HrzForm`
- `HrzField`
- `HrzInputText`
- `HrzValidationMessage`
- `HrzValidationSummary`

Add a policy provider abstraction:

```csharp
public interface IHrzLiveValidationPolicyProvider
{
    HrzLiveValidationPolicy GetPolicy<TModel>(
        TModel model,
        HrzValidationRootId rootId,
        HrzFieldPath fieldPath);
}
```

Target authoring shape:

```razor
<HrzForm Model="Invite" Action="/validation/minimal/proxy">
    <HrzField For="() => Invite.Email">
        <HrzLabel />
        <HrzInputText Type="email" LivePolicy="InvitePolicies.Email" />
        <HrzValidationMessage />
    </HrzField>
</HrzForm>
```

The framework would own:

- path generation
- ids and names
- attempted-value replay
- `aria-*`
- local validation metadata
- live policy metadata
- server slot metadata
- HTMX live wiring

### Pros

- biggest DX win
- removes the current attribute soup from app code
- creates a natural place to plug in Proposal B or C
- best long-term framework posture

### Cons

- largest scope
- not the fastest path to shipping one conditional live rule
- requires a migration story for existing plain-HTML forms

### Best fit

Use this when:

- validation and DX are top-level roadmap items
- you are willing to spend real framework budget now
- you want the next validation feature to land on a shape worth keeping

## 8. Comparison

| Proposal | Runtime change | DX improvement | Network efficiency | Complexity | Recommended use |
| --- | --- | --- | --- | --- | --- |
| A. Server-authoritative always armed | low | low | low | low | quickest incremental option |
| B. Server-rendered policy + thin client gate | medium | medium-high | medium-high | medium | best default direction |
| C. Policy coordinator endpoint | high | medium | high | high | complex dependency-heavy forms |
| D. Full authoring surface + policy providers | high | very high | depends on B or C | high | strategic framework investment |

## 9. Recommendation

The best path is **Proposal B as the runtime direction, packaged through Proposal D over time**.

Why:

- Proposal A keeps too much of the current weakness in place
- Proposal C is powerful, but it is likely overbuilt for the current stage
- Proposal B introduces the missing abstraction, which is live policy
- Proposal D gives that abstraction a usable authoring surface instead of exposing raw attributes everywhere

In practical terms:

1. add a first-class live policy model
2. make the client harness generic and policy-aware
3. add small framework controls or helpers that emit the metadata automatically
4. keep Proposal A as an escape hatch for apps that prefer always-armed server validation
5. only add Proposal C if real forms prove that policy recomputation needs its own server pass

### Ordering: B before D

Do not start with full Proposal D.

Proposal D is the authoring layer. It should wrap a runtime contract that is already known to be correct.

Recommended order:

1. define and prove the Proposal B runtime contract
2. implement the generic client harness and field-shell patching model
3. validate it on the demo with one or two conditional live rules
4. then build Proposal D controls and helpers on top of those semantics

The only part of D that can reasonably start early is a very small spike to prove the control shape. The real implementation should wait until the B contract is stable enough that the wrapper API will not churn.

## 10. Suggested Incremental Sequence

### Phase 1: Runtime cleanup

- introduce `HrzLiveValidationPolicy`
- keep `HrzLiveValidationPatch` unchanged
- define how disabled live policy clears server-owned slots

### Phase 2: Generic client harness

- replace demo-specific hooks like `data-hrz-local-email`
- support rule names such as `required`, `email`, and `min-length`
- support generic live gating via policy metadata

### Phase 3: Authoring helpers

- add a small wrapper layer for inputs
- stop making application code hand-assemble `aria-*`, slot ids, and `hx-*`

### Phase 4: Optional coordinator

- only if needed for complex server-owned activation logic

## 11. Concrete Decision Rules

If the goal is to ship the feature fast, choose **Proposal A**.

If the goal is to improve the framework without overcommitting, choose **Proposal B**.

If the goal is to support deeply conditional enterprise forms, choose **Proposal C**.

If the goal is to make validation a real strength of HyperRazor, choose **Proposal B + Proposal D** together.

## 12. How Far B + D Can Stretch

Proposal B plus Proposal D should support a lot of complexity, including:

- cross-field enable/disable rules
- one field updating multiple dependent fields
- tenant-specific or backend-specific live-rule activation
- mixed local-validation and server-validation gating
- targeted OOB updates for several affected fields from one request

That said, there is a ceiling.

B + D works best when:

- one request can reasonably recompute the affected live policies
- the dependency graph is understandable at field level
- targeted field-shell and slot updates remain legible

B + D starts to strain when:

- toggling rules depend on many distant fields at once
- the same change can alter policy for large portions of the form
- activation must be recomputed even when no specific live-validation field is active
- multiple async server lookups are needed just to know which rules are currently armed
- race handling becomes more about policy recomputation than validation itself

At that point, keep the D authoring surface, but add the Proposal C-style coordinator underneath it.

So the long-term answer is:

- **yes**, B + D can support very complex intertwined rules
- **no**, B alone should not be expected to elegantly handle the most extreme cases forever
- for the hardest forms, D remains the authoring API while the runtime grows from B toward C internally
