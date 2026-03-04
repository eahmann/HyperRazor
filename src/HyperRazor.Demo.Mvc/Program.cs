using HyperRazor.Components;
using HyperRazor.Components.Layouts;
using HyperRazor.Hosting;
using HyperRazor.Htmx;
using HyperRazor.Htmx.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddHyperRazor(options =>
{
    options.RootComponent = typeof(HrxApp<HrxMainLayout>);
    options.UseMinimalLayoutForHtmx = true;
});
builder.Services.AddHtmx(htmx =>
{
    htmx.SelfRequestsOnly = true;
    htmx.HistoryRestoreAsHxRequest = false;
    htmx.DefaultSwapStyle = "outerHTML";
    htmx.ResponseHandling =
    [
        new HtmxResponseHandlingRule { Code = "204", Swap = false },
        new HtmxResponseHandlingRule { Code = "[23]..", Swap = true },
        new HtmxResponseHandlingRule { Code = "422", Swap = true, Error = false },
        new HtmxResponseHandlingRule { Code = "[45]..", Swap = false, Error = true }
    ];
});

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

app.UseHyperRazor();

app.MapControllers();

app.Run();

public partial class Program;
