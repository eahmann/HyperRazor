namespace HyperRazor.Components.Validation;

public interface IHrzForms
{
    HrzFormScope<TModel> For<TModel>(
        TModel model,
        HrzValidationFormAddress address,
        HrzSubmitValidationState? validationState = null,
        HrzLiveValidationOptions? live = null,
        string? idPrefix = null,
        bool enableClientValidation = true);

    HrzFormScope<TModel> For<TModel>(
        TModel model,
        string formName,
        HrzSubmitValidationState? validationState = null,
        HrzLiveValidationOptions? live = null,
        string? idPrefix = null,
        bool enableClientValidation = true);

    HrzFormScope<TModel> For<TModel>(
        TModel model,
        HrzValidationRootId rootId,
        HrzSubmitValidationState? validationState = null,
        HrzLiveValidationOptions? live = null,
        string? idPrefix = null,
        bool enableClientValidation = true);
}
