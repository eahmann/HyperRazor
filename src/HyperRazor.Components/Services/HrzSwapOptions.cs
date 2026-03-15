namespace HyperRazor.Components.Services;

public sealed class HrzSwapOptions
{
    public HrzSwapTargetKind TargetKind { get; init; } = HrzSwapTargetKind.Region;

    public string? TargetId { get; init; }
}
