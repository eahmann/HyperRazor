# HyperRazor HTMX Conventions

## CI-H1: Return HTML fragments from HTMX endpoints
Use server-rendered HTML fragments and HTMX response headers instead of client-side JSON rendering.

## CI-H2: Vary caching by `HX-Request`
When an endpoint can render full page or fragment, include `Vary: HX-Request`.

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
