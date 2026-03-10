# HyperRazor Authoring Surface DX Spec

**Date:** 2026-03-10  
**Status:** Draft replacement  
**Scope:** Proposal D authoring surface on top of the current Proposal B runtime  
**Primary audience:** framework design and implementation planning

## 0. Summary

HyperRazor now has a working Proposal B runtime baseline:

- root-scoped submit validation state
- targeted live validation patches
- server-rendered live-validation policy
- hidden policy carriers
- a thin `htmx:configRequest` gate
- MVC and Minimal API parity for the live pipeline

The weak point is now clearly authoring DX, not runtime mechanics.

Current application code still hand-wires too much:

- field paths
- `name` and `id` values
- attempted-value replay
- `aria-invalid`
- `aria-describedby`
- server slot ids
- client slot ids
- live policy ids
- `hx-*` live validation attributes
- client-validation metadata

Proposal D should not redesign the runtime again. It should wrap the runtime that already exists.

The intended public direction is:

```razor
<HrzForm Model="Invite" Action="/users/invite">
    <HrzValidationSummary />

    <HrzField For="() => Invite.DisplayName">
        <HrzLabel />
        <HrzInputText />
        <HrzValidationMessage />
    </HrzField>

    <HrzField For="() => Invite.Email">
        <HrzLabel />
        <HrzInputText Type="email" />
        <HrzValidationMessage />
    </HrzField>
</HrzForm>
```

This document defines a full authoring-surface spec that is grounded in the codebase as it exists today.

## 1. Current Baseline In Code

### 1.1 Runtime already implemented

Current code already has these runtime pieces:

- `HrzSubmitValidationState`
- `HrzLiveValidationPatch`
- `HrzLiveValidationPolicy`
- `IHrzLiveValidationPolicyResolver`
- carrier-based live-policy rendering in the demo
- `hyperrazor.validation.js` as the current generic client harness
- server live-policy resolution and OOB carrier updates in the demo live endpoint

### 1.2 Current pain point

The current demo proves the runtime, but it also shows the DX problem clearly:

- the form component still resolves policy manually
- fields still manually wire ids and slots
- each field still manually renders `hx-post`, `hx-target`, `hx-vals`, and related metadata
- client and server message surfaces are still conceptually first-class in the app markup instead of being framework-owned

### 1.3 Design implication

Proposal D should be treated as:

- an authoring-layer replacement
- a render-contract emitter
- a reduction in application markup responsibility

Proposal D should not be treated as:

- a new live-validation runtime
- a new transport model
- a new business-rule system

## 2. Goals

- make validation authoring expression-based
- make `HrzForm` and `HrzField` the primary authoring posture
- keep the browser contract HTML-first
- keep MVC and Minimal API on the same validation runtime
- preserve attempted-value replay and server rerender flows
- keep local client validation and server live validation composable
- hide low-level `data-hrz-*` and `hx-*` details from normal application markup
- let Proposal D emit the current Proposal B contract instead of redefining it

## 3. Non-Goals

- do not replace the Proposal B runtime contract
- do not replace `HrzSubmitValidationState`, `HrzLiveValidationPatch`, or `HrzLiveValidationPolicy`
- do not switch the live path to JSON responses
- do not implement endpoint-helper ergonomics in the same phase as the field authoring layer
- do not solve the full input catalog in v1
- do not migrate to `data-val-*` in v1 just because older drafts mentioned it
- do not make layout/grid ownership a framework concern

## 4. Frozen Runtime Dependencies

Proposal D must emit and respect the current runtime decisions from `docs/validation-framework-live-policy-spec.md`.

That means the authoring surface must preserve:

- explicit validation roots
- nearest-form live request snapshots by default
- field-scoped live intent through `__hrz_fields`
- separate client-owned and server-owned validation surfaces
- hidden grouped live-policy carriers
- thin client gating only
- targeted field and summary updates only
- policy resolution through `IHrzLiveValidationPolicyResolver`

Proposal D owns authoring DX.  
Proposal B remains the runtime.

## 5. Public API Direction

### 5.1 Target feel

The long-term primary posture should be:

