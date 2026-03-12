# HyperRazor SSE Support Spec

**Date:** 2026-03-10  
**Status:** Draft  
**Scope:** Add first-party Server-Sent Events support for HTML-over-the-wire live updates in HyperRazor.

> This is a separate transport from Blazor `[StreamRendering]` and from WebSockets. The goal here is one-way server push that stays HTMX-first and HTML-first.

## Summary

Add a small, explicit SSE story to HyperRazor:

- vendor the official HTMX SSE extension in `HyperRazor.Client`
- build on ASP.NET Core's built-in SSE results instead of inventing a parallel transport contract
- add a HyperRazor rendering helper that turns components/fragments into SSE message payloads
- support OOB markup inside SSE messages
- keep head updates out of scope for v1 SSE messages
- document HTMX 2 and HTMX 4 markup separately while keeping the server contract the same

This gives HyperRazor a real server-push transport without introducing a WebSocket abstraction, JSON client rendering, or a background job framework.

## Problem Statement

Current HyperRazor transport is request/response oriented:

- normal page/fragment rendering via `HrController` and `HrzResults`
- HTMX headers for navigation and status behavior
- OOB swaps for multi-region updates from a single response
- optional head updates through `head-support`

That is enough for forms, navigation, and targeted fragment reloads, but it does not cover the cases where the server should push updates without polling:

- job/progress feeds
- live operational dashboards
- notification streams
- incremental background task output

The repo already has the key pieces needed to make SSE fit cleanly:

- `HrzComponentHost` can render fragment responses with OOB payloads
- `IHrzSwapService` can queue OOB content
- `HyperRazor.Client` already vendors HTMX-related scripts
- the current client baseline is HTMX 2, with HTMX 4 compatibility work already underway

The missing piece is a first-party contract for:

- writing SSE frames correctly
- rendering HyperRazor HTML into those frames
- documenting the HTMX client markup that consumes the stream

## Goals

- Keep the transport HTML-based, not JSON-based.
- Reuse the existing HyperRazor rendering pipeline wherever possible.
- Make SSE usable from both MVC and Minimal API endpoints.
- Support OOB content inside SSE messages so one event can update multiple regions.
- Stay aligned with the current HTMX 2 baseline without painting the design into an HTMX-2-only corner.
- Keep the public surface small and composable.
- Prove the behavior with integration tests and one browser-level demo flow.

## Non-Goals

- Do not introduce a WebSocket abstraction or SignalR-style hub layer.
- Do not build a distributed fanout/pub-sub system.
- Do not make background job orchestration part of this feature.
- Do not make JSON payloads or client templates the primary path.
- Do not make head/title updates over SSE part of v1.
- Do not make named SSE events the primary HTML swap path.
- Do not require a global app-wide SSE connection.
- Do not define durable replay semantics beyond exposing standard SSE IDs and allowing apps to inspect `Last-Event-ID`.

## Baseline In Repo

Current repo areas this design should build on:

- `src/HyperRazor.Components/Layouts/HrzAppLayout.razor`
- `src/HyperRazor.Demo.Mvc/Components/Layouts/AppLayout.razor`
- `src/HyperRazor.Client/wwwroot/hyperrazor.htmx.js`
- `src/HyperRazor.Components/HrzComponentHost.razor`
- `src/HyperRazor.Components/Services/IHrzSwapService.cs`
- `src/HyperRazor.Components/Services/HrzSwapService.cs`
- `src/HyperRazor.Rendering/HrzComponentViewService.cs`
- `src/HyperRazor.Mvc/HrzResults.cs`
- `src/HyperRazor.Demo.Mvc/Program.cs`

Important baseline constraints:

- HTMX 2 is the current demo/default client profile.
- `head-support` is already vendored and loaded from `HyperRazor.Client`.
- `IHrzSwapService` is scoped per request and already appends OOB content only when rendering HTMX-oriented output.
- `HrzComponentHost` currently includes `HrzHeadContent` and `HrzSwapContent` when rendering an HTMX fragment.
- HTMX 2 SSE uses the browser `EventSource` API, which does not let the client attach arbitrary request headers.

SSE support should extend this model, not introduce a second rendering stack.

## Key Design Decisions

### D1. Use the official HTMX SSE extension, not custom EventSource glue

For the current HTMX 2 baseline, HyperRazor should vendor the official HTMX SSE extension script in `HyperRazor.Client`, alongside the existing HTMX core and `head-support` assets.

Recommended asset:

