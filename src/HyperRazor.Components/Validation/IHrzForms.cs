namespace HyperRazor.Components.Validation;

public interface IHrzForms
{
    HrzFormView<TModel> For<TModel>(
        TModel model,
        string formName,
        HrzSubmitValidationState? validationState = null,
        HrzLiveValidationOptions? live = null,
        string? idPrefix = null,
        bool enableClientValidation = true);

    HrzFormView<TModel> For<TModel>(
        TModel model,
        HrzValidationRootId rootId,
        HrzSubmitValidationState? validationState = null,
        HrzLiveValidationOptions? live = null,
        string? idPrefix = null,
        bool enableClientValidation = true);
}