```razor
<HrzForm Model="Invite" Action="/users/invite" FormName="users-invite">
    <HrzValidationSummary />

    <HrzField For="() => Invite.DisplayName">
        <HrzLabel />
        <HrzInputText />
        <HrzValidationMessage />
    </HrzField>

    <HrzField For="() => Invite.Email">
        <HrzLabel />
        <HrzInputText Type="email" />
        <HrzValidationMessage />
    </HrzField>
</HrzForm>
```

For live validation, v1 will still need explicit configuration somewhere.  
The most realistic shape based on current runtime is:

```razor
<HrzForm Model="Invite"
         Action="/users/invite"
         FormName="users-invite"
         LiveValidationPath="/validation/live"
         EnableClientValidation="true">
    <HrzValidationSummary />

    <HrzField For="() => Invite.DisplayName">
        <HrzLabel />
        <HrzInputText />
        <HrzValidationMessage />
    </HrzField>

    <HrzField For="() => Invite.Email">
        <HrzLabel />
        <HrzInputText Type="email" />
        <HrzValidationMessage />
    </HrzField>
</HrzForm>
```

Important interpretation:

- form-level validation settings are defaults for the root
- field-level settings should be able to override those defaults
- the form should not be the only place where live/client behavior can be tuned

### 5.2 Public component set for v1

Proposal D v1 should define these primary components:

- `HrzForm`
- `HrzField`
- `HrzLabel`
- `HrzInputText`
- `HrzValidationMessage`
- `HrzValidationSummary`

These are enough to prove the authoring model before expanding into:

- `HrzInputNumber`
- `HrzTextArea`
- `HrzSelect`
- `HrzCheckbox`
- richer custom-validator metadata

## 6. Component Contracts

### 6.1 `HrzForm`

`HrzForm` is the root authoring component.

Responsibilities:

- own the model instance
- own the validation root identity
- emit the form tag
- emit antiforgery
- read current submit validation state
- own summary identity
- own form-level HTMX submit behavior
- own grouped hidden live-policy rendering
- provide cascading form context to descendant field components

Suggested public parameters:

- `Model`
- `Action`
- `FormName` or `RootId`
- `HxPost` optional override
- `EnableClientValidation` default for fields, default `true`
- `LiveValidationPath` default for fields
- `LiveTrigger` default for fields, defaulting to the current runtime trigger
- `LiveInclude` default for fields, defaulting to `closest form`
- `AdditionalAttributes`

Important v1 rule:

- `HrzForm` must remain explicit about root identity
- implicit root generation is a later convenience, not a v1 requirement
- form-level validation settings are defaults, not hard global rules

### 6.2 `HrzField`

`HrzField` is the field-scoped authoring container.

Responsibilities:

- accept a `For` expression
- resolve the canonical field path
- derive field-specific ids and slot ids
- determine current attempted value and server error state
- provide cascading field context to label, input, and message components
- register field live-participation with the form when live validation is enabled
- render a stable field shell by default

Suggested public parameters:

- `For`
- `Class`
- `EnableClientValidation` optional override
- `Live` optional override
- `LiveValidationPath` optional override
- `LiveTrigger` optional override
- `LiveInclude` optional override
- `Label` optional explicit text override
- `AdditionalAttributes`

Important v1 rule:

- `HrzField` owns field semantics, not page layout
- it may render a default wrapper, but it must not become a grid/layout system
- `HrzField` should be able to opt out of live validation or override form-level defaults when needed

### 6.3 `HrzLabel`

`HrzLabel` consumes `HrzField` context.

Responsibilities:

- render `for` pointing at the field input id
- choose display text
- default to display metadata or a split property name when explicit text is absent

Suggested public parameters:

- `Text` optional override
- `AdditionalAttributes`

### 6.4 `HrzInputText`

`HrzInputText` consumes `HrzField` context and emits the current runtime contract.

Responsibilities:

- render `name`
- render stable `id`
- render attempted or current value
- render `aria-invalid`
- render `aria-describedby`
- render current local-validation metadata
- render current live-validation metadata and `hx-*` attributes when live is enabled
- remain compatible with the current client harness and hidden carrier model

