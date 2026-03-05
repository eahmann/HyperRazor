# Adopting HyperRazor (v1)

1. Reference HyperRazor packages from your web app:
- `HyperRazor.Hosting`
- `HyperRazor.Htmx.AspNetCore`
- `HyperRazor.Mvc`

2. Register services:

```csharp
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

builder.Services.AddHyperRazorHtmx(htmx =>
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

5. For status-oriented responses, use `HrxResults` helpers:

```csharp
return HrxResults.NotFound<MyNotFoundComponent>(HttpContext);
return HrxResults.Forbidden<MyForbiddenComponent>(HttpContext);
return await HrxResults.Validation<MyFormResultComponent>(
    HttpContext,
    data: new { Errors = errors },
    statusCode: StatusCodes.Status200OK);
```

6. Optional strict validation semantics (`422`):

```csharp
builder.Services.AddHyperRazorHtmx(htmx =>
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
    <HrxAntiforgeryInput />
    ...
</form>
```
