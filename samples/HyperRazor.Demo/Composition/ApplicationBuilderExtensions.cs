using HyperRazor.Demo.Middleware;

namespace HyperRazor.Demo.Composition;

public static class ApplicationBuilderExtensions
{
    public static WebApplication UseSampleApp(this WebApplication app)
    {
        ArgumentNullException.ThrowIfNull(app);

        if (!app.Environment.IsDevelopment())
        {
            app.UseHsts();
        }

        app.UseHttpsRedirection();
        app.UseStaticFiles();
        app.UseRouting();
        app.UseHyperRazor();
        app.UseShellUpdates();

        return app;
    }
}
