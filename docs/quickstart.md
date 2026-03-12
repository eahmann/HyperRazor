# HyperRazor Quickstart

This is the current golden path for a server-rendered ASP.NET Core app that uses HTMX-aware Razor components.

## Packages

- For the normal HyperRazor app path, reference `HyperRazor`.
- If you only want typed HTMX support without HyperRazor rendering, reference `HyperRazor.Htmx`.

`HyperRazor` is the public golden path. It brings in the MVC and HTMX layers transitively.

The primary packages also add a small set of MVC/rendering-related HyperRazor namespace imports (via build-transitive global usings) for the happy path; they do not make every HyperRazor namespace implicit. If you reference lower-level packages directly, add any additional namespaces you need yourself.

## Service registration

```csharp
using HyperRazor;
using Microsoft.AspNetCore.Mvc;

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
using HyperRazor.Demo.Mvc.Components.Pages.Admin;

app.MapGet("/", (HttpContext context, CancellationToken cancellationToken) =>
    HrzResults.Page<DashboardPage>(context, cancellationToken: cancellationToken));

app.MapGet("/users", (HttpContext context, CancellationToken cancellationToken) =>
    HrzResults.Page<UsersPage>(context, cancellationToken: cancellationToken));

app.MapGet("/settings/branding", (HttpContext context, CancellationToken cancellationToken) =>
    HrzResults.Page<BrandingSettingsPage>(context, cancellationToken: cancellationToken));
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

For CI/release expectations, see [release-policy.md](release-policy.md).
