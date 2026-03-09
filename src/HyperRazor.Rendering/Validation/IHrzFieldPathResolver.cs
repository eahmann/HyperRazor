using System.Linq.Expressions;
using Microsoft.AspNetCore.Components.Forms;

namespace HyperRazor.Rendering;

public interface IHrzFieldPathResolver
{
    HrzFieldPath FromExpression<TValue>(Expression<Func<TValue>> accessor);
    HrzFieldPath FromFieldName(string value);
    HrzFieldPath Append(HrzFieldPath parent, string propertyName);
    HrzFieldPath Index(HrzFieldPath collection, int index);
    string Format(HrzFieldPath path);
    FieldIdentifier Resolve(object model, HrzFieldPath path);
}
