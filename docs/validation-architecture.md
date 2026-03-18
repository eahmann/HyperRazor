# HyperRazor Validation Architecture

**Date:** 2026-03-12  
**Status:** Current implementation note

This document is the shortest path to understanding the validation subsystem as it exists today.
Use it as the canonical map for the runtime.
Use the larger validation specs when you need design history or future direction.

## Purpose

The validation code spans the three shipped libraries plus the demo sample:

- component authoring and render-time metadata
- submit-time state transport
- Minimal API and MVC binding helpers
- server-side model evaluation
- browser-side gating and field-state updates
- demo-only policies and response composition

This note names those layers, points to the files that own them, and shows the live-validation flow end to end. Shared validation contracts import from `HyperRazor.Components.Validation`, and the runtime implementation APIs live under `src/HyperRazor/`.

## Layers

### 1. Authoring layer

This layer emits the DOM contract that the rest of the runtime consumes.
It supports two authoring lanes that share the same runtime model:

- component-first authoring through `HrzForm`, `HrzField`, and `HrzInput*`
- advanced/raw authoring by resolving `IHrzForms` and then working with `HrzFormScope` and `HrzFieldScope`

Primary files:

- `src/HyperRazor.Components/HrzForm.razor`
- `src/HyperRazor.Components/HrzField.razor`
- `src/HyperRazor.Components/HrzInputComponentBase.cs`
- `src/HyperRazor.Components/Validation/IHrzForms.cs`
- `src/HyperRazor.Components/Validation/HrzFormScope.cs`
- `src/HyperRazor.Components/Validation/HrzFieldScope.cs`
- `src/HyperRazor.Components/HrzValidationMessage.razor`
- `src/HyperRazor.Components/HrzValidationSummary.razor`
- `src/HyperRazor.Components/HrzValidationLivePolicyCarrier.razor`
- `src/HyperRazor.Components/HrzValidationLivePolicyRegion.razor`

Responsibilities:

- establish the validation root
- resolve field paths from expressions
- share form and field metadata between stock components and custom markup/components
- emit input `name` and `id` values
- emit client and server validation slot ids
- emit hidden live-policy carriers
- emit `hx-*` and `data-hrz-*` attributes for live validation

### 2. Transport and state layer

This layer carries validation state between server render paths and the browser contract.

Primary files:

- `src/HyperRazor.Components/Validation/HrzValidationRootId.cs`
- `src/HyperRazor.Components/Validation/HrzFieldPath.cs`
- `src/HyperRazor.Components/Validation/HrzFieldPaths.cs`
- `src/HyperRazor.Components/Validation/HrzAttemptedValue.cs`
- `src/HyperRazor.Components/Validation/HrzAttemptedValues.cs`
- `src/HyperRazor.Components/Validation/HrzValidationModels.cs`
- `src/HyperRazor.Components/Validation/HrzValidationHttpContextExtensions.cs`
- `src/HyperRazor.Components/Validation/HrzSubmitValidationStateExtensions.cs`

Core types:

- `HrzSubmitValidationState`
- `HrzLiveValidationPatch`
- `HrzLiveValidationPolicy`
- `HrzValidationScope`

Responsibilities:

- preserve attempted values across invalid submits
- carry field and summary errors through server rerenders
- represent field-scoped live validation intent
- represent server-owned live-policy decisions

### 3. Server evaluation layer

This layer binds requests, resolves field paths, converts framework validation into HyperRazor state, and evaluates server-owned rules.

Primary files:

- `src/HyperRazor/Mvc/HrController.cs`
- `src/HyperRazor/Mvc/HrzMinimalApiFormExtensions.cs`
- `src/HyperRazor/Rendering/HrzValidationBridge.cs`
- `src/HyperRazor/Rendering/HrzDataAnnotationsModelValidator.cs`
- `src/HyperRazor/Rendering/Validation/HrzFieldPathResolver.cs`
- `src/HyperRazor/Rendering/Validation/HrzDataAnnotationsClientValidationMetadataProvider.cs`
- `src/HyperRazor.Components/Validation/IHrzLiveValidationPolicyResolver.cs`
- `src/HyperRazor/Rendering/Validation/HrzDefaultLiveValidationPolicyResolver.cs`

Responsibilities:

- convert MVC `ModelState` into `HrzSubmitValidationState`
- bind and validate Minimal API form posts
- bind live-validation scope from `__hrz_root`, `__hrz_fields`, and `__hrz_validate_all`
- evaluate `DataAnnotations` and `IValidatableObject`
- map field paths back to `EditContext` field identifiers
- expose live-policy resolution as a server seam

### 4. Client runtime layer

