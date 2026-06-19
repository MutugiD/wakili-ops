using System.Security.Cryptography;
using WakiliDms.Core.Common;
using WakiliDms.Core.Documents;
using WakiliDms.Core.Domain;

namespace WakiliDms.Core.Scan;

public sealed class ScanFolderService
{
    private readonly IScanInboxRepository _scanInboxRepository;

    public ScanFolderService(IScanInboxRepository scanInboxRepository)
    {
        _scanInboxRepository = scanInboxRepository;
    }

    public async Task<Result<ScanFolderResult>> ScanOnceAsync(
        string scanFolderPath,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(scanFolderPath))
        {
            return Result<ScanFolderResult>.Fail("Scan folder path is required.");
        }

        if (!Directory.Exists(scanFolderPath))
        {
            return Result<ScanFolderResult>.Fail("Scan folder was not found.");
        }

        var queued = 0;
        var duplicates = 0;
        var ignored = 0;

        foreach (var filePath in Directory.EnumerateFiles(scanFolderPath))
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (!DocumentFilePolicy.IsSupported(filePath))
            {
                ignored++;
                continue;
            }

            var bytes = await File.ReadAllBytesAsync(filePath, cancellationToken);
            if (bytes.Length == 0)
            {
                ignored++;
                continue;
            }

            var hash = Convert.ToHexString(SHA256.HashData(bytes));
            var exists = await _scanInboxRepository.ExistsAsync(filePath, hash, cancellationToken);
            if (exists)
            {
                duplicates++;
                continue;
            }

            await _scanInboxRepository.AddAsync(
                ScanInboxItem.CreatePending(filePath, hash, bytes.LongLength),
                cancellationToken);
            queued++;
        }

        return Result<ScanFolderResult>.Ok(new ScanFolderResult(queued, duplicates, ignored));
    }
}
