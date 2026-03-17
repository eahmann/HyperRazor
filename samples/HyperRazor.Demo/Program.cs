using HyperRazor.Demo.Composition;
using HyperRazor.Demo.Endpoints;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSampleApp();

var app = builder.Build();

app.UseSampleApp();
app.MapAppPages();
app.MapAppStreams();
app.MapControllers();

app.Run();

public partial class Program;
