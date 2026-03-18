using System.Collections;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Text.Json;

namespace HyperRazor.Components.Validation;

internal sealed record HrzFieldDescriptor(
    HrzFieldPath FieldPath,
    string Name,
    string InputId,
    string ClientSlotId,
    string ServerSlotId,
    string LivePolicyId,
    Type ValueType,
    string LabelText,
    bool EnableClientValidation,
    PropertyInfo Property);

internal sealed record HrzFieldValueProjection(
    object? CurrentValue,
    HrzAttemptedValue? AttemptedValue,
    string? Value,
    IReadOnlyList<string> Values,
    bool IsChecked,
    IReadOnlyList<string> Errors,
    bool HasErrors);

internal sealed record HrzResolvedLiveValidation(
    bool Enabled,
    string? Path,
    string? Trigger,
    string? Include,
    string? Sync,
    string? ValuesJson);

internal sealed class HrzValidationScopeFactory : IHrzValidationScopeFactory
{
    private readonly HrzFieldDescriptorFactory _descriptorFactory;
    private readonly HrzFieldValueProjector _valueProjector;
    private readonly HrzClientValidationMetadataFactory _clientValidationFactory;
    private readonly HrzLiveMetadataFactory _liveMetadataFactory;

    public HrzValidationScopeFactory(
        HrzFieldDescriptorFactory descriptorFactory,
        HrzFieldValueProjector valueProjector,
        HrzClientValidationMetadataFactory clientValidationFactory,
        HrzLiveMetadataFactory liveMetadataFactory)
    {
        _descriptorFactory = descriptorFactory;
        _valueProjector = valueProjector;
        _clientValidationFactory = clientValidationFactory;
        _liveMetadataFactory = liveMetadataFactory;
    }

    public HrzFormScope<TModel> CreateFormScope<TModel>(
        TModel model,
        HrzValidationRootId rootId,
        string? idPrefix,
        HrzSubmitValidationState? validationState,
        bool enableClientValidation,
        HrzLiveValidationOptions? live)
    {
        return new HrzFormScope<TModel>(
            model,
            rootId,
            string.IsNullOrWhiteSpace(idPrefix) ? rootId.Value : idPrefix,
            validationState,
            enableClientValidation,
            NormalizeLiveOptions(live),
            this);
    }

    public HrzFieldScope<TValue> CreateFieldScope<TValue>(
        HrzFormScope form,
        Expression<Func<TValue>> accessor,
        Func<TValue> compiledAccessor,
        string? label,
        bool? enableClientValidation,
        HrzFieldLiveOptions? live)
    {
        var descriptor = _descriptorFactory.Create(form, accessor, label, enableClientValidation);
        var projection = _valueProjector.Project(form, descriptor.FieldPath, compiledAccessor);
        var clientValidationAttributes = _clientValidationFactory.Create(
            descriptor.Property,
            descriptor.LabelText,
            descriptor.EnableClientValidation);
        var liveMetadata = _liveMetadataFactory.Create(form, descriptor.FieldPath, live);

        return new HrzFieldScope<TValue>(
            form,
            descriptor,
            projection,
            clientValidationAttributes,
            liveMetadata,
            (TValue?)projection.CurrentValue);
    }

    private static HrzLiveValidationOptions? NormalizeLiveOptions(HrzLiveValidationOptions? live)
    {
        return live is null || string.IsNullOrWhiteSpace(live.Path)
            ? null
            : live;
    }
}

internal sealed class HrzFieldDescriptorFactory
{
    public HrzFieldDescriptor Create<TValue>(
        HrzFormScope form,
        Expression<Func<TValue>> accessor,
        string? explicitLabel,
        bool? enableClientValidation)
    {
        ArgumentNullException.ThrowIfNull(form);
        ArgumentNullException.ThrowIfNull(accessor);

        var property = ResolveProperty(accessor.Body);
        var fieldPath = ResolveRelativeFieldPath(accessor, form.Model);
        var idStem = $"{form.IdPrefix}-{BuildIdSuffix(fieldPath)}";
        var clientSlotId = $"{idStem}-client";
        var serverSlotId = $"{idStem}-server";
        var labelText = explicitLabel ?? ResolveLabelText(property);

        return new HrzFieldDescriptor(
            fieldPath,
            BuildInputName(fieldPath),
            idStem,
            clientSlotId,
            serverSlotId,
            $"{idStem}-live",
            property.PropertyType,
            labelText,
            enableClientValidation ?? form.EnableClientValidation,
            property);
    }

