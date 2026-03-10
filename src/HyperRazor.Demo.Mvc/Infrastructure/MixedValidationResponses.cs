using HyperRazor.Demo.Mvc.Components.Fragments;
using HyperRazor.Demo.Mvc.Components.Pages.Admin;
using HyperRazor.Demo.Mvc.Models;
using HyperRazor.Htmx;
using HyperRazor.Mvc;
using Microsoft.AspNetCore.Components;

namespace HyperRazor.Demo.Mvc.Infrastructure;

public static class MixedValidationResponses
{
    public static Task<IResult> RenderValidationAsync(
        HttpContext context,
        string validationPageParameterName,
        MixedValidationFormViewModel form,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(validationPageParameterName);
        ArgumentNullException.ThrowIfNull(form);

        if (context.HtmxRequest().IsHtmx)
        {
            return HrzResults.Validation<MixedValidationAuthoringForm>(
                context,
                new
                {
                    Form = form
                },
                cancellationToken: cancellationToken);
        }

        return HrzResults.Page<ValidationPage>(
            context,
            new Dictionary<string, object?>
            {
                [validationPageParameterName] = form
            },
            cancellationToken: cancellationToken);
    }
}
