using HyperRazor.Components.Validation;
using Microsoft.AspNetCore.Components.Forms;

namespace HyperRazor.Components;

internal sealed class HrzValidationFormContext
{
    public required HrzFormView View { get; init; }

    public object Model => View.Model;

    public HrzValidationRootId RootId => View.RootId;

    public string IdPrefix => View.IdPrefix;

    public HrzSubmitValidationState? ValidationState => View.ValidationState;

    public bool EnableClientValidation => View.EnableClientValidation;

    public HrzLiveValidationOptions? Live => View.Live;

    public string SummaryId => View.SummaryId;

    public string FormId => View.FormId;

    public string LivePolicyRegionId => View.LivePolicyRegionId;

    public IReadOnlyList<HrzFieldView> RegisteredLiveFields => View.RegisteredLiveFields;

    public static HrzValidationFormContext Create(HrzFormView view)
    {
        ArgumentNullException.ThrowIfNull(view);
        return new HrzValidationFormContext { View = view };
    }

    public static HrzValidationFormContext? ResolveFrom(EditContext? editContext)
    {
        return HrzEditFormState.TryGetFormView(editContext, out var formView)
            ? Create(formView!)
            : null;
    }

    public HrzFieldView<TValue> Field<TValue>(
        System.Linq.Expressions.Expression<Func<TValue>> accessor,
        Func<TValue> compiledAccessor,
        string? label,
        bool? enableClientValidation,
        HrzFieldLiveOptions? live)
    {
        ArgumentNullException.ThrowIfNull(accessor);
        ArgumentNullException.ThrowIfNull(compiledAccessor);

        return View.Field(accessor, compiledAccessor, label, enableClientValidation, live);
    }

    public void UnregisterField(HrzFieldPath fieldPath)
    {
        ArgumentNullException.ThrowIfNull(fieldPath);
        View.UnregisterField(fieldPath);
    }
}
