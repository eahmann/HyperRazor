using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;

namespace HyperRazor.Components.Validation;

public static class HrzAttemptedValues
{
    public static IReadOnlyDictionary<HrzFieldPath, HrzAttemptedValue> FromRequest(HttpRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (!request.HasFormContentType)
        {
            return new Dictionary<HrzFieldPath, HrzAttemptedValue>();
        }

        var resolver = request.HttpContext.RequestServices.GetService(typeof(IHrzFieldPathResolver)) as IHrzFieldPathResolver;
        var form = request.Form;
        var values = new Dictionary<HrzFieldPath, HrzAttemptedValue>();

        foreach (var entry in form)
        {
            var path = resolver?.FromFieldName(entry.Key) ?? HrzFieldPathOperations.FromFieldName(entry.Key);
            values[path] = new HrzAttemptedValue(entry.Value, Array.Empty<HrzAttemptedFile>());
        }

        foreach (var file in form.Files)
        {
            var path = resolver?.FromFieldName(file.Name) ?? HrzFieldPathOperations.FromFieldName(file.Name);
            values.TryGetValue(path, out var existing);

            var files = existing?.Files.ToList() ?? [];
            files.Add(new HrzAttemptedFile(file.Name, file.FileName, file.ContentType, file.Length));

            values[path] = new HrzAttemptedValue(existing?.Values ?? StringValues.Empty, files);
        }

        return values;
    }
}
