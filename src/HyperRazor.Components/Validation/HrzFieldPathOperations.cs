using System.Linq.Expressions;

namespace HyperRazor.Components.Validation;

internal static class HrzFieldPathOperations
{
    public static HrzFieldPath FromExpression<TValue>(Expression<Func<TValue>> accessor)
    {
        ArgumentNullException.ThrowIfNull(accessor);

        var raw = HrzFieldExpressionPath.BuildRawPath(accessor.Body);
        return FromFieldName(raw);
    }

    public static HrzFieldPath FromFieldName(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);

        var normalized = Normalize(value);
        return new HrzFieldPath(normalized);
    }

    public static HrzFieldPath Append(HrzFieldPath parent, string propertyName)
    {
        ArgumentNullException.ThrowIfNull(parent);
        ArgumentException.ThrowIfNullOrWhiteSpace(propertyName);

        return FromFieldName($"{parent.Value}.{propertyName}");
    }

    public static HrzFieldPath Index(HrzFieldPath collection, int index)
    {
        ArgumentNullException.ThrowIfNull(collection);

        return FromFieldName($"{collection.Value}[{index}]");
    }

    private static Expression Unwrap(Expression expression)
    {
        return HrzFieldExpressionPath.Unwrap(expression);
    }

    private static string BuildPath(Expression expression)
    {
        return HrzFieldExpressionPath.BuildRawPath(expression);
    }

    private static string BuildMemberPath(MemberExpression member)
    {
        var parent = BuildPath(Unwrap(member.Expression!));
        return string.IsNullOrEmpty(parent) ? member.Member.Name : $"{parent}.{member.Member.Name}";
    }

    private static string BuildIndexerPath(MethodCallExpression call)
    {
        var parent = BuildPath(Unwrap(call.Object!));
        var index = EvaluateIndex(call.Arguments[0]);
        return $"{parent}[{index}]";
    }

    private static string BuildArrayIndexPath(BinaryExpression binary)
    {
        var parent = BuildPath(Unwrap(binary.Left));
        var index = EvaluateIndex(binary.Right);
        return $"{parent}[{index}]";
    }

    private static bool IsIndexerCall(MethodCallExpression call) =>
        HrzFieldExpressionPath.IsIndexerCall(call);

    private static int EvaluateIndex(Expression expression)
    {
        return HrzFieldExpressionPath.EvaluateIndex(expression);
    }

    private static string Normalize(string value)
    {
        var trimmed = value.Trim();
        while (trimmed.StartsWith("Model.", StringComparison.OrdinalIgnoreCase)
            || trimmed.StartsWith("Input.", StringComparison.OrdinalIgnoreCase))
        {
            trimmed = trimmed[(trimmed.IndexOf('.') + 1)..];
        }

        var segments = HrzFieldPathParser.ParseSegments(trimmed);
        return string.Join(".", segments.Select(FormatSegment));
    }

    private static string FormatSegment(HrzFieldPathSegment segment)
    {
        var name = string.IsNullOrEmpty(segment.PropertyName)
            ? string.Empty
            : NormalizePropertyName(segment.PropertyName);
        var indexes = string.Concat(segment.Indices.Select(index => $"[{index}]"));
        return $"{name}{indexes}";
    }

    private static string NormalizePropertyName(string value)
    {
        if (string.IsNullOrEmpty(value) || !char.IsLetter(value[0]))
        {
            return value;
        }

        return char.ToUpperInvariant(value[0]) + value[1..];
    }
}
