namespace HyperRazor.Demo.Mvc.Middleware;

public static class DemoChromeUpdatesApplicationBuilderExtensions
{
    public static IApplicationBuilder UseDemoChromeUpdates(this IApplicationBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        return app.UseMiddleware<DemoChromeUpdatesMiddleware>();
    }
}
