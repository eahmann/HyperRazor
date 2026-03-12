# Adopting HyperRazor (v1)

For the current canonical setup, start with [quickstart.md](quickstart.md).
For package-surface definitions, see [package-surface.md](package-surface.md).
For CI and package/versioning expectations, see [release-policy.md](release-policy.md).

1. Reference HyperRazor packages from your web app:
- `HyperRazor` for the full framework path
- `HyperRazor.Htmx` for HTMX-only ASP.NET integration without the HyperRazor rendering stack

For the normal HyperRazor app path, install `HyperRazor`. The lower-level MVC and HTMX packages flow transitively from there.
The primary packages carry the common HyperRazor namespace imports for the happy path. If you reference lower-level packages directly, add the relevant namespaces explicitly in your app.

2. Register services:

```csharp
using Microsoft.AspNetCore.Mvc;

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
    options.UseMinimalLayoutForHtmx = true;
});

builder.Services.AddHtmx(htmx =>
{
    htmx.ClientProfile = HtmxClientProfile.Htmx2Defaults;
    htmx.SelfRequestsOnly = true;
    htmx.HistoryRestoreAsHxRequest = false;
    htmx.AllowNestedOobSwaps = false;
    htmx.ResponseHandling =
    [
        new HtmxResponseHandlingRule { Code = "4..", Swap = true, Error = false },
        new HtmxResponseHandlingRule { Code = "5..", Swap = true, Error = false }
    ];
});
```

3. Add middleware for diagnostics + cache safety:

```csharp
app.UseHyperRazor();
```

4. In endpoints, read/write HTMX via typed APIs:

```csharp
var (request, response) = HttpContext.Htmx();
response.Trigger("toast:show", new { message = "Saved" });
```

5. For status-oriented responses, use `HrzResults` helpers:

```csharp
return HrzResults.NotFound<MyNotFoundComponent>(HttpContext);
return HrzResults.Forbidden<MyForbiddenComponent>(HttpContext);
return await HrzResults.Validation<MyFormResultComponent>(
    HttpContext,
    data: new { Errors = errors },
    statusCode: StatusCodes.Status200OK);
```

6. Optional strict validation semantics (`422`):

```csharp
builder.Services.AddHtmx(htmx =>
{
    htmx.ResponseHandling =
    [
        new HtmxResponseHandlingRule
        {
            Code = "422",
            Swap = true,
            Error = false
        }
    ];
});
```

7. For component POST forms, include antiforgery input fallback:

```razor
<form action="/fragments/users/create" hx-post="/fragments/users/create">
    <HrzAntiforgeryInput />
    ...
</form>
```
