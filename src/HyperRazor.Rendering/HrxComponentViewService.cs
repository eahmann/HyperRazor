using HyperRazor.Components;
using HyperRazor.Htmx;
using HyperRazor.Htmx.AspNetCore;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.Options;
using Microsoft.Net.Http.Headers;

namespace HyperRazor.Rendering;

public sealed class HrxComponentViewService : IHrxComponentViewService
{
    private const string HtmlContentType = "text/html; charset=utf-8";

    private readonly IHrxHtmlRendererAdapter _renderer;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IHrxLayoutFamilyResolver _layoutFamilyResolver;
    private readonly HrxOptions _options;

    public HrxComponentViewService(
        IHrxHtmlRendererAdapter renderer,
        IHttpContextAccessor httpContextAccessor,
        IHrxLayoutFamilyResolver layoutFamilyResolver,
        IOptions<HrxOptions> options)
    {
        _renderer = renderer ?? throw new ArgumentNullException(nameof(renderer));
        _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
        _layoutFamilyResolver = layoutFamilyResolver ?? throw new ArgumentNullException(nameof(layoutFamilyResolver));
        _options = (options ?? throw new ArgumentNullException(nameof(options))).Value;
    }

    public Task<IResult> View<TComponent>(object? data = null, CancellationToken cancellationToken = default)
        where TComponent : IComponent
    {
        return View<TComponent>(HrxParameterDictionaryFactory.Create(data), cancellationToken);
    }

    public async Task<IResult> View<TComponent>(
        IReadOnlyDictionary<string, object?> data,
        CancellationToken cancellationToken = default)
        where TComponent : IComponent
    {
        ArgumentNullException.ThrowIfNull(data);

        var context = GetHttpContext();
        var request = context.HtmxRequest();
        var isHtmxRequest = request.RequestType == HtmxRequestType.Partial;
        var routeLayoutFamily = _layoutFamilyResolver.ResolveForPageComponent(typeof(TComponent));
        var shellContext = new HrxShellContext(routeLayoutFamily);
        var modelState = ResolveModelState(context);
        EnsureVaryForHtmxBranching(context.Response.Headers);

        var promotionMode = ResolvePromotionMode(
            request: request,
            context: context,
            routeLayoutFamily: routeLayoutFamily);

        if (promotionMode == HrxLayoutBoundaryPromotionMode.Redirect)
        {
            context.HtmxResponse().Redirect(GetRequestPathAndQuery(context.Request));
            return Results.Content(string.Empty, HtmlContentType);
        }

        if (promotionMode == HrxLayoutBoundaryPromotionMode.Refresh)
        {
            context.HtmxResponse().Refresh();
            return Results.Content(string.Empty, HtmlContentType);
        }

        var shouldRenderShellForPromotion = promotionMode == HrxLayoutBoundaryPromotionMode.ShellSwap;
        if (shouldRenderShellForPromotion)
        {
            context.HtmxResponse()
                .Retarget(_options.LayoutBoundary.ShellTargetSelector)
                .Reswap(_options.LayoutBoundary.ShellSwapStyle)
                .Reselect(_options.LayoutBoundary.ShellReselectSelector);
            isHtmxRequest = false;
        }

        var html = await RenderHostAsync(
            componentType: typeof(TComponent),
            componentParameters: data,
            context: context,
            modelState: modelState,
            isPartial: false,
            isHtmxRequest: isHtmxRequest,
            isHistoryRestoreRequest: request.IsHistoryRestoreRequest,
            shellContext: shellContext,
            cancellationToken: cancellationToken);

        return Results.Content(html, HtmlContentType);
    }

    public Task<IResult> PartialView<TComponent>(object? data = null, CancellationToken cancellationToken = default)
        where TComponent : IComponent
    {
        return PartialView<TComponent>(HrxParameterDictionaryFactory.Create(data), cancellationToken);
    }

    public async Task<IResult> PartialView<TComponent>(
        IReadOnlyDictionary<string, object?> data,
        CancellationToken cancellationToken = default)
        where TComponent : IComponent
    {
        ArgumentNullException.ThrowIfNull(data);

        var context = GetHttpContext();
        var request = context.HtmxRequest();
        var modelState = ResolveModelState(context);
        EnsureVaryForHtmxBranching(context.Response.Headers);

        var html = await RenderHostAsync(
            componentType: typeof(TComponent),
            componentParameters: data,
            context: context,
            modelState: modelState,
            isPartial: true,
            isHtmxRequest: request.RequestType == HtmxRequestType.Partial,
            isHistoryRestoreRequest: request.IsHistoryRestoreRequest,
            shellContext: null,
            cancellationToken: cancellationToken);

        return Results.Content(html, HtmlContentType);
    }

    public async Task<IResult> PartialView(CancellationToken cancellationToken = default, params RenderFragment[] fragments)
    {
        ArgumentNullException.ThrowIfNull(fragments);

        var context = GetHttpContext();
        var request = context.HtmxRequest();
        var modelState = ResolveModelState(context);
        EnsureVaryForHtmxBranching(context.Response.Headers);

        var html = await RenderHostAsync(
            componentType: typeof(HrxFragmentGroup),
            componentParameters: new Dictionary<string, object?>
            {
                [nameof(HrxFragmentGroup.Fragments)] = fragments
            },
            context: context,
            modelState: modelState,
            isPartial: true,
            isHtmxRequest: request.RequestType == HtmxRequestType.Partial,
            isHistoryRestoreRequest: request.IsHistoryRestoreRequest,
            shellContext: null,
            cancellationToken: cancellationToken);

        return Results.Content(html, HtmlContentType);
    }

