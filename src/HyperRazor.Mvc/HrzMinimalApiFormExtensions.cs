using HyperRazor.Rendering;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;

namespace HyperRazor.Mvc;

public static class HrzMinimalApiFormExtensions
{
    private const string ValidationRootField = "__hrz_root";
    private const string ValidationFieldsField = "__hrz_fields";
    private const string ValidateAllField = "__hrz_validate_all";

    public static async Task<HrzFormPostState<TModel>> BindFormAsync<TModel>(
        this HttpContext context,
        HrzValidationRootId rootId,
        CancellationToken cancellationToken = default)
        where TModel : class, new()
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(rootId);

        cancellationToken.ThrowIfCancellationRequested();

        var services = context.RequestServices;
        var metadataProvider = services.GetRequiredService<IModelMetadataProvider>();
        var binderFactory = services.GetRequiredService<IModelBinderFactory>();
        var fieldPathResolver = services.GetRequiredService<IHrzFieldPathResolver>();
        var actionContext = new ActionContext(context, context.GetRouteData(), new ActionDescriptor(), new ModelStateDictionary());
        var modelMetadata = metadataProvider.GetMetadataForType(typeof(TModel));
        var bindingInfo = new BindingInfo
        {
            BindingSource = BindingSource.Form
        };
        var modelBinder = binderFactory.CreateBinder(new ModelBinderFactoryContext
        {
            BindingInfo = bindingInfo,
            Metadata = modelMetadata,
            CacheToken = modelMetadata
        });
        var valueProvider = await CreateValueProviderAsync(actionContext, services);
        var bindingContext = DefaultModelBindingContext.CreateBindingContext(
            actionContext,
            valueProvider,
            modelMetadata,
            bindingInfo,
            modelName: string.Empty);

        bindingContext.ModelName = string.Empty;
        bindingContext.FieldName = string.Empty;

        await modelBinder.BindModelAsync(bindingContext);

        var model = bindingContext.Result.Model as TModel ?? new TModel();
        var attemptedValues = HrzAttemptedValues.FromRequest(context.Request);
        var submitValidationState = actionContext.ModelState.ToSubmitValidationState(rootId, fieldPathResolver, attemptedValues);

        return new HrzFormPostState<TModel>(model, submitValidationState);
    }

    public static async Task<HrzFormPostState<TModel>> BindFormAndValidateAsync<TModel>(
        this HttpContext context,
        HrzValidationRootId rootId,
        CancellationToken cancellationToken = default)
        where TModel : class, new()
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(rootId);

        var formPostState = await context.BindFormAsync<TModel>(rootId, cancellationToken);
        if (!formPostState.ValidationState.IsValid)
        {
            return formPostState;
        }

        var validator = context.RequestServices.GetRequiredService<IHrzModelValidator>();
        var validatedState = validator.Validate(formPostState.Model, rootId, formPostState.ValidationState.AttemptedValues);

        return new HrzFormPostState<TModel>(
            formPostState.Model,
            formPostState.ValidationState.Merge(validatedState));
    }

    public static async Task<HrzValidationScope?> BindLiveValidationScopeAsync(
        this HttpContext context,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);

        cancellationToken.ThrowIfCancellationRequested();

        var resolver = context.RequestServices.GetRequiredService<IHrzFieldPathResolver>();
        var form = context.Request.HasFormContentType
            ? await context.Request.ReadFormAsync(cancellationToken)
            : null;
        var rootValue = ReadValue(form, context.Request.Query, ValidationRootField);
        if (string.IsNullOrWhiteSpace(rootValue))
        {
            return null;
        }

        var fieldList = ReadValue(form, context.Request.Query, ValidationFieldsField);
        var validateAll = bool.TryParse(ReadValue(form, context.Request.Query, ValidateAllField), out var parsedValidateAll)
            && parsedValidateAll;
        var fields = string.IsNullOrWhiteSpace(fieldList)
            ? Array.Empty<HrzFieldPath>()
            : fieldList
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(resolver.FromFieldName)
                .ToArray();

        return new HrzValidationScope(new HrzValidationRootId(rootValue.Trim()), validateAll, fields);
    }

    private static async Task<CompositeValueProvider> CreateValueProviderAsync(
        ActionContext actionContext,
        IServiceProvider services)
    {
        var options = services.GetRequiredService<IOptions<MvcOptions>>().Value;
        var valueProviderFactoryContext = new ValueProviderFactoryContext(actionContext);

        foreach (var valueProviderFactory in options.ValueProviderFactories)
        {
            await valueProviderFactory.CreateValueProviderAsync(valueProviderFactoryContext);
        }

        return new CompositeValueProvider(valueProviderFactoryContext.ValueProviders);
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