This layer is the browser-side harness in `src/HyperRazor.Components/wwwroot/hyperrazor.validation.js`.

Internal modules in that file:

- `validationDom`
- `fieldStateRuntime`
- `formRequestRuntime`
- `localValidationRuntime`
- `livePolicyRuntime`
- `validationEvents`

Responsibilities:

- keep field `aria-invalid` and shell state in sync
- host the local-validation adapter lifecycle
- gate HTMX live requests when local validation fails
- gate HTMX live requests when the server-rendered live policy disables them
- clear stale server-owned slots and summaries when policy or local validity changes
- disable and restore configured form targets around HTMX requests

Public browser hooks:

- `window.hyperRazorValidation.setLocalValidationAdapterFactory(...)`
- `window.hyperRazorValidation.registerClientValidator(...)`
- `window.hyperRazorValidation.refreshLocalValidation(...)`
- `window.hyperRazorValidation.getLocalValidationAdapter()`
- `window.hyperRazorValidation.getLocalValidationAdapterName()`
- `window.hyperRazorValidation.getRegisteredClientValidators()`

### 5. Demo and example layer

This layer proves the framework runtime but also contains app-specific policy and response composition.

Primary files:

- `samples/HyperRazor.Demo.Mvc/Program.cs`
- `samples/HyperRazor.Demo.Mvc/Controllers/ValidationController.cs`
- `samples/HyperRazor.Demo.Mvc/Infrastructure/UserInviteValidationResponses.cs`
- `samples/HyperRazor.Demo.Mvc/Infrastructure/MixedValidationResponses.cs`
- `samples/HyperRazor.Demo.Mvc/Infrastructure/DemoValidationLivePolicyResolver.cs`
- `samples/HyperRazor.Demo.Mvc/Models/UserInviteValidationDefinitions.cs`
- `samples/HyperRazor.Demo.Mvc/Models/MixedValidationDefinitions.cs`

Responsibilities:

- host the `/validation` demo endpoints
- compose demo-specific validation forms and response fragments
- define demo-specific live-policy rules
- demonstrate MVC, Minimal API, proxy, and mixed-authoring flows

## End-to-End Flow

### Submit validation

1. The authoring layer emits a validation root, field paths, slot ids, and attempted-value-friendly `name` attributes.
2. MVC or Minimal API binding reads the form post and builds `HrzSubmitValidationState`.
3. Server validation adds summary and field errors keyed by `HrzFieldPath`.
4. The response rerenders the form with attempted values preserved.
5. The browser runtime recomputes field invalid state from the rerendered DOM.

### Live validation

1. The authoring layer emits `hx-*` live request metadata and hidden carriers that describe the current live-policy state for a field.
2. The client runtime listens to `input` and `change` events, runs local validation, and updates client-owned and server-owned slot state.
3. On `htmx:configRequest`, the client runtime blocks the request when:
   - local validation fails, or
   - the server-rendered live policy is currently disabled.
4. When a live request is allowed, the server binds `HrzValidationScope` from `__hrz_root` and `__hrz_fields`, then evaluates live policy and server-owned validation messages.
5. The response updates only server-owned field slots, summary slots, and hidden live-policy carriers.
6. On `htmx:afterSettle`, the client runtime rescans carrier state, clears stale server feedback when a policy turns off, optionally triggers an immediate recheck when a policy turns on, and recomputes field invalid UI.

## Where To Change Behavior

- Change shared field-path helpers in `src/HyperRazor.Components/Validation/HrzFieldPaths.cs` and the DI-backed resolver implementation in `src/HyperRazor/Rendering/Validation/HrzFieldPathResolver.cs`.
- Change Minimal API form binding or live-scope binding in `src/HyperRazor/Mvc/HrzMinimalApiFormExtensions.cs`.
- Change server-side `DataAnnotations` evaluation in `src/HyperRazor/Rendering/HrzDataAnnotationsModelValidator.cs`.
- Change MVC-to-HyperRazor submit-state capture in `src/HyperRazor/Mvc/HrController.cs`.
- Change Blazor `EditContext` message projection in `src/HyperRazor/Rendering/HrzValidationBridge.cs`.
- Change browser gating, local adapter behavior, or form disable/restore behavior in `src/HyperRazor.Components/wwwroot/hyperrazor.validation.js`.
- Change demo-only policies or examples in `samples/HyperRazor.Demo.Mvc/`.

## Relationship To Other Docs

Use this file for the current implementation map.

Use these files when you need deeper background:

- `docs/validation-framework-live-policy-spec.md` for the live-policy runtime contract
- `docs/validation-framework-dx-refactor-spec.md` for the intended authoring-surface direction
- `docs/validation-framework-phase-6-client-harness.md` for the client-harness design rationale
