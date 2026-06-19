using System.Text.Json;
using WakiliDms.Core.Licensing;

var options = ParseOptions(args);
if (!options.TryGetValue("command", out var command) || string.IsNullOrWhiteSpace(command))
{
    PrintUsage();
    return 2;
}

if (!options.TryGetValue("registry", out var registryPath) || string.IsNullOrWhiteSpace(registryPath))
{
    Console.Error.WriteLine("Missing --registry path.");
    PrintUsage();
    return 2;
}

var registry = new AdminInstallationRegistry(registryPath);
var cancellationToken = CancellationToken.None;

try
{
    switch (command.ToLowerInvariant())
    {
        case "list":
            await ListAsync(registry, cancellationToken);
            return 0;

        case "checkin":
            await CheckInAsync(registry, options, cancellationToken);
            return 0;

        case "enable":
            await SetStatusAsync(registry, options, enable: true, cancellationToken);
            return 0;

        case "disable":
            await SetStatusAsync(registry, options, enable: false, cancellationToken);
            return 0;

        case "delete":
            await DeleteAsync(registry, options, cancellationToken);
            return 0;

        default:
            Console.Error.WriteLine($"Unknown command: {command}");
            PrintUsage();
            return 2;
    }
}
catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException)
{
    Console.Error.WriteLine(ex.Message);
    return 1;
}

static async Task ListAsync(
    AdminInstallationRegistry registry,
    CancellationToken cancellationToken)
{
    var records = await registry.ListAsync(cancellationToken);
    foreach (var record in records)
    {
        Console.WriteLine($"{record.InstallationId} | {record.LicenseStatus} | {record.FirmDisplayName} | {record.DeviceNickname} | {record.AppVersion} | {record.LastCheckInAt:O}");
    }
}

static async Task CheckInAsync(
    AdminInstallationRegistry registry,
    IReadOnlyDictionary<string, string> options,
    CancellationToken cancellationToken)
{
    if (!options.TryGetValue("payload", out var payloadPath) || string.IsNullOrWhiteSpace(payloadPath))
    {
        throw new ArgumentException("Missing --payload path.");
    }

    await using var stream = File.OpenRead(payloadPath);
    var payload = await JsonSerializer.DeserializeAsync<InstallationCheckInPayload>(
        stream,
        new JsonSerializerOptions(JsonSerializerDefaults.Web),
        cancellationToken);
    if (payload is null)
    {
        throw new InvalidOperationException("Check-in payload could not be read.");
    }

    options.TryGetValue("notes", out var notes);
    var result = await registry.UpsertFromCheckInAsync(payload, notes ?? string.Empty, cancellationToken);
    if (!result.Succeeded || result.Value is null)
    {
        throw new InvalidOperationException(result.Error ?? "Check-in failed.");
    }

    Console.WriteLine($"{(result.Value.Created ? "CREATED" : "UPDATED")} {result.Value.Record.InstallationId} {result.Value.Record.LicenseStatus}");
}

static async Task SetStatusAsync(
    AdminInstallationRegistry registry,
    IReadOnlyDictionary<string, string> options,
    bool enable,
    CancellationToken cancellationToken)
{
    var installationId = ParseInstallationId(options);
    var result = enable
        ? await registry.EnableAsync(installationId, cancellationToken)
        : await registry.DisableAsync(installationId, cancellationToken);
    if (!result.Succeeded || result.Value is null)
    {
        throw new InvalidOperationException(result.Error ?? "Status update failed.");
    }

    Console.WriteLine($"{result.Value.InstallationId} {result.Value.LicenseStatus}");
}

static async Task DeleteAsync(
    AdminInstallationRegistry registry,
    IReadOnlyDictionary<string, string> options,
    CancellationToken cancellationToken)
{
    var installationId = ParseInstallationId(options);
    var result = await registry.DeleteAsync(installationId, cancellationToken);
    if (!result.Succeeded)
    {
        throw new InvalidOperationException(result.Error ?? "Delete failed.");
    }

    Console.WriteLine($"DELETED {installationId}");
}

static Guid ParseInstallationId(IReadOnlyDictionary<string, string> options)
{
    if (!options.TryGetValue("installation-id", out var installationIdText)
        || !Guid.TryParse(installationIdText, out var installationId))
    {
        throw new ArgumentException("A valid --installation-id value is required.");
    }

    return installationId;
}

static Dictionary<string, string> ParseOptions(string[] args)
{
    var parsed = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
    if (args.Length > 0 && !args[0].StartsWith("--", StringComparison.Ordinal))
    {
        parsed["command"] = args[0];
    }

    for (var i = 1; i < args.Length; i++)
    {
        var key = args[i];
        if (!key.StartsWith("--", StringComparison.Ordinal))
        {
            continue;
        }

        var normalizedKey = key[2..];
        var value = i + 1 < args.Length && !args[i + 1].StartsWith("--", StringComparison.Ordinal)
            ? args[++i]
            : "true";
        parsed[normalizedKey] = value;
    }

    return parsed;
}

static void PrintUsage()
{
    Console.WriteLine("""
        Windows Legal Document Vault Admin

        Commands:
          list --registry <path>
          checkin --registry <path> --payload <check-in-json> [--notes <text>]
          enable --registry <path> --installation-id <guid>
          disable --registry <path> --installation-id <guid>
          delete --registry <path> --installation-id <guid>
        """);
}