Suggested public parameters:

- `Type` default `"text"`
- `Placeholder`
- `Autocomplete`
- `InputMode`
- `Class`
- `AdditionalAttributes`

Important v1 rule:

- `HrzInputText` does not invent a second field contract
- it emits the current Proposal B runtime metadata

### 6.5 `HrzValidationMessage`

`HrzValidationMessage` is the singular public message component even though the runtime still separates client-owned and server-owned surfaces internally.

Responsibilities:

- consume field context
- render the field message region for the current field
- internally preserve separate client and server slots if needed
- hide slot-id management from application code

Suggested public parameters:

- no `For` required when inside `HrzField`
- `For` optional only for standalone usage later
- `Class`
- `AdditionalAttributes`

Important v1 rule:

- public API is one message component
- internal DOM may still use two owned regions

### 6.6 `HrzValidationSummary`

`HrzValidationSummary` is the public summary component for the current form.

Responsibilities:

- consume form context
- render the server-owned summary region
- hide summary id generation from application code
- participate in live summary updates through the current Proposal B contract

Suggested public parameters:

- `Class`
- `AdditionalAttributes`

## 7. Internal Authoring Model

Proposal D needs internal context types even if they are not all public.

### 7.1 `HrzFormContext`

This should carry at least:

- model instance
- resolved root id
- form id
- form action and submit config
- summary id
- current submit validation state
- client-validation enablement
- live-validation enablement
- live-validation path and trigger config
- a registrar for live-participating fields

### 7.2 `HrzFieldContext`

This should carry at least:

- `For` expression
- canonical field path
- derived input `name`
- derived input id
- client slot id
- server slot id
- live policy id
- current value or attempted value
- current field errors
- current invalid state
- label text default

### 7.3 Field registration and carrier rendering

The form should own grouped policy-carrier rendering.

That means:

- fields register themselves with the form during render
- the form resolves initial policy state for those fields
- the form renders the grouped hidden policy region after the visible form body
- individual fields do not manually render their own carriers

This keeps the public field markup clean and keeps live-policy emission centralized.

### 7.4 Transitional internal helpers

It is valid for the framework to use internal field-binding helpers before the full component stack is complete.

That means:

- an internal expression-based field binding layer is acceptable
- it is not itself the final public Proposal D API
- it should be treated as infrastructure for `HrzField` and related components

## 8. Rendering Contract Emitted By The Authoring Surface

Proposal D should hide the low-level runtime contract from the app, not change it.

### 8.1 Form root contract

`HrzForm` should emit:

- the `<form>` tag
- submit `hx-post`/target/swap metadata when enhancement is enabled
- antiforgery input
- root-level DOM marker such as `data-hrz-validation-root`

### 8.2 Field contract

`HrzField` plus `HrzInputText` should emit:

- field path metadata
- input name
- stable id
- `aria-invalid`
- `aria-describedby`
- client slot linkage
- server slot linkage
- summary linkage
- live policy linkage when live is enabled

Application code should not write those directly in the golden path.

### 8.3 Message contract

`HrzValidationMessage` should emit the current runtime message structure:

- client-owned region for local validation
- server-owned region for live and submit validation

But that split should stay internal to the framework-generated markup.

### 8.4 Policy region contract

When live validation is enabled for the form:

- `HrzForm` renders one hidden policy region for the root
- one stable carrier is emitted per participating field
- carrier ids remain stable
- carrier markup remains compatible with the current thin gate

## 9. Validation Behavior In Proposal D

### 9.1 Submit validation

Submit behavior remains root-scoped:

- endpoints still bind and validate the posted model
- invalid submit rerenders still use `HrzSubmitValidationState`
- the authoring surface reads that state and renders attempted values and server errors

Proposal D does not change submit semantics.

### 9.2 Local client validation

Proposal D v1 should keep the current client harness posture.

That means:

- the framework emits the current local-validation metadata that `hyperrazor.validation.js` understands
- application code no longer hand-wires those attributes

It does **not** mean:

- v1 must already migrate to `data-val-*`