    private Task<string> RenderHostAsync(
        Type componentType,
        IReadOnlyDictionary<string, object?> componentParameters,
        HttpContext context,
        ModelStateDictionary modelState,
        bool isPartial,
        bool isHtmxRequest,
        bool isHistoryRestoreRequest,
        HrxShellContext? shellContext,
        CancellationToken cancellationToken)
    {
        var hostParameters = new Dictionary<string, object?>
        {
            [nameof(HrxComponentHost.ComponentType)] = componentType,
            [nameof(HrxComponentHost.ComponentParameters)] = componentParameters,
            [nameof(HrxComponentHost.RootComponentType)] = _options.RootComponent,
            [nameof(HrxComponentHost.IsPartial)] = isPartial,
            [nameof(HrxComponentHost.IsHtmxRequest)] = isHtmxRequest,
            [nameof(HrxComponentHost.IsHistoryRestoreRequest)] = isHistoryRestoreRequest,
            [nameof(HrxComponentHost.UseMinimalLayoutForHtmx)] = _options.UseMinimalLayoutForHtmx,
            [nameof(HrxComponentHost.HttpContext)] = context,
            [nameof(HrxComponentHost.ModelState)] = modelState,
            [nameof(HrxComponentHost.ShellContext)] = shellContext
        };

        return _renderer.RenderAsync(typeof(HrxComponentHost), hostParameters, cancellationToken);
    }

    private HttpContext GetHttpContext()
    {
        return _httpContextAccessor.HttpContext
            ?? throw new InvalidOperationException("No active HttpContext is available for HyperRazor rendering.");
    }

    private void EnsureVaryForHtmxBranching(IHeaderDictionary headers)
    {
        EnsureVaryBy(headers, HtmxHeaderNames.Request);
        EnsureVaryBy(headers, HtmxHeaderNames.RequestType);
        EnsureVaryBy(headers, HtmxHeaderNames.HistoryRestoreRequest);

        var layoutBoundaryOptions = _options.LayoutBoundary;
        if (!layoutBoundaryOptions.Enabled || !layoutBoundaryOptions.AddVaryHeader)
        {
            return;
        }

        EnsureVaryBy(headers, layoutBoundaryOptions.LayoutFamilyHeaderName);
        EnsureVaryBy(headers, HtmxHeaderNames.Boosted);
    }

    private static void EnsureVaryBy(IHeaderDictionary headers, string varyHeader)
    {
        if (!headers.TryGetValue(HeaderNames.Vary, out var existingVary)
            || string.IsNullOrWhiteSpace(existingVary))
        {
            headers[HeaderNames.Vary] = varyHeader;
            return;
        }

        var values = existingVary
            .ToString()
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        if (values.Contains(varyHeader, StringComparer.OrdinalIgnoreCase))
        {
            return;
        }

        headers[HeaderNames.Vary] = $"{existingVary}, {varyHeader}";
    }

    private static ModelStateDictionary ResolveModelState(HttpContext context)
    {
        if (context.Items.TryGetValue(HrxContextItemKeys.ModelState, out var value)
            && value is ModelStateDictionary modelState)
        {
            return modelState;
        }

        return new ModelStateDictionary();
    }

    private HrxLayoutBoundaryPromotionMode ResolvePromotionMode(
        HtmxRequest request,
        HttpContext context,
        string routeLayoutFamily)
    {
        var options = _options.LayoutBoundary;
        if (!options.Enabled || options.PromotionMode == HrxLayoutBoundaryPromotionMode.Off)
        {
            return HrxLayoutBoundaryPromotionMode.Off;
        }

        if (!request.IsPartialRequest || request.IsHistoryRestoreRequest)
        {
            return HrxLayoutBoundaryPromotionMode.Off;
        }

        if (options.OnlyBoostedRequests && !request.IsBoosted)
        {
            return HrxLayoutBoundaryPromotionMode.Off;
        }

        var clientLayoutFamily = ResolveClientLayoutFamily(request, context, routeLayoutFamily, options.LayoutFamilyHeaderName);
        if (string.IsNullOrWhiteSpace(clientLayoutFamily))
        {
            return options.PromotionMode;
        }

        return string.Equals(clientLayoutFamily, routeLayoutFamily, StringComparison.OrdinalIgnoreCase)
            ? HrxLayoutBoundaryPromotionMode.Off
            : options.PromotionMode;
    }

    private static string? ResolveClientLayoutFamily(
        HtmxRequest request,
        HttpContext context,
        string routeLayoutFamily,
        string requestHeaderName)
    {
        if (context.Request.Headers.TryGetValue(requestHeaderName, out var headerValue)
            && !string.IsNullOrWhiteSpace(headerValue.ToString()))
        {
            return headerValue.ToString().Trim();
        }

        // Fallback when current URL matches requested URL and the client header is unavailable.
        if (request.CurrentUrl is not null
            && request.CurrentUrl.AbsolutePath.Equals(context.Request.Path.Value, StringComparison.OrdinalIgnoreCase))
        {
            return routeLayoutFamily;
        }

        return null;
    }

    private static string GetRequestPathAndQuery(HttpRequest request)
    {
        return $"{request.PathBase}{request.Path}{request.QueryString}";
    }
}
