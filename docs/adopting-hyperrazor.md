# Adopting HyperRazor

Start with [quickstart.md](quickstart.md) for the smallest public setup.
For package-surface definitions, see [package-surface.md](package-surface.md).
For CI and package/versioning expectations, see [release-policy.md](release-policy.md).

## Startup contract

The adoption path is intentionally explicit:

```csharp
builder.Services.AddControllersWithViews();
builder.Services.AddAntiforgery();
builder.Services.AddHyperRazor();
builder.Services.AddHtmx();

var app = builder.Build();

app.UseHyperRazor();
```

There is no wrapper registration method in the public story. `AddHyperRazor()` and `AddHtmx()` are both required, and `UseHyperRazor()` fails early when either registration is missing.

## Endpoint vocabulary

Use the same page/fragment terms everywhere:

- MVC controllers use `Page<TComponent>()`, `Fragment<TComponent>()`, and `RootSwap<TComponent>()` through `HrController`.
- Minimal APIs use `MapPage<TComponent>(pattern)` and `MapFragment<TComponent>(pattern)` for direct mappings.
- `HrzResults.Page<TComponent>(...)`, `HrzResults.Fragment<TComponent>(...)`, and `HrzResults.RootSwap<TComponent>(...)` remain available for advanced composition.

## Demo vs. onboarding

The demo app still uses internal composition helpers because they help organize showcase code. That demo composition is not part of the framework startup contract and should not be treated as required ceremony for a real app.
