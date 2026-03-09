using System.Linq.Expressions;
using System.Reflection;

namespace HyperRazor.Rendering;

internal static class HrzLambdaFieldPathResolver
{
    private static readonly MethodInfo FromExpressionMethod = typeof(IHrzFieldPathResolver)
        .GetMethods(BindingFlags.Public | BindingFlags.Instance)
        .Single(static method => method.Name == nameof(IHrzFieldPathResolver.FromExpression));

    public static HrzFieldPath Resolve(LambdaExpression expression, IHrzFieldPathResolver fieldPathResolver)
    {
        ArgumentNullException.ThrowIfNull(expression);
        ArgumentNullException.ThrowIfNull(fieldPathResolver);

        var method = FromExpressionMethod.MakeGenericMethod(expression.ReturnType);
        return (HrzFieldPath)(method.Invoke(fieldPathResolver, [expression])
            ?? throw new InvalidOperationException("Field path resolution returned null."));
    }
}
