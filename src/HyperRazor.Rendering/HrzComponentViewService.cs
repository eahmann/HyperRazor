using HyperRazor.Components;
using HyperRazor.Htmx;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.Net.Http.Headers;

namespace HyperRazor.Rendering;

public sealed class HrzComponentViewService : IHrzComponentViewService
{
    private const string HtmlContentType = "text/html; charset=utf-8";
    private const string AppShellSelector = "#hrz-app-shell";

    private readonly IHrzHtmlRendererAdapter _renderer;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IHrzLayoutTypeResolver _layoutTypeResolver;
    private readonly HrzOptions _options;

    public HrzComponentViewService(
        IHrzHtmlRendererAdapter renderer,
        IHttpContextAccessor httpContextAccessor,
        IServiceProvider services,
        IOptions<HrzOptions> options)
    {
        _renderer = renderer ?? throw new ArgumentNullException(nameof(renderer));
        _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
        ArgumentNullException.ThrowIfNull(services);
        _layoutTypeResolver = services.GetRequiredService<IHrzLayoutTypeResolver>();
        _options = (options ?? throw new ArgumentNullException(nameof(options))).Value;
    }

    public Task<IResult> View<TComponent>(object? data = null, CancellationToken cancellationToken = default)
        where TComponent : IComponent
    {
        return View<TComponent>(HrzParameterDictionaryFactory.Create(data), cancellationToken);
    }

    public async Task<IResult> View<TComponent>(
        IReadOnlyDictionary<string, object?> data,
        CancellationToken cancellationToken = default)
        where TComponent : IComponent
    {
        ArgumentNullException.ThrowIfNull(data);

        var context = GetHttpContext();
        var request = context.HtmxRequest();
        var layoutType = ResolveLayoutType(typeof(TComponent));
        var targetLayoutKey = HrzLayoutKey.Create(layoutType);
        var modelState = ResolveModelState(context);
        EnsurePageVaryForHtmxBranching(context.Response.Headers);

        var currentLayoutKey = TryReadCurrentLayoutKey(context.Request.Headers, out var layoutKey)
            ? layoutKey
            : null;
        var navigationMode = ResolveNavigationMode(request, currentLayoutKey, targetLayoutKey);
        StorePageNavigationDiagnostics(
            context,
            request,
            currentLayoutKey,
            targetLayoutKey,
            navigationMode);

        if (navigationMode == HrzPageNavigationResponseMode.HxLocation)
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

            return Results.Content(string.Empty, HtmlContentType);
        }

        if (navigationMode == HrzPageNavigationResponseMode.RootSwap)
        {
            context.HtmxResponse()
                .Retarget(AppShellSelector)
                .Reswap("outerHTML")
                .Reselect(AppShellSelector)
                .PushUrl(pushUrl: true);
        }

        var html = await RenderHostAsync(
            componentType: typeof(TComponent),
            componentParameters: data,
            layoutType: layoutType,
            currentLayoutKey: targetLayoutKey,
            context: context,
            modelState: modelState,
            isPartial: false,
            isHtmxRequest: navigationMode == HrzPageNavigationResponseMode.PageFragment,
            isHistoryRestoreRequest: request.IsHistoryRestoreRequest,
            renderHeadContent: navigationMode == HrzPageNavigationResponseMode.RootSwap ? true : null,
            renderSwapContent: navigationMode == HrzPageNavigationResponseMode.RootSwap ? true : null,
            cancellationToken: cancellationToken);

