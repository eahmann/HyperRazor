# HyperRazor Live Validation Policy Spec

**Date:** 2026-03-10  
**Status:** Draft proposal  
**Scope:** Proposal B runtime contract  
**Next step:** Proposal D authoring surface should build on this contract, not redefine it

## 0. Summary

This document turns Proposal B from `docs/live-validation-toggle-proposals.md` into a concrete runtime spec.

The core decision is:

- live validation requests remain HTML-first and HTMX-driven
- the browser sends the nearest form snapshot by default
- the request is still field-scoped through `__hrz_fields`
- the server renders live policy metadata into the DOM
- a thin client gate only enforces that rendered policy
- field policy changes are delivered through targeted HTML updates, usually OOB

This spec intentionally does **not** define the higher-level authoring surface. That is Proposal D work to do after this runtime is proven.

## 1. Problem Statement

HyperRazor already has:

- submit-time validation state
- live patch state
- scoped live validation requests
- MVC and Minimal API binding paths
- server-owned field and summary slots

What it does not have is a first-class model for conditional live validation.

Today, whether a field sends a live request is determined in ad hoc ways:

- a local JavaScript rule blocks the request
- the field may or may not render `hx-post`
- the server may or may not respond with a meaningful patch

That is enough for a demo where live validation is effectively always armed once a field has a live endpoint. It is not enough for the next class of scenarios:

- a field that becomes live-validatable only for certain tenant modes
- a field whose live rule depends on another field elsewhere in the root
- a field that must clear stale server feedback when policy turns off
- a field that becomes live-validatable after another field changes

## 2. Goals

- keep the browser-facing contract HTML-first
- keep the server authoritative for business rules and live-policy decisions
- keep submit validation and live validation separate
- support turning live validation on and off per field
- preserve focus, input value, and local client state during live updates
- support MVC and Minimal API with the same semantics
- keep the request shape safe for complex rules
- create a runtime contract that Proposal D can wrap later

## 3. Non-Goals

- do not design the Proposal D control library here
- do not replace submit-time validation contracts
- do not choose a client-side validation library
- do not require a separate coordinator endpoint in v1
- do not optimize live requests down to a minimal field-only payload in v1
- do not make direct JSON-to-browser validation a first-class path

## 4. Frozen Decisions

### 4.1 Request scope

Live validation requests are:

- field-scoped in intent
- form-scoped in data by default

That means:

- `__hrz_fields` tells the server which field or fields are being live-validated
- `hx-include="closest form"` remains the default payload behavior

Reason:

- the server often needs sibling values to recompute policy safely
- field-only payloads are too brittle for intertwined rules
- the current runtime already binds the full form model during live validation

### 4.2 Dependency data outside the form

If a live rule depends on values outside the nearest form but still within the same logical root, the request must explicitly include those values in addition to `closest form`.

Example:

```html
hx-include="#invite-root [name='TenantId'], closest form"
```

Default posture:

- nearest form snapshot first
- explicit extra selectors only when the live dependency graph crosses form boundaries

### 4.3 Thin client gate only

The client harness does not decide business rules.

It only:

- runs local validation
- reads the server-rendered live policy
- blocks requests when local validation fails
- blocks requests when live policy is disabled
- clears server-owned state when required by policy

### 4.4 Targeted updates only

Live validation must not rerender the whole form.

Allowed update units:

- server-owned field validation slots
- server-owned summary slot
- hidden live-policy carriers for affected fields

### 4.5 Proposal D sequencing

Proposal D should not start as the primary implementation track.

Order:

1. prove this Proposal B runtime
2. validate the DOM and response contract on the demo
3. build the Proposal D authoring surface on top

## 5. Baseline From Current Implementation

Current implemented behavior:

- live inputs use `hx-include="closest form"` in `UserInviteValidationForm.razor`
- the server binds the full model during `/validation/live`
- `validation-live.js` blocks requests during local invalid states
- the response patches server-owned field slots and summary slots