- `src/HyperRazor.Client/wwwroot/vendor/htmx/sse.js`

Recommended layout wiring:

- add the script to `HrzAppLayout.razor`
- add the script to the demo `AppLayout.razor`

No new custom browser glue should be required for the baseline. HyperRazor already benefits from letting HTMX own the client-side transport behavior.

### D2. Keep connection setup explicit in markup

HyperRazor should not auto-open SSE connections from global config or body-level JavaScript.

Connection setup should stay explicit in the component markup that owns the live region.

For HTMX 2, the expected pattern is:

```html
<section hx-ext="sse" sse-connect="/operations/live" sse-close="done">
    <div id="operations-feed" sse-swap="message">
        Waiting for updates...
    </div>
</section>
```

For HTMX 4, the expected pattern is different and should be documented separately:

```html
<div id="operations-feed"
     hx-sse:connect="/operations/live"
     hx-sse:close="done">
    Waiting for updates...
</div>
```

HyperRazor docs should show both variants and state clearly which one maps to which client profile.

### D3. Make unnamed `message` events the primary HTML swap contract

The primary server contract should be:

- SSE messages without an `event:` field carry HTML
- the client swaps that HTML into the target region

This is the safest cross-version contract:

- HTMX 2 uses `sse-swap="message"` for the unnamed/default event
- HTMX 4 also swaps unnamed messages as HTML content
- named events diverge more across versions and are better treated as an advanced option

Do not make the primary HyperRazor API depend on named-event swapping.

Named events should be reserved for:

- graceful close signals such as `done`
- explicit client-side hooks
- follow-up request triggers when a flow really needs them

### D4. Treat SSE payloads as HTMX fragments

Each SSE message payload should be rendered as HTML fragment content, not as a full page.

That means the SSE rendering path should:

- render the component/fragments in partial mode
- allow OOB payloads to be appended to the same message
- suppress head output for v1 SSE messages

Why suppress head output:

- SSE messages are long-lived fragment updates, not navigations
- head/title mutation in the middle of a stream is harder to reason about
- this avoids awkward interaction with `head-support` until there is a concrete need and browser proof

Why allow OOB:

- it fits the existing HyperRazor model
- it lets one server event update a primary live region and a secondary badge/toast/inspector region
- it avoids inventing a separate multi-target SSE mechanism

### D5. Reuse the existing rendering pipeline instead of duplicating it

SSE rendering should reuse the same core host path as normal HyperRazor partial rendering.

Recommended implementation direction:

- factor the existing `HrzComponentViewService` partial rendering path so it can render to an SSE message payload
- keep `HrzComponentHost` as the core wrapper
- add an SSE-specific render mode that:
  - renders the component body
  - includes `HrzSwapContent`
  - skips `HrzHeadContent`

This render mode must be explicit. It must not infer "this is an SSE render" from HTMX request headers, because that breaks the HTMX 2 path where the connection is created with `EventSource` and does not carry arbitrary HTMX headers.

The key point is architectural, not naming-specific: do not create a second unrelated component-to-HTML pipeline just for SSE.

### D6. Use ASP.NET Core's platform SSE primitives as the foundation

HyperRazor needs two layers:

1. Platform SSE results and items for the transport.
2. A higher-level HyperRazor renderer that can turn components into SSE items.

Recommended shape:

Low-level transport contract:

```csharp
IAsyncEnumerable<SseItem<string>>
TypedResults.ServerSentEvents(...)
Results.ServerSentEvents(...)
```

Higher-level renderer integration in `HyperRazor.Rendering`:

```csharp
public interface IHrzSseRenderer
{
    Task<SseItem<string>> RenderComponent<TComponent>(
        object? data = null,
        string? eventType = null,
        string? id = null,
        TimeSpan? retryAfter = null,
        CancellationToken cancellationToken = default)
        where TComponent : IComponent;

    Task<SseItem<string>> RenderFragments(
        string? eventType = null,
        string? id = null,
        TimeSpan? retryAfter = null,
        CancellationToken cancellationToken = default,
        params RenderFragment[] fragments);
}
```

A small HyperRazor helper is still reasonable if it only adds behavior the platform result does not own cleanly, such as:

- heartbeat comment support
- default proxy-buffering headers
- a convenience API for blank-data close events

Do not ship a brand-new `SseMessage` plus `SseResults` abstraction as the primary foundation when the platform already has `SseItem<T>` and `ServerSentEvents(...)`.

Microsoft's documented SSE support works in both Minimal APIs and controller-based apps, which matches HyperRazor's existing endpoint story.

