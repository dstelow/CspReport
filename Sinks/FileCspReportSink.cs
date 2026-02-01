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

    public async Task<IReadOnlyList<CspReportEnvelope>> GetReportsAsync(int skip = 0, int take = 10, CancellationToken ct = default)
    {
        if (!File.Exists(_path))
            return Array.Empty<CspReportEnvelope>();

        await _lock.WaitAsync(ct);
        try
        {
            // Read all lines first, then reverse to get newest first (DESC order)
            var allLines = new List<string>();
            using (var sr = new StreamReader(_path))
            {
                string? line;
                while ((line = await sr.ReadLineAsync(ct)) is not null)
                {
                    allLines.Add(line);
                }
            }
            
            // Reverse to get newest first
            allLines.Reverse();
            
            var reports = new List<CspReportEnvelope>();
            int currentLine = 0;
            
            foreach (var line in allLines)
            {
                if (currentLine >= skip && reports.Count < take)
                {
                    try
                    {
                        var envelope = JsonSerializer.Deserialize<CspReportEnvelope>(line, JsonOptions);
                        if (envelope is not null)
                            reports.Add(envelope);
                    }
                    catch (JsonException)
                    {
                        // Skip malformed lines
                    }
                }
                
                currentLine++;
                
                if (reports.Count >= take)
                    break;
            }
            
            return reports;
        }
        finally
        {
            _lock.Release();
        }
    }
}
