# Adopting HyperRazor (v1)

1. Reference `HyperRazor.Htmx.AspNetCore` from your web app.
2. Register services:

```csharp
builder.Services.AddHyperRazorHtmx(htmx =>
{
    htmx.SelfRequestsOnly = true;
    htmx.HistoryRestoreAsHxRequest = false;
});
```

3. Add middleware for cache safety:

```csharp
app.UseHyperRazorHtmxVary();
```

4. In endpoints, read/write HTMX via typed APIs:

```csharp
var (request, response) = HttpContext.Htmx();
response.Trigger("toast:show", new { message = "Saved" });
```
