using HyperRazor.Demo.Mvc.Components.Pages;
using HyperRazor.Demo.Mvc.Components.Pages.SideNav;
using HyperRazor.Mvc;
using Microsoft.AspNetCore.Mvc;

namespace HyperRazor.Demo.Mvc.Controllers;

[ApiController]
public sealed class FeatureController : HrController
{
    [HttpGet("/")]
    public Task<IResult> Home(CancellationToken cancellationToken)
    {
        return View<HomePage>(cancellationToken: cancellationToken);
    }

    [HttpGet("/demos/basic")]
    public Task<IResult> ServerTrigger(CancellationToken cancellationToken)
    {
        return View<BasicDemoPage>(cancellationToken: cancellationToken);
    }

    [HttpGet("/demos/search")]
    public Task<IResult> LiveSearch(CancellationToken cancellationToken)
    {
        return View<SearchDemoPage>(cancellationToken: cancellationToken);
    }

    [HttpGet("/demos/redirects")]
    public Task<IResult> RedirectHeaders(CancellationToken cancellationToken)
    {
        return View<RedirectDemoPage>(cancellationToken: cancellationToken);
    }

    [HttpGet("/demos/errors")]
    public Task<IResult> StatusHandling(CancellationToken cancellationToken)
    {
        return View<ErrorsDemoPage>(cancellationToken: cancellationToken);
    }

    [HttpGet("/demos/validation")]
    public Task<IResult> FormValidation(CancellationToken cancellationToken)
    {
        return View<ValidationDemoPage>(cancellationToken: cancellationToken);
    }

    [HttpGet("/demos/oob")]
    public Task<IResult> OobSwaps(CancellationToken cancellationToken)
    {
        return View<FeaturePage>(cancellationToken: cancellationToken);
    }

    [HttpGet("/demos/head")]
    public Task<IResult> HeadHandling(CancellationToken cancellationToken)
    {
        return View<HeadDemoPage>(cancellationToken: cancellationToken);
    }

    [HttpGet("/demos/layout-swap")]
    public Task<IResult> LayoutSwap(CancellationToken cancellationToken)
    {
        return View<LayoutSwapDemoPage>(cancellationToken: cancellationToken);
    }

    [HttpGet("/demos/layout-swap/details")]
    public Task<IResult> LayoutSwapDetails(CancellationToken cancellationToken)
    {
        return View<LayoutSwapDetailsPage>(cancellationToken: cancellationToken);
    }
}
