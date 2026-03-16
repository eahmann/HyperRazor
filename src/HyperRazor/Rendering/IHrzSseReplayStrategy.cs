namespace HyperRazor.Rendering;

public interface IHrzSseReplayStrategy
{
    ValueTask<HrzSseReplayDecision> DecideAsync(
        HrzSseReplayRequest request,
        CancellationToken cancellationToken = default);
}
