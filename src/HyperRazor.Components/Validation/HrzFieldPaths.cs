using System.Linq.Expressions;

namespace HyperRazor.Components.Validation;

public static class HrzFieldPaths
{
    public static HrzFieldPath For<TValue>(Expression<Func<TValue>> accessor) =>
        HrzFieldPathOperations.FromExpression(accessor);

    public static HrzFieldPath FromFieldName(string value) =>
        HrzFieldPathOperations.FromFieldName(value);

    public static HrzFieldPath Append(HrzFieldPath parent, string propertyName) =>
        HrzFieldPathOperations.Append(parent, propertyName);

    public static HrzFieldPath Index(HrzFieldPath collection, int index) =>
        HrzFieldPathOperations.Index(collection, index);

    public static IReadOnlyList<HrzFieldPathSegment> ParseSegments(HrzFieldPath path)
    {
        ArgumentNullException.ThrowIfNull(path);
        return HrzFieldPathParser.ParseSegments(path.Value);
    }

    public static IReadOnlyList<HrzFieldPathSegment> ParseSegments(string value) =>
        HrzFieldPathParser.ParseSegments(value);
}