Current gap:

- there is no rendered live-policy contract
- there is no stable patch target for policy changes
- there is no generic client harness for enabled or disabled live validation

## 6. Core Runtime Model

### 6.1 `HrzLiveValidationPolicy`

Proposal B introduces a policy model distinct from `HrzLiveValidationPatch`.

Suggested shape:

```csharp
public sealed record HrzLiveValidationPolicy(
    bool Enabled,
    IReadOnlyList<HrzFieldPath> DependsOn,
    IReadOnlyList<HrzFieldPath> AffectedFields,
    IReadOnlyList<HrzFieldPath> ClearFields,
    bool ReplaceSummaryWhenDisabled,
    bool ImmediateRecheckWhenEnabled);
```

Meaning:

- `Enabled`
  whether the thin gate should allow a live request for the field right now
- `DependsOn`
  fields whose values are required to compute the policy safely
- `AffectedFields`
  fields whose hidden policy carrier or server slot may need updates when this field triggers a request
- `ClearFields`
  server-owned fields to clear when policy turns off
- `ReplaceSummaryWhenDisabled`
  whether the summary slot should also be cleared when the rule turns off
- `ImmediateRecheckWhenEnabled`
  whether the client should schedule one immediate live request when the field transitions from disabled to enabled and its current value is locally valid

### 6.2 Policy evaluation seam

Proposal B needs a runtime seam for policy resolution.

Suggested shape:

```csharp
public interface IHrzLiveValidationPolicyResolver
{
    Task<HrzLiveValidationPolicy> ResolveAsync<TModel>(
        TModel model,
        HrzValidationRootId rootId,
        HrzFieldPath fieldPath,
        IReadOnlyDictionary<HrzFieldPath, HrzAttemptedValue> attemptedValues,
        CancellationToken cancellationToken = default);
}
```

Notes:

- this is a runtime seam, not the final Proposal D authoring API
- the server remains the source of truth for policy decisions
- existing live-validation logic may stay app-owned initially while policy resolution is extracted first

### 6.3 Relationship to `HrzLiveValidationPatch`

`HrzLiveValidationPatch` remains the model for server-owned validation messages.

Proposal B does **not** replace it.

Instead:

- `HrzLiveValidationPolicy` answers "should live validation run right now?"
- `HrzLiveValidationPatch` answers "what server-owned validation UI should change?"

## 7. DOM Contract

### 7.1 Validation root

The root remains explicit:

```html
<section id="invite-root" data-hrz-validation-root="invite-user">
```

This continues to scope:

- submit validation state
- live validation scope
- field policy updates

### 7.2 Field shell

Fields may still use a stable shell for layout and structure:

```html
<div
  id="invite-email-field"
  data-hrz-field-shell
  data-hrz-field="Email">
  ...
</div>
```

But Proposal B does **not** use the field shell as the default policy patch target.

The field shell exists for:

- stable layout structure
- label, control, and slot grouping
- optional field-level chrome later

The field shell is not the normal transport unit for policy toggles because replacing a visible shell that contains the focused input risks losing:

- focus
- caret position
- typed-but-unsaved DOM state
- per-element JS state

### 7.3 Control metadata

Suggested control shape:

```html
<input
  id="invite-email"
  name="email"
  type="email"
  data-hrz-live-policy-id="invite-email-live"
  data-hrz-local-validation="email"
  data-hrz-client-slot-id="invite-email-client"
  data-hrz-server-slot-id="invite-email-server"
  hx-post="/validation/live"
  hx-trigger="input changed delay:400ms, blur"
  hx-target="#invite-email-server"
  hx-swap="outerHTML"
  hx-include="closest form"
  hx-sync="closest form:abort" />
```

Proposal B intentionally leaves the `hx-*` transport visible in the DOM. Proposal D can later emit those attributes automatically.

### 7.4 Hidden policy carrier

Each live-toggleable control should point at a hidden policy carrier.

Suggested shape:

