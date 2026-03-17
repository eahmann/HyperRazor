using HyperRazor.Components.Validation;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace HyperRazor.Mvc;

internal sealed class HrzFormPostBinder : IHrzFormPostBinder
{
    public async Task<HrzFormPostState<TModel>> BindAsync<TModel>(
        HttpContext context,
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

    public async Task<HrzFormPostState<TModel>> BindAndValidateAsync<TModel>(
        HttpContext context,
        HrzValidationRootId rootId,
        CancellationToken cancellationToken = default)
        where TModel : class, new()
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(rootId);

        var formPostState = await BindAsync<TModel>(context, rootId, cancellationToken);
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
}
