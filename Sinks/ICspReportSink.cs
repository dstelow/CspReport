using CspReport.Models;

namespace CspReport.Sinks;

public interface ICspReportSink
{
    Task WriteAsync(CspReportEnvelope report, CancellationToken ct);
    Task<long> GetCountAsync(CancellationToken ct = default);
    Task<IReadOnlyList<CspReportEnvelope>> GetReportsAsync(int skip = 0, int take = 10, CancellationToken ct = default);
}
