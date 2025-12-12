using CspReport.Models;

namespace CspReport.Sinks;

public interface ICspReportSink
{
    Task WriteAsync(CspReportEnvelope report, CancellationToken ct);
    Task<long> GetCountAsync(CancellationToken ct = default);
}
