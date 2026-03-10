namespace HyperRazor.Components;

public sealed record HrzSelectOption(
    string Value,
    string Label,
    bool Disabled = false);
