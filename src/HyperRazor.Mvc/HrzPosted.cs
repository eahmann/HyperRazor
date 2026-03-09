using HyperRazor.Rendering;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;

namespace HyperRazor.Mvc;

public sealed class HrzPosted<TModel>
    where TModel : class, new()
{
    public required HttpContext HttpContext { get; init; }

    public required HrzValidationRootId RootId { get; init; }

    public required TModel Model { get; init; }

    public required HrzSubmitValidationState ValidationState { get; init; }

    public bool IsValid => ValidationState.IsValid;

    public Task<IResult> Invalid<TComponent>(
        object? data = null,
        CancellationToken cancellationToken = default)
        where TComponent : IComponent =>
        Invalid<TComponent>(ValidationState, data, cancellationToken);

    public Task<IResult> Invalid<TComponent>(
        HrzSubmitValidationState validationState,
        object? data = null,
        CancellationToken cancellationToken = default)
        where TComponent : IComponent
    {
        ArgumentNullException.ThrowIfNull(validationState);

        HttpContext.SetSubmitValidationState(validationState);
        return HrzValidationRequestRenderer.RenderForRequestAsync<TComponent>(HttpContext, data, cancellationToken);
    }

    public Task<IResult> Valid<TComponent>(
        object? data = null,
        CancellationToken cancellationToken = default)
        where TComponent : IComponent
    {
        HttpContext.ClearSubmitValidationState();
        return HrzValidationRequestRenderer.RenderForRequestAsync<TComponent>(HttpContext, data, cancellationToken);
    }

    public static async ValueTask<HrzPosted<TModel>> BindAsync(HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var rootId = await ReadRootIdAsync(context, context.RequestAborted);
        var formPostState = await context.BindFormAndValidateAsync<TModel>(rootId, context.RequestAborted);

        return new HrzPosted<TModel>
        {
            HttpContext = context,
            RootId = rootId,
            Model = formPostState.Model,
            ValidationState = formPostState.ValidationState
        };
    }

    private static async Task<HrzValidationRootId> ReadRootIdAsync(HttpContext context, CancellationToken cancellationToken)
    {
        var form = context.Request.HasFormContentType
            ? await context.Request.ReadFormAsync(cancellationToken)
            : null;
        var rootValue = ReadValue(form, context.Request.Query, HrzValidationFormFields.Root);
        if (string.IsNullOrWhiteSpace(rootValue))
        {
            throw new BadHttpRequestException(
                $"Required form field '{HrzValidationFormFields.Root}' was not supplied.");
        }

        return new HrzValidationRootId(rootValue.Trim());
    }

    private static string? ReadValue(IFormCollection? form, IQueryCollection query, string key)
    {
        if (form is not null
            && form.TryGetValue(key, out var formValue)
            && !StringValues.IsNullOrEmpty(formValue))
        {
            return formValue.ToString();
        }

        return query.TryGetValue(key, out var queryValue) && !StringValues.IsNullOrEmpty(queryValue)
            ? queryValue.ToString()
            : null;
    }
}
