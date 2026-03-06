# HyperRazor Quickstart

This is the current golden path for a server-rendered ASP.NET Core app that uses HTMX-aware Razor components.

## Packages

Reference these packages in your web app:

- `HyperRazor.Hosting`
- `HyperRazor.Htmx.AspNetCore`
- `HyperRazor.Mvc`

## Service registration

```csharp
using HyperRazor.Components;
using HyperRazor.Components.Layouts;
using HyperRazor.Hosting;
using HyperRazor.Htmx;
using HyperRazor.Htmx.AspNetCore;
using HyperRazor.Rendering;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews(options =>
{
    options.Filters.Add(new AutoValidateAntiforgeryTokenAttribute());
});
builder.Services.AddAntiforgery(options =>
{
    options.HeaderName = "RequestVerificationToken";
});

builder.Services.AddHyperRazor(options =>
{
    options.RootComponent = typeof(HrzApp<HrzAppLayout>);
    options.UseMinimalLayoutForHtmx = true;
});

builder.Services.AddHtmx(htmx =>
{
    htmx.ClientProfile = HtmxClientProfile.Htmx2Defaults;
    htmx.HistoryRestoreAsHxRequest = false;
    htmx.AllowNestedOobSwaps = false;
    htmx.EnableHeadSupport = true;
    htmx.AntiforgeryMetaName = "hrz-antiforgery";
    htmx.AntiforgeryHeaderName = "RequestVerificationToken";
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

`UseHyperRazor()` is the one middleware call you should reach for first. It enables HyperRazor diagnostics in Development and applies HTMX-aware `Vary` behavior.

## MVC controller pattern

For MVC controllers, inherit from `HrController` and use `View<TComponent>()` for page components:

```csharp
using HyperRazor.Demo.Mvc.Components.Pages;
using HyperRazor.Mvc;
using Microsoft.AspNetCore.Mvc;

[ApiController]
public sealed class FeatureController : HrController
{
    [HttpGet("/")]
    public Task<IResult> Home(CancellationToken cancellationToken)
    {
        return View<HomePage>(cancellationToken: cancellationToken);
    }
}
```

## Minimal API pattern

For Minimal APIs, the canonical result helpers are:

- `HrzResults.Page<TComponent>(...)` for routable page components
- `HrzResults.Partial<TComponent>(...)` for fragment endpoints

```csharp
using HyperRazor.Demo.Mvc.Components.Pages;
using HyperRazor.Mvc;

var minimalPages = app.MapGroup("/minimal");

minimalPages.MapGet("/", (HttpContext context, CancellationToken cancellationToken) =>
    HrzResults.Page<HomePage>(context, cancellationToken: cancellationToken));

minimalPages.MapGet("/basic", (HttpContext context, CancellationToken cancellationToken) =>
    HrzResults.Page<BasicDemoPage>(context, cancellationToken: cancellationToken));
```

`Page<TComponent>` is the preferred name here because it represents a routable component endpoint, not an MVC view file.

## Fragment endpoints

Use `Partial<TComponent>` when the endpoint should return a fragment body instead of a full page shell:

```csharp
app.MapGet("/fragments/toast/success", (HttpContext context, CancellationToken cancellationToken) =>
    HrzResults.Partial<ToastSuccess>(
        context,
        data: new { Message = "Saved successfully." },
        configureResponse: response => response.Trigger("toast:show", new { message = "Saved successfully." }),
        cancellationToken: cancellationToken));
```

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

For CI/release expectations, see [release-policy.md](/home/eric/repos/HyperRazor/docs/release-policy.md).
