# Prefix Strategy RFC (`Hrz` vs `Htmx`)

Status: Accepted  
Date: 2026-03-05  
Scope: Public API, component naming, CSS/data attributes, custom headers

## 1) Decision

- Framework/UI-owned concepts use `Hrz*` / `hrz-*`.
- HTMX protocol concepts use `Htmx*`.
- `Hrx*` / `hrx-*` have been removed from the repository (no alias layer).

## 2) Why

- `Hrz` aligns with product branding (`HyperRaZor`) for framework surface area.
- `Htmx*` remains precise for request/response protocol semantics.
- This split keeps future UI components brand-native while preserving HTMX clarity.

## 3) Taxonomy

### Keep `Htmx*` (protocol-level)

- `HtmxRequest`
- `HtmxResponse`
- `HtmxHeaderNames`
- `HtmxConfig`
- `HtmxResponseWriter`
- `HtmxRequestAttribute` / `HtmxResponseAttribute`

### Use `Hrz*` (framework-level)

- `HrzApp`
- `HrzLayout`
- `HrzComponentHost`
- `HrzResults`
- `HrzHeadService`
- `HrzSwapService`
- `HrzSwappable`
- `HrzSwapContent`
- `HrzLayoutFamilyAttribute`
- `HrzShellContext`

### DOM/contracts

- `#hrz-app-shell`
- `#hrz-main-layout`
- `data-hrz-*`
- `X-Hrz-Layout-Family`
- `hrz-antiforgery`

## 4) UI component direction

- New UI components should be `Hrz*` (for example `HrzButton`, `HrzCard`, `HrzFormField`).
- HTMX support should be capability-based (attrs/helpers), not encoded into component names.

## 5) Guardrails

- Do not introduce new `Hrx*` or `hrx-*` identifiers.
- For new protocol features, prefer `Htmx*`.
- For framework abstractions and UI, prefer `Hrz*`.
