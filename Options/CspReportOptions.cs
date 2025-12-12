namespace CspReport.Options;

public sealed class CspReportOptions
{
    public string LogDirectory { get; set; } = "Logs";
    public string FileName { get; set; } = "csp-report-uri.jsonl";
    public int MaxBodyBytes { get; set; } = 256 * 1024;
}
