using System.ComponentModel.DataAnnotations;
using System.Collections;
using System.Globalization;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Text.Json;
using HyperRazor.Rendering;

namespace HyperRazor.Components;

internal sealed class HrzValidationFieldContext
{
    public required HrzValidationFormContext Form { get; init; }

    public required HrzFieldPath FieldPath { get; init; }

    public required string Name { get; init; }

    public required string InputId { get; init; }

    public required string ClientSlotId { get; init; }

    public required string ServerSlotId { get; init; }

    public required string LivePolicyId { get; init; }

    public required Type ValueType { get; init; }

    public required object? CurrentValue { get; init; }

    public required HrzAttemptedValue? AttemptedValue { get; init; }

    public required string? Value { get; init; }

    public required IReadOnlyList<string> Values { get; init; }

    public required bool IsChecked { get; init; }

    public required IReadOnlyList<string> Errors { get; init; }

    public required bool HasErrors { get; init; }

    public required string LabelText { get; init; }

    public required bool EnableClientValidation { get; init; }

    public required IReadOnlyDictionary<string, string> ClientValidationAttributes { get; init; }

    public required string AriaDescribedBy { get; init; }

    public required bool HasLiveValidation { get; init; }

    public string? LiveValidationPath { get; init; }

    public string? LiveTrigger { get; init; }

    public string? LiveInclude { get; init; }

    public string? LiveSync { get; init; }

    public string? LiveValidationValuesJson { get; init; }

    public static HrzValidationFieldContext Create<TValue>(
        HrzValidationFormContext formContext,
        Expression<Func<TValue>> accessor,
        string? explicitLabel,
        bool? enableClientValidationOverride,
        bool? liveOverride,
        string? liveValidationPathOverride,
        string? liveTriggerOverride,
        string? liveIncludeOverride,
        string? liveSyncOverride)
    {
        ArgumentNullException.ThrowIfNull(formContext);
        ArgumentNullException.ThrowIfNull(accessor);

        var property = ResolveProperty(accessor.Body);
        var fieldPath = ResolveRelativeFieldPath(accessor, formContext.Model);
        var idStem = $"{formContext.IdPrefix}-{BuildIdSuffix(fieldPath)}";
        var clientSlotId = $"{idStem}-client";
        var serverSlotId = $"{idStem}-server";
        var effectiveClientValidation = enableClientValidationOverride ?? formContext.EnableClientValidation;
        var resolvedLivePath = liveValidationPathOverride ?? formContext.LiveValidationPath;
        var participatesInLiveValidation = (liveOverride ?? true) && !string.IsNullOrWhiteSpace(resolvedLivePath);
        var liveTrigger = participatesInLiveValidation
            ? liveTriggerOverride ?? formContext.LiveTrigger
            : null;
        var liveInclude = participatesInLiveValidation
            ? liveIncludeOverride ?? formContext.LiveInclude
            : null;
        var liveSync = participatesInLiveValidation
            ? liveSyncOverride ?? formContext.LiveSync
            : null;
        var currentValue = accessor.Compile().Invoke();
        var attemptedValue = HrzFormRendering.AttemptedValueFor(formContext.ValidationState, fieldPath);
        var values = attemptedValue?.Values
            .Where(static value => value is not null)
            .Select(static value => value!)
            .ToArray()
            ?? GetValues(currentValue);
        var labelText = explicitLabel ?? ResolveLabelText(property);
        var clientValidationAttributes = effectiveClientValidation
            ? ResolveClientValidationAttributes(property, labelText)
            : EmptyClientValidationAttributes;

        return new HrzValidationFieldContext
        {
            Form = formContext,
            FieldPath = fieldPath,
            Name = BuildInputName(fieldPath),
            InputId = idStem,
            ClientSlotId = clientSlotId,
            ServerSlotId = serverSlotId,
            LivePolicyId = $"{idStem}-live",
            ValueType = property.PropertyType,
            CurrentValue = currentValue,
            AttemptedValue = attemptedValue,
            Value = values.FirstOrDefault(),
            Values = values,
            IsChecked = ResolveCheckedState(attemptedValue, currentValue),
            Errors = HrzFormRendering.ErrorsFor(formContext.ValidationState, fieldPath),
            HasErrors = HrzFormRendering.HasErrors(formContext.ValidationState, fieldPath),
            LabelText = labelText,
            EnableClientValidation = effectiveClientValidation,
            ClientValidationAttributes = clientValidationAttributes,
            AriaDescribedBy = $"{clientSlotId} {serverSlotId}",
            HasLiveValidation = participatesInLiveValidation,
            LiveValidationPath = participatesInLiveValidation ? resolvedLivePath : null,
            LiveTrigger = liveTrigger,
            LiveInclude = liveInclude,
            LiveSync = liveSync,
            LiveValidationValuesJson = participatesInLiveValidation
                ? JsonSerializer.Serialize(new Dictionary<string, string>
                {
                    ["__hrz_root"] = formContext.RootId.Value,
                    ["__hrz_fields"] = fieldPath.Value
                })
                : null
        };
    }