```html
<div
  id="invite-email-live"
  hidden
  data-hrz-live-enabled="false"
  data-hrz-live-clear-fields="Email,DisplayName"
  data-hrz-live-affects="Email,DisplayName"
  data-hrz-summary-slot-id="invite-summary"
  data-hrz-live-replace-summary-when-disabled="true"
  data-hrz-live-policy-version="3"
  data-hrz-immediate-recheck-when-enabled="true"></div>
```

Responsibilities:

- carry the current server-rendered live policy for one field
- provide a tiny OOB swap target when policy changes
- keep the visible control subtree stable
- give the thin client gate a single place to read live-policy state
- provide the authoritative DOM representation of clear and affect behavior
- provide the authoritative DOM representation of summary-clear behavior

### 7.5 Required metadata

Required for fields using live policy:

- stable control id
- stable policy carrier id
- stable server slot id
- stable client slot id if local validation is enabled
- `data-hrz-live-policy-id` on the control
- `data-hrz-live-enabled` on the carrier
- `data-hrz-live-clear-fields` on the carrier
- `data-hrz-live-affects` on the carrier
- `data-hrz-live-replace-summary-when-disabled` on the carrier

Optional:

- field shell id
- `data-hrz-summary-slot-id` on the carrier
- explicit `hx-include` extras beyond `closest form`
- `data-hrz-live-policy-version` on the carrier
- `data-hrz-immediate-recheck-when-enabled` on the carrier

### 7.6 Server-owned slots remain separate

Client-owned and server-owned validation content must stay separate.

That means:

- local client messages remain in client-owned slots
- live server messages remain in server-owned slots
- submit rerender messages remain part of the submit-time rerender path

Proposal B does not merge those surfaces.

## 8. Request Contract

### 8.1 Default request payload

The default live request should send:

- all fields from the nearest form
- `__hrz_root`
- `__hrz_fields`

Example:

```html
<input
  ...
  hx-include="closest form"
  hx-vals='{"__hrz_root":"invite-user","__hrz_fields":"Email"}' />
```

### 8.2 Why not field-only payloads

Field-only payloads are not the default because the server may need:

- dependency field values
- values that drive whether the live rule is enabled at all
- attempted values for parse failures
- consistency across MVC and Minimal API bindings

This is especially important for intertwined enterprise rules.

### 8.3 Out-of-form dependencies

If a live rule depends on fields outside the form but inside the root, the request must include them explicitly.

Example:

```html
hx-include="#invite-root [name='TenantId'], closest form"
```

This keeps the contract explicit and avoids hidden server assumptions.

### 8.4 Disabled controls

If policy resolution depends on disabled controls, the runtime must mirror those values into included hidden inputs or otherwise provide an explicit includable source of truth.

Reason:

- disabled controls are omitted from form submission
- relying on `closest form` alone will silently drop them
- live-policy resolution must not depend on values that disappear from the request unexpectedly

### 8.5 `__hrz_fields` remains field-scoped intent

Even though the browser sends the nearest form snapshot, the request still remains field-scoped through `__hrz_fields`.

This tells the server:

- which field triggered the live request
- which field or fields should be treated as primary
- which policy and validation rules should run

## 9. Thin Client Gate Contract

### 9.1 Interception point

Proposal B should keep using `htmx:configRequest`.

Reason:

- the current demo already gates live requests there
- it allows cancellation before the network call
- it works for MVC and Minimal API equally

### 9.2 Required gate behavior

For an input participating in local or live validation:

1. run local validation
2. if locally invalid:
   - render client-owned message
   - clear server-owned slots
   - prevent the live request
3. resolve the control's policy carrier
4. if the policy carrier is missing:
   - fail closed
   - prevent the live request
5. if locally valid but the carrier says `data-hrz-live-enabled != "true"`:
   - clear server-owned slots named by policy
   - optionally clear summary
   - prevent the live request
6. otherwise allow the request

