using HyperRazor.Demo.Mvc.Models;
using HyperRazor.Components.Validation;
using HyperRazor.Htmx;
using HyperRazor.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.DependencyInjection;

namespace HyperRazor.Demo.Mvc.Infrastructure;

internal static class DemoValidationFeedback
{
    public static int CountErrors(HrzSubmitValidationState validationState)
    {
        ArgumentNullException.ThrowIfNull(validationState);

        return validationState.SummaryErrors.Count
            + validationState.FieldErrors.Sum(static pair => pair.Value.Count);
    }

    public static void TriggerInvalid(HttpContext context, HrzSubmitValidationState validationState)
    {
        ArgumentNullException.ThrowIfNull(validationState);

        TriggerInvalid(context, CountErrors(validationState));
    }

    public static void TriggerInvalid(HttpContext context, int errorCount)
    {
        ArgumentNullException.ThrowIfNull(context);

        context.HtmxResponse().Trigger("form:invalid", new
        {
            errorCount
        });
    }

    public static void TriggerValid(HttpContext context, InviteUserInput input, int count)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(input);

        context.HtmxResponse().Trigger("form:valid", new
        {
            name = input.DisplayName,
            email = input.Email,
            count
        });
    }

    public static void SetSubmitValidationState(
        HttpContext context,
        ModelStateDictionary modelState,
        HrzValidationRootId rootId)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(modelState);

        var resolver = context.RequestServices.GetRequiredService<IHrzFieldPathResolver>();
        context.SetSubmitValidationState(modelState.ToSubmitValidationState(
            rootId,
            resolver,
            HrzAttemptedValues.FromRequest(context.Request)));
    }
}
