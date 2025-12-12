using CspReport.Endpoints;
using CspReport.Options;
using CspReport.Sinks;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<CspReportOptions>(
    builder.Configuration.GetSection("CspReport"));

builder.Services.AddSingleton<ICspReportSink, FileCspReportSink>();

var app = builder.Build();

app.MapCspReportEndpoints();

// Prefer /health over /. 
// In newer services, / is often reserved for: static sites, reverse proxies, future UI
app.MapGet("/", () => "CSP Report API is running");

app.MapGet("/health", () => Results.Ok("ok"));

app.Run();