        return Results.Content(html, HtmlContentType);
    }

    public Task<IResult> PartialView<TComponent>(object? data = null, CancellationToken cancellationToken = default)
        where TComponent : IComponent
    {
        return PartialView<TComponent>(HrzParameterDictionaryFactory.Create(data), cancellationToken);
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
        EnsureFragmentVaryForHtmxBranching(context.Response.Headers);

        var html = await RenderHostAsync(
            componentType: typeof(TComponent),
            componentParameters: data,
            layoutType: ResolveLayoutType(typeof(TComponent)),
            currentLayoutKey: null,
            context: context,
            modelState: modelState,
            isPartial: true,
            isHtmxRequest: request.RequestType == HtmxRequestType.Partial,
            isHistoryRestoreRequest: request.IsHistoryRestoreRequest,
            renderHeadContent: null,
            renderSwapContent: null,
            cancellationToken: cancellationToken);

        return Results.Content(html, HtmlContentType);
    }

    public async Task<IResult> PartialView(CancellationToken cancellationToken = default, params RenderFragment[] fragments)
    {
        ArgumentNullException.ThrowIfNull(fragments);

        var context = GetHttpContext();
        var request = context.HtmxRequest();
        var modelState = ResolveModelState(context);
        EnsureFragmentVaryForHtmxBranching(context.Response.Headers);

        var html = await RenderHostAsync(
            componentType: typeof(HrzFragmentGroup),
            componentParameters: new Dictionary<string, object?>
            {
                [nameof(HrzFragmentGroup.Fragments)] = fragments
            },
            layoutType: null,
            currentLayoutKey: null,
            context: context,
            modelState: modelState,
            isPartial: true,
            isHtmxRequest: request.RequestType == HtmxRequestType.Partial,
            isHistoryRestoreRequest: request.IsHistoryRestoreRequest,
            renderHeadContent: null,
            renderSwapContent: null,
            cancellationToken: cancellationToken);

        return Results.Content(html, HtmlContentType);
    }

    private Task<string> RenderHostAsync(
        Type componentType,
        IReadOnlyDictionary<string, object?> componentParameters,
        Type? layoutType,
        string? currentLayoutKey,
        HttpContext context,
        ModelStateDictionary modelState,
        bool isPartial,
        bool isHtmxRequest,
        bool isHistoryRestoreRequest,
        bool? renderHeadContent,
        bool? renderSwapContent,
        CancellationToken cancellationToken)
    {
        var hostParameters = new Dictionary<string, object?>
        {
            [nameof(HrzComponentHost.ComponentType)] = componentType,
            [nameof(HrzComponentHost.ComponentParameters)] = componentParameters,
            [nameof(HrzComponentHost.LayoutType)] = layoutType,
            [nameof(HrzComponentHost.CurrentLayoutKey)] = currentLayoutKey,
            [nameof(HrzComponentHost.RootComponentType)] = _options.RootComponent,
            [nameof(HrzComponentHost.IsPartial)] = isPartial,
            [nameof(HrzComponentHost.IsHtmxRequest)] = isHtmxRequest,
            [nameof(HrzComponentHost.IsHistoryRestoreRequest)] = isHistoryRestoreRequest,
            [nameof(HrzComponentHost.HttpContext)] = context,
            [nameof(HrzComponentHost.ModelState)] = modelState,
            [nameof(HrzComponentHost.RenderHeadContent)] = renderHeadContent,
            [nameof(HrzComponentHost.RenderSwapContent)] = renderSwapContent
        };

        return _renderer.RenderAsync(typeof(HrzComponentHost), hostParameters, cancellationToken);
    }

    private HttpContext GetHttpContext()
    {
        return _httpContextAccessor.HttpContext
            ?? throw new InvalidOperationException("No active HttpContext is available for HyperRazor rendering.");
    }

    private static void EnsurePageVaryForHtmxBranching(IHeaderDictionary headers)
    {
        EnsureVaryBy(headers, HtmxHeaderNames.Request);
        EnsureVaryBy(headers, HtmxHeaderNames.RequestType);
        EnsureVaryBy(headers, HtmxHeaderNames.HistoryRestoreRequest);
        EnsureVaryBy(headers, HtmxHeaderNames.Boosted);
        EnsureVaryBy(headers, HrzInternalHeaderNames.CurrentLayout);
    }

    private static void EnsureFragmentVaryForHtmxBranching(IHeaderDictionary headers)
    {
        EnsureVaryBy(headers, HtmxHeaderNames.Request);
        EnsureVaryBy(headers, HtmxHeaderNames.RequestType);
        EnsureVaryBy(headers, HtmxHeaderNames.HistoryRestoreRequest);
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
        if (context.Items.TryGetValue(HrzContextItemKeys.ModelState, out var value)
            && value is ModelStateDictionary modelState)
        {
            return modelState;
        }

        return new ModelStateDictionary();
    }

    private Type? ResolveLayoutType(Type componentType)
    {
        return _layoutTypeResolver.ResolveForComponent(componentType);
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

    private static HrzPageNavigationResponseMode ResolveNavigationMode(
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

    private static void StorePageNavigationDiagnostics(
        HttpContext context,
        HtmxRequest request,
        string? sourceLayout,
        string targetLayout,
        HrzPageNavigationResponseMode mode)
    {
        context.Items[typeof(HtmxPageNavigationDiagnostics)] = new HtmxPageNavigationDiagnostics(
            CurrentUrl: request.CurrentUrl?.ToString(),
            SourceLayout: sourceLayout,
            TargetLayout: targetLayout,
            Mode: mode.ToString());
    }

    private static string GetRequestPathAndQuery(HttpRequest request)
    {
        return $"{request.PathBase}{request.Path}{request.QueryString}";
    }
}
