using HyperRazor.Htmx.AspNetCore;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace HyperRazor.Mvc.Filters;

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
public sealed class HtmxRequestAttribute : ActionFilterAttribute
{
    public HtmxRequestAttribute()
    {
    }

    public HtmxRequestAttribute(string target)
    {
        Target = target;
    }

    public string? Target { get; set; }

    public int FailureStatusCode { get; set; } = StatusCodes.Status400BadRequest;

    public override void OnActionExecuting(ActionExecutingContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var request = context.HttpContext.HtmxRequest();
        if (!request.IsHtmx)
        {
            context.Result = new StatusCodeResult(FailureStatusCode);
            return;
        }

        if (!string.IsNullOrWhiteSpace(Target)
            && !string.Equals(request.Target, Target, StringComparison.Ordinal))
        {
            context.Result = new StatusCodeResult(FailureStatusCode);
        }
    }
}