### 9.3 Example logic

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

  const policy = resolvePolicyCarrier(input);
  if (!policy) {
    event.preventDefault();
    return;
  }

  if (policy.dataset.hrzLiveEnabled !== 'true') {
    clearServerStateFromPolicy(input);
    event.preventDefault();
    return;
  }
});
```

### 9.4 Immediate recheck rule

When a field's policy carrier transitions from disabled to enabled and the current value is:

- non-empty
- locally valid

the client should schedule one immediate live request for that field.

This avoids requiring the user to type again just because policy changed around the field.

### 9.5 Fail-closed rule

If an input declares `data-hrz-live-policy-id` but the carrier cannot be found, the runtime should block the live request.

Reason:

- missing policy state should not silently become "enabled"
- fail-closed is safer for correctness and easier to debug

### 9.6 Carrier observation

The generic client harness should observe policy-carrier replacements after HTMX swaps settle.

When a carrier replacement is detected, the harness should:

- compare previous and current `data-hrz-live-enabled` values
- if the carrier transitioned from disabled to enabled, apply the immediate recheck rule
- if the carrier transitioned from enabled to disabled, clear any server-owned state named by policy

This should be done without replacing the visible control element.

## 10. Response Contract

### 10.1 Main response target

The primary target for a live request should remain the field's server-owned validation slot.

Reason:

- that is already the current demo behavior
- it avoids replacing the focused input on every live request
- it keeps the main path cheap when only a message changed

### 10.2 OOB policy-carrier updates

When live policy changes, the response should normally include an OOB swap for the field's hidden policy carrier.

Example:

```html
<div
  id="invite-email-live"
  hidden
  hx-swap-oob="outerHTML"
  data-hrz-live-enabled="false"
  data-hrz-live-clear-fields="Email,DisplayName"
  data-hrz-live-affects="Email,DisplayName"
  data-hrz-live-replace-summary-when-disabled="true"
  data-hrz-summary-slot-id="invite-summary"></div>
```

Use policy-carrier OOB swaps when:

- another field toggles the target field on or off
- clear or affects metadata changed
- summary-clear behavior changed
- immediate-recheck behavior changed

### 10.3 OOB field-shell updates

OOB field-shell swaps should be reserved for cases where visible field chrome must change, not as the default policy transport.

Examples:

- a field becomes visibly disabled or enabled
- a field needs a different visible hint, icon, or badge
- a field wrapper class changes for non-validation UX reasons

### 10.4 OOB server slot and summary updates

The response may also include:

- OOB field server slot clears or updates
- OOB summary slot clears or updates

Example:

```html
<div id="invite-email-server" hx-swap-oob="outerHTML"></div>
<div id="invite-summary" hx-swap-oob="outerHTML"></div>
```

### 10.5 Same-field policy changes

If the triggering field changes its own live policy, the default behavior should still be:

- primary response updates the field's server slot
- policy changes for that same field arrive as an OOB update to the hidden policy carrier, not the visible field shell

Reason:

- it preserves the existing target model
- it avoids replacing the focused control subtree by default

This means the response may contain:

- one direct field-slot fragment
- one OOB policy-carrier fragment for the same field

That redundancy is acceptable if the rendered content is consistent and ids remain stable.

### 10.6 Whole-form rerenders are forbidden

Proposal B must not use whole-form rerenders for live-policy changes.

That would:

- break focus and caret stability
- blur the boundary with submit validation
- reintroduce exactly the state churn live validation is supposed to avoid

## 11. Server Processing Model

Suggested live request flow:

1. bind and validate the live scope
2. bind the current form snapshot into the live model
3. resolve current live policy for the primary field
4. resolve any affected field policies if the primary rule can toggle others
5. if the primary field policy is disabled:
   - return clears and any needed OOB policy updates
   - do not run server live validation for that field
6. if the primary field policy is enabled:
   - run server live validation for the requested scope
   - return the primary field slot update
   - include OOB policy-carrier or slot updates for affected fields as needed

### 11.1 Disabled-policy response behavior

When the live rule is disabled, the server should return a normal `200` HTML response that clears relevant server-owned state rather than relying on the client alone.

Reason:

- the browser gate may not reflect the latest server policy yet
- the server response should be authoritative and self-correcting

### 11.2 No-op behavior

`204 No Content` is still valid only when:

- the request is malformed for live validation
- the root or field cannot be resolved
- there is genuinely nothing to patch

It should not be the default response for disabled-policy cases if stale server state needs clearing.

## 12. Request and Response Examples

### 12.1 Enabled field validating itself

Request:

- user types in `Email`
- `Email` is locally valid
- the `invite-email-live` carrier says `data-hrz-live-enabled="true"`
- browser sends `closest form` plus `__hrz_fields="Email"`

Response:

- direct update for `#invite-email-server`
- optional OOB summary update

