using HyperRazor.Demo.Mvc.Composition;
using HyperRazor.Demo.Mvc.Endpoints;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDemoMvc();

var app = builder.Build();

app.UseDemoMvc();
app.MapDemoPages();
app.MapDemoValidationEndpoints();
app.MapDemoSseEndpoints();
app.MapControllers();

app.Run();

public partial class Program;
