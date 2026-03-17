using HyperRazor.Components.Validation;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace HyperRazor.Mvc;

public static class HrzMinimalApiFormExtensions
{
    public static async Task<HrzFormPostState<TModel>> BindFormAsync<TModel>(
        this HttpContext context,
        HrzValidationRootId rootId,
        CancellationToken cancellationToken = default)
        where TModel : class, new()
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(rootId);

        var binder = context.RequestServices.GetRequiredService<IHrzFormPostBinder>();
        return await binder.BindAsync<TModel>(context, rootId, cancellationToken);
    }

    public static async Task<HrzFormPostState<TModel>> BindFormAndValidateAsync<TModel>(
        this HttpContext context,
        HrzValidationRootId rootId,
        CancellationToken cancellationToken = default)
        where TModel : class, new()
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(rootId);

        var binder = context.RequestServices.GetRequiredService<IHrzFormPostBinder>();
        return await binder.BindAndValidateAsync<TModel>(context, rootId, cancellationToken);
    }

    public static async Task<HrzLiveValidationRequest?> BindLiveValidationRequestAsync(
        this HttpContext context,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);

        var binder = context.RequestServices.GetRequiredService<IHrzLiveValidationRequestBinder>();
        return await binder.BindAsync(context, cancellationToken);
    }

    [Obsolete("Use BindLiveValidationRequestAsync instead.")]
    public static Task<HrzLiveValidationRequest?> BindLiveValidationScopeAsync(
        this HttpContext context,
        CancellationToken cancellationToken = default)
    {
        return context.BindLiveValidationRequestAsync(cancellationToken);
    }
}
