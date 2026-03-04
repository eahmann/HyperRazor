using HyperRazor.Components;
using HyperRazor.Components.Services;
using HyperRazor.Htmx;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.RenderTree;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace HyperRazor.Rendering.Tests;

#pragma warning disable BL0006

public class HrxSwapServiceTests
{
    [Fact]
    public void SwapStyle_ToHtmxString_UsesExpectedFormats()
    {
        Assert.Equal("outerHTML", SwapStyle.OuterHtml.ToHtmxString());
        Assert.Equal("beforeend:#toast-stack", BuildOobValue(SwapStyle.BeforeEnd, "#toast-stack"));
    }

    [Fact]
    public void RenderToFragment_WithoutHtmxRequest_ExcludesSwappables()
    {
        var service = CreateService(isHtmx: false);
        service.AddSwappableContent("toast-item", "Created");

        var fragment = service.RenderToFragment();
        var frames = RenderFrames(fragment);

        Assert.Equal(0, frames.Count);
        Assert.True(service.ContentAvailable);
    }

    [Fact]
    public void RenderToFragment_WithHtmxRequest_IncludesSwappables()
    {
        var service = CreateService(isHtmx: true);
        service.AddSwappableContent("toast-item", "Created");

        var fragment = service.RenderToFragment();
        var frames = RenderFrames(fragment);

        Assert.True(frames.Count > 0);
    }

    [Fact]
    public void AddRawContent_WithoutOptIn_DoesNotRenderForNonHtmx()
    {
        var service = CreateService(isHtmx: false, allowRawContentOnNonHtmx: false);
        service.AddRawContent("<p id=\"raw-content\">Raw</p>");

        var fragment = service.RenderToFragment();
        var frames = RenderFrames(fragment);

        Assert.Equal(0, frames.Count);
    }

    [Fact]
    public void AddRawContent_WithOptIn_RendersForNonHtmx()
    {
        var service = CreateService(isHtmx: false, allowRawContentOnNonHtmx: true);
        service.AddRawContent("<p id=\"raw-content\">Raw</p>");

        var fragment = service.RenderToFragment();
        var frames = RenderFrames(fragment);

        Assert.True(frames.Count > 0);
    }

    [Fact]
    public void RenderToFragment_ClearTrue_DrainsQueuedItems()
    {
        var service = CreateService(isHtmx: true);
        service.AddSwappableComponent<TestBadgeComponent>("badge-item", new { Message = "Hello" });

        _ = service.RenderToFragment(clear: true);

        Assert.False(service.ContentAvailable);
    }

    private static HrxSwapService CreateService(bool isHtmx, bool allowRawContentOnNonHtmx = false)
    {
        var context = new DefaultHttpContext();
        if (isHtmx)
        {
            context.Request.Headers[HtmxHeaderNames.Request] = "true";
        }

        var accessor = new HttpContextAccessor
        {
            HttpContext = context
        };

        var options = Options.Create(new HrxSwapOptions
        {
            AllowRawContentOnNonHtmx = allowRawContentOnNonHtmx
        });

        return new HrxSwapService(accessor, options);
    }

    private static ArrayRange<RenderTreeFrame> RenderFrames(RenderFragment fragment)
    {
        var builder = new RenderTreeBuilder();
        fragment(builder);
        return builder.GetFrames();
    }

    private static string BuildOobValue(SwapStyle style, string selector)
    {
        return $"{style.ToHtmxString()}:{selector}";
    }

    private sealed class TestBadgeComponent : ComponentBase
    {
        [Parameter]
        public string Message { get; set; } = string.Empty;
    }
}

#pragma warning restore BL0006
