using System.Collections;
using System.Linq.Expressions;
using HyperRazor.Components.Validation;
using Microsoft.AspNetCore.Components.Forms;

namespace HyperRazor.Rendering;

public sealed class HrzFieldPathResolver : IHrzFieldPathResolver
{
    public HrzFieldPath FromExpression<TValue>(Expression<Func<TValue>> accessor)
        => HrzFieldPaths.For(accessor);

    public HrzFieldPath FromFieldName(string value)
        => HrzFieldPaths.FromFieldName(value);

    public HrzFieldPath Append(HrzFieldPath parent, string propertyName)
        => HrzFieldPaths.Append(parent, propertyName);

    public HrzFieldPath Index(HrzFieldPath collection, int index)
        => HrzFieldPaths.Index(collection, index);

    public string Format(HrzFieldPath path)
    {
        ArgumentNullException.ThrowIfNull(path);
        return path.Value;
    }

    public FieldIdentifier Resolve(object model, HrzFieldPath path)
    {
        ArgumentNullException.ThrowIfNull(model);
        ArgumentNullException.ThrowIfNull(path);

        var segments = HrzFieldPaths.ParseSegments(path);
        if (segments.Count == 0)
        {
            throw new InvalidOperationException($"Unable to resolve field path '{path.Value}'.");
        }

        object? current = model;
        for (var index = 0; index < segments.Count - 1; index++)
        {
            current = ResolveSegmentValue(current, segments[index], path.Value);
        }

        if (current is null)
        {
            throw new InvalidOperationException($"Unable to resolve field path '{path.Value}' because an intermediate model value was null.");
        }

        var finalSegment = segments[^1];
        if (finalSegment.Indices.Count > 0)
        {
            current = ResolveSegmentValue(current, finalSegment with { Indices = [] }, path.Value);
            finalSegment = finalSegment with { PropertyName = finalSegment.PropertyName, Indices = [] };
        }

        if (string.IsNullOrWhiteSpace(finalSegment.PropertyName))
        {
            throw new InvalidOperationException($"Field path '{path.Value}' did not resolve to a property.");
        }

        var property = FindProperty(current!.GetType(), finalSegment.PropertyName, path.Value);
        return new FieldIdentifier(current, property.Name);
    }
    private static object? ResolveSegmentValue(object? current, HrzFieldPathSegment segment, string originalPath)
    {
        if (current is null)
        {
            return null;
        }

        object? value = current;
        if (!string.IsNullOrWhiteSpace(segment.PropertyName))
        {
            var property = FindProperty(current.GetType(), segment.PropertyName, originalPath);
            value = property.GetValue(current);
        }

        foreach (var index in segment.Indices)
        {
            value = ResolveIndexedValue(value, index, originalPath);
        }

        return value;
    }

    private static System.Reflection.PropertyInfo FindProperty(Type type, string propertyName, string originalPath)
    {
        var property = type.GetProperty(
            propertyName,
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.IgnoreCase);
        if (property is null)
        {
            throw new InvalidOperationException($"Unable to resolve property '{propertyName}' on '{type.Name}' while resolving '{originalPath}'.");
        }

        return property;
    }

    private static object? ResolveIndexedValue(object? value, int index, string originalPath)
    {
        return value switch
        {
            null => null,
            Array array => array.GetValue(index),
            IList list => list[index],
            IEnumerable enumerable => ResolveEnumerableIndex(enumerable, index, originalPath),
            _ => throw new InvalidOperationException($"Unable to index into '{value.GetType().Name}' while resolving '{originalPath}'.")
        };
    }

    private static object? ResolveEnumerableIndex(IEnumerable enumerable, int index, string originalPath)
    {
        var currentIndex = 0;
        foreach (var item in enumerable)
        {
            if (currentIndex == index)
            {
                return item;
            }

            currentIndex++;
        }

        throw new InvalidOperationException($"Unable to resolve index {index} while resolving '{originalPath}'.");
    }
}
