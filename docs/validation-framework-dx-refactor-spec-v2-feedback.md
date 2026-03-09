## Review Feedback

This revision is **materially better**.

It fixes the three biggest problems in the earlier draft:

1. it no longer hard-centers MVC metadata as the literal core model,
2. it no longer implies `RemoteAttribute` wire compatibility,
3. it no longer overcommits to a specific browser validation library.

So as a **direction/spec-of-intent**, I think this is good.

I would approve it as the parent design document.

What I would **not** do yet is treat it as implementation-ready. A few important things are still underspecified.

---

## What is now strong

The center of gravity is finally right:

- runtime primitives stay in place,
- authoring surface becomes primary,
- browser contract stays HTML-first,
- MVC and Minimal API are treated as one framework,
- backend/API validation remains server-to-server and is mapped back into HTML.

That is the right architecture for HyperRazor.

The revised constraints are also much healthier:

- normalized HyperRazor descriptors as the internal model,
- `For="..."` explicitly framed as inference rather than interactive lifecycle,
- `FormName` as the public root concept,
- `data-val-*` as the local browser contract,
- live validation as HTML patching rather than JSON validation.

That is coherent.

---

## What still needs to be nailed down

### 1. The primary authoring shape is still slightly ambiguous

The spec still leaves this open:

- `HrzField` + child controls
- standalone `HrzInput* For=...`
- or both

That should not stay open for long, because it affects everything else.

### Recommendation

Make **`HrzField` + child controls** the documented default.

Let standalone `HrzInput* For=...` be a later convenience or advanced path.

### Why

`HrzField` gives the framework one place to own:

- field context
- generated ids
- `aria-describedby`
- invalid class hooks
- label/message association
- live-validation targeting metadata

That is how you actually reduce author markup.

If both shapes become first-class too early, the framework will end up with two parallel authoring models.

---

### 2. `HrzForm Model="..."` + `HrzField For="..."` still needs an explicit non-interactive runtime explanation

The spec says expressions are for inference, not interactive Blazor semantics. Good.

But it still needs one concrete explanation of **how the inference works** in a non-interactive SSR pipeline.

Right now the implied mechanism is something like:

- `HrzForm` cascades a form/model context,
- `HrzField` consumes `For="..."`,
- the framework resolves:
  - path
  - name
  - id
  - display metadata
  - model getter
  - attempted value lookup
  - descriptor lookup

That needs to be stated explicitly.

Otherwise people will project normal `EditContext` expectations onto it.

### Recommendation

Add a short subsection like this:

> `HrzForm` provides a request-scoped form context containing the model instance, form identity, validation state, and descriptor graph.  
> `HrzField For="..."` resolves metadata and value access against that context without requiring interactive Blazor lifecycle or `EditContext` ownership.

That one paragraph would prevent a lot of confusion.

---

### 3. The first-pass control list is still a little aggressive

`HrzCheckbox` is more defensible than `Select` or `Number`, but it still has tricky semantics:

- checked vs unchecked transport
- hidden false companion input behavior
- attempted-value replay rules
- nullable bool / tri-state edge cases

It can still be first-pass, but only if you explicitly define its HTML output and precedence rules.

### Recommendation

Either:

- keep `HrzCheckbox` in first pass **and define its semantics explicitly**, or
- move it to phase 1.1 instead of phase 1.0.

If you keep it, add a sentence like:

> `HrzCheckbox` is a special-case first-pass control and must define hidden-false transport semantics explicitly.

---

### 4. The descriptor model needs one concrete minimum shape

You correctly say HyperRazor should own a normalized descriptor model.

That is right, but the doc still stops one step too early.

It would help to define the **minimum** required concepts now, even if the final API names change.

For example:

```csharp
public sealed class HrzValidationDescriptor
{
    public required Type ModelType { get; init; }
    public required IReadOnlyList<HrzFieldDescriptor> Fields { get; init; }
}

public sealed class HrzFieldDescriptor
{
    public required string Path { get; init; }
    public string? DisplayName { get; init; }

    public IReadOnlyDictionary<string, string> LocalRules { get; init; }
        = new Dictionary<string, string>();

    public HrzLiveRuleDescriptor? LiveRule { get; init; }
}
```

That would make the architecture much more concrete without overcommitting.

---

### 5. The spec should say more clearly what local client validation is guaranteed to support

Right now the document is directionally correct, but slightly optimistic.

If local client validation is driven by `data-val-*`, then v1 local validation support is only guaranteed for rules that can be emitted into the chosen browser metadata contract.

### Recommendation

Say this explicitly:

- DataAnnotations-backed rules are the default local-client path.
- Other validator sources may participate in server validation immediately.
- They only participate in local browser validation if an adapter emits compatible metadata.

That matters especially for future FluentValidation integration.

Otherwise the spec reads a little like “all validator sources can become local client rules,” which is not automatically true.

---

### 6. Summary behavior needs one explicit rule

This is subtle but important.

You say `HrzValidationSummary` should render:

- submit-time server errors
- live-validation server errors
- client-side local errors

That is fine as a goal, but the merge behavior must be stated.

At minimum the spec should define:

- whether client-only errors appear in summary by default,
- whether live-validation patches can update summary independently,
- whether a full invalid rerender replaces summary state entirely,
- whether local client invalid state blocks live server requests and summary patching.

Without that, summary behavior will become inconsistent fast.

---

### 7. MVC and Minimal API helper signatures need one more level of realism

The helper examples are good directionally, but they still hide one important case:

- invalid due to local bind/validation,
- invalid due to downstream API/application validation,
- invalid due to merged errors from both.

The eventual helper surface needs to support all three without awkward branching.

### Recommendation

Strengthen that section with an explicit requirement:

> MVC and Minimal API invalid-render helpers must accept either locally-derived validation state, backend-mapped validation state, or a merged result.

That will keep the helper design honest.

---

## One wording change I would make

This sentence is close, but I would sharpen it:

> “This becomes the source of truth for authoring and DOM emission.”

I would change it to:

> “This becomes the normalized internal source of truth for field identity, validation metadata, and DOM emission.”

That makes it clearer that request-specific validation state still lives in the runtime layer, not the descriptor layer.

Descriptors define the **shape of the field and rules**.  
Submit/live state defines the **current validation result**.

That distinction is important.

---

## Bottom line

This revised spec is now **good enough to adopt as the design-direction document**.

The main remaining work is not conceptual anymore. It is **API clarification**:

- choose one primary authoring shape,
- define the non-interactive form/field context more explicitly,
- pin down the minimum descriptor model,
- clarify local-client support limits,
- define summary merge behavior,
- tighten the invalid-render helper contract.

So the state of this draft is:

- **architecturally strong**
- **directionally ready**
- **not yet the final API spec**

That is a good place to be.