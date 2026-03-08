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

    private IHrzComponentViewService ViewService =>
        HttpContext.RequestServices.GetRequiredService<IHrzComponentViewService>();

    private void CaptureValidationState(HrzValidationRootId? validationRootId)
    {
        HttpContext.Items[HrzContextItemKeys.ModelState] = ModelState;

        if (validationRootId is null)
        {
            return;
        }

        var resolver = HttpContext.RequestServices.GetRequiredService<IHrzFieldPathResolver>();
        var attemptedValues = HrzAttemptedValues.FromRequest(Request);
        HttpContext.SetSubmitValidationState(ModelState.ToSubmitValidationState(validationRootId, resolver, attemptedValues));
    }
}
