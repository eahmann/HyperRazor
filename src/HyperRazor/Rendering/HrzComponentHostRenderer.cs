using HyperRazor.Components;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.Options;

namespace HyperRazor.Rendering;

internal interface IHrzComponentHostRenderer
{
    Type? ResolveLayoutType(Type componentType);

    ModelStateDictionary ResolveModelState(HttpContext context);

    Task<string> RenderAsync(HrzComponentHostRenderRequest request, CancellationToken cancellationToken = default);

    Task<string> RenderIsolatedAsync(HrzComponentHostRenderRequest request, CancellationToken cancellationToken = default);
}

internal sealed class HrzComponentHostRenderer : IHrzComponentHostRenderer
{
    private readonly IHrzHtmlRendererAdapter _renderer;
    private readonly IHrzLayoutTypeResolver _layoutTypeResolver;
    private readonly HrzOptions _options;

    public HrzComponentHostRenderer(
        IHrzHtmlRendererAdapter renderer,
        IHrzLayoutTypeResolver layoutTypeResolver,
        IOptions<HrzOptions> options)
    {
        _renderer = renderer ?? throw new ArgumentNullException(nameof(renderer));
        _layoutTypeResolver = layoutTypeResolver ?? throw new ArgumentNullException(nameof(layoutTypeResolver));
        _options = (options ?? throw new ArgumentNullException(nameof(options))).Value;
    }

    public Type? ResolveLayoutType(Type componentType)
    {
        ArgumentNullException.ThrowIfNull(componentType);
        return _layoutTypeResolver.ResolveForComponent(componentType);
    }

    public ModelStateDictionary ResolveModelState(HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (context.Items.TryGetValue(HrzContextItemKeys.ModelState, out var value)
            && value is ModelStateDictionary modelState)
        {
            return modelState;
        }

        return new ModelStateDictionary();
    }

    public Task<string> RenderAsync(HrzComponentHostRenderRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        return _renderer.RenderAsync(typeof(HrzComponentHost), CreateHostParameters(request), cancellationToken);
    }

    public Task<string> RenderIsolatedAsync(HrzComponentHostRenderRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        return _renderer.RenderIsolatedAsync(typeof(HrzComponentHost), CreateHostParameters(request), cancellationToken);
    }

    private IReadOnlyDictionary<string, object?> CreateHostParameters(HrzComponentHostRenderRequest request)
    {
        return new Dictionary<string, object?>
        {
            [nameof(HrzComponentHost.ComponentType)] = request.ComponentType,
            [nameof(HrzComponentHost.ComponentParameters)] = request.ComponentParameters,
            [nameof(HrzComponentHost.LayoutType)] = request.LayoutType,
            [nameof(HrzComponentHost.CurrentLayoutKey)] = request.CurrentLayoutKey,
            [nameof(HrzComponentHost.RootComponentType)] = _options.RootComponent,
            [nameof(HrzComponentHost.IsPartial)] = request.IsPartial,
            [nameof(HrzComponentHost.IsHtmxRequest)] = request.IsHtmxRequest,
            [nameof(HrzComponentHost.IsHistoryRestoreRequest)] = request.IsHistoryRestoreRequest,
            [nameof(HrzComponentHost.HttpContext)] = request.HttpContext,
            [nameof(HrzComponentHost.ModelState)] = request.ModelState,
            [nameof(HrzComponentHost.RenderHeadContent)] = request.RenderHeadContent,
            [nameof(HrzComponentHost.RenderSwapContent)] = request.RenderSwapContent
        };
    }
}
