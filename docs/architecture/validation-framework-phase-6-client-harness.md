# HyperRazor Validation Framework — Phase 6 Client Validation Harness

**Date:** 2026-03-07  
**Status:** Proposed  
**Depends on:** `docs/architecture/validation-framework-spec-greenfield.md`, `docs/archive/validation/validation-framework-phase-4-live-validation.md`, `docs/architecture/validation-framework-phase-6-authoring-surface.md`

---

## 0) Goal

Define a generic browser-side validation harness that HyperRazor controls can hook into through stable `data-*` attributes.

This phase is not about choosing a validation library. It is about defining the contract that lets:

- plain HTML controls opt into local validation
- client-owned validation slots update immediately
- live server validation defer when local validation already failed
- stale server-owned validation slots clear when local input becomes invalid

---

## 1) Problem

The current `/validation` demo has working client-side validation, but the harness is hard-coded to demo-specific selectors:

- `data-hrz-local-email`
- `data-hrz-local-display-name`

That is enough to prove the runtime, but it is not reusable.

What we need instead:

- one generic hook for “this control participates in local validation”
- one generic way to name the local validation rule
- one generic way to locate the client/server/summary slots
- one generic rule for when live server validation should be blocked

---

## 2) Core Decision

The harness should be:

- DOM-driven
- attribute-based
- control-level
- library-agnostic

The control tells the harness what rule to run. The harness does not special-case “email field” or “display name field.”

---

## 3) Non-Goals

- do not choose Alpine, Zod, Vest, or any other validation library here
- do not move layout ownership into the framework
- do not redesign server live-validation transport
- do not replace native browser validation
- do not require a schema engine in v1

---

## 4) DOM Contract

### 4.1 Required control hooks

Each locally-validated control should be able to emit:

```html
data-hrz-local-validation="email"
data-hrz-client-slot-id="invite-email-client"
```

Optional hooks:

```html
data-hrz-server-slot-id="invite-email-server"
data-hrz-summary-slot-id="invite-summary"
data-hrz-dependent-server-slot-ids="invite-display-name-server,invite-role-server"
data-hrz-live-mode="block-when-local-invalid"
```

### 4.2 Meaning

- `data-hrz-local-validation`
  Declares the local validation rule name.

- `data-hrz-client-slot-id`
  The element ID where the harness renders client-owned messages.

- `data-hrz-server-slot-id`
  The server-owned field-validation slot tied to the control.

- `data-hrz-summary-slot-id`
  The server-owned summary slot tied to the form root.

- `data-hrz-dependent-server-slot-ids`
  Comma-separated server-owned slot IDs that should also be cleared when the control becomes locally invalid.

- `data-hrz-live-mode`
  Controls what happens to live server validation when local validation fails.

V1 supported value:

- `block-when-local-invalid`

If omitted, the default should be `block-when-local-invalid`.

---

## 5) Rule Model

### 5.1 Rule names

The harness should dispatch by rule name, not by element ID or field name.

Examples:

- `email`
- `required`
- `min-length`
- `none`

### 5.2 Rule inputs

Each rule receives:

- the control element
- its current value
- any rule options from `data-*`

### 5.3 Rule output

Each rule returns a small result object:

```ts
type HrzLocalValidationResult = {
  valid: boolean;
  message?: string;
};
```

That is enough for v1.

No severity levels, multiple messages, or async local rules are needed in the first pass.

---

## 6) Rule Options

Some rules need parameters. The harness should support rule options through additional `data-*` attributes.

Examples:

```html
data-hrz-local-validation="min-length"
data-hrz-local-min-length="3"
```

```html
data-hrz-local-validation="required"
```

```html
data-hrz-local-validation="email"
```

The naming rule should be:

- rule selector: `data-hrz-local-validation="<rule-name>"`
- rule-specific options: `data-hrz-local-<option-name>`

V1 does not need nested config blobs or JSON payloads for local validation options.

---

## 7) Harness Behavior

### 7.1 Input/change handling

The harness should listen on the document and react to controls carrying `data-hrz-local-validation`.

At minimum:

- `input`
- `change`

Optional:

- `blur`

