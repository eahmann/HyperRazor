namespace HyperRazor.Rendering;

public sealed class HrzDefaultSseReplayStrategy : IHrzSseReplayStrategy
{
    public ValueTask<HrzSseReplayDecision> DecideAsync(
        HrzSseReplayRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request.HttpContext);

        return ValueTask.FromResult(HrzSseReplayDecision.Live());
    }
}