### 12.2 Another field disables email live validation

Request:

- user changes `DirectoryType`
- server determines `Email` live validation is now disabled

Response:

```html
<div id="invite-directory-type-server">...</div>
<div id="invite-email-live" hidden hx-swap-oob="outerHTML"
     data-hrz-live-enabled="false"
     data-hrz-live-clear-fields="Email,DisplayName"
     data-hrz-live-affects="Email,DisplayName"
     data-hrz-live-replace-summary-when-disabled="true"
     data-hrz-summary-slot-id="invite-summary"></div>
<div id="invite-email-server" hx-swap-oob="outerHTML"></div>
<div id="invite-summary" hx-swap-oob="outerHTML"></div>
```

### 12.3 Another field enables email live validation

Request:

- user changes `DirectoryType`
- server determines `Email` is now live-validatable

Response:

- OOB `outerHTML` swap for `#invite-email-live` with `data-hrz-live-enabled="true"`
- optional OOB server slot clear

Client follow-up:

- if the current email value is non-empty and locally valid, schedule one immediate live request for `Email`

## 13. MVC and Minimal API Compatibility

Proposal B keeps the existing split:

- MVC may bind via controller actions
- Minimal API may bind via `BindLiveValidationScopeAsync` and `BindFormAsync<TModel>`

The new contract is not about route style. It is about:

- rendered live-policy metadata
- client gating behavior
- targeted response composition

That means both stacks should share:

- the same hidden scope values
- the same DOM contract
- the same policy resolution semantics

## 14. Accessibility and State Preservation

- field ids must stay stable
- policy carrier ids must stay stable
- client slot ids and server slot ids must stay stable
- the main live target should remain a server-owned slot
- replacing the focused input should be avoided in the normal path
- OOB policy-carrier swaps should not disturb visible label or described-by relationships

Proposal B should be judged in-browser primarily on:

- focus stability
- caret stability
- correct clearing of stale server messages
- no interference with client-owned local validation

## 15. Testing Requirements

### 15.1 Integration coverage

Add tests for:

- live request binds nearest form snapshot by default
- `__hrz_fields` still scopes validation intent
- disabled live policy prevents server validation from running
- disabled live policy clears server slot and summary slot
- changing one field can OOB-update another field's hidden policy carrier
- enabling a field updates `data-hrz-live-enabled="true"` on the carrier
- out-of-form dependencies are included when configured
- disabled dependency values are mirrored through includable hidden inputs when needed
- missing policy carrier fails closed

### 15.2 End-to-end coverage

Add browser tests for:

- locally invalid input blocks the live request
- locally valid but policy-disabled input blocks the live request
- a dependency change OOB-disables another field
- a dependency change OOB-enables another field
- policy updates do not replace the visible input subtree in the normal path
- enabling a field triggers one immediate recheck when current value is already valid
- live updates never rerender the full form

## 16. Implementation Checklist

### Phase 1: Runtime contracts

- [ ] Add `HrzLiveValidationPolicy` to the shared validation model surface.
- [ ] Add `IHrzLiveValidationPolicyResolver` and service registration.
- [ ] Decide the default framework behavior when no live policy is configured.
- [ ] Keep `HrzLiveValidationPatch` unchanged.