If HyperRazor ever multi-targets pre-.NET-10 TFMs later, any fallback SSE writer should stay internal rather than becoming the public baseline abstraction.

For `SseItem<string>`, the platform formatter writes string payloads directly, which is exactly what HyperRazor needs for HTML fragments.

### D7. OOB content should clear per emitted message

An SSE connection is one long request scope, which means the current scoped `IHrzSwapService` would otherwise keep accumulating queued OOB content across messages.

That would be wrong for streaming semantics.

Required behavior for the SSE renderer:

- render the current component/fragments
- append currently queued OOB payloads
- clear the queued swap content after the message is rendered

The default should be "clear after each message".

### D8. The stream writer should handle transport details, not app code

The transport layer should own the standard SSE mechanics:

- `Content-Type: text/event-stream; charset=utf-8`
- `Cache-Control: no-cache`
- optional `X-Accel-Buffering: no` when proxy-buffer suppression is enabled
- flushing after each message
- periodic heartbeat comments when idle
- stopping cleanly when `RequestAborted` is cancelled

If HyperRazor adds a wrapper, it should stay thin and sit on top of the platform result.

### D9. Close-event semantics need an explicit helper or test

The HTML SSE processing model only dispatches an event if the message block has a non-empty data buffer.

That means a graceful close event cannot be emitted as just:

```text
event: done

```

It must include a `data:` line, even if blank:

```text
event: done
data:

```

HyperRazor should make this hard to get wrong. Either:

- add a tiny helper for "close event with blank data"
- or add an explicit regression test for that exact wire format

On .NET 10, `new SseItem<string>(string.Empty, eventType: "done")` is an acceptable canonical shape for that close event because the platform formatter still emits the required `data:` line for an empty string payload.

### D10. Resume/replay is optional and app-owned

If the server emits SSE `id` values, browsers can reconnect with `Last-Event-ID`.

HyperRazor should:

- preserve the ID field when provided
- not impose a built-in replay store or cursor model
- let apps inspect `Request.Headers["Last-Event-ID"]` if they need custom resume behavior

That is enough for v1.

## Client Contract

### HTMX 2 contract

Baseline install:

- load `htmx-2.x`
- load the vendored SSE extension script
- opt into SSE per live region with `hx-ext="sse"`

Primary recommended markup:

```html
<section hx-ext="sse" sse-connect="/demos/sse/stream" sse-close="done">
    <div id="demo-sse-feed" sse-swap="message">
        Waiting for updates...
    </div>
</section>
```

Notes:

- use the unnamed/default `message` event for HTML swaps
- use `sse-close="done"` when the server sends `event: done`
- named event hooks are advanced usage, not the primary path

### HTMX 4 contract

HTMX 4 uses a different SSE extension model.

Primary recommended markup:

```html
<div id="demo-sse-feed"
     hx-sse:connect="/demos/sse/stream"
     hx-sse:close="done">
    Waiting for updates...
</div>
```

Important difference:

- HTMX 4 swaps unnamed messages as HTML
- named SSE events are dispatched as DOM events instead of being the primary HTML swap mechanism

That is the reason HyperRazor should standardize its server-side HTML stream contract on unnamed messages.

### Config stance

Initial SSE support does not need new `AddHtmx(...)` options.

Why:

- HTMX 2 only needs the extension script and explicit markup
- HTMX 4 has richer `htmx.config.sse` options, but HyperRazor does not need to expose them to ship a correct baseline

If HTMX 4 becomes the primary profile later, adding a structured `HtmxConfig.Sse` object can be evaluated as follow-up work.

## Server Contract

### Endpoint behavior

First-party examples should use `GET`, because that matches HTMX 2's `EventSource`-based model and keeps the baseline story simple.

The architecture should not assume SSE is GET-only, because HTMX 4 can stream any HTMX request whose response is `text/event-stream`.

SSE endpoints should:

- return `200 OK` once the stream is established
- return `text/event-stream; charset=utf-8`
- avoid output caching
- write and flush messages incrementally

If an endpoint cannot establish the stream, it should fail before beginning the SSE response rather than mixing normal HTML and SSE on the same response.

### Message formatting

The underlying transport must still honor SSE rules:

- close/control events still need a `data:` line to dispatch
- `id` values should flow through when present
- retry/reconnection hints should flow through when present

Heartbeat support should use SSE comments:

```text
: keep-alive

```

Recommended default heartbeat interval:

- 15 seconds

### Rendering components into messages

