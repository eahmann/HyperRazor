using HyperRazor.Components;
using HyperRazor.Rendering;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Options;

namespace HyperRazor.Rendering.Tests;

public class HrzLayoutFamilyResolverTests
{
    private static IHrzLayoutFamilyResolver CreateResolver(string defaultLayoutFamily = "main")
    {
        return new HrzLayoutFamilyResolver(Options.Create(new HrzOptions
        {
            LayoutBoundary = new HrzLayoutBoundaryOptions
            {
                DefaultLayoutFamily = defaultLayoutFamily
            }
        }));
    }

    [Fact]
    public void ResolveForPageComponent_UsesExplicitLayoutFamilyAttribute()
    {
        var resolver = CreateResolver();

        var family = resolver.ResolveForPageComponent(typeof(SidePageComponent));

        Assert.Equal("side", family);
    }

    [Fact]
    public void ResolveForLayoutType_UsesNamingConvention_WhenAttributeMissing()
    {
        var resolver = CreateResolver();

        var family = resolver.ResolveForLayoutType(typeof(NamedMainLayout));

        Assert.Equal("namedmain", family);
    }

    [Fact]
    public void ResolveForPageComponent_UsesDefaultFamily_WhenNoLayoutAttribute()
    {
        var resolver = CreateResolver(defaultLayoutFamily: "fallback");

        var family = resolver.ResolveForPageComponent(typeof(NoLayoutPageComponent));

        Assert.Equal("fallback", family);
    }

    [Layout(typeof(SideDemoLayout))]
    private sealed class SidePageComponent : ComponentBase
    {
    }

    private sealed class NoLayoutPageComponent : ComponentBase
    {
    }

    [HrzLayoutFamily("side")]
    private sealed class SideDemoLayout : LayoutComponentBase
    {
    }

    private sealed class NamedMainLayout : LayoutComponentBase
    {
    }
}
