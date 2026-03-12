using HyperRazor;
using HyperRazor.Demo.Mvc.Middleware;

namespace HyperRazor.Demo.Mvc.Composition;

public static class DemoApplicationBuilderExtensions
{
    public static WebApplication UseDemoMvc(this WebApplication app)
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
        app.UseDemoChromeUpdates();

        return app;
    }
}
