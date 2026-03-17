namespace HyperRazor.Demo.Middleware;

public static class ShellUpdatesApplicationBuilderExtensions
{
    public static IApplicationBuilder UseShellUpdates(this IApplicationBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);
        return app.UseMiddleware<ShellUpdatesMiddleware>();
    }
}
