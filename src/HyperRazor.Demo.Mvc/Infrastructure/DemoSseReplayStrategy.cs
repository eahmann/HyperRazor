using System.Net.ServerSentEvents;
using System.Runtime.CompilerServices;
using HyperRazor.Components.Services;
using HyperRazor.Rendering;

namespace HyperRazor.Demo.Mvc.Infrastructure;

internal sealed class DemoSseReplayStrategy : IHrzSseReplayStrategy
{
    private readonly IHrzSseRenderer _sseRenderer;
    private readonly IHrzSwapService _swapService;

    public DemoSseReplayStrategy(
        IHrzSseRenderer sseRenderer,
        IHrzSwapService swapService)
    {
        _sseRenderer = sseRenderer;
        _swapService = swapService;
    }

    public ValueTask<HrzSseReplayDecision> DecideAsync(
        HrzSseReplayRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!string.Equals(request.StreamName, DemoSseReplayScenario.StreamName, StringComparison.Ordinal))
        {
            return ValueTask.FromResult(HrzSseReplayDecision.Live());
        }

        if (!DemoSseReplayScenario.TryGetReplayEntries(request.ResumeContext.LastEventId, out var entries))
        {
            return ValueTask.FromResult(HrzSseReplayDecision.Live());
        }

        return ValueTask.FromResult(HrzSseReplayDecision.Replay(
            RenderReplayEntriesAsync(entries, request.ResumeContext, cancellationToken)));
    }

    private async IAsyncEnumerable<SseItem<string>> RenderReplayEntriesAsync(
        IReadOnlyList<DemoSseReplayEntry> entries,
        HrzSseResumeContext resumeContext,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        foreach (var entry in entries)
        {
            yield return await DemoSseReplayScenario.RenderEntryAsync(
                entry,
                resumeContext,
                _sseRenderer,
                _swapService,
                cancellationToken);
        }
    }
}
