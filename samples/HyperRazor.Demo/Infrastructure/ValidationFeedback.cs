using HyperRazor.Components.Validation;
using HyperRazor.Mvc;
using HyperRazor.Rendering;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.DependencyInjection;

namespace HyperRazor.Demo.Infrastructure;

internal static class ValidationFeedback
{
    public static void SetSubmitValidationState(
        HttpContext context,
        ModelStateDictionary modelState,
        HrzValidationRootId rootId)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(modelState);
        ArgumentNullException.ThrowIfNull(rootId);

        var resolver = context.RequestServices.GetRequiredService<IHrzFieldPathResolver>();
        context.Items[HrzContextItemKeys.ModelState] = modelState;
        context.SetSubmitValidationState(modelState.ToSubmitValidationState(
            rootId,
            resolver,
            HrzAttemptedValues.FromRequest(context.Request)));
    }
}
