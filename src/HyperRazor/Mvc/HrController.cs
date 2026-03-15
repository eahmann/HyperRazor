using HyperRazor.Components.Validation;
using HyperRazor.Rendering;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;

namespace HyperRazor.Mvc;

public abstract class HrController : ControllerBase
{
    protected Task<IResult> Page<TComponent>(
        object? data = null,
        CancellationToken cancellationToken = default,
        HrzValidationRootId? validationRootId = null)
        where TComponent : IComponent
    {
        CaptureValidationState(validationRootId);
        return RenderService.Page<TComponent>(data, cancellationToken);
    }

    protected Task<IResult> Page<TComponent>(
        IReadOnlyDictionary<string, object?> data,
        CancellationToken cancellationToken = default,
        HrzValidationRootId? validationRootId = null)
        where TComponent : IComponent
    {
        ArgumentNullException.ThrowIfNull(data);

        CaptureValidationState(validationRootId);
        return RenderService.Page<TComponent>(data, cancellationToken);
    }

    protected Task<IResult> Fragment<TComponent>(
        object? data = null,
        CancellationToken cancellationToken = default,
        HrzValidationRootId? validationRootId = null)
        where TComponent : IComponent
    {
        CaptureValidationState(validationRootId);
        return RenderService.Fragment<TComponent>(data, cancellationToken);
    }

    protected Task<IResult> Fragment<TComponent>(
        IReadOnlyDictionary<string, object?> data,
        CancellationToken cancellationToken = default,
        HrzValidationRootId? validationRootId = null)
        where TComponent : IComponent
    {
        ArgumentNullException.ThrowIfNull(data);

        CaptureValidationState(validationRootId);
        return RenderService.Fragment<TComponent>(data, cancellationToken);
    }

    protected Task<IResult> Fragment(CancellationToken cancellationToken = default, params RenderFragment[] fragments)
    {
        ArgumentNullException.ThrowIfNull(fragments);

        CaptureValidationState(validationRootId: null);
        return RenderService.Fragment(cancellationToken, fragments);
    }

    protected Task<IResult> RootSwap<TComponent>(
        object? data = null,
        CancellationToken cancellationToken = default,
        HrzValidationRootId? validationRootId = null)
        where TComponent : IComponent
    {
        CaptureValidationState(validationRootId);
        return RenderService.RootSwap<TComponent>(data, cancellationToken);
    }

    private IHrzRenderService RenderService =>
        HrzRegistrationRequirements.ResolveRenderService(HttpContext.RequestServices);

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
