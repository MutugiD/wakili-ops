using WakiliDms.Core.Domain;
using WakiliDms.Core.Setup;
using WakiliDms.Infrastructure.Matter;
using WakiliDms.Infrastructure.Settings;
using WakiliDms.Infrastructure.Vault;

var tests = new (string Name, Action Test)[]
{
    ("Setup validation accepts complete local-first settings", SetupValidationAcceptsCompleteSettings),
    ("Setup validation rejects cloud backup in V1", SetupValidationRejectsCloudBackup),
    ("Matter creation trims required name", MatterCreationTrimsName),
    ("Filed and served document statuses are immutable", FiledAndServedStatusesAreImmutable),
    ("JSON settings store saves and loads setup state", JsonSettingsStoreSavesAndLoadsSetupState),
    ("Encrypted vault creates manifest and stores unreadable object bytes", EncryptedVaultStoresUnreadableObjectBytes),
    ("Encrypted vault rejects wrong recovery key", EncryptedVaultRejectsWrongRecoveryKey),
    ("SQLite matter repository persists and lists matters", SqliteMatterRepositoryPersistsAndListsMatters),
    ("SQLite matter repository updates matter details", SqliteMatterRepositoryUpdatesMatterDetails)
};

var failures = 0;

foreach (var (name, test) in tests)
{
    try
    {
        test();
        Console.WriteLine($"PASS {name}");
    }
    catch (Exception ex)
    {
        failures++;
        Console.WriteLine($"FAIL {name}: {ex.Message}");
    }
}

if (failures > 0)
{
    Environment.Exit(1);
}

static void SetupValidationAcceptsCompleteSettings()
{
    var settings = ValidSettings();
    var result = SetupValidator.Validate(settings);

    Assert(result.Succeeded, result.Error ?? "Expected settings to be valid.");
}

static void SetupValidationRejectsCloudBackup()
{
    var settings = ValidSettings() with { CloudBackupEnabled = true };
    var result = SetupValidator.Validate(settings);

    Assert(!result.Succeeded, "Cloud backup must be rejected in V1.");
}

static void MatterCreationTrimsName()
{
    var matter = Matter.Create("  Jane Doe v Acme Ltd  ");

    Assert(matter.Name == "Jane Doe v Acme Ltd", "Matter name should be trimmed.");
}

static void FiledAndServedStatusesAreImmutable()
{
    Assert(DocumentLifecycle.IsImmutable(DocumentStatus.Filed), "Filed status should be immutable.");
    Assert(DocumentLifecycle.IsImmutable(DocumentStatus.Served), "Served status should be immutable.");
    Assert(!DocumentLifecycle.IsImmutable(DocumentStatus.Draft), "Draft status should be mutable.");
}

static void JsonSettingsStoreSavesAndLoadsSetupState()
{
    var tempRoot = Path.Combine(Path.GetTempPath(), "WakiliDms.Tests", Guid.NewGuid().ToString("N"));
    var settingsPath = Path.Combine(tempRoot, "settings.json");
    var store = new JsonSettingsStore(settingsPath);
    var settings = ValidSettings() with { SetupCompletedAt = DateTimeOffset.UtcNow };

    store.SaveAsync(settings, CancellationToken.None).GetAwaiter().GetResult();
    var loaded = store.LoadAsync(CancellationToken.None).GetAwaiter().GetResult();

    try
    {
        Assert(loaded is not null, "Settings should load after save.");
        Assert(loaded!.FirmName == settings.FirmName, "Firm name should round trip.");
        Assert(loaded.SetupCompletedAt is not null, "Setup completion timestamp should round trip.");
    }
    finally
    {
        if (Directory.Exists(tempRoot))
        {
            Directory.Delete(tempRoot, recursive: true);
        }
    }
}

static void EncryptedVaultStoresUnreadableObjectBytes()
{
    var tempRoot = Path.Combine(Path.GetTempPath(), "WakiliDms.Tests", Guid.NewGuid().ToString("N"));
    var vault = new EncryptedVaultService();
    var recoveryKey = "test recovery key";
    var plainText = "Confidential plaint facts for Jane Doe v Acme Ltd.";
    var plainBytes = System.Text.Encoding.UTF8.GetBytes(plainText);

    try
    {
        var create = vault.CreateVaultAsync(tempRoot, recoveryKey, CancellationToken.None).GetAwaiter().GetResult();
        Assert(create.Succeeded, create.Error ?? "Vault creation failed.");

        var stored = vault.StoreObjectAsync(tempRoot, recoveryKey, "plaint.pdf", plainBytes, CancellationToken.None).GetAwaiter().GetResult();
        Assert(stored.Succeeded, stored.Error ?? "Object storage failed.");
        Assert(stored.Value is not null, "Stored object metadata should be returned.");

        var objectPath = Path.Combine(tempRoot, "objects", $"{stored.Value!.ObjectId}.json");
        var objectJson = File.ReadAllText(objectPath);
        Assert(!objectJson.Contains(plainText, StringComparison.Ordinal), "Encrypted vault object should not contain plain text.");

        var read = vault.ReadObjectAsync(tempRoot, recoveryKey, stored.Value.ObjectId, CancellationToken.None).GetAwaiter().GetResult();
        Assert(read.Succeeded, read.Error ?? "Object read failed.");
        Assert(read.Value is not null, "Read object bytes should be returned.");
        Assert(System.Text.Encoding.UTF8.GetString(read.Value!) == plainText, "Read object should match original plain text.");
    }
    finally
    {
        if (Directory.Exists(tempRoot))
        {
            Directory.Delete(tempRoot, recursive: true);
        }
    }
}