### Phase 2: Demo DOM contract

- [ ] Introduce grouped hidden live-policy carriers per validation root in the demo.
- [ ] Add `data-hrz-live-policy-id` to live-participating controls.
- [ ] Keep visible field shells structural only.
- [ ] Preserve existing server slot ids and summary ids.

### Phase 3: Client harness

- [ ] Replace demo-specific local validation selectors with the generic local-validation registry surface.
- [ ] Add policy-carrier lookup through `data-hrz-live-policy-id`.
- [ ] Implement fail-closed behavior when a referenced carrier is missing.
- [ ] Implement summary and server-slot clearing from carrier metadata.
- [ ] Observe carrier swaps after HTMX settles.
- [ ] Trigger immediate recheck when a carrier transitions from disabled to enabled and the current value is locally valid.

### Phase 4: Server live pipeline

- [ ] Resolve live policy before running server live validation.
- [ ] Return carrier OOB updates when one field toggles another field's live policy.
- [ ] Return slot and summary clears when policy disables a rule.
- [ ] Keep the primary live response targeted at the field's server-owned slot.
- [ ] Avoid whole-form rerenders in all live-policy cases.

### Phase 5: Demo conversion

- [ ] Convert the `/validation` harness to the carrier-based runtime.
- [ ] Demonstrate one field toggling another field's live behavior.
- [ ] Demonstrate disabled-policy clearing behavior.
- [ ] Demonstrate immediate recheck after enable.

### Phase 6: Verification

- [ ] Add rendering and integration coverage for carrier metadata.
- [ ] Add integration coverage for disabled-policy and fail-closed behavior.
- [ ] Add browser coverage for carrier OOB updates and focus stability.
- [ ] Verify no live-policy path replaces the visible control subtree in the normal case.

### Phase 7: Proposal D handoff

- [ ] Update the Phase 6 authoring-surface doc to target carriers instead of field-shell policy swaps.
- [ ] Define the minimum Proposal D wrapper API that emits the Proposal B runtime contract.

## 17. Migration Strategy

### Phase 1

- introduce `HrzLiveValidationPolicy`
- add a policy-resolution seam
- keep current demo live validation logic mostly intact

### Phase 2

- add stable hidden policy carriers for live-toggleable fields
- replace demo-specific local-validation hooks with the generic harness
- render `data-hrz-live-policy-id`, `data-hrz-live-enabled`, and related metadata

### Phase 3

- update the demo so one field can toggle another field's live behavior
- prove OOB policy-carrier updates and stale-state clearing

### Phase 4

- build Proposal D authoring helpers that emit this contract automatically

## 18. Why Proposal D Comes Later

Proposal D should wrap this contract, not be used to discover it.

If D comes first:

- control APIs will churn
- the wrong abstractions may get frozen
- implementation details from the current demo may leak into public components

If B comes first:

- the runtime semantics get proven on the demo
- D can emit a stable contract
- the framework can still choose how opinionated the final authoring surface should be

## 19. Escalation Boundary

Proposal B should handle a large range of complex scenarios:

- cross-field enable and disable rules
- one trigger updating several fields
- tenant-aware live-policy decisions
- targeted OOB policy and validation updates

When to escalate beyond B:

- policy recomputation spans large portions of the root
- toggles depend on many distant fields and multiple async lookups
- the runtime needs to recompute policy even when no specific live-validation field is active

At that point, keep the Proposal D authoring surface but add a Proposal C-style coordinator underneath it.

## 20. Final Recommendation

Implement Proposal B with these commitments:

- nearest form snapshot by default
- field-scoped intent through `__hrz_fields`
- explicit server-rendered live policy metadata
- generic `htmx:configRequest` gate
- stable hidden policy carriers for policy updates
- OOB policy-carrier updates when one field toggles another

Then build Proposal D as the authoring layer that emits this runtime contract automatically.
