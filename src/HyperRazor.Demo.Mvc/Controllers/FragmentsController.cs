using HyperRazor.Demo.Mvc.Components.Fragments;
using HyperRazor.Htmx.AspNetCore;
using HyperRazor.Mvc;
using HyperRazor.Mvc.Filters;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace HyperRazor.Demo.Mvc.Controllers;

[ApiController]
[Route("fragments")]
public sealed class FragmentsController : HrController
{
    [HttpGet("users/search")]
    [HtmxRequest]
    public Task<IResult> SearchUsers([FromQuery] string? query, CancellationToken cancellationToken)
    {
        return PartialView<UserSearchResults>(new { Query = query }, cancellationToken);
    }

    [HttpGet("toast/success")]
    [HtmxResponse(Trigger = "toast:show")]
    public Task<IResult> ToastSuccess(CancellationToken cancellationToken)
    {
        return PartialView<ToastSuccess>(cancellationToken: cancellationToken);
    }

    [HttpPost("navigation/soft")]
    public IActionResult SoftRedirect()
    {
        HttpContext.HtmxResponse().Location("/");
        return NoContent();
    }

    [HttpPost("navigation/hard")]
    public IActionResult HardRedirect()
    {
        HttpContext.HtmxResponse().Redirect("/");
        return NoContent();
    }
}
