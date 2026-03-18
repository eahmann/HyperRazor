using System.Collections;
using System.Reflection;

namespace HyperRazor.Components;

public static class HrzParameterDictionaryFactory
{
    public static IReadOnlyDictionary<string, object?> Create(object? data)
    {
        if (data is null)
        {
            return Empty;
        }

        if (data is IReadOnlyDictionary<string, object?> readOnlyDictionary)
        {
            return Copy(readOnlyDictionary);
        }

        if (data is IDictionary<string, object?> dictionary)
        {
            return CopyDictionary(dictionary);
        }

        if (data is IDictionary nonGenericDictionary)
        {
            return CopyNonGeneric(nonGenericDictionary);
        }

        return CopyFromProperties(data);
    }

    private static IReadOnlyDictionary<string, object?> Copy(IReadOnlyDictionary<string, object?> source)
    {
        var copy = new Dictionary<string, object?>(source.Count, StringComparer.Ordinal);
        foreach (var entry in source)
        {
            copy[entry.Key] = entry.Value;
        }

        return copy;
    }

    private static IReadOnlyDictionary<string, object?> CopyDictionary(IDictionary<string, object?> source)
    {
        var copy = new Dictionary<string, object?>(source.Count, StringComparer.Ordinal);
        foreach (var entry in source)
        {
            copy[entry.Key] = entry.Value;
        }

        return copy;
    }

    private static IReadOnlyDictionary<string, object?> CopyNonGeneric(IDictionary source)
    {
        var copy = new Dictionary<string, object?>(source.Count, StringComparer.Ordinal);
        foreach (DictionaryEntry entry in source)
        {
            if (entry.Key is string key)
            {
                copy[key] = entry.Value;
            }
        }

        return copy;
    }

    private static IReadOnlyDictionary<string, object?> CopyFromProperties(object source)
    {
        var properties = source
            .GetType()
            .GetProperties(BindingFlags.Instance | BindingFlags.Public)
            .Where(static property => property.CanRead && property.GetIndexParameters().Length == 0);

        var map = new Dictionary<string, object?>(StringComparer.Ordinal);
        foreach (var property in properties)
        {
            map[property.Name] = property.GetValue(source);
        }

        return map;
    }

    private static readonly IReadOnlyDictionary<string, object?> Empty = new Dictionary<string, object?>();
}
