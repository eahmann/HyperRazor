using HyperRazor.Components;
using HyperRazor.Htmx.AspNetCore;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.Options;

namespace HyperRazor.Rendering;

public sealed class HrxComponentViewService : IHrxComponentViewService
{
    private const string HtmlContentType = "text/html; charset=utf-8";

    private readonly IHrxHtmlRendererAdapter _renderer;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly HrxOptions _options;

    public HrxComponentViewService(
        IHrxHtmlRendererAdapter renderer,
        IHttpContextAccessor httpContextAccessor,
        IOptions<HrxOptions> options)
    {
        _renderer = renderer ?? throw new ArgumentNullException(nameof(renderer));
        _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
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
        var isHtmxRequest = IsHtmxRequestForLayout(context);
        var modelState = ResolveModelState(context);

        var html = await RenderHostAsync(
            componentType: typeof(TComponent),
            componentParameters: data,
            context: context,
            modelState: modelState,
            isPartial: false,
            isHtmxRequest: isHtmxRequest,
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
        var modelState = ResolveModelState(context);

        var html = await RenderHostAsync(
            componentType: typeof(TComponent),
            componentParameters: data,
            context: context,
            modelState: modelState,
            isPartial: true,
            isHtmxRequest: true,
            cancellationToken: cancellationToken);

        return Results.Content(html, HtmlContentType);
    }

    public async Task<IResult> PartialView(CancellationToken cancellationToken = default, params RenderFragment[] fragments)
    {
        ArgumentNullException.ThrowIfNull(fragments);

        var context = GetHttpContext();
        var modelState = ResolveModelState(context);

        var html = await RenderHostAsync(
            componentType: typeof(HrxFragmentGroup),
            componentParameters: new Dictionary<string, object?>
            {
                [nameof(HrxFragmentGroup.Fragments)] = fragments
            },
            context: context,
            modelState: modelState,
            isPartial: true,
            isHtmxRequest: true,
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
        CancellationToken cancellationToken)
    {
        var hostParameters = new Dictionary<string, object?>
        {
            [nameof(HrxComponentHost.ComponentType)] = componentType,
            [nameof(HrxComponentHost.ComponentParameters)] = componentParameters,
            [nameof(HrxComponentHost.RootComponentType)] = _options.RootComponent,
            [nameof(HrxComponentHost.IsPartial)] = isPartial,
            [nameof(HrxComponentHost.IsHtmxRequest)] = isHtmxRequest,
            [nameof(HrxComponentHost.UseMinimalLayoutForHtmx)] = _options.UseMinimalLayoutForHtmx,
            [nameof(HrxComponentHost.HttpContext)] = context,
            [nameof(HrxComponentHost.ModelState)] = modelState
        };

        return _renderer.RenderAsync(typeof(HrxComponentHost), hostParameters, cancellationToken);
    }

    private HttpContext GetHttpContext()
    {
        return _httpContextAccessor.HttpContext
            ?? throw new InvalidOperationException("No active HttpContext is available for HyperRazor rendering.");
    }

    private static bool IsHtmxRequestForLayout(HttpContext context)
    {
        var request = context.HtmxRequest();
        return request.IsHtmx && !request.IsHistoryRestoreRequest;
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
}
