namespace HyperRazor.Components.Validation;

public sealed class HrzLiveValidationOptions
{
    public string? Path { get; init; }

    public string Trigger { get; init; } = "input changed delay:400ms, blur";

    public string Include { get; init; } = "closest form";

    public string Sync { get; init; } = "closest form:abort";
}

public sealed class HrzFieldLiveOptions
{
    public bool? Enabled { get; init; }

    public string? Path { get; init; }

    public string? Trigger { get; init; }

    public string? Include { get; init; }

    public string? Sync { get; init; }
}
