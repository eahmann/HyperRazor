using Microsoft.AspNetCore.Components.Forms;

namespace HyperRazor.Components.Validation;

internal static class HrzEditFormState
{
    private static readonly object FormViewKey = typeof(HrzFormView);

    public static void SetFormView(EditContext editContext, HrzFormView formView)
    {
        ArgumentNullException.ThrowIfNull(editContext);
        ArgumentNullException.ThrowIfNull(formView);

        editContext.Properties[FormViewKey] = formView;
    }

    public static bool TryGetFormView(EditContext? editContext, out HrzFormView? formView)
    {
        if (editContext is not null
            && editContext.Properties.TryGetValue(FormViewKey, out var value)
            && value is HrzFormView typedFormView)
        {
            formView = typedFormView;
            return true;
        }

        formView = null;
        return false;
    }

    public static void ClearFormView(EditContext? editContext)
    {
        _ = editContext?.Properties.Remove(FormViewKey);
    }
}
