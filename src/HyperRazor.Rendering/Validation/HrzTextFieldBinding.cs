using System.Linq.Expressions;
using System.Text;
using System.Text.Json;

namespace HyperRazor.Rendering;

/// <summary>
/// Expression-based field binding for text-like inputs. This is the first
/// authoring-layer helper for reducing repeated field-path and DOM metadata
/// wiring while the broader Proposal D surface is still forming.
/// </summary>
public sealed record HrzTextFieldBinding(
    HrzFieldPath FieldPath,
    string Name,
    string InputId,
    string ClientSlotId,
    string ServerSlotId,
    string LivePolicyId,
    string? Value,
    IReadOnlyList<string> Errors,
    bool HasErrors,
    string AriaDescribedBy,
    string? LiveValidationValuesJson)
{
    public static HrzTextFieldBinding Create(
        Expression<Func<string?>> accessor,
        HrzSubmitValidationState? validationState,
        string idPrefix,
        HrzValidationRootId? liveValidationRootId = null)
    {
        ArgumentNullException.ThrowIfNull(accessor);
        ArgumentException.ThrowIfNullOrWhiteSpace(idPrefix);

        var fieldPath = HrzFieldPaths.For(accessor);
        var idStem = $"{idPrefix}-{BuildIdSuffix(fieldPath)}";
        var clientSlotId = $"{idStem}-client";
        var serverSlotId = $"{idStem}-server";

        return new HrzTextFieldBinding(
            FieldPath: fieldPath,
            Name: BuildInputName(fieldPath),
            InputId: idStem,
            ClientSlotId: clientSlotId,
            ServerSlotId: serverSlotId,
            LivePolicyId: $"{idStem}-live",
            Value: HrzFormRendering.ValueOrAttempted(validationState, fieldPath, accessor.Compile().Invoke()),
            Errors: HrzFormRendering.ErrorsFor(validationState, fieldPath),
            HasErrors: HrzFormRendering.HasErrors(validationState, fieldPath),
            AriaDescribedBy: $"{clientSlotId} {serverSlotId}",
            LiveValidationValuesJson: BuildLiveValidationValuesJson(liveValidationRootId, fieldPath));
    }

    private static string? BuildLiveValidationValuesJson(HrzValidationRootId? rootId, HrzFieldPath fieldPath)
    {
        if (rootId is null)
        {
            return null;
        }

        return JsonSerializer.Serialize(new Dictionary<string, string>
        {
            ["__hrz_root"] = rootId.Value,
            ["__hrz_fields"] = fieldPath.Value
        });
    }

    private static string BuildInputName(HrzFieldPath fieldPath)
    {
        return string.Join(
            ".",
            fieldPath.Value
                .Split('.', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(ToLowerCamelSegment));
    }

    private static string ToLowerCamelSegment(string segment)
    {
        var bracketIndex = segment.IndexOf('[');
        var propertyName = bracketIndex >= 0 ? segment[..bracketIndex] : segment;
        var suffix = bracketIndex >= 0 ? segment[bracketIndex..] : string.Empty;
        if (string.IsNullOrEmpty(propertyName))
        {
            return segment;
        }

        return char.ToLowerInvariant(propertyName[0]) + propertyName[1..] + suffix;
    }

    private static string BuildIdSuffix(HrzFieldPath fieldPath)
    {
        var builder = new StringBuilder();
        foreach (var character in fieldPath.Value)
        {
            if (character is '.' or '[' or ']')
            {
                AppendSeparator(builder);
                continue;
            }

            if (char.IsUpper(character) && builder.Length > 0 && builder[^1] != '-')
            {
                AppendSeparator(builder);
            }

            builder.Append(char.ToLowerInvariant(character));
        }

        return builder.ToString().Trim('-');
    }

    private static void AppendSeparator(StringBuilder builder)
    {
        if (builder.Length == 0 || builder[^1] == '-')
        {
            return;
        }

        builder.Append('-');
    }
}
