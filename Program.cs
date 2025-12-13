using CspReport.Endpoints;
using CspReport.Options;
using CspReport.Sinks;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<CspReportOptions>(
    builder.Configuration.GetSection("CspReport"));

builder.Services.AddSingleton<ICspReportSink, FileCspReportSink>();

var app = builder.Build();

app.UseDefaultFiles();
app.UseStaticFiles();

app.MapCspReportEndpoints();

app.MapGet("/health", () => Results.Ok("ok"));

app.Run();
