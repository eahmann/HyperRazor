using HyperRazor.Components.Validation;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Primitives;

namespace HyperRazor.Mvc;

internal sealed class HrzLiveValidationRequestBinder : IHrzLiveValidationRequestBinder
{
    private const string ValidationRootField = "__hrz_root";
    private const string ValidationFieldsField = "__hrz_fields";
    private const string ValidateAllField = "__hrz_validate_all";

    public async Task<HrzLiveValidationRequest?> BindAsync(
        HttpContext context,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);

        cancellationToken.ThrowIfCancellationRequested();

        var resolver = context.RequestServices.GetRequiredService<IHrzFieldPathResolver>();
        var form = context.Request.HasFormContentType
            ? await context.Request.ReadFormAsync(cancellationToken)
            : null;
        var rootValue = ReadValue(form, context.Request.Query, ValidationRootField);
        if (string.IsNullOrWhiteSpace(rootValue))
        {
            return null;
        }

        var fieldList = ReadValue(form, context.Request.Query, ValidationFieldsField);
        var validateAll = bool.TryParse(ReadValue(form, context.Request.Query, ValidateAllField), out var parsedValidateAll)
            && parsedValidateAll;
        var fields = string.IsNullOrWhiteSpace(fieldList)
            ? Array.Empty<HrzFieldPath>()
            : fieldList
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(resolver.FromFieldName)
                .ToArray();

        return new HrzLiveValidationRequest(new HrzValidationRootId(rootValue.Trim()), validateAll, fields);
    }

    private static string? ReadValue(IFormCollection? form, IQueryCollection query, string key)
    {
        if (form is not null
            && form.TryGetValue(key, out var formValue)
            && !StringValues.IsNullOrEmpty(formValue))
        {
            return formValue.ToString();
        }

        return query.TryGetValue(key, out var queryValue) && !StringValues.IsNullOrEmpty(queryValue)
            ? queryValue.ToString()
            : null;
    }
}
