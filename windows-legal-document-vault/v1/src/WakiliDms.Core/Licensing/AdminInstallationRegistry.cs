using System.Text.Json;
using WakiliDms.Core.Common;

namespace WakiliDms.Core.Licensing;

public sealed class AdminInstallationRegistry
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true
    };

    private readonly string _registryPath;

    public AdminInstallationRegistry(string registryPath)
    {
        if (string.IsNullOrWhiteSpace(registryPath))
        {
            throw new ArgumentException("Registry path is required.", nameof(registryPath));
        }

        _registryPath = registryPath;
    }

    public async Task<IReadOnlyList<AdminInstallationRecord>> ListAsync(CancellationToken cancellationToken)
    {
        var records = await LoadAsync(cancellationToken);
        return records
            .OrderByDescending(record => record.LastCheckInAt)
            .ThenBy(record => record.FirmDisplayName, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    public async Task<Result<AdminRegistryResult>> UpsertFromCheckInAsync(
        InstallationCheckInPayload payload,
        string supportNotes,
        CancellationToken cancellationToken)
    {
        if (payload.InstallationId == Guid.Empty)
        {
            return Result<AdminRegistryResult>.Fail("Installation ID is required.");
        }

        var records = await LoadAsync(cancellationToken);
        var index = records.FindIndex(record => record.InstallationId == payload.InstallationId);
        var now = payload.CheckedInAt;
        var created = index < 0;
        var existing = created ? null : records[index];
        var record = new AdminInstallationRecord(
            payload.InstallationId,
            payload.FirmDisplayName,
            payload.DeviceNickname,
            payload.LicenseKey,
            payload.AppVersion,
            existing?.LicenseStatus ?? payload.LicenseStatus,
            payload.Features.CloudBackup,
            existing?.CreatedAt ?? now,
            payload.CheckedInAt,
            now,
            supportNotes.Trim());

        if (created)
        {
            records.Add(record);
        }
        else
        {
            records[index] = record;
        }

        await SaveAsync(records, cancellationToken);
        return Result<AdminRegistryResult>.Ok(new AdminRegistryResult(record, created));
    }

    public Task<Result<AdminInstallationRecord>> EnableAsync(
        Guid installationId,
        CancellationToken cancellationToken)
    {
        return SetStatusAsync(installationId, LicenseStatus.Active, cancellationToken);
    }

    public Task<Result<AdminInstallationRecord>> DisableAsync(
        Guid installationId,
        CancellationToken cancellationToken)
    {
        return SetStatusAsync(installationId, LicenseStatus.Disabled, cancellationToken);
    }

    public async Task<Result> DeleteAsync(
        Guid installationId,
        CancellationToken cancellationToken)
    {
        var records = await LoadAsync(cancellationToken);
        var removed = records.RemoveAll(record => record.InstallationId == installationId);
        if (removed == 0)
        {
            return Result.Fail("Installation ID was not found.");
        }

        await SaveAsync(records, cancellationToken);
        return Result.Ok();
    }

    private async Task<Result<AdminInstallationRecord>> SetStatusAsync(
        Guid installationId,
        LicenseStatus licenseStatus,
        CancellationToken cancellationToken)
    {
        var records = await LoadAsync(cancellationToken);
        var index = records.FindIndex(record => record.InstallationId == installationId);
        if (index < 0)
        {
            return Result<AdminInstallationRecord>.Fail("Installation ID was not found.");
        }

        var updated = records[index] with
        {
            LicenseStatus = licenseStatus,
            UpdatedAt = DateTimeOffset.UtcNow
        };
        records[index] = updated;
        await SaveAsync(records, cancellationToken);
        return Result<AdminInstallationRecord>.Ok(updated);
    }

    private async Task<List<AdminInstallationRecord>> LoadAsync(CancellationToken cancellationToken)
    {
        if (!File.Exists(_registryPath))
        {
            return [];
        }

        await using var stream = File.OpenRead(_registryPath);
        return await JsonSerializer.DeserializeAsync<List<AdminInstallationRecord>>(
            stream,
            SerializerOptions,
            cancellationToken) ?? [];
    }

    private async Task SaveAsync(
        IReadOnlyList<AdminInstallationRecord> records,
        CancellationToken cancellationToken)
    {
        var directory = Path.GetDirectoryName(_registryPath);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        await using var stream = File.Create(_registryPath);
        await JsonSerializer.SerializeAsync(stream, records, SerializerOptions, cancellationToken);
    }
}
