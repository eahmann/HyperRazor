using System.Linq.Expressions;

namespace HyperRazor.Rendering;

public static class HrzFieldPaths
{
    public static HrzFieldPath For<TValue>(Expression<Func<TValue>> accessor) =>
        HrzFieldPathResolver.Default.FromExpression(accessor);

    public static HrzFieldPath FromFieldName(string value) =>
        HrzFieldPathResolver.Default.FromFieldName(value);

    public static HrzFieldPath Append(HrzFieldPath parent, string propertyName) =>
        HrzFieldPathResolver.Default.Append(parent, propertyName);

    public static HrzFieldPath Index(HrzFieldPath collection, int index) =>
        HrzFieldPathResolver.Default.Index(collection, index);
}
