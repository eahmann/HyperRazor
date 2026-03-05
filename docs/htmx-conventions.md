# HyperRazor HTMX Conventions

## CI-H1: Return HTML fragments from HTMX endpoints
Use server-rendered HTML fragments and HTMX response headers instead of client-side JSON rendering.

## CI-H2: Vary caching by request-shaping headers
When an endpoint can branch between full page and fragment, include:
- `Vary: HX-Request`
- `Vary: HX-Request-Type`
- `Vary: HX-History-Restore-Request`

## CI-H3: Disable history restore as HX request
Default `historyRestoreAsHxRequest` to `false` for safer full-page restoration behavior.

## CI-H4: Keep stable target IDs
Use stable swap targets (`#main`, `#panel`, `#search-results`) to keep fragments reusable.

## CI-H5: Default nested OOB swaps to explicit behavior
Set `allowNestedOobSwaps` intentionally in your HTMX config when reusable fragments can contain OOB blocks.
- Recommended baseline for HyperRazor demos: `allowNestedOobSwaps=false`
- Keep OOB blocks adjacent to the main fragment response where possible.

## CI-H6: Default validation swaps to `200` for demos
For baseline demo UX, return `200` with inline validation errors to avoid non-2xx swap handling complexity.
- Teams that require strict `422` semantics can opt into HTMX `responseHandling` rules.

## CI-H7: Antiforgery must be automatic for unsafe HTMX methods
Use a predictable token source and transport:
- emit a token meta tag (`hrx-antiforgery`)
- attach `RequestVerificationToken` on HTMX requests
- include hidden antiforgery inputs in POST forms as a fallback

## CI-H8: Scope head-support to explicit flows
Do not force `head-support` globally unless you want head processing on every request.
- Keep the script available
- opt in per flow (`hx-ext="head-support"`) for head/title updates

## CI-H9: Standardize status handling behavior
For HTMX 2 demos and docs, use `responseHandling` config for 4xx/5xx swap behavior.
- Keep 200-default validation behavior
- keep 422 as an explicit opt-in variant
