using HyperRazor.Components;
using HyperRazor.Htmx;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace HyperRazor.Rendering;

public sealed class HrzRenderService : IHrzRenderService
{
    private const string HtmlContentType = "text/html; charset=utf-8";

    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IHrzComponentHostRenderer _hostRenderer;

    public HrzRenderService(
        IHttpContextAccessor httpContextAccessor,
        IServiceProvider services)
    {
        _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
        ArgumentNullException.ThrowIfNull(services);
        _hostRenderer = services.GetService<IHrzComponentHostRenderer>()
            ?? new HrzComponentHostRenderer(
                services.GetRequiredService<IHrzHtmlRendererAdapter>(),
                services.GetRequiredService<IHrzLayoutTypeResolver>(),
                services.GetRequiredService<IOptions<HrzOptions>>());
    }

    public Task<IResult> Page<TComponent>(object? data = null, CancellationToken cancellationToken = default)
        where TComponent : IComponent
    {
        return RenderPageAsync<TComponent>(
            HrzParameterDictionaryFactory.Create(data),
            rootSwapOverride: false,
            cancellationToken);
    }

    public Task<IResult> RootSwap<TComponent>(object? data = null, CancellationToken cancellationToken = default)
        where TComponent : IComponent
    {
        return RenderPageAsync<TComponent>(
            HrzParameterDictionaryFactory.Create(data),
            rootSwapOverride: true,
            cancellationToken);
    }

    public Task<IResult> Fragment<TComponent>(object? data = null, CancellationToken cancellationToken = default)
        where TComponent : IComponent
    {
        return RenderFragmentAsync<TComponent>(
            HrzParameterDictionaryFactory.Create(data),
            cancellationToken);
    }

    public async Task<IResult> Fragment(CancellationToken cancellationToken = default, params RenderFragment[] fragments)
    {
        ArgumentNullException.ThrowIfNull(fragments);

        var context = GetHttpContext();
        var request = context.HtmxRequest();
        var modelState = _hostRenderer.ResolveModelState(context);
        EnsureFragmentVaryForHtmxBranching(context.Response.Headers);

        var html = await _hostRenderer.RenderAsync(
            new HrzComponentHostRenderRequest(
                ComponentType: typeof(HrzFragmentGroup),
                ComponentParameters: new Dictionary<string, object?>
                {
                    [nameof(HrzFragmentGroup.Fragments)] = fragments
                },
                LayoutType: null,
                CurrentLayoutKey: null,
                HttpContext: context,
                ModelState: modelState,
                IsPartial: true,
                IsHtmxRequest: request.RequestType == HtmxRequestType.Partial,
                IsHistoryRestoreRequest: request.IsHistoryRestoreRequest,
                RenderHeadContent: null,
                RenderSwapContent: null),
            cancellationToken);

        return Results.Content(html, HtmlContentType);
    }

    private async Task<IResult> RenderPageAsync<TComponent>(
        IReadOnlyDictionary<string, object?> data,
        bool rootSwapOverride,
        CancellationToken cancellationToken = default)
        where TComponent : IComponent
    {
        ArgumentNullException.ThrowIfNull(data);

        var context = GetHttpContext();
        var request = context.HtmxRequest();
        var layoutType = _hostRenderer.ResolveLayoutType(typeof(TComponent));
        var modelState = _hostRenderer.ResolveModelState(context);
        EnsurePageVaryForHtmxBranching(context.Response.Headers);

        var navigationPlan = HrzPageNavigationPlanner.Create(
            context,
            request,
            HrzLayoutKey.Create(layoutType),
            rootSwapOverride);
        HrzPageNavigationPlanner.StoreDiagnostics(context, request, navigationPlan);

        var immediateResult = HrzPageNavigationPlanner.TryCreateImmediateResult(
            context,
            request,
            navigationPlan,
            HtmlContentType);
        if (immediateResult is not null)
        {
            return immediateResult;
        }

        var html = await _hostRenderer.RenderAsync(
            new HrzComponentHostRenderRequest(
                ComponentType: typeof(TComponent),
                ComponentParameters: data,
                LayoutType: layoutType,
                CurrentLayoutKey: navigationPlan.TargetLayoutKey,
                HttpContext: context,
                ModelState: modelState,
                IsPartial: false,
                IsHtmxRequest: navigationPlan.Mode == HrzPageNavigationResponseMode.PageFragment,
                IsHistoryRestoreRequest: request.IsHistoryRestoreRequest,
                RenderHeadContent: navigationPlan.RenderHeadContent,
                RenderSwapContent: navigationPlan.RenderSwapContent),
            cancellationToken);

        return Results.Content(html, HtmlContentType);
    }

    private async Task<IResult> RenderFragmentAsync<TComponent>(
        IReadOnlyDictionary<string, object?> data,
        CancellationToken cancellationToken = default)
        where TComponent : IComponent
    {
        ArgumentNullException.ThrowIfNull(data);

        var context = GetHttpContext();
        var request = context.HtmxRequest();
        var modelState = _hostRenderer.ResolveModelState(context);
        EnsureFragmentVaryForHtmxBranching(context.Response.Headers);

        var html = await _hostRenderer.RenderAsync(
            new HrzComponentHostRenderRequest(
                ComponentType: typeof(TComponent),
                ComponentParameters: data,
                LayoutType: _hostRenderer.ResolveLayoutType(typeof(TComponent)),
                CurrentLayoutKey: null,
                HttpContext: context,
                ModelState: modelState,
                IsPartial: true,
                IsHtmxRequest: request.RequestType == HtmxRequestType.Partial,
                IsHistoryRestoreRequest: request.IsHistoryRestoreRequest,
                RenderHeadContent: null,
                RenderSwapContent: null),
            cancellationToken);

        return Results.Content(html, HtmlContentType);
    }

    private HttpContext GetHttpContext()
    {
        return _httpContextAccessor.HttpContext
            ?? throw new InvalidOperationException("No active HttpContext is available for HyperRazor rendering.");
    }

    private static void EnsurePageVaryForHtmxBranching(IHeaderDictionary headers)
    {
        HtmxVaryHeaders.EnsureForRequestBranching(headers);
        HtmxVaryHeaders.EnsureBy(headers, HtmxHeaderNames.Boosted);
        HtmxVaryHeaders.EnsureBy(headers, HrzInternalHeaderNames.CurrentLayout);
    }

    private static void EnsureFragmentVaryForHtmxBranching(IHeaderDictionary headers)
    {
        HtmxVaryHeaders.EnsureForRequestBranching(headers);
    }
}
