## Review Feedback

This is the first proposal in this thread that really moves toward Rizzy-level DX instead of only hiding a few attributes.

### Overall assessment

**Yes, this is the right direction.**  
**No, I would not adopt it exactly as written.**

The strongest improvement in this draft is that it finally makes the **authoring layer primary**:

- keep the existing submit/live runtime primitives
- stop exposing those primitives as the app authoring model
- move to expression-first field binding
- make MVC and Minimal API converge on one invalid-rerender pipeline
- keep the browser contract HTML-first

That is the correct center of gravity for HyperRazor.

---

## What this proposal gets right

### 1. It preserves the runtime investment

The existing runtime primitives still matter and should remain:

- `HrzSubmitValidationState`
- `HrzLiveValidationPatch`
- attempted-value replay
- canonical field paths
- backend `ValidationProblemDetails` mapping
- full-root submit vs targeted live patch split

Those are the engine.

The proposal is right that they should **stop being the primary app authoring model**.

### 2. It moves to an authoring surface that can actually compete on DX

This is the first proposal that clearly aims for:

- `HrzForm`
- `HrzField`
- `HrzValidationMessage`
- `HrzValidationSummary`
- expression-based binding via `For="..."`

That is the right move if the goal is to get closer to:

- ASP.NET MVC tag helper ergonomics
- Blazor `ValidationMessage` mental model
- Rizzy-style form authoring

### 3. It keeps the browser contract HTML-first

That part is still correct.

Live validation should remain:

- HTMX-driven
- HTML patch based
- field-targeted
- summary-aware
- non-JSON for the browser contract

That is a strong part of HyperRazor’s identity and should not change.

### 4. It recognizes that MVC and Minimal API should feel like one framework

That is also correct.

The invalid-rerender story should be one semantic pipeline with multiple endpoint adapters, not two separate validation systems.

---

## The biggest changes I would make

## 1. Do **not** make raw ASP.NET validation metadata the literal source of truth

I agree that ASP.NET validation metadata is the best **default source** and the best **default integration target**.

I do **not** think it should become the literal source of truth for HyperRazor.

### Why

If HyperRazor makes MVC-flavored validation metadata the actual core model, it risks over-coupling to:

- MVC model metadata infrastructure
- DataAnnotations assumptions
- the exact conventions of ASP.NET unobtrusive validation

That is too narrow for the required path matrix, especially because HyperRazor must also support:

- downstream API/application-layer validation
- Minimal API parity
- alternative validator sources such as FluentValidation

### Recommendation

Use a normalized HyperRazor validation metadata model internally.

ASP.NET validation metadata should be the **primary adapter into that model**, not the model itself.

A better framing is:

- HyperRazor owns a normalized validation descriptor model
- DataAnnotations / ASP.NET metadata populate it by default
- other adapters can populate it later

That keeps the public authoring surface stable even if metadata sources evolve.

---

## 2. Treat `RemoteAttribute` as **semantic inspiration**, not transport compatibility

The proposal is right to look toward ASP.NET remote validation semantics:

- dependent fields
- endpoint selection
- validation timing
- remote rule declaration

But HyperRazor’s live validation transport is **not** classic ASP.NET remote validation.

### Why

Classic ASP.NET remote validation is fundamentally:

- client request
- JSON-ish response (`true`, `false`, or message)
- client-side message update

HyperRazor live validation wants:

- client request
- targeted HTML fragment response
- server-owned field/summary patching

That is a different transport contract.

### Recommendation

Keep the semantics aligned with `RemoteAttribute`, but do not imply wire compatibility.

The spec should say something closer to:

> HyperRazor live validation should support RemoteAttribute-like semantics
> (endpoint selection, dependent fields, timing options),
> while retaining an HTML-patch transport contract.

That is much safer and more accurate.

---

## 3. Do **not** lock the framework contract to one browser validation library yet

The move toward `data-val-*` is good.

That is the correct direction because it:

- aligns with ASP.NET validation conventions
- avoids inventing a second HyperRazor-only rule vocabulary
- keeps custom validator integration familiar
- works better across MVC and component rendering

However, I would not make a specific client library the framework contract yet.

### Recommendation

Make the browser-facing contract:

- standard `data-val-*` metadata for local validation
- HyperRazor metadata for live-validation coordination

Then treat the browser library as:

- the default adapter
- the reference implementation
- or an official companion package

But not the hardcoded framework contract.

That gives HyperRazor room to evolve the default browser adapter later without changing the authoring model.

---

## What I would trim in the first authoring-surface pass

I agree with:

- `HrzForm`
- `HrzField`
- `HrzLabel`
- `HrzValidationMessage`
- `HrzValidationSummary`

I would be more conservative about the first wave of input components.

### Recommended first pass

Ship only the controls needed to prove the model:

- `HrzInputText`
- `HrzTextArea`
- `HrzCheckbox`
- maybe `HrzSelect`

### Defer for later

Defer the more complex control types until the metadata/emission/runtime model is proven:

- `HrzInputNumber`
- radio groups
- file inputs
- date/time inputs
- other specialized controls

The goal of the first pass is to prove the **form/field/message model**, not to solve every input quirk immediately.

---

## Important guardrail: expressions must not imply interactive Blazor semantics

The proposal’s expression-first direction is good, but it needs one explicit guardrail:

> `For="..."` exists for metadata inference.
> It does **not** imply interactive Blazor submit or validation lifecycle.

That means `For="..."` should be used to derive:

- canonical field path
- `name`
- `id`
- display metadata
- validation metadata
- live-validation targeting

It should **not** imply:

- interactive Blazor events
- `OnValidSubmit` as the primary submission model
- `EditContext` as the source of truth
- websocket/circuit-based validation lifecycle

This distinction needs to be explicit in the spec so the authoring model stays honest to HyperRazor’s actual runtime.

---

## Public root concept

I like the move toward a public `FormName` concept.

That is a good public abstraction because developers already recognize the idea of named forms.

### Recommendation

Use:

- `FormName` as the primary public concept
- map it internally to `HrzValidationRootId`
- allow explicit override when needed

That gives the framework an explicit, stable root identity without exposing low-level validation root plumbing in normal app code.

---

## Suggested refined direction

If this proposal moves forward, I would refine it into this position:

### Keep

- authoring layer as the primary public surface
- expression-first binding
- HTML-first invalid rerendering
- one semantic runtime for MVC and Minimal API
- backend API validation mapped back into HTML
- `data-val-*` as the preferred local-validation metadata contract

### Change

1. ASP.NET validation metadata is the **primary adapter**, not the literal source of truth.
2. `RemoteAttribute` is **semantic inspiration**, not transport compatibility.
3. the browser validation library is a **reference/default adapter**, not the framework contract.
4. the initial input-component surface should be smaller than the full proposed family.
5. the spec should explicitly state that `For="..."` is for inference, not interactive Blazor semantics.

---

## Bottom line

This is the best proposal so far **if the goal is Rizzy-like DX without copying Rizzy’s riskiest couplings**.

It is the right refactor direction because it finally does the important thing:

- preserve the runtime primitives
- replace the public authoring model

That said, it should be tightened before adoption:

- normalize validation metadata internally instead of hard-centering MVC metadata
- borrow Remote-style semantics without borrowing JSON transport assumptions
- standardize on `data-val-*` without hard-binding the whole framework to one client library
- prove the field/form/message model before shipping a large control family
- make it explicit that expression-based authoring does not imply interactive Blazor