using HyperRazor.Components.Services;
using HyperRazor.Htmx;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.RenderTree;
using Microsoft.AspNetCore.Http;

namespace HyperRazor.Rendering.Tests;

#pragma warning disable BL0006

public class HrzHeadServiceTests
{
    [Fact]
    public void RenderToFragment_WithoutHtmxRequest_DoesNotEmitHeadPayload()
    {
        var service = CreateService(isHtmx: false);
        service.AddTitle("Users");

        var fragment = service.RenderToFragment();
        var frames = RenderFrames(fragment);

        Assert.Equal(0, frames.Count);
        Assert.True(service.ContentAvailable);
    }

    [Fact]
    public void RenderToFragment_WithHtmxRequest_EmitsHeadPayload()
    {
        var service = CreateService(isHtmx: true);
        service.AddTitle("Users");
        service.AddMeta("description", "Users screen");

        var fragment = service.RenderToFragment();
        var frames = RenderFrames(fragment);

        Assert.True(frames.Count > 0);
        Assert.Contains(
            frames.Array.Take(frames.Count),
            frame => frame.FrameType == RenderTreeFrameType.Element && frame.ElementName == "head");
    }

    [Fact]
    public void RenderToFragment_ClearTrue_DrainsQueuedHeadItems()
    {
        var service = CreateService(isHtmx: true);
        service.AddTitle("Users");

        _ = service.RenderToFragment(clear: true);

        Assert.False(service.ContentAvailable);
    }

    [Fact]
    public void ContentItemsUpdated_RaisesOnAddAndClear()
    {
        var service = CreateService(isHtmx: true);
        var updates = 0;
        service.ContentItemsUpdated += (_, _) => updates++;

        service.AddTitle("Users");
        service.AddMeta("description", "Users screen");
        service.Clear();

        Assert.Equal(3, updates);
    }

    private static HrzHeadService CreateService(bool isHtmx)
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

        return new HrzHeadService(accessor);
    }

    private static ArrayRange<RenderTreeFrame> RenderFrames(RenderFragment fragment)
    {
        var builder = new RenderTreeBuilder();
        fragment(builder);
        return builder.GetFrames();
    }
}

#pragma warning restore BL0006