    private static PropertyInfo ResolveProperty(Expression expression)
    {
        expression = HrzFieldExpressionPath.Unwrap(expression);
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
        var rawPath = HrzFieldExpressionPath.BuildRawPath(accessor.Body);
        if (string.IsNullOrWhiteSpace(rawPath))
        {
            throw new InvalidOperationException($"Expression '{accessor}' did not resolve to a field path.");
        }

        var segments = rawPath.Split('.', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var boundaryIndex = FindModelBoundary(HrzFieldExpressionPath.Unwrap(accessor.Body), model);
        if (boundaryIndex >= 0 && boundaryIndex + 1 < segments.Length)
        {
            return HrzFieldPaths.FromFieldName(string.Join(".", segments[(boundaryIndex + 1)..]));
        }

        return HrzFieldPaths.FromFieldName(rawPath);
    }

    private static int FindModelBoundary(Expression expression, object model)
    {
        var chain = GetMemberChain(expression);
        var current = EvaluateChainRoot(chain);
        for (var index = 0; index < chain.Count; index++)
        {
            var memberExpression = (MemberExpression)chain[index];
            current = EvaluateMember(memberExpression.Member, current);

            if (ReferenceEquals(current, model))
            {
                return index;
            }
        }

        return -1;
    }

    private static object? EvaluateChainRoot(IReadOnlyList<Expression> chain)
    {
        if (chain.Count == 0)
        {
            return null;
        }

        return chain[0] is MemberExpression firstMember
            ? EvaluateLeaf(HrzFieldExpressionPath.Unwrap(firstMember.Expression!))
            : null;
    }

    private static object? EvaluateLeaf(Expression expression)
    {
        if (expression is ConstantExpression constant)
        {
            return constant.Value;
        }

        if (expression is MemberExpression member)
        {
            var parent = EvaluateLeaf(HrzFieldExpressionPath.Unwrap(member.Expression!));
            return EvaluateMember(member.Member, parent);
        }

        return Expression.Lambda<Func<object?>>(Expression.Convert(expression, typeof(object))).Compile().Invoke();
    }

    private static object? EvaluateMember(MemberInfo member, object? target)
    {
        return member switch
        {
            PropertyInfo property => property.GetValue(target),
            FieldInfo field => field.GetValue(target),
            _ => null
        };
    }

    private static IReadOnlyList<Expression> GetMemberChain(Expression expression)
    {
        var chain = new List<Expression>();
        expression = HrzFieldExpressionPath.Unwrap(expression);
        while (expression is MemberExpression member)
        {
            chain.Add(member);
            expression = HrzFieldExpressionPath.Unwrap(member.Expression!);
        }

        chain.Reverse();
        return chain;
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
}

internal sealed class HrzFieldValueProjector
{
    public HrzFieldValueProjection Project<TValue>(
        HrzFormScope form,
        HrzFieldPath fieldPath,
        Func<TValue> compiledAccessor)
    {
        ArgumentNullException.ThrowIfNull(form);
        ArgumentNullException.ThrowIfNull(fieldPath);
        ArgumentNullException.ThrowIfNull(compiledAccessor);

        var currentValue = compiledAccessor();
        var attemptedValue = HrzFormRendering.AttemptedValueFor(form.ValidationState, fieldPath);
        var values = attemptedValue?.Values
            .Where(static value => value is not null)
            .Select(static value => value!)
            .ToArray()
            ?? GetValues(currentValue);
        var errors = HrzFormRendering.ErrorsFor(form.ValidationState, fieldPath);

        return new HrzFieldValueProjection(
            currentValue,
            attemptedValue,
            values.FirstOrDefault(),
            values,
            ResolveCheckedState(attemptedValue, currentValue),
            errors,
            errors.Count > 0);
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
}

internal sealed class HrzClientValidationMetadataFactory
{
    private readonly IReadOnlyList<IHrzClientValidationMetadataProvider> _providers;

    public HrzClientValidationMetadataFactory(IEnumerable<IHrzClientValidationMetadataProvider> providers)
    {
        _providers = providers.ToArray();
    }

    public IReadOnlyDictionary<string, string> Create(
        PropertyInfo property,
        string labelText,
        bool enabled)
    {
        if (!enabled || _providers.Count == 0)
        {
            return EmptyClientValidationAttributes;
        }

        var attributes = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var provider in _providers)
        {
            provider.AddValidationAttributes(property, labelText, attributes);
        }

        if (attributes.Count == 0)
        {
            return EmptyClientValidationAttributes;
        }

        if (!attributes.ContainsKey("data-val"))
        {
            attributes["data-val"] = "true";
        }

        return attributes;
    }

    private static readonly IReadOnlyDictionary<string, string> EmptyClientValidationAttributes =
        new Dictionary<string, string>(StringComparer.Ordinal);
}

internal sealed class HrzLiveMetadataFactory
{
    public HrzResolvedLiveValidation Create(
        HrzFormScope form,
        HrzFieldPath fieldPath,
        HrzFieldLiveOptions? live)
    {
        var resolvedPath = live?.Path ?? form.Live?.Path;
        var enabled = (live?.Enabled ?? true) && !string.IsNullOrWhiteSpace(resolvedPath);
        if (!enabled)
        {
            return new HrzResolvedLiveValidation(false, null, null, null, null, null);
        }

        var trigger = live?.Trigger ?? form.Live?.Trigger ?? "input changed delay:400ms, blur";
        var include = live?.Include ?? form.Live?.Include ?? "closest form";
        var sync = live?.Sync ?? form.Live?.Sync ?? "closest form:abort";

        return new HrzResolvedLiveValidation(
            true,
            resolvedPath,
            trigger,
            include,
            sync,
            JsonSerializer.Serialize(new Dictionary<string, string>
            {
                ["__hrz_root"] = form.RootId.Value,
                ["__hrz_fields"] = fieldPath.Value
            }));
    }
}
