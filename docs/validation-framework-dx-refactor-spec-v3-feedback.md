## Review Feedback

This is now **good enough to freeze as the design-direction document**.

It is a real improvement over the earlier drafts. The architecture is now coherent, the DX goal is clear, and the spec is honest about the non-interactive SSR + HTMX runtime instead of smuggling in Blazor lifecycle assumptions.

My read is:

- **Yes**: this is ready to become the parent refactor spec.
- **No**: I still would not treat it as the final API spec without a few targeted edits.

The strongest parts now are:

- the authoring surface is finally primary,
- the runtime primitives stay preserved,
- MVC and Minimal API are treated as one framework,
- `data-val-*` is the local browser contract,
- live validation keeps HTML patch transport,
- backend validation remains server-to-server,
- the first-pass control family is restrained.

That is the right shape.

The remaining issues are smaller now, but they matter.

---

## 1. `HrzField` should fully own field context by default

You now say `HrzField` plus child controls is the primary authoring shape. Good.

If that is the default, then inside `HrzField`, the child components should not need to repeat field targeting unless they are used out of context.

So in the detailed API pass, I would make this the default:

```razor
<HrzField For="() => Invite.Email">
    <HrzLabel />
    <HrzInputText Type="email" />
    <HrzValidationMessage />
</HrzField>
```

And treat these as optional escape hatches only:

```razor
<HrzLabel For="..." />
<HrzValidationMessage For="..." />
```

That is an important DX refinement. If `HrzField` is the primary authoring shape, it should be the thing that removes repetition.

---

## 2. `HtmlIdPrefix` in the descriptor model is the one part I still distrust

This piece is the most likely to age badly:

```csharp
public required string HtmlIdPrefix { get; init; }
```

I think that probably belongs **closer to rendering context than descriptor context**.

### Why

- field identity and validation metadata are descriptor concerns,
- but final DOM ids are often **form-instance scoped**,
- and the same field can appear in more than one form or repeated region.

So I would be careful not to bake final DOM-id assumptions into the descriptor model too early.

A safer direction would be something like:

- descriptor owns canonical path and html name,
- form/field rendering context derives the final id from `FormName + field path`.

That keeps descriptors more reusable.

---

## 3. `HrzInputText Type="email"` is slightly awkward naming

This is not a blocker, but it is a smell.

If the component is meant to cover text-like inputs such as:

- text
- email
- search
- tel
- url
- maybe password

then `HrzInputText` is a bit misleading.

Two reasonable options:

- rename it to `HrzInput`, or
- explicitly document that `HrzInputText` means “text-like input types,” not only `type="text"`.

I would resolve that before the detailed API pass, because naming drift here will be annoying later.

---

## 4. `Enhance` needs an exact definition

This line is still slightly too hand-wavy:

```razor
<HrzForm Model="Invite" Action="/users/invite" FormName="users-invite" Enhance>
```

Because in the wider ecosystem, “enhance” can mean different things.

In HyperRazor’s world, the spec should define exactly what `Enhance` does.

For example, does it mean:

- emit normal `<form>` markup plus HTMX enhancement,
- wire antiforgery automatically,
- enable local validation harness hookup,
- enable live validation metadata emission,
- or some subset of the above?

I would add one explicit definition so this does not become a fuzzy “does framework stuff” flag.

---

## 5. Collection/indexed field paths need to be called out explicitly

The spec is much tighter now, but there is one practical area that still needs to be named as a design risk:

- nested objects
- collections
- indexed field names

Examples like:

```csharp
() => Invite.Addresses[0].Street
```

are where path resolution, html names, attempted-value replay, local validation metadata, and live-validation targeting all get more fragile.

You do not need to solve that in this design-direction doc, but I would add one sentence in the open questions or API pass section making it explicit that:

> nested and indexed field identity must be validated in the detailed API pass before the authoring model is considered complete.

That is one of the places these systems usually crack.

---

## 6. The helper contract is now good, but I would make one thing even more explicit

This section is strong now:

- local invalid state
- backend-mapped invalid state
- merged invalid state

That is exactly right.

I would add just one more explicit expectation:

> helper overloads may accept `ModelState` or `ValidationProblemDetails`, but normalization to `HrzSubmitValidationState` should happen before render-time authoring components consume the result.

That would reinforce the layering and stop helper APIs from leaking too much transport detail upward.

---

## 7. Summary defaults are finally sane

This part is much better now:

- full submit rerender owns summary,
- live patch can update summary only when requested,
- client-only local field errors stay out of summary by default.

I think that is the correct v1 default. I would keep it exactly as written.

---

## Bottom line

I would move forward with this spec.

If I were editing it before freezing, I would make only these final changes:

- make `HrzField` the true default field-context owner,
- reconsider whether `HtmlIdPrefix` belongs in the descriptor layer,
- resolve `HrzInputText` naming,
- define `Enhance` precisely,
- call out nested/indexed field identity as a required detailed API pass item,
- reinforce that normalized submit state is the render-time common denominator.

After that, I think it is ready to hand off into the detailed API spec.