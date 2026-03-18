# HyperRazor Quickstart

This is the smallest explicit setup for a server-rendered ASP.NET Core app that uses HyperRazor pages, partials, and HTMX-aware responses.

## Which package do I install?

- Full HyperRazor app: install `HyperRazor`.
- Typed HTMX only: install `HyperRazor.Htmx`.
- Advanced component composition: install `HyperRazor.Components` only when you are intentionally composing on that layer.

## Happy-path package reference

For the normal page/fragment app path, install only the primary `HyperRazor` package:

```bash
dotnet add package HyperRazor
```

`HyperRazor` is the default onboarding package. It brings in `HyperRazor.Components` and `HyperRazor.Htmx` transitively, while continuing to expose the `HyperRazor.Mvc` and `HyperRazor.Rendering` namespaces from the `HyperRazor` assembly.

If you only want typed HTMX support in an existing ASP.NET Core app, install `HyperRazor.Htmx` instead and stop there:

```bash
dotnet add package HyperRazor.Htmx
```

## Service registration

```csharp
using HyperRazor;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();
builder.Services.AddAntiforgery();
builder.Services.AddHyperRazor();
```

`AddHyperRazor()` is the full happy-path registration. It also registers the default HTMX configuration used by the top-level package.

Call `AddHtmx(...)` separately only when you want to customize HTMX defaults after the main HyperRazor registration:

```csharp
builder.Services.AddHyperRazor();
builder.Services.AddHtmx(htmx =>
{
    htmx.EnableHeadSupport = true;
});
```

## Middleware

```csharp
var app = builder.Build();

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

app.UseHyperRazor();

app.MapControllers();

app.Run();
```

`UseHyperRazor()` enables HyperRazor diagnostics in Development and applies HTMX-aware `Vary` behavior. It also fails early when `AddHyperRazor()` was not called during service registration.

## MVC controller pattern

For MVC controllers, inherit from `HrController` and use `Page<TComponent>()` for routable components:

```csharp
using Microsoft.AspNetCore.Mvc;

[ApiController]
public sealed class FeatureController : HrController
{
    [HttpGet("/")]
    public Task<IResult> Home(CancellationToken cancellationToken)
    {
        return Page<HomePage>(cancellationToken: cancellationToken);
    }
}
```

Use `Fragment<TComponent>()` when the action should return a fragment instead of a full page shell:

```csharp
[HttpPost("/fragments/toast/success")]
public Task<IResult> Toast(CancellationToken cancellationToken)
{
    return Fragment<ToastSuccess>(
        new { Message = "Saved successfully." },
        cancellationToken);
}
```

## Minimal API pattern

For straightforward GET routes, map components directly:

```csharp
app.MapPage<HomePage>("/");
app.MapPage<UsersPage>("/users");
app.MapPage<BrandingSettingsPage>("/settings/branding");
```

For straightforward fragment endpoints:

```csharp
app.MapFragment<SystemStatusPanel>("/fragments/system-status");
```

When the endpoint needs route data, form data, or custom HTMX response behavior, drop to `HrzResults.Page<TComponent>(...)` or `HrzResults.Fragment<TComponent>(...)` inside a route handler.

## Antiforgery in component forms

Include the fallback helper in HTMX form components:

```razor
<form action="/fragments/users/create" hx-post="/fragments/users/create">
    <HrzAntiforgeryInput />
    <input name="displayName" />
    <button type="submit">Create</button>
</form>
```

The bundled client script reads the antiforgery meta tag and adds the configured request header automatically for unsafe HTMX verbs.

## Available later

Advanced but supported composition features remain available, but they are not part of the first working app:

- layout-aware page navigation
- custom HTMX response handling
- SSE replay customization
- validation policy overrides

If you adopt the advanced validation surface later, import shared validation contracts from `HyperRazor.Components.Validation`. Validation authoring in `HyperRazor.Components` supports both `HrzForm` / `HrzField` / `HrzInput*` and the advanced lane where you resolve `IHrzForms` to create `HrzFormScope` / `HrzFieldScope`. The implementation APIs remain under the `HyperRazor.Rendering` namespace inside the `HyperRazor` package.

For CI and package/versioning expectations, see [release-policy.md](release-policy.md).
