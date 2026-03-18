namespace HyperRazor.Components.Validation;

internal static class HrzFieldPathParser
{
    public static IReadOnlyList<HrzFieldPathSegment> ParseSegments(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);

        var segments = new List<HrzFieldPathSegment>();
        var parts = value.Split('.', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        foreach (var part in parts)
        {
            segments.Add(ParseSegment(part));
        }

        return segments;
    }

    private static HrzFieldPathSegment ParseSegment(string value)
    {
        var propertyName = value;
        var indices = new List<int>();
        var bracketIndex = value.IndexOf('[');
        if (bracketIndex >= 0)
        {
            propertyName = bracketIndex == 0 ? string.Empty : value[..bracketIndex];
            var cursor = bracketIndex;
            while (cursor >= 0 && cursor < value.Length)
            {
                var open = value.IndexOf('[', cursor);
                if (open < 0)
                {
                    break;
                }

                var close = value.IndexOf(']', open + 1);
                if (close < 0)
                {
                    throw new InvalidOperationException($"Field path segment '{value}' contains an unterminated indexer.");
                }

                indices.Add(int.Parse(value[(open + 1)..close], System.Globalization.CultureInfo.InvariantCulture));
                cursor = close + 1;
            }
        }

        return new HrzFieldPathSegment(propertyName, indices);
    }
}
