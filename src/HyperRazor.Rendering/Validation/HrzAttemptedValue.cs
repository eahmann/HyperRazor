using Microsoft.Extensions.Primitives;

namespace HyperRazor.Rendering;

public sealed record HrzAttemptedFile(
    string Name,
    string? FileName,
    string? ContentType,
    long? Length);

public sealed record HrzAttemptedValue(
    StringValues Values,
    IReadOnlyList<HrzAttemptedFile> Files);