    private static IReadOnlyList<string> GetValues(object? value)
    {
        if (value is null)
        {
            return Array.Empty<string>();
        }

        if (value is string text)
        {
            return new[] { text };
        }

        if (value is IEnumerable enumerable)
        {
            var values = new List<string>();
            foreach (var item in enumerable)
            {
                var formatted = FormatValue(item);
                if (formatted is not null)
                {
                    values.Add(formatted);
                }
            }

            return values;
        }

        var scalar = FormatValue(value);
        return scalar is null ? Array.Empty<string>() : new[] { scalar };
    }

    private static bool ResolveCheckedState(HrzAttemptedValue? attemptedValue, object? currentValue)
    {
        if (attemptedValue is not null)
        {
            foreach (var value in attemptedValue.Values)
            {
                if (TryParseBoolean(value, out var parsed) && parsed)
                {
                    return true;
                }
            }

            return false;
        }

        return currentValue switch
        {
            bool flag => flag,
            _ when TryParseBoolean(FormatValue(currentValue), out var parsed) => parsed,
            _ => false
        };
    }

    private static bool TryParseBoolean(string? value, out bool parsed)
    {
        if (bool.TryParse(value, out parsed))
        {
            return true;
        }

        switch (value?.Trim().ToLowerInvariant())
        {
            case "1":
            case "on":
            case "yes":
                parsed = true;
                return true;
            case "0":
            case "off":
            case "no":
                parsed = false;
                return true;
            default:
                parsed = false;
                return false;
        }
    }

    private static string? FormatValue(object? value)
    {
        return value switch
        {
            null => null,
            string text => text,
            bool flag => flag ? "true" : "false",
            DateOnly dateOnly => dateOnly.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            DateTime dateTime => dateTime.ToString("O", CultureInfo.InvariantCulture),
            DateTimeOffset dateTimeOffset => dateTimeOffset.ToString("O", CultureInfo.InvariantCulture),
            Enum enumValue => enumValue.ToString(),
            IFormattable formattable => formattable.ToString(null, CultureInfo.InvariantCulture),
            _ => value.ToString()
        };
    }

    private static PropertyInfo ResolveProperty(Expression expression)
    {
        expression = Unwrap(expression);
        return expression switch
        {
            MemberExpression { Member: PropertyInfo property } => property,
            _ => throw new NotSupportedException($"Expression '{expression}' must resolve to a property.")
        };
    }

    private static HrzFieldPath ResolveRelativeFieldPath(
        LambdaExpression accessor,
        object model)
    {
        var rawPath = BuildRawPath(Unwrap(accessor.Body));
        if (string.IsNullOrWhiteSpace(rawPath))
        {
            throw new InvalidOperationException($"Expression '{accessor}' did not resolve to a field path.");
        }

        var segments = rawPath.Split('.', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var boundaryIndex = FindModelBoundary(Unwrap(accessor.Body), model);
        if (boundaryIndex >= 0 && boundaryIndex + 1 < segments.Length)
        {
            return HrzFieldPaths.FromFieldName(string.Join(".", segments[(boundaryIndex + 1)..]));
        }

        return HrzFieldPaths.FromFieldName(rawPath);
    }

    private static int FindModelBoundary(Expression expression, object model)
    {
        var chain = GetMemberChain(expression);
        for (var index = 0; index < chain.Count; index++)
        {
            var lambda = Expression.Lambda<Func<object?>>(
                Expression.Convert(chain[index], typeof(object)));
            if (ReferenceEquals(lambda.Compile().Invoke(), model))
            {
                return index;
            }
        }

        return -1;
    }

