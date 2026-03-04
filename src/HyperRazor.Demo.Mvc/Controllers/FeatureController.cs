using HyperRazor.Demo.Mvc.Components.Pages;
using HyperRazor.Mvc;
using Microsoft.AspNetCore.Http;
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
}
