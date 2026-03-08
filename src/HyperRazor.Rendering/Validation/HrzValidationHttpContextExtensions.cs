using Microsoft.AspNetCore.Http;

namespace HyperRazor.Rendering;

public static class HrzValidationHttpContextExtensions
{
    public static void SetSubmitValidationState(this HttpContext context, HrzSubmitValidationState state)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(state);

        context.Items[HrzContextItemKeys.SubmitValidationState] = state;
    }

    public static HrzSubmitValidationState? GetSubmitValidationState(this HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        return context.Items.TryGetValue(HrzContextItemKeys.SubmitValidationState, out var value)
            ? value as HrzSubmitValidationState
            : null;
    }
}
