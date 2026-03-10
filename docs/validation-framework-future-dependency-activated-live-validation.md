# HyperRazor Validation Framework — Dependency-Activated Live Validation

**Date:** 2026-03-09  
**Status:** Implemented baseline behavior  
**Related:** `docs/validation-framework-detailed-api-spec.md`, `docs/validation-framework-phase-4-live-validation.md`

## Goal

Reduce unnecessary live-validation requests by allowing the server to activate or deactivate field-level HTMX live validation based on current form state.

## Current Behavior

Fields can render dormant live-validation metadata and only gain `hx-*` transport when the server activates them.

That means:

- a field can start local-only even when it already knows its live endpoint and dependencies
- the server can patch a hidden `--live-state` slot to activate or deactivate that field later
- the client harness mirrors the active state into the actual `hx-*` attributes in place
- live requests stay transport-scoped to the triggering field, reserved HyperRazor live fields, and any declared dependent fields

Example:

- `Email` always has live validation
- `DisplayName` starts dormant
- when `Email == "shared-mailbox@example.com"`, the server activates `DisplayName`
- when the email changes away from that case, the server deactivates `DisplayName` again

## Implemented Contract

The current contract is:

- inputs keep their live request shape in `data-hrz-live-*`
- `data-hrz-live-active="true|false"` is the client-visible current state
- `HrzValidationMessage` renders a hidden `--live-state` slot for each live-capable field
- live responses can patch those slots OOB without replacing the field control
- the browser harness updates `hx-post`, `hx-trigger`, `hx-target`, `hx-swap`, `hx-include`, and `hx-vals` when the live state changes

## Constraints

Any future implementation should preserve the current live-validation principles:

- no whole-form rerender during field-level live validation
- targeted field-slot and summary-slot patching only
- client-local validation remains separate from server-live validation
- activation changes must not wipe existing local or server validation UI unexpectedly

## Remaining Future Work

The current baseline still leaves room for follow-up work:

- move more authoring paths from manual live metadata to descriptor-backed live rules
- define a higher-level server helper for producing activation-state patches
- tighten diagnostics around malformed live-state contracts in dev mode
