using HyperRazor.Rendering;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;

namespace HyperRazor.Mvc;

public abstract class HrController : ControllerBase
{
    protected Task<IResult> View<TComponent>(
        object? data = null,
        CancellationToken cancellationToken = default,
        HrzValidationRootId? validationRootId = null)
        where TComponent : IComponent
    {
        CaptureValidationState(validationRootId);
        return ViewService.View<TComponent>(data, cancellationToken);
    }

    protected Task<IResult> View<TComponent>(
        IReadOnlyDictionary<string, object?> data,
        CancellationToken cancellationToken = default,
        HrzValidationRootId? validationRootId = null)
        where TComponent : IComponent
    {
        ArgumentNullException.ThrowIfNull(data);

        CaptureValidationState(validationRootId);
        return ViewService.View<TComponent>(data, cancellationToken);
    }

    protected Task<IResult> PartialView<TComponent>(
        object? data = null,
        CancellationToken cancellationToken = default,
        HrzValidationRootId? validationRootId = null)
        where TComponent : IComponent
    {
        CaptureValidationState(validationRootId);
        return ViewService.PartialView<TComponent>(data, cancellationToken);
    }

    protected Task<IResult> PartialView<TComponent>(
        IReadOnlyDictionary<string, object?> data,
        CancellationToken cancellationToken = default,
        HrzValidationRootId? validationRootId = null)
        where TComponent : IComponent
    {
        ArgumentNullException.ThrowIfNull(data);

        CaptureValidationState(validationRootId);
        return ViewService.PartialView<TComponent>(data, cancellationToken);
    }

    protected Task<IResult> PartialView(CancellationToken cancellationToken = default, params RenderFragment[] fragments)
    {
        ArgumentNullException.ThrowIfNull(fragments);

        CaptureValidationState(validationRootId: null);
        return ViewService.PartialView(cancellationToken, fragments);
    }

    protected Task<IResult> HrzInvalid<TComponent>(
        object? data = null,
        CancellationToken cancellationToken = default,
        HrzValidationRootId? validationRootId = null)
        where TComponent : IComponent
    {
        var validationState = validationRootId is not null
            ? HrzValidationRequestRenderer.NormalizeModelState(HttpContext, ModelState, validationRootId)
            : HttpContext.GetSubmitValidationState()
                ?? throw new InvalidOperationException(
                    $"{nameof(HrzInvalid)} requires either an explicit {nameof(HrzValidationRootId)} or a pre-populated {nameof(HrzSubmitValidationState)}.");

        return HrzInvalid<TComponent>(validationState, data, cancellationToken);
    }

    protected Task<IResult> HrzInvalid<TComponent>(
        HrzSubmitValidationState validationState,
        object? data = null,
        CancellationToken cancellationToken = default)
        where TComponent : IComponent
    {
        ArgumentNullException.ThrowIfNull(validationState);

        HrzValidationRequestRenderer.CaptureModelState(HttpContext, ModelState);
        HttpContext.SetSubmitValidationState(validationState);
        return HrzValidationRequestRenderer.RenderForRequestAsync<TComponent>(HttpContext, data, cancellationToken);
    }

    protected Task<IResult> HrzValid<TComponent>(
        object? data = null,
        CancellationToken cancellationToken = default)
        where TComponent : IComponent
    {
        HrzValidationRequestRenderer.CaptureModelState(HttpContext, ModelState);
        HttpContext.ClearSubmitValidationState();
        return HrzValidationRequestRenderer.RenderForRequestAsync<TComponent>(HttpContext, data, cancellationToken);
    }

    private IHrzComponentViewService ViewService =>
        HttpContext.RequestServices.GetRequiredService<IHrzComponentViewService>();

    private void CaptureValidationState(HrzValidationRootId? validationRootId)
    {
        HrzValidationRequestRenderer.CaptureModelState(HttpContext, ModelState);

        if (validationRootId is null)
        {
            return;
        }

        HttpContext.SetSubmitValidationState(HrzValidationRequestRenderer.NormalizeModelState(HttpContext, ModelState, validationRootId));
    }
}
