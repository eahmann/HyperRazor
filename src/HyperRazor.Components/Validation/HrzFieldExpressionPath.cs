using System.Linq.Expressions;

namespace HyperRazor.Components.Validation;

internal static class HrzFieldExpressionPath
{
    public static Expression Unwrap(Expression expression)
    {
        while (expression is UnaryExpression unary
            && (unary.NodeType == ExpressionType.Convert || unary.NodeType == ExpressionType.ConvertChecked))
        {
            expression = unary.Operand;
        }

        return expression;
    }

    public static string BuildRawPath(Expression expression)
    {
        expression = Unwrap(expression);
        return expression switch
        {
            MemberExpression member => BuildRawMemberPath(member),
            MethodCallExpression call when IsIndexerCall(call) => BuildRawIndexerPath(call),
            BinaryExpression binary when binary.NodeType == ExpressionType.ArrayIndex => BuildRawArrayIndexPath(binary),
            ParameterExpression => string.Empty,
            ConstantExpression => string.Empty,
            _ => throw new NotSupportedException($"Expression '{expression}' cannot be converted into a field path.")
        };
    }

    public static bool IsIndexerCall(MethodCallExpression call) =>
        call.Method.Name == "get_Item" && call.Object is not null && call.Arguments.Count == 1;

    public static int EvaluateIndex(Expression expression)
    {
        var lambda = Expression.Lambda<Func<int>>(Expression.Convert(Unwrap(expression), typeof(int)));
        return lambda.Compile().Invoke();
    }

    private static string BuildRawMemberPath(MemberExpression member)
    {
        var parent = BuildRawPath(member.Expression!);
        return string.IsNullOrEmpty(parent) ? member.Member.Name : $"{parent}.{member.Member.Name}";
    }

    private static string BuildRawIndexerPath(MethodCallExpression call)
    {
        var parent = BuildRawPath(call.Object!);
        var index = EvaluateIndex(call.Arguments[0]);
        return $"{parent}[{index}]";
    }

    private static string BuildRawArrayIndexPath(BinaryExpression binary)
    {
        var parent = BuildRawPath(binary.Left);
        var index = EvaluateIndex(binary.Right);
        return $"{parent}[{index}]";
    }
}
