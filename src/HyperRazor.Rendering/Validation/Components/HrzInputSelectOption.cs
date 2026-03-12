namespace HyperRazor.Components;

public sealed record HrzInputSelectOption(
    string Value,
    string Label,
    bool Disabled = false);
