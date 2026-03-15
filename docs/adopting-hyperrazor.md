# Adopting HyperRazor

Start with [quickstart.md](quickstart.md) for the smallest public setup.
For package-surface definitions, see [package-surface.md](package-surface.md).
For CI and package/versioning expectations, see [release-policy.md](release-policy.md).

## Which package do I install?

- Full HyperRazor app: install `HyperRazor`.
- Typed HTMX only: install `HyperRazor.Htmx`.
- Advanced composition: install the lower-level packages directly only when you are intentionally composing on those layers.

## Happy-path package reference

For a full HyperRazor app, adopt the default onboarding package first:

```bash
dotnet add package HyperRazor
```

If you only need typed HTMX support in an existing ASP.NET Core app, install `HyperRazor.Htmx` instead. Reference the lower-level packages directly only when you are intentionally composing on those layers.

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

The demo app still uses internal composition helpers because they help organize showcase code. That demo composition is not part of the default onboarding path and should not be treated as required ceremony for a real app.
