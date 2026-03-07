# Live Email Feedback Note

**Date:** 2026-03-06  
**Status:** Discussion note

## Idea

Add live feedback for the email field on the validation demo form by wiring HTMX directly to the email input with a debounce.

Suggested shape:

- trigger from the email input with `hx-post` or `hx-get`
- use `hx-trigger="input changed delay:400ms, blur"`
- target a small email-status region beside the field
- keep the main form submit on `/demos/validation`

## Recommendation

Do **not** validate the whole model on every keystroke.

Instead:

1. Keep the current `/demos/validation` POST as the authoritative whole-form validation path.
2. Add a separate email-check endpoint for live feedback.
3. Validate only email-scoped rules in that endpoint:
   - required / empty behavior if desired
   - email format
   - server-only uniqueness rule
4. Return a tiny fragment for the email field status/message.

## Why

Whole-model validation on every input event creates the wrong failure surface:

- untouched required fields start producing noise
- the server has to validate and then suppress unrelated errors
- the UI becomes harder to reason about because a field-level check is pretending to be a submit

The cleaner split is:

- field-level endpoint for live advisory feedback
- whole-model submit for final authoritative validation

## Reuse Strategy

If duplication becomes a concern, extract a shared validator service instead of running full-model validation and filtering the result.

That service can support both:

- `ValidateAll(CreateUserInput input)`
- `ValidateEmail(string? email, ...)`

If email rules later depend on another field, include only that dependency in the HTMX request with `hx-include`.

## Response Semantics

For the live field check, prefer `200 OK` or `204 No Content` over `422`.

Reason:

- a debounced field check is advisory feedback, not a failed form submit
- reserve `422` for actual submit-time validation failures on the main form POST

## Fit With Current 5.1 Work

This fits the current MVC-first validation design:

- whole-form submit remains in [ValidationDemoController.cs](/home/eric/repos/HyperRazor/src/HyperRazor.Demo.Mvc/Controllers/ValidationDemoController.cs)
- the form authoring surface remains in [ValidationDemoForm.razor](/home/eric/repos/HyperRazor/src/HyperRazor.Demo.Mvc/Components/Fragments/ValidationDemoForm.razor)
- the server-only uniqueness rule already exists in the form submit path and could move into a reusable validator service

## Bottom Line

The likely right implementation is:

- HTMX on the email input with debounce
- a dedicated email-validation endpoint
- field-scoped validation logic
- shared validator extraction only if reuse pressure is real
- whole-model validation only on submit
