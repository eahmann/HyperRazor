using HyperRazor.Htmx;
using HyperRazor.Rendering;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.DependencyInjection;

namespace HyperRazor.Mvc;

internal static class HrzValidationRequestRenderer
{
    public static HrzSubmitValidationState NormalizeModelState(
        HttpContext context,
        ModelStateDictionary modelState,
        HrzValidationRootId rootId)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(modelState);
        ArgumentNullException.ThrowIfNull(rootId);

        var resolver = context.RequestServices.GetRequiredService<IHrzFieldPathResolver>();
        var attemptedValues = HrzAttemptedValues.FromRequest(context.Request);
        return modelState.ToSubmitValidationState(rootId, resolver, attemptedValues);
    }

    public static void CaptureModelState(HttpContext context, ModelStateDictionary modelState)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(modelState);

        context.Items[HrzContextItemKeys.ModelState] = modelState;
    }

    public static Task<IResult> RenderForRequestAsync<TComponent>(
        HttpContext context,
        object? data = null,
        CancellationToken cancellationToken = default)
        where TComponent : IComponent
    {
        ArgumentNullException.ThrowIfNull(context);

        var viewService = context.RequestServices.GetRequiredService<IHrzComponentViewService>();
        return context.HtmxRequest().IsHtmx
            ? viewService.PartialView<TComponent>(data, cancellationToken)
            : viewService.View<TComponent>(data, cancellationToken);
    }
}
