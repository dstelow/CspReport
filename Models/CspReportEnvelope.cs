using System.Text.Json;

namespace CspReport.Models;

public sealed record CspReportEnvelope
{
    public string Schema => "report-uri";
    public DateTimeOffset ReceivedAt { get; init; }
    public string? RemoteIp { get; init; }
    public string? UserAgent { get; init; }
    public string? Referer { get; init; }

    // CSP fields
    public string? DocumentUri { get; init; }
    public string? BlockedUri { get; init; }
    public string? ViolatedDirective { get; init; }
    public string? EffectiveDirective { get; init; }
    public string? OriginalPolicy { get; init; }

    // Raw CSP report object
    public JsonElement Raw { get; init; }

    public static CspReportEnvelope FromLegacy(JsonElement report, HttpContext ctx)
    {
        return new CspReportEnvelope
        {
            ReceivedAt = DateTimeOffset.UtcNow,
            RemoteIp = ctx.Connection.RemoteIpAddress?.ToString(),
            UserAgent = ctx.Request.Headers.UserAgent.ToString(),
            Referer = ctx.Request.Headers.Referer.ToString(),

            DocumentUri = Get(report, "document-uri"),
            BlockedUri = Get(report, "blocked-uri"),
            ViolatedDirective = Get(report, "violated-directive"),
            EffectiveDirective = Get(report, "effective-directive"),
            OriginalPolicy = Get(report, "original-policy"),

            Raw = report
        };
    }

    private static string? Get(JsonElement el, string name) =>
        el.ValueKind == JsonValueKind.Object &&
        el.TryGetProperty(name, out var p) &&
        p.ValueKind == JsonValueKind.String
            ? p.GetString()
            : null;
}
