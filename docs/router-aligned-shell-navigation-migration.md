# HyperRazor - Layout Hint Navigation Migration

## Slogan

- layouts are authored like Blazor
- routing decides the page
- HTMX optimizes within the current layout
- changing layouts swaps the app root when it can

## Why This Change

The old promotion model solved a real problem, but it carried too much framework ceremony:

- the page layout is resolved from `@layout` or `_Imports.razor`
- the page response emits a hidden current-layout marker
- client JavaScript reads that marker and sends `X-Hrz-Current-Layout`
- the server compares the current layout to the target page's layout
- the response becomes either a same-layout fragment, a one-request root swap, or `HX-Location` fallback

That is the smallest useful client/server handshake for layout-aware HTMX navigation. It drops layout families, route inference, and configurable promotion behavior without giving up automatic cross-layout switching.

## Target Mental Model

HyperRazor should feel close to Blazor's routing model:

1. ASP.NET Core endpoint routing resolves the request to a page component.
2. The page component resolves its layout using normal Blazor rules.
3. Page responses emit the current layout as an inert DOM marker.
4. HTMX decides only how to transport the navigation:
   - same layout -> optimized partial page response
   - different layout with a usable current-layout hint -> swap `#hrz-app-shell`
   - different layout without a usable current-layout hint -> normal navigation via `HX-Location`
5. Explicit fragment endpoints stay explicit fragment endpoints.

The framework should not guess the current layout from routes or ask page authors to annotate transitions.

## Core Runtime Model

The stable thing to cache is only component layout resolution:

- component type -> resolved layout type

Suggested internal shape:

```csharp
internal static class HrzLayoutKey
{
    public static string Create(Type? layoutType);
}
```

At request time, the runtime should:

- resolve the target page's layout from the target component type
- read the current layout from `X-Hrz-Current-Layout`
- compare them to choose fragment vs root swap vs `HX-Location`

If the current layout hint is missing or unusable, prefer normal navigation.

## Recommended Navigation Rules

- boosted `GET` to a page with the same current and target layout -> return a page fragment for the page outlet
- boosted `GET` to a page with a different current and target layout -> return the destination app root and swap `#hrz-app-shell`
- boosted `GET` with missing or invalid current layout -> return `HX-Location`
- non-HTMX request -> return the full page
- explicit fragment endpoint -> return the fragment only
- history restore -> return the full page

## What We Remove

- `X-Hrz-Layout-Family`
- `data-hrz-layout-family`
- layout-family client JavaScript behavior
- `HrzLayoutFamilyAttribute` as a first-class authoring concept
- `IHrzLayoutFamilyResolver`
- `HrzLayoutBoundaryOptions`
- legacy shell-swap promotion behavior
- route-manifest page navigation
- MVC page-component metadata used only for navigation

## Migration Story

### Phase 1 - Introduce layout-key navigation

Add a small internal layout-key helper and a fixed internal request header.

Goals:

- keep `@layout` as the only authoring surface
- resolve target layout from the actual target component type
- read current layout from the page already on screen
- require no new route metadata

### Phase 2 - Emit the current-layout marker

Update the page renderer so `View<T>` responses emit a hidden marker such as:

```html
<template data-hrz-current-layout="..."></template>
```

Page responses include it. Fragment responses do not.

### Phase 3 - Switch page navigation policy

Use the current-layout header and the resolved target layout to decide whether the response stays partial or becomes navigation:

- `X-Hrz-Current-Layout` -> current layout
- target component -> target layout
- same layout -> page fragment
- different layout with a valid current-layout hint -> root swap using `HX-Retarget: #hrz-app-shell` and `HX-Reswap: outerHTML`
- different layout without a valid current-layout hint -> `HX-Location`

### Phase 4 - Delete legacy route inference

Remove any route-manifest or page-metadata navigation logic and keep only:

- component type -> layout cache
- current-layout marker
- fixed internal current-layout header