The rendering helper should allow app code to stay component-oriented:

```csharp
app.MapGet("/demos/sse/stream", (
    IHrzSseRenderer sseRenderer,
    CancellationToken cancellationToken) =>
{
    async IAsyncEnumerable<SseItem<string>> Stream(
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken ct)
    {
        yield return await sseRenderer.RenderComponent<SseStepCard>(
            new { Step = 1, Message = "Starting import..." },
            id: "1",
            cancellationToken: ct);

        await Task.Delay(TimeSpan.FromSeconds(1), ct);

        yield return await sseRenderer.RenderComponent<SseStepCard>(
            new { Step = 2, Message = "Import complete." },
            id: "2",
            cancellationToken: ct);

        yield return new SseItem<string>(string.Empty, eventType: "done");
    }

    return TypedResults.ServerSentEvents(Stream(cancellationToken));
});
```

The exact demo component names can differ, but this is the experience the spec should optimize for.

## OOB Over SSE

OOB support is part of the value proposition and should be included in the initial design.

Expected behavior:

- app code queues OOB content through `IHrzSwapService`
- the SSE renderer includes that OOB markup next to the main fragment in the emitted message payload
- HTMX processes the primary swap and the OOB swaps from the same message

This should be treated as "supported with browser proof", not as a vague stretch goal.

One explicit browser-level acceptance check is required for it.

## Security And Operational Notes

- First-party HTMX 2 baseline examples use `GET`, so HyperRazor's antiforgery transport does not apply to the primary documented path.
- Same-origin usage should remain the documented baseline.
- Existing cookie/session auth should continue to work naturally for same-origin SSE.
- Cross-origin credentialed EventSource usage is out of scope for v1 docs.
- Streams should be cancellation-aware and stop work promptly when the client disconnects.
- Proxy buffering can break perceived streaming behavior, so disabling proxy buffering should be part of the stream result defaults where practical.
- HTMX 2 and HTMX 4 both add reconnection behavior around their SSE helpers; HTMX 4's `hx-sse:connect` also uses exponential backoff and pauses streams while the tab is backgrounded by default.
- Browser connection limits matter. On HTTP/1.x, browsers commonly allow only a small number of EventSource-style SSE connections per browser and domain, so docs should discourage scattering many independent live regions across one page.
- `204 No Content` should be documented as the "stop reconnecting" response when the server needs to terminate a stream without relying on a graceful close event being processed.

## Demo Plan

Add one focused demo page proving the transport end to end.

Recommended route:

- `GET /demos/sse`

Recommended behavior:

- page loads a live region that connects to an SSE endpoint
- the stream emits a short deterministic sequence of HTML updates over a few seconds
- at least one message also appends an OOB update to a secondary region
- the stream ends by sending `event: done`

What the demo should prove:

- no polling is required
- the main region updates incrementally
- OOB still works during SSE
- the connection can close cleanly
- the story stays HTML-first

Keep the demo deterministic and self-contained. It should not require a real background worker or external broker.

## Test Plan

### Unit tests

- `IHrzSseRenderer` renders correctly without HTMX request headers present
- close-event helper or close-event test emits a blank-data `done` event
- OOB content is cleared between emitted messages
- OOB content is still cleared after a render failure
- thin wrapper behavior, if implemented:
  - heartbeat comments emitted when idle
  - proxy-buffering header applied when enabled

### Integration tests

- Minimal API or MVC SSE endpoint returns `text/event-stream`
- first message can be read without buffering the full response
- rendered component HTML appears inside `data:` lines
- OOB markup appears in streamed payload when queued
- `event: done` is emitted at the end of the sequence
- the SSE renderer path works when the request has no HTMX headers
- `Last-Event-ID` round-trips when the stream emits IDs and reconnects

### Browser E2E

At least one Playwright test is required because:

- browser EventSource behavior is the real contract
- OOB-inside-SSE must be proven in a real HTMX page
- connection close behavior is easiest to verify in-browser

Required E2E assertions:

- the live region changes at least twice without polling
- the secondary OOB target changes during the stream
- the final named event closes the connection and no extra updates arrive afterward
- a blank-data `done` event closes the connection successfully

## Implementation Checklist

### Client assets and markup