    private static IReadOnlyList<Expression> GetMemberChain(Expression expression)
    {
        var chain = new List<Expression>();
        expression = Unwrap(expression);
        while (expression is MemberExpression member)
        {
            chain.Add(member);
            expression = Unwrap(member.Expression!);
        }

        chain.Reverse();
        return chain;
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

    private static string BuildRawPath(Expression expression)
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

    private static bool IsIndexerCall(MethodCallExpression call) =>
        call.Method.Name == "get_Item" && call.Object is not null && call.Arguments.Count == 1;

    private static int EvaluateIndex(Expression expression)
    {
        var lambda = Expression.Lambda<Func<int>>(Expression.Convert(Unwrap(expression), typeof(int)));
        return lambda.Compile().Invoke();
    }

    private static string ResolveLabelText(PropertyInfo property)
    {
        var display = property.GetCustomAttribute<DisplayAttribute>();
        if (!string.IsNullOrWhiteSpace(display?.GetName()))
        {
            return display.GetName()!;
        }

        return SplitPascalCase(property.Name);
    }

    private static IReadOnlyDictionary<string, string> ResolveClientValidationAttributes(
        PropertyInfo property,
        string labelText)
    {
        var attributes = new Dictionary<string, string>(StringComparer.Ordinal);

        AddValidationAttribute(attributes, "required", property.GetCustomAttribute<RequiredAttribute>(), labelText);
        AddValidationAttribute(attributes, "email", property.GetCustomAttribute<EmailAddressAttribute>(), labelText);
        AddValidationAttribute(attributes, "creditcard", property.GetCustomAttribute<CreditCardAttribute>(), labelText);
        AddValidationAttribute(attributes, "url", property.GetCustomAttribute<UrlAttribute>(), labelText);
        AddValidationAttribute(attributes, "phone", property.GetCustomAttribute<PhoneAttribute>(), labelText);

        var stringLength = property.GetCustomAttribute<StringLengthAttribute>();
        if (stringLength is not null)
        {
            attributes["data-val"] = "true";
            attributes["data-val-length"] = stringLength.FormatErrorMessage(labelText);
            attributes["data-val-length-max"] = stringLength.MaximumLength.ToString(CultureInfo.InvariantCulture);
            if (stringLength.MinimumLength > 0)
            {
                attributes["data-val-length-min"] = stringLength.MinimumLength.ToString(CultureInfo.InvariantCulture);
            }
        }

        var minLength = property.GetCustomAttribute<MinLengthAttribute>();
        if (minLength is not null)
        {
            attributes["data-val"] = "true";
            attributes["data-val-minlength"] = minLength.FormatErrorMessage(labelText);
            attributes["data-val-minlength-min"] = minLength.Length.ToString(CultureInfo.InvariantCulture);
        }

        var maxLength = property.GetCustomAttribute<MaxLengthAttribute>();
        if (maxLength is not null)
        {
            attributes["data-val"] = "true";
            attributes["data-val-maxlength"] = maxLength.FormatErrorMessage(labelText);
            attributes["data-val-maxlength-max"] = maxLength.Length.ToString(CultureInfo.InvariantCulture);
        }

        var range = property.GetCustomAttribute<RangeAttribute>();
        if (range is not null)
        {
            attributes["data-val"] = "true";
            attributes["data-val-range"] = range.FormatErrorMessage(labelText);
            attributes["data-val-range-min"] = Convert.ToString(range.Minimum, CultureInfo.InvariantCulture) ?? string.Empty;
            attributes["data-val-range-max"] = Convert.ToString(range.Maximum, CultureInfo.InvariantCulture) ?? string.Empty;
        }

        var regex = property.GetCustomAttribute<RegularExpressionAttribute>();
        if (regex is not null)
        {
            attributes["data-val"] = "true";
            attributes["data-val-regex"] = regex.FormatErrorMessage(labelText);
            attributes["data-val-regex-pattern"] = regex.Pattern;
        }

        return attributes;
    }

    private static void AddValidationAttribute(
        IDictionary<string, string> attributes,
        string ruleName,
        ValidationAttribute? attribute,
        string labelText)
    {
        if (attribute is null)
        {
            return;
        }

        attributes["data-val"] = "true";
        attributes[$"data-val-{ruleName}"] = attribute.FormatErrorMessage(labelText);
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

    private static string SplitPascalCase(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return value;
        }

        var builder = new StringBuilder();
        for (var index = 0; index < value.Length; index++)
        {
            var character = value[index];
            if (index > 0 && char.IsUpper(character) && !char.IsWhiteSpace(value[index - 1]))
            {
                builder.Append(' ');
            }

            builder.Append(character);
        }

        return builder.ToString();
    }

    private static readonly IReadOnlyDictionary<string, string> EmptyClientValidationAttributes =
        new Dictionary<string, string>(StringComparer.Ordinal);
}
