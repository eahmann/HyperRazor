using Microsoft.AspNetCore.Components.Forms;

namespace HyperRazor.Components.Validation;

internal static class HrzEditFormState
{
    private static readonly object FormScopeKey = typeof(HrzFormScope);

    public static void SetFormScope(EditContext editContext, HrzFormScope formScope)
    {
        ArgumentNullException.ThrowIfNull(editContext);
        ArgumentNullException.ThrowIfNull(formScope);

        editContext.Properties[FormScopeKey] = formScope;
    }

    public static bool TryGetFormScope(EditContext? editContext, out HrzFormScope? formScope)
    {
        if (editContext is not null
            && editContext.Properties.TryGetValue(FormScopeKey, out var value)
            && value is HrzFormScope typedFormScope)
        {
            formScope = typedFormScope;
            return true;
        }

        formScope = null;
        return false;
    }

    public static void ClearFormScope(EditContext? editContext)
    {
        _ = editContext?.Properties.Remove(FormScopeKey);
    }
}
