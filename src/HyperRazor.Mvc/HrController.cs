using HyperRazor.Rendering;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;

namespace HyperRazor.Mvc;

public abstract class HrController : ControllerBase
{
    protected Task<IResult> View<TComponent>(object? data = null, CancellationToken cancellationToken = default)
        where TComponent : IComponent
    {
        CaptureModelState();
        return ViewService.View<TComponent>(data, cancellationToken);
    }

    protected Task<IResult> View<TComponent>(IReadOnlyDictionary<string, object?> data, CancellationToken cancellationToken = default)
        where TComponent : IComponent
    {
        ArgumentNullException.ThrowIfNull(data);

        CaptureModelState();
        return ViewService.View<TComponent>(data, cancellationToken);
    }

    protected Task<IResult> PartialView<TComponent>(object? data = null, CancellationToken cancellationToken = default)
        where TComponent : IComponent
    {
        CaptureModelState();
        return ViewService.PartialView<TComponent>(data, cancellationToken);
    }

    protected Task<IResult> PartialView<TComponent>(IReadOnlyDictionary<string, object?> data, CancellationToken cancellationToken = default)
        where TComponent : IComponent
    {
        ArgumentNullException.ThrowIfNull(data);

        CaptureModelState();
        return ViewService.PartialView<TComponent>(data, cancellationToken);
    }

    protected Task<IResult> PartialView(CancellationToken cancellationToken = default, params RenderFragment[] fragments)
    {
        ArgumentNullException.ThrowIfNull(fragments);

        CaptureModelState();
        return ViewService.PartialView(cancellationToken, fragments);
    }

    private IHrzComponentViewService ViewService =>
        HttpContext.RequestServices.GetRequiredService<IHrzComponentViewService>();

    private void CaptureModelState()
    {
        HttpContext.Items[HrzContextItemKeys.ModelState] = ModelState;
    }
}
