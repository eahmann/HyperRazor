using HyperRazor.Demo.Mvc.Components.Pages;
using HyperRazor.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace HyperRazor.Demo.Mvc.Controllers;

[ApiController]
public sealed class FeatureController : HrController
{
    [HttpGet("/feature")]
    public Task<IResult> Get(CancellationToken cancellationToken)
    {
        return View<FeaturePage>(cancellationToken: cancellationToken);
    }
}
