# HyperRazor Validation Framework — Future: Dependency-Activated Live Validation

**Date:** 2026-03-09  
**Status:** Deferred future work  
**Related:** `docs/validation-framework-detailed-api-spec.md`, `docs/validation-framework-phase-4-live-validation.md`

## Goal

Reduce unnecessary live-validation requests by allowing the server to activate or deactivate field-level HTMX live validation based on current form state.

## Current v1 Behavior

In v1, fields that declare a live rule emit their `hx-*` live-validation metadata eagerly.

That means:

- a field can post live-validation requests even before a backend-owned dependency rule is currently relevant
- dependent-field behavior is resolved on the server after the request arrives
- this keeps the model simple and predictable, but it is not selective

Example:

- `Email` always has live validation
- `DisplayName` also has live validation
- when `Email == "shared-mailbox@example.com"`, the server begins treating `DisplayName` as backend-relevant
- before that point, `DisplayName` may still post live requests even though the server usually has nothing meaningful to add

## Desired Future Behavior

Allow the server to declare when a dependent field should begin or stop participating in live server validation.

Target outcome:

- `Email` can remain live-active from the start
- `DisplayName` can remain local-only until the server determines that the shared-mailbox rule is active
- when the dependency becomes active, the client begins live-posting `DisplayName`
- when the dependency is no longer active, the client drops `DisplayName` back to local-only

## Why This Is Deferred

This requires more than a small HTMX tweak.

The framework needs an activation contract for:

- how the server declares that a field is now live-active or live-inactive
- how the client updates field-level live metadata without replacing whole field markup
- how that activation state survives targeted and OOB validation updates
- how to avoid losing local validation state, focus, or attempted values during activation changes

## Constraints

Any future implementation should preserve the current live-validation principles:

- no whole-form rerender during field-level live validation
- targeted field-slot and summary-slot patching only
- client-local validation remains separate from server-live validation
- activation changes must not wipe existing local or server validation UI unexpectedly

## Possible Direction

One viable direction is to let the server return activation metadata for dependent fields alongside the existing validation slot patches, and let the client harness update live-validation behavior in place.

That would need explicit answers for:

- where activation metadata lives in the DOM
- whether HTMX attributes are patched directly or mirrored through HyperRazor metadata
- how activation interacts with debounce, blur, and dependent-field OOB updates

## Acceptance Criteria For A Future Pass

- dependent fields do not send live-validation requests until the server says they are active
- dependent fields stop sending live-validation requests when the dependency is no longer active
- existing targeted/OOB validation patching still works
- no whole-field or whole-form replacement is required just to toggle live activation
- browser and integration tests cover activation, deactivation, and state preservation
