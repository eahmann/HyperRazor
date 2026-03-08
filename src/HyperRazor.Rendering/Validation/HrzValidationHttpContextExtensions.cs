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

    public static HrzSubmitValidationState? GetSubmitValidationState(
        this HttpContext context,
        HrzValidationRootId rootId)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(rootId);

        var state = context.GetSubmitValidationState();
        return state is not null && state.RootId == rootId
            ? state
            : null;
    }
}
