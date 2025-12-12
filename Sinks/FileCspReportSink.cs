using System.Text.Json;
using Microsoft.Extensions.Options;
using CspReport.Models;
using CspReport.Options;

namespace CspReport.Sinks;

public sealed class FileCspReportSink : ICspReportSink
{
    private static readonly JsonSerializerOptions JsonOptions =
        new(JsonSerializerDefaults.Web);

    private readonly string _path;
    private readonly SemaphoreSlim _lock = new(1, 1);

    public FileCspReportSink(IOptions<CspReportOptions> options)
    {
        var opt = options.Value;
        Directory.CreateDirectory(opt.LogDirectory);
        _path = Path.Combine(opt.LogDirectory, opt.FileName);
    }

    public async Task WriteAsync(CspReportEnvelope report, CancellationToken ct)
    {
        var line = JsonSerializer.Serialize(report, JsonOptions);

        await _lock.WaitAsync(ct);
        try
        {
            await File.AppendAllTextAsync(_path, line + Environment.NewLine, ct);
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task<long> GetCountAsync(CancellationToken ct = default)
    {
        if (!File.Exists(_path))
            return 0;

        await _lock.WaitAsync(ct);
        try
        {
            long count = 0;
            using var sr = new StreamReader(_path);
            while (await sr.ReadLineAsync(ct) is not null)
                count++;
            return count;
        }
        finally
        {
            _lock.Release();
        }
    }
}
