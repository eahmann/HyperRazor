using HyperRazor.Htmx.AspNetCore;
using Microsoft.AspNetCore.Mvc.Filters;

namespace HyperRazor.Mvc.Filters;

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
public sealed class HtmxResponseAttribute : ResultFilterAttribute
{
    public string? Redirect { get; set; }

    public string? Location { get; set; }

    public string? PushUrl { get; set; }

    public string? ReplaceUrl { get; set; }

    public string? Retarget { get; set; }

    public string? Reswap { get; set; }

    public string? Reselect { get; set; }

    public string? Trigger { get; set; }

    public string? TriggerAfterSwap { get; set; }

    public string? TriggerAfterSettle { get; set; }

    public bool Refresh { get; set; }

    public override void OnResultExecuting(ResultExecutingContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var response = context.HttpContext.HtmxResponse();

        if (!string.IsNullOrWhiteSpace(Redirect))
        {
            response.Redirect(Redirect);
        }

        if (!string.IsNullOrWhiteSpace(Location))
        {
            response.Location(Location);
        }

        if (!string.IsNullOrWhiteSpace(PushUrl))
        {
            response.PushUrl(PushUrl);
        }

        if (!string.IsNullOrWhiteSpace(ReplaceUrl))
        {
            response.ReplaceUrl(ReplaceUrl);
        }

        if (!string.IsNullOrWhiteSpace(Retarget))
        {
            response.Retarget(Retarget);
        }

        if (!string.IsNullOrWhiteSpace(Reswap))
        {
            response.Reswap(Reswap);
        }

        if (!string.IsNullOrWhiteSpace(Reselect))
        {
            response.Reselect(Reselect);
        }

        if (!string.IsNullOrWhiteSpace(Trigger))
        {
            response.Trigger(Trigger);
        }

        if (!string.IsNullOrWhiteSpace(TriggerAfterSwap))
        {
            response.TriggerAfterSwap(TriggerAfterSwap);
        }

        if (!string.IsNullOrWhiteSpace(TriggerAfterSettle))
        {
            response.TriggerAfterSettle(TriggerAfterSettle);
        }

        if (Refresh)
        {
            response.Refresh();
        }
    }
}