### 7.2 On local invalid

When a control becomes locally invalid:

1. render the client-owned message into `data-hrz-client-slot-id`
2. clear the control’s server-owned slot, if configured
3. clear dependent server-owned slots, if configured
4. clear the server-owned summary slot, if configured
5. block live server validation for that control

### 7.3 On local valid

When a control becomes locally valid:

1. clear the client-owned message
2. allow live server validation to proceed
3. do not clear server-owned state unless the control previously became invalid in the same interaction

---

## 8) Live Validation Integration

The client harness should not own live server validation. It should only gate it.

The rule is:

- local validation runs first
- if local validation fails, do not send the live request
- if local validation passes, allow the existing HTMX live request to run

This preserves the current architecture:

- local rules are immediate
- server rules remain server-owned

---

## 9) Suggested First-Pass Rule Set

Start small:

- `required`
- `email`
- `min-length`

That is enough to replace the current demo-specific logic without overdesigning the client harness.

`email` may use native browser validity under the hood. That is acceptable.

---

## 10) JavaScript Surface

V1 should expose a tiny registry:

```ts
type HrzLocalValidator = (
  element: HTMLElement,
  value: string
) => HrzLocalValidationResult;

interface HrzClientValidationRegistry {
  register(name: string, validator: HrzLocalValidator): void;
  validate(element: HTMLElement): HrzLocalValidationResult | null;
}
```

This allows:

- built-in validators shipped by HyperRazor
- later integration with Alpine or another JS layer
- app-specific validators without editing the framework script

---

## 11) Example

### 11.1 Markup

```razor
<label for="invite-email">Email</label>
<HrzInputText
    Id="invite-email"
    Name="email"
    Type="email"
    Path="EmailPath"
    RootId="Form.RootId"
    Value="Form.Input.Email"
    ValidationState="ValidationState"
    ClientSlotId="invite-email-client"
    ServerSlotId="invite-email-server"
    SummarySlotId="invite-summary"
    LiveValidationPath="Form.LiveValidationPath"
    AdditionalAttributes="@(new Dictionary<string, object?>
    {
        ["data-hrz-local-validation"] = "email"
    })" />

<div id="invite-email-client" data-hrz-client-validation-for="Email"></div>
<div id="invite-email-server" data-hrz-server-validation-for="Email"></div>
```

### 11.2 Harness behavior

- on invalid email format:
  - client slot gets the local message
  - live HTMX request is blocked
  - server slot and summary clear

- on valid email format:
  - client slot clears
  - live HTMX request is allowed

---

## 12) Implementation Plan

### 12.1 First pass

Replace the current demo-local hard-coded logic in:

- [hyperrazor.validation.js](/src/HyperRazor.Client/wwwroot/hyperrazor.validation.js)

with a generic rule registry and generic `data-*` selectors.

### 12.2 First consumers

Migrate:

- the validation harness email field
- the validation harness display-name field

from hard-coded markers to:

- `data-hrz-local-validation="email"`
- `data-hrz-local-validation="min-length"`

### 12.3 Later work

If the harness holds up, move it out of demo-local JS into a framework-owned client asset.

---

## 13) Test Surface

Add coverage for:

- local invalid writes the client slot
- local invalid clears server slot and summary
- local invalid blocks live HTMX request
- local valid clears the client slot
- rule dispatch is based on `data-hrz-local-validation`, not field ID

Likely homes:

- `tests/HyperRazor.E2E`
- integration coverage where request blocking can be observed

---

## 14) Exit Criteria

- the client harness no longer hard-codes `email` and `display name` as special field selectors
- controls opt in through `data-hrz-local-validation`
- the rule registry supports at least `required`, `email`, and `min-length`
- live server validation is blocked when local validation already fails
- client-owned and server-owned validation slots remain separate

---

## 15) Open Questions

- Should the rule registry live on `window.HyperRazorValidation`, `window.HyperRazor.ClientValidation`, or a different namespace?
- Should `min-length` use a generic `data-hrz-local-min-length` option, or should all rules receive a single option bag later?
- Should the harness clear server summary immediately on local invalid, or only clear the field-specific server slot by default?
