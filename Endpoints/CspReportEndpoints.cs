using System.Text.Json;
using Microsoft.Extensions.Options;
using CspReport.Options;
using CspReport.Models;
using CspReport.Sinks;

namespace CspReport.Endpoints;

public static class CspReportEndpoints
{
    public static void MapCspReportEndpoints(this WebApplication app)
    {
        app.MapPost("/csp/report-uri", HandleLegacyReport);
        app.MapGet("/csp/count", GetCount);
        app.MapGet("/csp/reports", GetReports);
    }

    private static async Task<IResult> HandleLegacyReport(
        HttpContext ctx,
        ICspReportSink sink,
        IOptions<CspReportOptions> options)
    {
        var opt = options.Value;

        if (ctx.Request.ContentLength is > 0 && ctx.Request.ContentLength > opt.MaxBodyBytes)
            return Results.StatusCode(StatusCodes.Status413PayloadTooLarge);

        using var doc = await JsonDocument.ParseAsync(ctx.Request.Body);

        var report = doc.RootElement.TryGetProperty("csp-report", out var inner)
            ? inner
            : doc.RootElement;

        var envelope = CspReportEnvelope.FromLegacy(report, ctx);
        await sink.WriteAsync(envelope, ctx.RequestAborted);

        return Results.NoContent();
    }

    private static async Task<IResult> GetCount(ICspReportSink sink)
    {
        var count = await sink.GetCountAsync();
        return Results.Ok(new { count });
    }

    private static async Task<IResult> GetReports(
        ICspReportSink sink,
        int skip = 0,
        int take = 400,
        string? fromDate = null,
        string? toDate = null)
    {
        if (take < 1 || take > 400)
            return Results.BadRequest(new { error = "take must be between 1 and 400" });

        if (skip < 0)
            return Results.BadRequest(new { error = "skip must be non-negative" });

        // Parse date strings as local dates
        DateTimeOffset? fromDateOffset = null;
        DateTimeOffset? toDateOffset = null;
        
        if (!string.IsNullOrEmpty(fromDate) && DateOnly.TryParse(fromDate, out var fromDateOnly))
        {
            fromDateOffset = new DateTimeOffset(fromDateOnly.ToDateTime(TimeOnly.MinValue), TimeZoneInfo.Local.GetUtcOffset(DateTime.Now));
        }
        
        if (!string.IsNullOrEmpty(toDate) && DateOnly.TryParse(toDate, out var toDateOnly))
        {
            toDateOffset = new DateTimeOffset(toDateOnly.ToDateTime(TimeOnly.MaxValue), TimeZoneInfo.Local.GetUtcOffset(DateTime.Now));
        }

        var reports = await sink.GetReportsAsync(skip, take, fromDateOffset, toDateOffset);
        var totalInRange = await sink.GetCountInRangeAsync(fromDateOffset, toDateOffset);
        var hasMore = totalInRange > reports.Count;
        
        return Results.Ok(new { skip, take, count = reports.Count, totalInRange, hasMore, reports });
    }

}