`data-val-*` can remain a later enhancement after the authoring surface stabilizes.

### 9.3 Live validation

Proposal D must emit the current live-validation transport contract:

- live path when configured
- `hx-trigger`
- `hx-target`
- `hx-swap`
- `hx-include="closest form"` by default
- `hx-vals` carrying `__hrz_root` and `__hrz_fields`
- `hx-disinherit="hx-disabled-elt"` where needed to avoid submit-only inheritance bugs

Configuration rule:

- `HrzForm` provides root-wide defaults for live validation
- `HrzField` may override participation, endpoint, trigger, or include behavior
- if a field has no effective live-validation path after defaults and overrides are applied, it does not participate in live validation

### 9.4 Policy-aware live enablement

Proposal D does not decide live policy in the browser.

It still relies on:

- `IHrzLiveValidationPolicyResolver`
- server-rendered carriers
- the existing thin client gate

The authoring surface only makes that contract automatic.

## 10. MVC And Minimal API Implications

Proposal D authoring components should work regardless of whether the invalid HTML is rendered through:

- MVC controllers
- Minimal API handlers
- backend problem-details mapping followed by HTML rerender

That means:

- the authoring surface must not be MVC-specific
- the same rendered contract must work for both invalid submit rerenders and live updates
- endpoint-helper ergonomics remain separate work from the component authoring layer

## 11. V1 Scope

Proposal D v1 should include:

- `HrzForm`
- `HrzField`
- `HrzLabel`
- `HrzInputText`
- `HrzValidationMessage`
- `HrzValidationSummary`
- grouped hidden carrier emission through the form
- form and field contexts internal to the authoring layer

Proposal D v1 should also require:

- explicit root identity
- explicit live-validation path when live validation is enabled
- compatibility with the current client harness

## 12. V1 Non-Goals

Proposal D v1 should not require:

- a full `data-val-*` migration
- a full input component catalog
- endpoint binder/helper redesign
- custom layout primitives
- automatic live-endpoint inference
- elimination of all low-level escape hatches

## 13. Migration Strategy

### Phase 1: internal scaffolding

Build:

- form context
- field context
- field registration
- carrier-region ownership

Goal:

- make the component stack possible without changing runtime semantics

### Phase 2: core public components

Build:

- `HrzForm`
- `HrzField`
- `HrzLabel`
- `HrzInputText`
- `HrzValidationMessage`
- `HrzValidationSummary`

Goal:

- rewrite the demo invite form using the new surface

### Phase 3: runtime parity proof

Prove:

- submit invalid rerender still works
- live validation still works
- policy toggles still work
- local invalid states still block live requests
- focus and carrier behavior remain stable

### Phase 4: surface expansion

Add:

- more input components
- more validation metadata adapters
- optional standalone `For` support for message components

## 14. Open Questions

These need answers before implementation starts:

1. Should `LiveValidationPath` remain a simple parameter pair on `HrzForm` and `HrzField`, or should live settings be grouped into an options object?
2. Should `HrzValidationMessage` outside `HrzField` be supported in v1, or should that wait until after the basic field surface lands?
3. How should field-level live opt-out be expressed: `Live="false"`, `ParticipatesInLiveValidation="false"`, or another API?
4. Should `HrzField` always render a wrapper element, or should wrapper rendering be optional for advanced scenarios?
5. How much of the current local-validation metadata should remain HyperRazor-specific in v1 before a later `data-val-*` move?
6. Should `EnableClientValidation` also be a form-default-plus-field-override setting, or should field-level local validation be inferred entirely from metadata and field type?

## 15. Recommendation

Proceed with Proposal D only after locking these decisions:

- `HrzForm` owns root identity, summary identity, and carrier-region rendering
- `HrzField` owns field identity and cascades field context
- `HrzLabel`, `HrzInputText`, and `HrzValidationMessage` consume that field context
- v1 targets the current Proposal B runtime instead of trying to redesign it
- v1 keeps current client-validation metadata and current live-policy runtime
- broader input coverage and `data-val-*` migration come later

This gives HyperRazor a realistic path to the desired authoring surface without reopening the runtime work that is already now in place.