static void EncryptedVaultRejectsWrongRecoveryKey()
{
    var tempRoot = Path.Combine(Path.GetTempPath(), "WakiliDms.Tests", Guid.NewGuid().ToString("N"));
    var vault = new EncryptedVaultService();
    var recoveryKey = "correct recovery key";
    var wrongKey = "wrong recovery key";
    var plainBytes = System.Text.Encoding.UTF8.GetBytes("Sensitive affidavit content.");

    try
    {
        var create = vault.CreateVaultAsync(tempRoot, recoveryKey, CancellationToken.None).GetAwaiter().GetResult();
        Assert(create.Succeeded, create.Error ?? "Vault creation failed.");

        var stored = vault.StoreObjectAsync(tempRoot, recoveryKey, "affidavit.pdf", plainBytes, CancellationToken.None).GetAwaiter().GetResult();
        Assert(stored.Succeeded, stored.Error ?? "Object storage failed.");
        Assert(stored.Value is not null, "Stored object metadata should be returned.");

        var read = vault.ReadObjectAsync(tempRoot, wrongKey, stored.Value!.ObjectId, CancellationToken.None).GetAwaiter().GetResult();
        Assert(!read.Succeeded, "Wrong recovery key should not unlock the object.");
    }
    finally
    {
        if (Directory.Exists(tempRoot))
        {
            Directory.Delete(tempRoot, recursive: true);
        }
    }
}

static void SqliteMatterRepositoryPersistsAndListsMatters()
{
    var tempRoot = Path.Combine(Path.GetTempPath(), "WakiliDms.Tests", Guid.NewGuid().ToString("N"));
    var dbPath = Path.Combine(tempRoot, "wakili-dms.db");

    try
    {
        var repository = new SqliteMatterRepository(dbPath);
        repository.InitializeAsync(CancellationToken.None).GetAwaiter().GetResult();

        var matter = Matter.Create(
            "Jane Doe v Acme Ltd",
            courtCaseNumber: "ELC-123",
            clientName: "Jane Doe");
        repository.AddAsync(matter, CancellationToken.None).GetAwaiter().GetResult();

        var reloadedRepository = new SqliteMatterRepository(dbPath);
        reloadedRepository.InitializeAsync(CancellationToken.None).GetAwaiter().GetResult();
        var matters = reloadedRepository.ListAsync(CancellationToken.None).GetAwaiter().GetResult();

        Assert(matters.Count == 1, "Matter list should contain one persisted matter.");
        Assert(matters[0].Name == "Jane Doe v Acme Ltd", "Persisted matter name should round trip.");
        Assert(matters[0].CourtCaseNumber == "ELC-123", "Court case number should round trip.");
    }
    finally
    {
        if (Directory.Exists(tempRoot))
        {
            Directory.Delete(tempRoot, recursive: true);
        }
    }
}

static void SqliteMatterRepositoryUpdatesMatterDetails()
{
    var tempRoot = Path.Combine(Path.GetTempPath(), "WakiliDms.Tests", Guid.NewGuid().ToString("N"));
    var dbPath = Path.Combine(tempRoot, "wakili-dms.db");

    try
    {
        var repository = new SqliteMatterRepository(dbPath);
        repository.InitializeAsync(CancellationToken.None).GetAwaiter().GetResult();

        var matter = Matter.Create("Old Matter Name", clientName: "Old Client");
        repository.AddAsync(matter, CancellationToken.None).GetAwaiter().GetResult();

        var updated = matter.WithUpdatedDetails("New Matter Name", clientName: "New Client");
        repository.UpdateAsync(updated, CancellationToken.None).GetAwaiter().GetResult();

        var loaded = repository.GetAsync(matter.Id, CancellationToken.None).GetAwaiter().GetResult();

        Assert(loaded is not null, "Updated matter should load by ID.");
        Assert(loaded!.Name == "New Matter Name", "Matter name should update.");
        Assert(loaded.ClientName == "New Client", "Client name should update.");
    }
    finally
    {
        if (Directory.Exists(tempRoot))
        {
            Directory.Delete(tempRoot, recursive: true);
        }
    }
}

static AppSettings ValidSettings()
{
    return new AppSettings
    {
        FirmName = "Wakili Test Firm",
        PrimaryUser = "Test Advocate",
        VaultPath = @"C:\WakiliVault",
        ScanFolderPath = @"C:\WakiliScans",
        BackupTargetPath = @"D:\WakiliBackups",
        RecoveryKeyConfirmed = true,
        CloudBackupEnabled = false
    };
}

static void Assert(bool condition, string message)
{
    if (!condition)
    {
        throw new InvalidOperationException(message);
    }
}
