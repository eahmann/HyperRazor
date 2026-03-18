using HyperRazor.Htmx;
using Microsoft.AspNetCore.Http;

namespace HyperRazor.Rendering;

internal static class HrzPageNavigationPlanner
{
    private const string AppShellSelector = "#hrz-app-shell";

    public static HrzPageNavigationPlan Create(
        HttpContext context,
        HtmxRequest request,
        string targetLayoutKey,
        bool rootSwapOverride)
    {
        ArgumentNullException.ThrowIfNull(context);

        var currentLayoutKey = TryReadCurrentLayoutKey(context.Request.Headers, out var layoutKey)
            ? layoutKey
            : null;
        var mode = rootSwapOverride
            ? ResolveRootSwapMode(request)
            : ResolvePageNavigationMode(request, currentLayoutKey, targetLayoutKey);

        return new HrzPageNavigationPlan(targetLayoutKey, currentLayoutKey, mode);
    }

    public static IResult? TryCreateImmediateResult(
        HttpContext context,
        HtmxRequest request,
        HrzPageNavigationPlan plan,
        string htmlContentType)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(plan);
        ArgumentException.ThrowIfNullOrWhiteSpace(htmlContentType);

        if (plan.Mode == HrzPageNavigationResponseMode.HxLocation)
        {
            context.HtmxResponse()
                .Location(new
                {
                    path = GetRequestPathAndQuery(context.Request),
                    target = AppShellSelector,
                    swap = "outerHTML",
                    select = AppShellSelector,
                    headers = new Dictionary<string, string>
                    {
                        [HtmxHeaderNames.RequestType] = "full"
                    }
                });

            return Results.Content(string.Empty, htmlContentType);
        }

        if (plan.Mode == HrzPageNavigationResponseMode.RootSwap)
        {
            context.HtmxResponse()
                .Retarget(AppShellSelector)
                .Reswap("outerHTML")
                .Reselect(AppShellSelector)
                .PushUrl(GetRequestPathAndQuery(context.Request));
        }

        return null;
    }

    public static void StoreDiagnostics(HttpContext context, HtmxRequest request, HrzPageNavigationPlan plan)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(plan);

        context.Items[typeof(HtmxPageNavigationDiagnostics)] = new HtmxPageNavigationDiagnostics(
            CurrentUrl: request.CurrentUrl?.ToString(),
            SourceLayout: plan.CurrentLayoutKey,
            TargetLayout: plan.TargetLayoutKey,
            Mode: plan.Mode.ToString());
    }

    private static bool TryReadCurrentLayoutKey(IHeaderDictionary headers, out string layoutKey)
    {
        layoutKey = string.Empty;

        if (!headers.TryGetValue(HrzInternalHeaderNames.CurrentLayout, out var values))
        {
            return false;
        }

        return HrzLayoutKey.TryNormalize(values.ToString(), out layoutKey);
    }

    private static HrzPageNavigationResponseMode ResolvePageNavigationMode(
        HtmxRequest request,
        string? currentLayoutKey,
        string targetLayoutKey)
    {
        if (request.RequestType != HtmxRequestType.Partial || request.IsHistoryRestoreRequest)
        {
            return HrzPageNavigationResponseMode.FullPage;
        }

        if (!request.IsBoosted)
        {
            return HrzPageNavigationResponseMode.PageFragment;
        }

        if (string.IsNullOrWhiteSpace(currentLayoutKey))
        {
            return HrzPageNavigationResponseMode.HxLocation;
        }

        return string.Equals(currentLayoutKey, targetLayoutKey, StringComparison.Ordinal)
            ? HrzPageNavigationResponseMode.PageFragment
            : HrzPageNavigationResponseMode.RootSwap;
    }

    private static HrzPageNavigationResponseMode ResolveRootSwapMode(HtmxRequest request)
    {
        if (request.RequestType != HtmxRequestType.Partial || request.IsHistoryRestoreRequest)
        {
            return HrzPageNavigationResponseMode.FullPage;
        }

        return HrzPageNavigationResponseMode.RootSwap;
    }

    private static string GetRequestPathAndQuery(HttpRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        return string.Concat(
            request.PathBase.Value,
            request.Path.Value,
            request.QueryString.Value);
    }
}
