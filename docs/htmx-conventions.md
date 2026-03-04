# HyperRazor HTMX Conventions

## CI-H1: Return HTML fragments from HTMX endpoints
Use server-rendered HTML fragments and HTMX response headers instead of client-side JSON rendering.

## CI-H2: Vary caching by `HX-Request`
When an endpoint can render full page or fragment, include `Vary: HX-Request`.

## CI-H3: Disable history restore as HX request
Default `historyRestoreAsHxRequest` to `false` for safer full-page restoration behavior.

## CI-H4: Keep stable target IDs
Use stable swap targets (`#main`, `#panel`, `#search-results`) to keep fragments reusable.
