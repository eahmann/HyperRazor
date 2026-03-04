using System.Text.Encodings.Web;
using HyperRazor.Htmx;
using HyperRazor.Htmx.AspNetCore;
using Microsoft.AspNetCore.Mvc;

namespace HyperRazor.Demo.Mvc.Controllers;

[ApiController]
public sealed class FeatureController(HtmxConfig config) : ControllerBase
{
    [HttpGet("/feature")]
    public IActionResult Get()
    {
        var htmx = HttpContext.HtmxRequest();
        if (htmx.IsHtmx && !htmx.IsHistoryRestoreRequest)
        {
            return Content(
                "<section id=\"feature-panel\"><h2>Feature Fragment</h2><p>Fragment response for HTMX request.</p></section>",
                "text/html");
        }

        var encodedConfig = HtmlEncoder.Default.Encode(config.ToJson());
        var html = $$"""
                     <!doctype html>
                     <html lang="en">
                     <head>
                         <meta charset="utf-8" />
                         <meta name="viewport" content="width=device-width, initial-scale=1" />
                         <title>Feature</title>
                         <meta name="htmx-config" content="{{encodedConfig}}" />
                     </head>
                     <body>
                         <header id="app-shell">
                             <h1>Feature Full Page</h1>
                         </header>
                         <main id="main">
                             <section id="feature-panel">
                                 <p>Full page response for non-HTMX (or history restore) request.</p>
                             </section>
                         </main>
                     </body>
                     </html>
                     """;

        return Content(html, "text/html");
    }
}
