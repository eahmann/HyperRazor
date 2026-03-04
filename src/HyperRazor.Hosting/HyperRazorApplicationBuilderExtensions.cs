using HyperRazor.Htmx.AspNetCore;
using Microsoft.AspNetCore.Builder;

namespace HyperRazor.Hosting;

public static class HyperRazorApplicationBuilderExtensions
{
    public static IApplicationBuilder UseHyperRazor(this IApplicationBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        return app.UseHyperRazorHtmxVary();
    }
}
