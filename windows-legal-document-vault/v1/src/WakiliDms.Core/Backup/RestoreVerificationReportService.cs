using System.Text.Json;
using WakiliDms.Core.Common;

namespace WakiliDms.Core.Backup;

public sealed class RestoreVerificationReportService
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true
    };

    public async Task<Result<string>> WriteAsync(
        string reportDirectory,
        RestoreVerificationReport report,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(reportDirectory))
        {
            return Result<string>.Fail("Report directory is required.");
        }

        if (string.IsNullOrWhiteSpace(report.SourceKind))
        {
            return Result<string>.Fail("Report source kind is required.");
        }

        if (string.IsNullOrWhiteSpace(report.RestoreDirectory))
        {
            return Result<string>.Fail("Report restore directory is required.");
        }

        try
        {
            Directory.CreateDirectory(reportDirectory);
            var reportPath = Path.Combine(reportDirectory, "restore-verification-report.json");
            await using var stream = File.Create(reportPath);
            await JsonSerializer.SerializeAsync(stream, report, SerializerOptions, cancellationToken);
            return Result<string>.Ok(reportPath);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException)
        {
            return Result<string>.Fail($"Restore verification report failed: {ex.Message}");
        }
    }
}

