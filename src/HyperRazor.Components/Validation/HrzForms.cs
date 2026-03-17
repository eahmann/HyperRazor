using Microsoft.AspNetCore.Http;

namespace HyperRazor.Components.Validation;

internal interface IHrzValidationScopeFactory
{
    HrzFormScope<TModel> CreateFormScope<TModel>(
        TModel model,
        HrzValidationRootId rootId,
        string? idPrefix,
        HrzSubmitValidationState? validationState,
        bool enableClientValidation,
        HrzLiveValidationOptions? live);

    HrzFieldScope<TValue> CreateFieldScope<TValue>(
        HrzFormScope form,
        System.Linq.Expressions.Expression<Func<TValue>> accessor,
        Func<TValue> compiledAccessor,
        string? label,
        bool? enableClientValidation,
        HrzFieldLiveOptions? live);
}

public sealed class HrzForms : IHrzForms
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IHrzValidationScopeFactory _scopeFactory;

    internal HrzForms(
        IHttpContextAccessor httpContextAccessor,
        IHrzValidationScopeFactory scopeFactory)
    {
        _httpContextAccessor = httpContextAccessor;
        _scopeFactory = scopeFactory;
    }

    public HrzFormScope<TModel> For<TModel>(
        TModel model,
        string formName,
        HrzSubmitValidationState? validationState = null,
        HrzLiveValidationOptions? live = null,
        string? idPrefix = null,
        bool enableClientValidation = true)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(formName);

        return For(
            model,
            new HrzValidationRootId(formName),
            validationState,
            live,
            idPrefix,
            enableClientValidation);
    }

    public HrzFormScope<TModel> For<TModel>(
        TModel model,
        HrzValidationRootId rootId,
        HrzSubmitValidationState? validationState = null,
        HrzLiveValidationOptions? live = null,
        string? idPrefix = null,
        bool enableClientValidation = true)
    {
        ArgumentNullException.ThrowIfNull(model);
        ArgumentNullException.ThrowIfNull(rootId);

        validationState ??= _httpContextAccessor.HttpContext?.GetSubmitValidationState(rootId);

        return _scopeFactory.CreateFormScope(
            model,
            rootId,
            idPrefix,
            validationState,
            enableClientValidation,
            live);
    }
}
