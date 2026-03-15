using System.Linq.Expressions;

namespace HyperRazor.Components.Validation;

internal static class HrzFieldPathOperations
{
    public static HrzFieldPath FromExpression<TValue>(Expression<Func<TValue>> accessor)
    {
        ArgumentNullException.ThrowIfNull(accessor);

        var raw = BuildPath(Unwrap(accessor.Body));
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
        while (expression is UnaryExpression unary
            && (unary.NodeType == ExpressionType.Convert || unary.NodeType == ExpressionType.ConvertChecked))
        {
            expression = unary.Operand;
        }

        return expression;
    }

    private static string BuildPath(Expression expression)
    {
        return expression switch
        {
            MemberExpression member => BuildMemberPath(member),
            MethodCallExpression call when IsIndexerCall(call) => BuildIndexerPath(call),
            BinaryExpression binary when binary.NodeType == ExpressionType.ArrayIndex => BuildArrayIndexPath(binary),
            ParameterExpression => string.Empty,
            ConstantExpression => string.Empty,
            _ => throw new NotSupportedException($"Expression '{expression}' cannot be converted into a field path.")
        };
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
        call.Method.Name == "get_Item" && call.Object is not null && call.Arguments.Count == 1;

    private static int EvaluateIndex(Expression expression)
    {
        var lambda = Expression.Lambda<Func<int>>(Expression.Convert(Unwrap(expression), typeof(int)));
        return lambda.Compile().Invoke();
    }

    private static string Normalize(string value)
    {
        var trimmed = value.Trim();
        while (trimmed.StartsWith("Model.", StringComparison.OrdinalIgnoreCase)
            || trimmed.StartsWith("Input.", StringComparison.OrdinalIgnoreCase))
        {
            trimmed = trimmed[(trimmed.IndexOf('.') + 1)..];
        }

        var segments = Parse(trimmed);
        return string.Join(".", segments.Select(FormatSegment));
    }

    private static string FormatSegment(PathSegment segment)
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

    private static IReadOnlyList<PathSegment> Parse(string value)
    {
        var segments = new List<PathSegment>();
        var parts = value.Split('.', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        foreach (var part in parts)
        {
            segments.Add(ParseSegment(part));
        }

        return segments;
    }

    private static PathSegment ParseSegment(string value)
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

        return new PathSegment(propertyName, indices);
    }

    private sealed record PathSegment(string PropertyName, IReadOnlyList<int> Indices);
}