- [ ] Vendor the official HTMX SSE extension in `src/HyperRazor.Client/wwwroot/vendor/htmx/`.
- [ ] Load the SSE extension script from `src/HyperRazor.Components/Layouts/HrzAppLayout.razor`.
- [ ] Load the SSE extension script from `src/HyperRazor.Demo.Mvc/Components/Layouts/AppLayout.razor`.
- [ ] Keep SSE connection setup explicit in component markup; do not add body-level auto-connect behavior to `hyperrazor.htmx.js`.
- [ ] Add doc examples for both HTMX 2 (`hx-ext="sse"`, `sse-connect`, `sse-swap`) and HTMX 4 (`hx-sse:connect`, `hx-sse:close`) usage.

### Rendering and public API

- [ ] Add `IHrzSseRenderer` to the public rendering surface.
- [ ] Implement `IHrzSseRenderer` using `SseItem<string>` rather than a custom HyperRazor transport record.
- [ ] Reuse the existing HyperRazor partial-rendering path instead of creating a second HTML renderer.
- [ ] Add an explicit SSE render mode to `HrzComponentHost` or equivalent host plumbing.
- [ ] Ensure SSE render mode includes `HrzSwapContent`.
- [ ] Ensure SSE render mode suppresses `HrzHeadContent`.
- [ ] Ensure SSE render mode does not depend on HTMX request headers.
- [ ] Clear queued OOB content after each emitted SSE render.
- [ ] Clear queued OOB content even when a render fails.

### Transport integration

- [ ] Use `TypedResults.ServerSentEvents(...)` / `Results.ServerSentEvents(...)` as the transport foundation on `net10.0`.
- [ ] Confirm string payloads are emitted as raw HTML content through `SseItem<string>`.
- [ ] Decide whether HyperRazor needs a thin SSE wrapper for heartbeat comments and proxy-buffering defaults.
- [ ] If a wrapper is added, keep it thin and platform-backed rather than introducing a parallel transport abstraction.
- [ ] Add a canonical helper or documented pattern for blank-data close events such as `done`.
- [ ] Document `204 No Content` as the no-reconnect termination response.
- [ ] Document `Last-Event-ID` handling for apps that opt into event IDs.

### Demo implementation

- [ ] Add a focused SSE demo page, recommended at `GET /demos/sse`.
- [ ] Add a deterministic SSE stream endpoint for the demo.
- [ ] Stream at least two HTML updates into a primary live region.
- [ ] Include at least one OOB update in the streamed sequence.
- [ ] End the demo stream with a blank-data `done` event.
- [ ] Keep the demo self-contained; do not require background workers or external brokers.

### Verification

- [ ] Add unit coverage for no-header SSE rendering, blank-data close events, OOB clearing, and render-failure cleanup.
- [ ] Add integration coverage for `text/event-stream`, incremental reads, OOB-in-stream payloads, and `Last-Event-ID` round-trips.
- [ ] Add Playwright coverage for incremental DOM updates, OOB behavior during SSE, and connection close behavior.
- [ ] Verify in-browser that the final `done` event closes the connection and prevents further updates.

## Acceptance Criteria

SSE support is done when all of the following are true:

1. HyperRazor ships a first-party way to return `text/event-stream` from ASP.NET endpoints.
2. HyperRazor apps can render server-side components/fragments into SSE messages without hand-building HTML strings.
3. HTMX 2 usage is documented and supported through the official SSE extension.
4. HTMX 4 differences are documented clearly enough that the server contract does not need to be redesigned later.
5. OOB updates work from streamed SSE messages in a real browser test.
6. No head/title semantics are implied or half-supported in v1 SSE messages.
7. The feature adds one focused demo instead of a speculative framework layer.

## Follow-Up Work Explicitly Deferred

- HTMX 4-specific `htmx.config.sse` exposure through `HtmxConfig`
- richer named-event helper APIs
- durable replay/catch-up infrastructure
- auth refresh flows for very long-lived streams
- WebSocket parity or a transport abstraction layer
- head update semantics during SSE

## References

- ASP.NET Core server-sent events: https://learn.microsoft.com/en-us/aspnet/core/fundamentals/minimal-apis/responses?view=aspnetcore-10.0#server-sent-events-sse
- `System.Net.ServerSentEvents.SseItem<T>`: https://learn.microsoft.com/en-us/dotnet/api/system.net.serversentevents.sseitem-1?view=net-10.0
- HTMX 2 SSE extension: https://htmx.org/extensions/sse/
- HTMX 4 SSE extension: https://four.htmx.org/extensions/sse/
- MDN, Using server-sent events: https://developer.mozilla.org/en-US/docs/Web/API/Server-sent_events/Using_server-sent_events
- HTML Living Standard, server-sent events: https://html.spec.whatwg.org/multipage/server-sent-events.html
