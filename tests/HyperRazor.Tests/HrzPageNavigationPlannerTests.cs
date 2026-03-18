using HyperRazor.Htmx;
using HyperRazor.Rendering;
using Microsoft.AspNetCore.Http;

namespace HyperRazor.Tests;

public class HrzPageNavigationPlannerTests
{
    [Fact]
    public void Create_RejectsNullRequest()
    {
        var context = new DefaultHttpContext();

        Assert.Throws<ArgumentNullException>(() =>
            HrzPageNavigationPlanner.Create(
                context,
                null!,
                HrzLayoutKey.None,
                rootSwapOverride: false));
    }

    [Fact]
    public void Create_RejectsNullTargetLayoutKey()
    {
        var context = new DefaultHttpContext();
        var request = new HtmxRequest { RequestType = HtmxRequestType.Partial };

        Assert.Throws<ArgumentNullException>(() =>
            HrzPageNavigationPlanner.Create(
                context,
                request,
                null!,
                rootSwapOverride: false));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_RejectsBlankTargetLayoutKey(string targetLayoutKey)
    {
        var context = new DefaultHttpContext();
        var request = new HtmxRequest { RequestType = HtmxRequestType.Partial };

        Assert.Throws<ArgumentException>(() =>
            HrzPageNavigationPlanner.Create(
                context,
                request,
                targetLayoutKey!,
                rootSwapOverride: false));
    }
}
