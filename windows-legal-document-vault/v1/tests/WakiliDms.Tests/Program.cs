using System.Text.Json;
using WakiliDms.Core.Backup;
using WakiliDms.Core.CloudBackup;
using WakiliDms.Core.CourtOutput;
using WakiliDms.Core.Domain;
using WakiliDms.Core.Documents;
using WakiliDms.Core.Filing;
using WakiliDms.Core.Licensing;
using WakiliDms.Core.Setup;
using WakiliDms.Core.Scan;
using WakiliDms.Core.Search;
using WakiliDms.Infrastructure.Documents;
using WakiliDms.Infrastructure.CloudBackup;
using WakiliDms.Infrastructure.Matter;
using WakiliDms.Infrastructure.Scan;
using WakiliDms.Infrastructure.Search;
using WakiliDms.Infrastructure.Settings;
using WakiliDms.Infrastructure.Vault;

var tests = new (string Name, Action Test)[]
{
    ("Setup validation accepts complete local-first settings", SetupValidationAcceptsCompleteSettings),
    ("Setup validation rejects cloud backup during first-run setup", SetupValidationRejectsCloudBackupDuringFirstRunSetup),
    ("Matter creation trims required name", MatterCreationTrimsName),
    ("Filed and served document statuses are immutable", FiledAndServedStatusesAreImmutable),
    ("JSON settings store saves and loads setup state", JsonSettingsStoreSavesAndLoadsSetupState),
    ("Default app paths honor test environment overrides", DefaultAppPathsHonorTestEnvironmentOverrides),
    ("Encrypted vault creates manifest and stores unreadable object bytes", EncryptedVaultStoresUnreadableObjectBytes),
    ("Encrypted vault rejects wrong recovery key", EncryptedVaultRejectsWrongRecoveryKey),
    ("SQLite matter repository persists and lists matters", SqliteMatterRepositoryPersistsAndListsMatters),
    ("SQLite matter repository updates matter details", SqliteMatterRepositoryUpdatesMatterDetails),
    ("Document import stores PDF bytes encrypted and registers metadata", DocumentImportStoresPdfEncryptedAndRegistersMetadata),
    ("Document import accepts DOCX files and round trips vault bytes", DocumentImportAcceptsDocxAndRoundTripsVaultBytes),
    ("Document import rejects unsupported file types", DocumentImportRejectsUnsupportedFileTypes),
    ("Scan folder queues supported files once", ScanFolderQueuesSupportedFilesOnce),
    ("Scan inbox import marks pending scan imported", ScanInboxImportMarksPendingScanImported),
    ("Document import creates initial version metadata", DocumentImportCreatesInitialVersionMetadata),
    ("Document classification updates type and status", DocumentClassificationUpdatesTypeAndStatus),
    ("Filed document classification is immutable", FiledDocumentClassificationIsImmutable),
    ("DOCX text extractor returns searchable body text", DocxTextExtractorReturnsSearchableBodyText),
    ("Document indexing searches encrypted DOCX by matter", DocumentIndexingSearchesEncryptedDocxByMatter),
    ("Text-like PDF extraction can be indexed and searched", TextLikePdfExtractionCanBeIndexedAndSearched),
    ("Windows end-to-end matter workflow completes", WindowsEndToEndMatterWorkflowCompletes),
    ("Filing pack export writes decrypted copies and manifest", FilingPackExportWritesDecryptedCopiesAndManifest),
    ("Court output capture stores filing receipt under matter", CourtOutputCaptureStoresFilingReceiptUnderMatter),
    ("Court output capture rejects non-output document type", CourtOutputCaptureRejectsNonOutputDocumentType),
    ("Backup snapshot copies encrypted vault and database with manifest", BackupSnapshotCopiesEncryptedVaultAndDatabaseWithManifest),
    ("Restore drill verifies backup hashes and copies restorable files", RestoreDrillVerifiesBackupHashesAndCopiesRestorableFiles),
    ("Restore drill verifies backup copied from another machine", RestoreDrillVerifiesBackupCopiedFromAnotherMachine),
    ("Restore verification report excludes document text", RestoreVerificationReportExcludesDocumentText),
    ("Local backup catalog lists restorable snapshots", LocalBackupCatalogListsRestorableSnapshots),
    ("Backup snapshot rejects target inside vault", BackupSnapshotRejectsTargetInsideVault),
    ("Restore drill rejects destructive target paths without deleting backup", RestoreDrillRejectsDestructiveTargetPathsWithoutDeletingBackup),
    ("Restore drill rejects tampered backup hashes", RestoreDrillRejectsTamperedBackupHashes),
    ("Cloud backup upload requires entitlement", CloudBackupUploadRequiresEntitlement),
    ("Cloud backup upload stores encrypted package and redacted metadata", CloudBackupUploadStoresEncryptedPackageAndRedactedMetadata),
    ("Cloud backup download restores snapshot for restore drill", CloudBackupDownloadRestoresSnapshotForRestoreDrill),
    ("Installation identity is generated and preserved", InstallationIdentityIsGeneratedAndPreserved),
    ("Disabled installation blocks licensed feature access without deleting data", DisabledInstallationBlocksLicensedFeatureAccessWithoutDeletingData),
    ("Installation check-in payload excludes document and matter details", InstallationCheckInPayloadExcludesDocumentAndMatterDetails),
    ("Admin registry can enable and disable installation IDs", AdminRegistryCanEnableAndDisableInstallationIds),
    ("Admin registry delete does not touch vault data", AdminRegistryDeleteDoesNotTouchVaultData)
};

var filter = ReadFilter(args);
var selectedTests = string.IsNullOrWhiteSpace(filter)
    ? tests
    : tests.Where(test => test.Name.Contains(filter, StringComparison.OrdinalIgnoreCase)).ToArray();
if (selectedTests.Length == 0)
{
    Console.WriteLine($"FAIL No tests matched filter: {filter}");
    Environment.Exit(1);
}

var failures = 0;

foreach (var (name, test) in selectedTests)
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

static string? ReadFilter(string[] args)
{
    for (var index = 0; index < args.Length; index++)
    {
        if (string.Equals(args[index], "--filter", StringComparison.OrdinalIgnoreCase)
            && index + 1 < args.Length)
        {
            return args[index + 1];
        }
    }

    return null;
}

static void SetupValidationAcceptsCompleteSettings()
{
    var settings = ValidSettings();
    var result = SetupValidator.Validate(settings);

    Assert(result.Succeeded, result.Error ?? "Expected settings to be valid.");
}

static void SetupValidationRejectsCloudBackupDuringFirstRunSetup()
{
    var settings = ValidSettings() with { CloudBackupEnabled = true };
    var result = SetupValidator.Validate(settings);

    Assert(!result.Succeeded, "Cloud backup must be rejected during first-run setup.");
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
    var settings = ValidSettings() with
    {
        SetupCompletedAt = DateTimeOffset.UtcNow,
        CloudBackupProviderPath = Path.Combine(tempRoot, "cloud-provider")
    };

    store.SaveAsync(settings, CancellationToken.None).GetAwaiter().GetResult();
    var loaded = store.LoadAsync(CancellationToken.None).GetAwaiter().GetResult();

    try
    {
        Assert(loaded is not null, "Settings should load after save.");
        Assert(loaded!.FirmName == settings.FirmName, "Firm name should round trip.");
        Assert(loaded.SetupCompletedAt is not null, "Setup completion timestamp should round trip.");
        Assert(loaded.CloudBackupProviderPath == settings.CloudBackupProviderPath, "Cloud backup provider path should round trip.");
    }
    finally
    {
        if (Directory.Exists(tempRoot))
        {
            Directory.Delete(tempRoot, recursive: true);
        }
    }
}

static void DefaultAppPathsHonorTestEnvironmentOverrides()
{
    const string settingsOverrideName = "WAKILI_DMS_SETTINGS_PATH";
    const string databaseOverrideName = "WAKILI_DMS_DATABASE_PATH";
    var previousSettingsOverride = Environment.GetEnvironmentVariable(settingsOverrideName);
    var previousDatabaseOverride = Environment.GetEnvironmentVariable(databaseOverrideName);
    var tempRoot = Path.Combine(Path.GetTempPath(), "WakiliDms.Tests", Guid.NewGuid().ToString("N"));
    var settingsPath = Path.Combine(tempRoot, "settings.json");
    var databasePath = Path.Combine(tempRoot, "wakili-dms.db");

    try
    {
        Environment.SetEnvironmentVariable(settingsOverrideName, settingsPath);
        Environment.SetEnvironmentVariable(databaseOverrideName, databasePath);

        Assert(DefaultAppPaths.SettingsPath() == settingsPath, "Settings path override should be used.");
        Assert(DefaultAppPaths.DatabasePath() == databasePath, "Database path override should be used.");
    }
    finally
    {
        Environment.SetEnvironmentVariable(settingsOverrideName, previousSettingsOverride);
        Environment.SetEnvironmentVariable(databaseOverrideName, previousDatabaseOverride);
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

static void DocumentImportStoresPdfEncryptedAndRegistersMetadata()
{
    var tempRoot = Path.Combine(Path.GetTempPath(), "WakiliDms.Tests", Guid.NewGuid().ToString("N"));
    var dbPath = Path.Combine(tempRoot, "wakili-dms.db");
    var vaultPath = Path.Combine(tempRoot, "vault");
    var sourcePath = Path.Combine(tempRoot, "plaint.pdf");
    var recoveryKey = "document import recovery key";
    var plainText = "%PDF-1.7\nConfidential plaint facts for Jane Doe v Acme Ltd.\n%%EOF";
    var sourceBytes = System.Text.Encoding.UTF8.GetBytes(plainText);

    try
    {
        Directory.CreateDirectory(tempRoot);
        File.WriteAllBytes(sourcePath, sourceBytes);

        var matterRepository = new SqliteMatterRepository(dbPath);
        var documentRepository = new SqliteDocumentRepository(dbPath);
        var documentVersionRepository = new SqliteDocumentVersionRepository(dbPath);
        var vault = new EncryptedVaultService();
        matterRepository.InitializeAsync(CancellationToken.None).GetAwaiter().GetResult();
        documentRepository.InitializeAsync(CancellationToken.None).GetAwaiter().GetResult();
        documentVersionRepository.InitializeAsync(CancellationToken.None).GetAwaiter().GetResult();
        vault.CreateVaultAsync(vaultPath, recoveryKey, CancellationToken.None).GetAwaiter().GetResult();

        var matter = Matter.Create("Jane Doe v Acme Ltd");
        matterRepository.AddAsync(matter, CancellationToken.None).GetAwaiter().GetResult();

        var service = new DocumentImportService(matterRepository, documentRepository, documentVersionRepository, vault);
        var result = service.ImportAsync(
            new DocumentImportRequest(matter.Id, sourcePath, vaultPath, recoveryKey, DocumentType.Pleading),
            CancellationToken.None).GetAwaiter().GetResult();

        Assert(result.Succeeded, result.Error ?? "Document import failed.");
        Assert(result.Value is not null, "Imported document should be returned.");
        Assert(result.Value!.OriginalFileName == "plaint.pdf", "Original file name should be stored.");
        Assert(result.Value.DocumentType == DocumentType.Pleading, "Document type should be stored.");
        Assert(result.Value.Status == DocumentStatus.Imported, "Imported document should start in Imported status.");

        var documents = documentRepository.ListByMatterAsync(matter.Id, CancellationToken.None).GetAwaiter().GetResult();
        Assert(documents.Count == 1, "Matter should list the imported document.");
        Assert(documents[0].VaultObjectId == result.Value.VaultObjectId, "Document should reference the stored vault object.");

        var objectPath = Path.Combine(vaultPath, "objects", $"{result.Value.VaultObjectId}.json");
        var objectJson = File.ReadAllText(objectPath);
        Assert(!objectJson.Contains("Confidential plaint facts", StringComparison.Ordinal), "Vault object should not contain plain document text.");

        var read = vault.ReadObjectAsync(vaultPath, recoveryKey, result.Value.VaultObjectId, CancellationToken.None).GetAwaiter().GetResult();
        Assert(read.Succeeded, read.Error ?? "Vault read failed.");
        Assert(read.Value is not null, "Vault read should return bytes.");
        Assert(sourceBytes.SequenceEqual(read.Value!), "Vault bytes should match the imported source file.");
    }
    finally
    {
        if (Directory.Exists(tempRoot))
        {
            Directory.Delete(tempRoot, recursive: true);
        }
    }
}

static void DocumentImportAcceptsDocxAndRoundTripsVaultBytes()
{
    var tempRoot = Path.Combine(Path.GetTempPath(), "WakiliDms.Tests", Guid.NewGuid().ToString("N"));
    var dbPath = Path.Combine(tempRoot, "wakili-dms.db");
    var vaultPath = Path.Combine(tempRoot, "vault");
    var sourcePath = Path.Combine(tempRoot, "draft-affidavit.docx");
    var recoveryKey = "docx import recovery key";

    try
    {
        Directory.CreateDirectory(tempRoot);
        CreateMinimalDocx(sourcePath, "Draft affidavit content.");

        var matterRepository = new SqliteMatterRepository(dbPath);
        var documentRepository = new SqliteDocumentRepository(dbPath);
        var documentVersionRepository = new SqliteDocumentVersionRepository(dbPath);
        var vault = new EncryptedVaultService();
        matterRepository.InitializeAsync(CancellationToken.None).GetAwaiter().GetResult();
        documentRepository.InitializeAsync(CancellationToken.None).GetAwaiter().GetResult();
        documentVersionRepository.InitializeAsync(CancellationToken.None).GetAwaiter().GetResult();
        vault.CreateVaultAsync(vaultPath, recoveryKey, CancellationToken.None).GetAwaiter().GetResult();

        var matter = Matter.Create("Estate of Test Deceased");
        matterRepository.AddAsync(matter, CancellationToken.None).GetAwaiter().GetResult();

        var service = new DocumentImportService(matterRepository, documentRepository, documentVersionRepository, vault);
        var result = service.ImportAsync(
            new DocumentImportRequest(matter.Id, sourcePath, vaultPath, recoveryKey, DocumentType.Affidavit),
            CancellationToken.None).GetAwaiter().GetResult();

        Assert(result.Succeeded, result.Error ?? "DOCX import failed.");
        Assert(result.Value is not null, "Imported DOCX should be returned.");
        Assert(result.Value!.Extension == ".docx", "DOCX extension should be stored.");

        var read = vault.ReadObjectAsync(vaultPath, recoveryKey, result.Value.VaultObjectId, CancellationToken.None).GetAwaiter().GetResult();
        Assert(read.Succeeded, read.Error ?? "Vault read failed.");
        Assert(File.ReadAllBytes(sourcePath).SequenceEqual(read.Value!), "Stored DOCX bytes should match source bytes.");
    }
    finally
    {
        if (Directory.Exists(tempRoot))
        {
            Directory.Delete(tempRoot, recursive: true);
        }
    }
}

static void DocumentImportRejectsUnsupportedFileTypes()
{
    var tempRoot = Path.Combine(Path.GetTempPath(), "WakiliDms.Tests", Guid.NewGuid().ToString("N"));
    var dbPath = Path.Combine(tempRoot, "wakili-dms.db");
    var vaultPath = Path.Combine(tempRoot, "vault");
    var sourcePath = Path.Combine(tempRoot, "notes.txt");
    var recoveryKey = "unsupported import recovery key";

    try
    {
        Directory.CreateDirectory(tempRoot);
        File.WriteAllText(sourcePath, "Plain notes should not import in this slice.");

        var matterRepository = new SqliteMatterRepository(dbPath);
        var documentRepository = new SqliteDocumentRepository(dbPath);
        var documentVersionRepository = new SqliteDocumentVersionRepository(dbPath);
        var vault = new EncryptedVaultService();
        matterRepository.InitializeAsync(CancellationToken.None).GetAwaiter().GetResult();
        documentRepository.InitializeAsync(CancellationToken.None).GetAwaiter().GetResult();
        documentVersionRepository.InitializeAsync(CancellationToken.None).GetAwaiter().GetResult();
        vault.CreateVaultAsync(vaultPath, recoveryKey, CancellationToken.None).GetAwaiter().GetResult();

        var matter = Matter.Create("Test Matter");
        matterRepository.AddAsync(matter, CancellationToken.None).GetAwaiter().GetResult();

        var service = new DocumentImportService(matterRepository, documentRepository, documentVersionRepository, vault);
        var result = service.ImportAsync(
            new DocumentImportRequest(matter.Id, sourcePath, vaultPath, recoveryKey),
            CancellationToken.None).GetAwaiter().GetResult();

        Assert(!result.Succeeded, "Unsupported TXT file should be rejected.");
    }
    finally
    {
        if (Directory.Exists(tempRoot))
        {
            Directory.Delete(tempRoot, recursive: true);
        }
    }
}

static void ScanFolderQueuesSupportedFilesOnce()
{
    var tempRoot = Path.Combine(Path.GetTempPath(), "WakiliDms.Tests", Guid.NewGuid().ToString("N"));
    var dbPath = Path.Combine(tempRoot, "wakili-dms.db");
    var scanFolderPath = Path.Combine(tempRoot, "scan");

    try
    {
        Directory.CreateDirectory(scanFolderPath);
        File.WriteAllText(Path.Combine(scanFolderPath, "registry-notice.pdf"), "%PDF-1.7\nRegistry notice\n%%EOF");
        File.WriteAllText(Path.Combine(scanFolderPath, "desktop.ini"), "Not a legal document.");

        var repository = new SqliteScanInboxRepository(dbPath);
        repository.InitializeAsync(CancellationToken.None).GetAwaiter().GetResult();
        var service = new ScanFolderService(repository);

        var first = service.ScanOnceAsync(scanFolderPath, CancellationToken.None).GetAwaiter().GetResult();
        Assert(first.Succeeded, first.Error ?? "First scan failed.");
        Assert(first.Value is not null, "First scan result should be returned.");
        Assert(first.Value!.QueuedCount == 1, "First scan should queue one supported file.");
        Assert(first.Value.IgnoredCount == 1, "First scan should ignore one unsupported file.");
        Assert(first.Value.DuplicateCount == 0, "First scan should not count duplicates.");

        var second = service.ScanOnceAsync(scanFolderPath, CancellationToken.None).GetAwaiter().GetResult();
        Assert(second.Succeeded, second.Error ?? "Second scan failed.");
        Assert(second.Value is not null, "Second scan result should be returned.");
        Assert(second.Value!.QueuedCount == 0, "Second scan should not queue the same file twice.");
        Assert(second.Value.DuplicateCount == 1, "Second scan should count the supported file as duplicate.");

        var pending = repository.ListPendingAsync(CancellationToken.None).GetAwaiter().GetResult();
        Assert(pending.Count == 1, "Scan inbox should contain one pending file.");
        Assert(pending[0].OriginalFileName == "registry-notice.pdf", "Pending scan should keep the original file name.");
    }
    finally
    {
        if (Directory.Exists(tempRoot))
        {
            Directory.Delete(tempRoot, recursive: true);
        }
    }
}

static void ScanInboxImportMarksPendingScanImported()
{
    var tempRoot = Path.Combine(Path.GetTempPath(), "WakiliDms.Tests", Guid.NewGuid().ToString("N"));
    var dbPath = Path.Combine(tempRoot, "wakili-dms.db");
    var vaultPath = Path.Combine(tempRoot, "vault");
    var scanFolderPath = Path.Combine(tempRoot, "scan");
    var sourcePath = Path.Combine(scanFolderPath, "signed-affidavit.pdf");
    var recoveryKey = "scan inbox import recovery key";

    try
    {
        Directory.CreateDirectory(scanFolderPath);
        File.WriteAllText(sourcePath, "%PDF-1.7\nSigned affidavit from scanner\n%%EOF");

        var matterRepository = new SqliteMatterRepository(dbPath);
        var documentRepository = new SqliteDocumentRepository(dbPath);
        var documentVersionRepository = new SqliteDocumentVersionRepository(dbPath);
        var scanInboxRepository = new SqliteScanInboxRepository(dbPath);
        var vault = new EncryptedVaultService();
        matterRepository.InitializeAsync(CancellationToken.None).GetAwaiter().GetResult();
        documentRepository.InitializeAsync(CancellationToken.None).GetAwaiter().GetResult();
        documentVersionRepository.InitializeAsync(CancellationToken.None).GetAwaiter().GetResult();
        scanInboxRepository.InitializeAsync(CancellationToken.None).GetAwaiter().GetResult();
        vault.CreateVaultAsync(vaultPath, recoveryKey, CancellationToken.None).GetAwaiter().GetResult();

        var matter = Matter.Create("Jane Doe v Acme Ltd");
        matterRepository.AddAsync(matter, CancellationToken.None).GetAwaiter().GetResult();

        var scanService = new ScanFolderService(scanInboxRepository);
        var scan = scanService.ScanOnceAsync(scanFolderPath, CancellationToken.None).GetAwaiter().GetResult();
        Assert(scan.Succeeded, scan.Error ?? "Scan folder refresh failed.");

        var pending = scanInboxRepository.ListPendingAsync(CancellationToken.None).GetAwaiter().GetResult();
        Assert(pending.Count == 1, "Pending scan should be queued before import.");

        var importService = new DocumentImportService(matterRepository, documentRepository, documentVersionRepository, vault);
        var imported = importService.ImportAsync(
            new DocumentImportRequest(matter.Id, pending[0].SourcePath, vaultPath, recoveryKey, DocumentType.Affidavit),
            CancellationToken.None).GetAwaiter().GetResult();
        Assert(imported.Succeeded, imported.Error ?? "Pending scan import failed.");
        Assert(imported.Value is not null, "Imported pending scan should return a document.");

        scanInboxRepository.MarkImportedAsync(
            pending[0].Id,
            imported.Value!.Id,
            DateTimeOffset.UtcNow,
            CancellationToken.None).GetAwaiter().GetResult();

        var pendingAfterImport = scanInboxRepository.ListPendingAsync(CancellationToken.None).GetAwaiter().GetResult();
        var documents = documentRepository.ListByMatterAsync(matter.Id, CancellationToken.None).GetAwaiter().GetResult();
        Assert(pendingAfterImport.Count == 0, "Imported scan should leave the pending inbox.");
        Assert(documents.Count == 1, "Imported scan should register one matter document.");
    }
    finally
    {
        if (Directory.Exists(tempRoot))
        {
            Directory.Delete(tempRoot, recursive: true);
        }
    }
}

static void DocumentImportCreatesInitialVersionMetadata()
{
    var tempRoot = Path.Combine(Path.GetTempPath(), "WakiliDms.Tests", Guid.NewGuid().ToString("N"));
    var dbPath = Path.Combine(tempRoot, "wakili-dms.db");
    var vaultPath = Path.Combine(tempRoot, "vault");
    var sourcePath = Path.Combine(tempRoot, "submissions.pdf");
    var recoveryKey = "version metadata recovery key";

    try
    {
        Directory.CreateDirectory(tempRoot);
        File.WriteAllText(sourcePath, "%PDF-1.7\nWritten submissions\n%%EOF");

        var matterRepository = new SqliteMatterRepository(dbPath);
        var documentRepository = new SqliteDocumentRepository(dbPath);
        var documentVersionRepository = new SqliteDocumentVersionRepository(dbPath);
        var vault = new EncryptedVaultService();
        matterRepository.InitializeAsync(CancellationToken.None).GetAwaiter().GetResult();
        documentRepository.InitializeAsync(CancellationToken.None).GetAwaiter().GetResult();
        documentVersionRepository.InitializeAsync(CancellationToken.None).GetAwaiter().GetResult();
        vault.CreateVaultAsync(vaultPath, recoveryKey, CancellationToken.None).GetAwaiter().GetResult();

        var matter = Matter.Create("Jane Doe v Acme Ltd");
        matterRepository.AddAsync(matter, CancellationToken.None).GetAwaiter().GetResult();

        var importService = new DocumentImportService(matterRepository, documentRepository, documentVersionRepository, vault);
        var imported = importService.ImportAsync(
            new DocumentImportRequest(matter.Id, sourcePath, vaultPath, recoveryKey, DocumentType.Submission),
            CancellationToken.None).GetAwaiter().GetResult();
        Assert(imported.Succeeded, imported.Error ?? "Document import failed.");
        Assert(imported.Value is not null, "Imported document should be returned.");

        var versions = documentVersionRepository.ListByDocumentAsync(imported.Value!.Id, CancellationToken.None).GetAwaiter().GetResult();
        Assert(versions.Count == 1, "Imported document should have one initial version.");
        Assert(versions[0].VersionNumber == 1, "Initial version number should be 1.");
        Assert(versions[0].VaultObjectId == imported.Value.VaultObjectId, "Initial version should reference the imported vault object.");
        Assert(versions[0].Status == DocumentStatus.Imported, "Initial version should preserve imported status.");
    }
    finally
    {
        if (Directory.Exists(tempRoot))
        {
            Directory.Delete(tempRoot, recursive: true);
        }
    }
}

static void DocumentClassificationUpdatesTypeAndStatus()
{
    var tempRoot = Path.Combine(Path.GetTempPath(), "WakiliDms.Tests", Guid.NewGuid().ToString("N"));
    var dbPath = Path.Combine(tempRoot, "wakili-dms.db");

    try
    {
        Directory.CreateDirectory(tempRoot);
        var matterRepository = new SqliteMatterRepository(dbPath);
        var repository = new SqliteDocumentRepository(dbPath);
        matterRepository.InitializeAsync(CancellationToken.None).GetAwaiter().GetResult();
        repository.InitializeAsync(CancellationToken.None).GetAwaiter().GetResult();
        var matter = Matter.Create("Classification Test Matter");
        matterRepository.AddAsync(matter, CancellationToken.None).GetAwaiter().GetResult();

        var document = LegalDocument.CreateImported(
            matter.Id,
            "draft-notice.pdf",
            "vault-object-1",
            new string('A', 64),
            100,
            DocumentType.Unknown);
        repository.AddAsync(document, CancellationToken.None).GetAwaiter().GetResult();

        var updated = document.WithClassification(DocumentType.Notice, DocumentStatus.Reviewed);
        repository.UpdateClassificationAsync(updated, CancellationToken.None).GetAwaiter().GetResult();

        var loaded = repository.GetAsync(document.Id, CancellationToken.None).GetAwaiter().GetResult();
        Assert(loaded is not null, "Document should load after classification update.");
        Assert(loaded!.DocumentType == DocumentType.Notice, "Document type should update.");
        Assert(loaded.Status == DocumentStatus.Reviewed, "Document status should update.");
    }
    finally
    {
        if (Directory.Exists(tempRoot))
        {
            Directory.Delete(tempRoot, recursive: true);
        }
    }
}

static void FiledDocumentClassificationIsImmutable()
{
    var document = LegalDocument.CreateImported(
        Guid.NewGuid(),
        "filed-plaint.pdf",
        "vault-object-2",
        new string('B', 64),
        200,
        DocumentType.Pleading);

    var filed = document.WithClassification(DocumentType.Pleading, DocumentStatus.Filed);
    var threw = false;

    try
    {
        _ = filed.WithClassification(DocumentType.Pleading, DocumentStatus.Corrected);
    }
    catch (InvalidOperationException)
    {
        threw = true;
    }

    Assert(threw, "Filed document classification should be immutable.");
}

static void DocxTextExtractorReturnsSearchableBodyText()
{
    var tempRoot = Path.Combine(Path.GetTempPath(), "WakiliDms.Tests", Guid.NewGuid().ToString("N"));
    var sourcePath = Path.Combine(tempRoot, "draft-submissions.docx");

    try
    {
        Directory.CreateDirectory(tempRoot);
        CreateMinimalDocx(sourcePath, "Urgent injunction submissions for land parcel Nairobi Block 42.");

        var extractor = new LocalDocumentTextExtractor();
        var result = extractor.ExtractTextAsync(
            sourcePath,
            File.ReadAllBytes(sourcePath),
            CancellationToken.None).GetAwaiter().GetResult();

        Assert(result.Succeeded, result.Error ?? "DOCX extraction failed.");
        Assert(result.Value is not null, "Extracted DOCX text should be returned.");
        Assert(result.Value!.Contains("Urgent injunction submissions", StringComparison.OrdinalIgnoreCase), "DOCX body text should be searchable.");
    }
    finally
    {
        if (Directory.Exists(tempRoot))
        {
            Directory.Delete(tempRoot, recursive: true);
        }
    }
}

static void DocumentIndexingSearchesEncryptedDocxByMatter()
{
    var tempRoot = Path.Combine(Path.GetTempPath(), "WakiliDms.Tests", Guid.NewGuid().ToString("N"));
    var dbPath = Path.Combine(tempRoot, "wakili-dms.db");
    var vaultPath = Path.Combine(tempRoot, "vault");
    var sourcePath = Path.Combine(tempRoot, "supporting-affidavit.docx");
    var recoveryKey = "search indexing recovery key";

    try
    {
        Directory.CreateDirectory(tempRoot);
        CreateMinimalDocx(sourcePath, "Supporting affidavit mentions Kileleshwa lease dispute and rent arrears.");

        var matterRepository = new SqliteMatterRepository(dbPath);
        var documentRepository = new SqliteDocumentRepository(dbPath);
        var documentVersionRepository = new SqliteDocumentVersionRepository(dbPath);
        var searchRepository = new SqliteDocumentSearchRepository(dbPath);
        var vault = new EncryptedVaultService();
        matterRepository.InitializeAsync(CancellationToken.None).GetAwaiter().GetResult();
        documentRepository.InitializeAsync(CancellationToken.None).GetAwaiter().GetResult();
        documentVersionRepository.InitializeAsync(CancellationToken.None).GetAwaiter().GetResult();
        searchRepository.InitializeAsync(CancellationToken.None).GetAwaiter().GetResult();
        vault.CreateVaultAsync(vaultPath, recoveryKey, CancellationToken.None).GetAwaiter().GetResult();

        var matter = Matter.Create("Lease Dispute");
        matterRepository.AddAsync(matter, CancellationToken.None).GetAwaiter().GetResult();

        var importService = new DocumentImportService(matterRepository, documentRepository, documentVersionRepository, vault);
        var imported = importService.ImportAsync(
            new DocumentImportRequest(matter.Id, sourcePath, vaultPath, recoveryKey, DocumentType.Affidavit),
            CancellationToken.None).GetAwaiter().GetResult();
        Assert(imported.Succeeded, imported.Error ?? "DOCX import failed.");
        Assert(imported.Value is not null, "Imported DOCX should be returned.");

        var indexingService = new DocumentIndexingService(
            documentRepository,
            vault,
            new LocalDocumentTextExtractor(),
            searchRepository);
        var indexed = indexingService.IndexDocumentAsync(
            imported.Value!.Id,
            vaultPath,
            recoveryKey,
            CancellationToken.None).GetAwaiter().GetResult();
        Assert(indexed.Succeeded, indexed.Error ?? "Document indexing failed.");

        var results = searchRepository.SearchAsync(matter.Id, "Kileleshwa arrears", CancellationToken.None).GetAwaiter().GetResult();
        Assert(results.Count == 1, "Matter search should return the indexed DOCX.");
        Assert(results[0].OriginalFileName == "supporting-affidavit.docx", "Search result should identify the source document.");
    }
    finally
    {
        if (Directory.Exists(tempRoot))
        {
            Directory.Delete(tempRoot, recursive: true);
        }
    }
}

static void TextLikePdfExtractionCanBeIndexedAndSearched()
{
    var tempRoot = Path.Combine(Path.GetTempPath(), "WakiliDms.Tests", Guid.NewGuid().ToString("N"));
    var dbPath = Path.Combine(tempRoot, "wakili-dms.db");
    var vaultPath = Path.Combine(tempRoot, "vault");
    var sourcePath = Path.Combine(tempRoot, "ruling.pdf");
    var recoveryKey = "pdf search recovery key";

    try
    {
        Directory.CreateDirectory(tempRoot);
        File.WriteAllText(sourcePath, "%PDF-1.7\nBT (Court ruling grants conservatory orders) Tj ET\n%%EOF");

        var matterRepository = new SqliteMatterRepository(dbPath);
        var documentRepository = new SqliteDocumentRepository(dbPath);
        var documentVersionRepository = new SqliteDocumentVersionRepository(dbPath);
        var searchRepository = new SqliteDocumentSearchRepository(dbPath);
        var vault = new EncryptedVaultService();
        matterRepository.InitializeAsync(CancellationToken.None).GetAwaiter().GetResult();
        documentRepository.InitializeAsync(CancellationToken.None).GetAwaiter().GetResult();
        documentVersionRepository.InitializeAsync(CancellationToken.None).GetAwaiter().GetResult();
        searchRepository.InitializeAsync(CancellationToken.None).GetAwaiter().GetResult();
        vault.CreateVaultAsync(vaultPath, recoveryKey, CancellationToken.None).GetAwaiter().GetResult();

        var matter = Matter.Create("Constitutional Petition");
        matterRepository.AddAsync(matter, CancellationToken.None).GetAwaiter().GetResult();

        var importService = new DocumentImportService(matterRepository, documentRepository, documentVersionRepository, vault);
        var imported = importService.ImportAsync(
            new DocumentImportRequest(matter.Id, sourcePath, vaultPath, recoveryKey, DocumentType.Ruling),
            CancellationToken.None).GetAwaiter().GetResult();
        Assert(imported.Succeeded, imported.Error ?? "PDF import failed.");

        var indexingService = new DocumentIndexingService(
            documentRepository,
            vault,
            new LocalDocumentTextExtractor(),
            searchRepository);
        var indexed = indexingService.IndexDocumentAsync(
            imported.Value!.Id,
            vaultPath,
            recoveryKey,
            CancellationToken.None).GetAwaiter().GetResult();
        Assert(indexed.Succeeded, indexed.Error ?? "PDF indexing failed.");

        var results = searchRepository.SearchAsync(matter.Id, "conservatory orders", CancellationToken.None).GetAwaiter().GetResult();
        Assert(results.Count == 1, "Matter search should return the indexed text-like PDF.");
    }
    finally
    {
        if (Directory.Exists(tempRoot))
        {
            Directory.Delete(tempRoot, recursive: true);
        }
    }
}

static void WindowsEndToEndMatterWorkflowCompletes()
{
    var tempRoot = Path.Combine(Path.GetTempPath(), "WakiliDms.E2E", Guid.NewGuid().ToString("N"));
    var settingsPath = Path.Combine(tempRoot, "settings.json");
    var dbPath = Path.Combine(tempRoot, "wakili-dms.db");
    var vaultPath = Path.Combine(tempRoot, "vault");
    var scanFolderPath = Path.Combine(tempRoot, "scan-inbox");
    var backupTargetPath = Path.Combine(tempRoot, "backups");
    var exportRootPath = Path.Combine(tempRoot, "filing-packs");
    var restoreTargetPath = Path.Combine(tempRoot, "restore-output");
    var adminRegistryPath = Path.Combine(tempRoot, "admin", "installations.json");
    var affidavitPath = Path.Combine(tempRoot, "supporting-affidavit.pdf");
    var receiptPath = Path.Combine(tempRoot, "filing-receipt.pdf");
    var recoveryKey = "e2e recovery key";

    try
    {
        Directory.CreateDirectory(tempRoot);
        Directory.CreateDirectory(scanFolderPath);
        File.WriteAllText(
            affidavitPath,
            "%PDF-1.7\nBT (Supporting affidavit mentions Kileleshwa rent arrears and urgent conservatory orders) Tj ET\n%%EOF");
        File.WriteAllText(
            receiptPath,
            "%PDF-1.7\nBT (Filing receipt number WLVD-E2E-001) Tj ET\n%%EOF");
        CreateMinimalDocx(
            Path.Combine(scanFolderPath, "draft-submissions.docx"),
            "Draft submissions mention Nairobi Block 42 and Kileleshwa rent arrears.");
        File.WriteAllText(Path.Combine(scanFolderPath, "desktop.ini"), "ignored");

        var settingsStore = new JsonSettingsStore(settingsPath);
        var installationControl = new InstallationControlService();
        var settings = installationControl.EnsureInstallationIdentity(ValidSettings() with
        {
            FirmName = "End To End Advocates LLP",
            PrimaryUser = "Test Advocate",
            LicenseKey = "WLVD-E2E-001",
            DeviceNickname = "Windows E2E PC",
            VaultPath = vaultPath,
            ScanFolderPath = scanFolderPath,
            BackupTargetPath = backupTargetPath,
            RecoveryKeyConfirmed = true,
            SetupCompletedAt = DateTimeOffset.UtcNow
        }, DateTimeOffset.UtcNow);
        Assert(SetupValidator.Validate(settings).Succeeded, "E2E settings should validate.");
        settingsStore.SaveAsync(settings, CancellationToken.None).GetAwaiter().GetResult();
        var loadedSettings = settingsStore.LoadAsync(CancellationToken.None).GetAwaiter().GetResult();
        Assert(loadedSettings is not null, "E2E settings should reload.");
        Assert(loadedSettings!.InstallationId == settings.InstallationId, "E2E installation ID should persist.");

        var matterRepository = new SqliteMatterRepository(dbPath);
        var documentRepository = new SqliteDocumentRepository(dbPath);
        var documentVersionRepository = new SqliteDocumentVersionRepository(dbPath);
        var scanInboxRepository = new SqliteScanInboxRepository(dbPath);
        var searchRepository = new SqliteDocumentSearchRepository(dbPath);
        var vault = new EncryptedVaultService();
        matterRepository.InitializeAsync(CancellationToken.None).GetAwaiter().GetResult();
        documentRepository.InitializeAsync(CancellationToken.None).GetAwaiter().GetResult();
        documentVersionRepository.InitializeAsync(CancellationToken.None).GetAwaiter().GetResult();
        scanInboxRepository.InitializeAsync(CancellationToken.None).GetAwaiter().GetResult();
        searchRepository.InitializeAsync(CancellationToken.None).GetAwaiter().GetResult();
        Assert(vault.CreateVaultAsync(vaultPath, recoveryKey, CancellationToken.None).GetAwaiter().GetResult().Succeeded, "E2E vault should be created.");

        var matter = Matter.Create("End To End Matter", courtCaseNumber: "E2E-001", clientName: "Synthetic Client");
        matterRepository.AddAsync(matter, CancellationToken.None).GetAwaiter().GetResult();

        var scanService = new ScanFolderService(scanInboxRepository);
        var scan = scanService.ScanOnceAsync(scanFolderPath, CancellationToken.None).GetAwaiter().GetResult();
        Assert(scan.Succeeded && scan.Value is not null, scan.Error ?? "E2E scan failed.");
        Assert(scan.Value!.QueuedCount == 1, "E2E scan should queue one DOCX.");
        Assert(scan.Value.IgnoredCount == 1, "E2E scan should ignore unsupported file.");
        var duplicateScan = scanService.ScanOnceAsync(scanFolderPath, CancellationToken.None).GetAwaiter().GetResult();
        Assert(duplicateScan.Value!.DuplicateCount == 1, "E2E second scan should detect duplicate.");

        var importService = new DocumentImportService(matterRepository, documentRepository, documentVersionRepository, vault);
        var pendingScan = scanInboxRepository.ListPendingAsync(CancellationToken.None).GetAwaiter().GetResult().Single();
        var scanImport = importService.ImportAsync(
            new DocumentImportRequest(matter.Id, pendingScan.SourcePath, vaultPath, recoveryKey, DocumentType.Submission),
            CancellationToken.None).GetAwaiter().GetResult();
        Assert(scanImport.Succeeded && scanImport.Value is not null, scanImport.Error ?? "E2E scan import failed.");
        scanInboxRepository.MarkImportedAsync(pendingScan.Id, scanImport.Value!.Id, DateTimeOffset.UtcNow, CancellationToken.None).GetAwaiter().GetResult();

        var affidavitImport = importService.ImportAsync(
            new DocumentImportRequest(matter.Id, affidavitPath, vaultPath, recoveryKey, DocumentType.Affidavit),
            CancellationToken.None).GetAwaiter().GetResult();
        Assert(affidavitImport.Succeeded && affidavitImport.Value is not null, affidavitImport.Error ?? "E2E affidavit import failed.");

        var updatedAffidavit = affidavitImport.Value!.WithClassification(DocumentType.Affidavit, DocumentStatus.Reviewed);
        documentRepository.UpdateClassificationAsync(updatedAffidavit, CancellationToken.None).GetAwaiter().GetResult();
        var reloadedAffidavit = documentRepository.GetAsync(updatedAffidavit.Id, CancellationToken.None).GetAwaiter().GetResult();
        Assert(reloadedAffidavit!.Status == DocumentStatus.Reviewed, "E2E classification should persist.");

        var indexingService = new DocumentIndexingService(documentRepository, vault, new LocalDocumentTextExtractor(), searchRepository);
        var indexedDocx = indexingService.IndexDocumentAsync(scanImport.Value.Id, vaultPath, recoveryKey, CancellationToken.None).GetAwaiter().GetResult();
        var indexedPdf = indexingService.IndexDocumentAsync(affidavitImport.Value.Id, vaultPath, recoveryKey, CancellationToken.None).GetAwaiter().GetResult();
        Assert(indexedDocx.Succeeded && indexedDocx.Value > 0, indexedDocx.Error ?? "E2E DOCX indexing failed.");
        Assert(indexedPdf.Succeeded && indexedPdf.Value > 0, indexedPdf.Error ?? "E2E PDF indexing failed.");
        var searchResults = searchRepository.SearchAsync(matter.Id, "Kileleshwa arrears", CancellationToken.None).GetAwaiter().GetResult();
        Assert(searchResults.Count >= 1, "E2E search should find indexed matter text.");

        var versions = documentVersionRepository.ListByDocumentAsync(scanImport.Value.Id, CancellationToken.None).GetAwaiter().GetResult();
        Assert(versions.Count == 1, "E2E imported scan should have initial version metadata.");

        var exportService = new FilingPackExportService(matterRepository, documentRepository, vault);
        var filingPack = exportService.ExportAsync(
            new FilingPackExportRequest(
                matter.Id,
                [scanImport.Value.Id, affidavitImport.Value.Id],
                vaultPath,
                recoveryKey,
                exportRootPath),
            CancellationToken.None).GetAwaiter().GetResult();
        Assert(filingPack.Succeeded && filingPack.Value is not null, filingPack.Error ?? "E2E filing pack export failed.");
        Assert(File.Exists(filingPack.Value!.ManifestPath), "E2E filing pack manifest should exist.");
        Assert(Directory.GetFiles(filingPack.Value.ExportDirectory).Length >= 4, "E2E filing pack should contain documents, manifest, and checklist.");

        var courtOutputCapture = new CourtOutputCaptureService(importService);
        var receiptCapture = courtOutputCapture.CaptureAsync(
            new CourtOutputCaptureRequest(matter.Id, receiptPath, vaultPath, recoveryKey, DocumentType.FilingReceipt),
            CancellationToken.None).GetAwaiter().GetResult();
        Assert(receiptCapture.Succeeded && receiptCapture.Value is not null, receiptCapture.Error ?? "E2E receipt capture failed.");

        var matterDocuments = documentRepository.ListByMatterAsync(matter.Id, CancellationToken.None).GetAwaiter().GetResult();
        Assert(matterDocuments.Count == 3, "E2E matter should contain imported DOCX, affidavit PDF, and filing receipt.");

        var backup = new BackupSnapshotService().CreateSnapshotAsync(
            new BackupSnapshotRequest(vaultPath, dbPath, backupTargetPath, recoveryKey),
            CancellationToken.None).GetAwaiter().GetResult();
        Assert(backup.Succeeded && backup.Value is not null, backup.Error ?? "E2E backup failed.");
        var restore = new RestoreDrillService().RunAsync(
            new RestoreDrillRequest(backup.Value!.BackupDirectory, restoreTargetPath, recoveryKey),
            CancellationToken.None).GetAwaiter().GetResult();
        Assert(restore.Succeeded && restore.Value is not null, restore.Error ?? "E2E restore drill failed.");
        Assert(restore.Value!.VerifiedFileCount == backup.Value.BackedUpFileCount, "E2E restore should verify every backup file.");

        var payload = installationControl.CreateCheckInPayload(
            settings,
            "1.0.0-e2e",
            new BackupHealthSummary(backup.Value.CreatedAt, null, "LocalBackupHealthy"),
            DateTimeOffset.UtcNow);
        var adminRegistry = new AdminInstallationRegistry(adminRegistryPath);
        var registered = adminRegistry.UpsertFromCheckInAsync(payload, "E2E install", CancellationToken.None).GetAwaiter().GetResult();
        Assert(registered.Succeeded && registered.Value is not null, registered.Error ?? "E2E admin registry failed.");
        var disabled = adminRegistry.DisableAsync(settings.InstallationId, CancellationToken.None).GetAwaiter().GetResult();
        Assert(disabled.Succeeded && disabled.Value!.LicenseStatus == LicenseStatus.Disabled, disabled.Error ?? "E2E disable failed.");
    }
    finally
    {
        if (Directory.Exists(tempRoot))
        {
            Directory.Delete(tempRoot, recursive: true);
        }
    }
}

static void FilingPackExportWritesDecryptedCopiesAndManifest()
{
    var tempRoot = Path.Combine(Path.GetTempPath(), "WakiliDms.Tests", Guid.NewGuid().ToString("N"));
    var dbPath = Path.Combine(tempRoot, "wakili-dms.db");
    var vaultPath = Path.Combine(tempRoot, "vault");
    var exportRoot = Path.Combine(tempRoot, "exports");
    var plaintPath = Path.Combine(tempRoot, "plaint.pdf");
    var affidavitPath = Path.Combine(tempRoot, "affidavit.docx");
    var recoveryKey = "filing pack recovery key";
    var plaintText = "%PDF-1.7\nPlaint for export\n%%EOF";

    try
    {
        Directory.CreateDirectory(tempRoot);
        File.WriteAllText(plaintPath, plaintText);
        CreateMinimalDocx(affidavitPath, "Affidavit for filing pack export.");

        var matterRepository = new SqliteMatterRepository(dbPath);
        var documentRepository = new SqliteDocumentRepository(dbPath);
        var documentVersionRepository = new SqliteDocumentVersionRepository(dbPath);
        var vault = new EncryptedVaultService();
        matterRepository.InitializeAsync(CancellationToken.None).GetAwaiter().GetResult();
        documentRepository.InitializeAsync(CancellationToken.None).GetAwaiter().GetResult();
        documentVersionRepository.InitializeAsync(CancellationToken.None).GetAwaiter().GetResult();
        vault.CreateVaultAsync(vaultPath, recoveryKey, CancellationToken.None).GetAwaiter().GetResult();

        var matter = Matter.Create("Jane Doe v Acme Ltd", courtCaseNumber: "ELC-100");
        matterRepository.AddAsync(matter, CancellationToken.None).GetAwaiter().GetResult();

        var importService = new DocumentImportService(matterRepository, documentRepository, documentVersionRepository, vault);
        var plaint = importService.ImportAsync(
            new DocumentImportRequest(matter.Id, plaintPath, vaultPath, recoveryKey, DocumentType.Pleading),
            CancellationToken.None).GetAwaiter().GetResult();
        var affidavit = importService.ImportAsync(
            new DocumentImportRequest(matter.Id, affidavitPath, vaultPath, recoveryKey, DocumentType.Affidavit),
            CancellationToken.None).GetAwaiter().GetResult();
        Assert(plaint.Succeeded && plaint.Value is not null, plaint.Error ?? "Plaint import failed.");
        Assert(affidavit.Succeeded && affidavit.Value is not null, affidavit.Error ?? "Affidavit import failed.");

        var exportService = new FilingPackExportService(matterRepository, documentRepository, vault);
        var exported = exportService.ExportAsync(
            new FilingPackExportRequest(
                matter.Id,
                [plaint.Value!.Id, affidavit.Value!.Id],
                vaultPath,
                recoveryKey,
                exportRoot),
            CancellationToken.None).GetAwaiter().GetResult();

        Assert(exported.Succeeded, exported.Error ?? "Filing pack export failed.");
        Assert(exported.Value is not null, "Filing pack export result should be returned.");
        Assert(exported.Value!.ExportedDocumentCount == 2, "Filing pack should export two documents.");
        Assert(File.Exists(exported.Value.ManifestPath), "Filing pack manifest should exist.");
        Assert(File.Exists(exported.Value.ChecklistPath), "Filing readiness checklist should exist.");
        Assert(Directory.GetFiles(exported.Value.ExportDirectory, "*.pdf").Length == 1, "Export should include the PDF copy.");
        Assert(Directory.GetFiles(exported.Value.ExportDirectory, "*.docx").Length == 1, "Export should include the DOCX copy.");
        Assert(File.ReadAllText(exported.Value.ManifestPath).Contains("ELC-100", StringComparison.Ordinal), "Manifest should include matter case number.");
        Assert(File.ReadAllText(exported.Value.ChecklistPath).Contains("Delete temporary export copies", StringComparison.Ordinal), "Checklist should include export cleanup reminder.");
    }
    finally
    {
        if (Directory.Exists(tempRoot))
        {
            Directory.Delete(tempRoot, recursive: true);
        }
    }
}

static void CourtOutputCaptureStoresFilingReceiptUnderMatter()
{
    var tempRoot = Path.Combine(Path.GetTempPath(), "WakiliDms.Tests", Guid.NewGuid().ToString("N"));
    var dbPath = Path.Combine(tempRoot, "wakili-dms.db");
    var vaultPath = Path.Combine(tempRoot, "vault");
    var receiptPath = Path.Combine(tempRoot, "efiling-receipt.pdf");
    var recoveryKey = "court output recovery key";

    try
    {
        Directory.CreateDirectory(tempRoot);
        File.WriteAllText(receiptPath, "%PDF-1.7\nE-filing receipt number KEN-12345\n%%EOF");

        var matterRepository = new SqliteMatterRepository(dbPath);
        var documentRepository = new SqliteDocumentRepository(dbPath);
        var documentVersionRepository = new SqliteDocumentVersionRepository(dbPath);
        var vault = new EncryptedVaultService();
        matterRepository.InitializeAsync(CancellationToken.None).GetAwaiter().GetResult();
        documentRepository.InitializeAsync(CancellationToken.None).GetAwaiter().GetResult();
        documentVersionRepository.InitializeAsync(CancellationToken.None).GetAwaiter().GetResult();
        vault.CreateVaultAsync(vaultPath, recoveryKey, CancellationToken.None).GetAwaiter().GetResult();

        var matter = Matter.Create("Jane Doe v Acme Ltd");
        matterRepository.AddAsync(matter, CancellationToken.None).GetAwaiter().GetResult();

        var importService = new DocumentImportService(matterRepository, documentRepository, documentVersionRepository, vault);
        var captureService = new CourtOutputCaptureService(importService);
        var captured = captureService.CaptureAsync(
            new CourtOutputCaptureRequest(matter.Id, receiptPath, vaultPath, recoveryKey, DocumentType.FilingReceipt),
            CancellationToken.None).GetAwaiter().GetResult();

        Assert(captured.Succeeded, captured.Error ?? "Receipt capture failed.");
        Assert(captured.Value is not null, "Captured receipt should be returned.");
        Assert(captured.Value!.DocumentType == DocumentType.FilingReceipt, "Captured document type should be filing receipt.");

        var documents = documentRepository.ListByMatterAsync(matter.Id, CancellationToken.None).GetAwaiter().GetResult();
        Assert(documents.Count == 1, "Matter should contain the captured receipt.");
        Assert(documents[0].OriginalFileName == "efiling-receipt.pdf", "Captured receipt file name should round trip.");
    }
    finally
    {
        if (Directory.Exists(tempRoot))
        {
            Directory.Delete(tempRoot, recursive: true);
        }
    }
}

static void CourtOutputCaptureRejectsNonOutputDocumentType()
{
    var dbPath = Path.Combine(Path.GetTempPath(), "WakiliDms.Tests", Guid.NewGuid().ToString("N"), "wakili-dms.db");
    var importService = new DocumentImportService(
        new SqliteMatterRepository(dbPath),
        new SqliteDocumentRepository(dbPath),
        new SqliteDocumentVersionRepository(dbPath),
        new EncryptedVaultService());
    var captureService = new CourtOutputCaptureService(importService);

    var result = captureService.CaptureAsync(
        new CourtOutputCaptureRequest(Guid.NewGuid(), "plaint.pdf", "vault", "key", DocumentType.Pleading),
        CancellationToken.None).GetAwaiter().GetResult();

    Assert(!result.Succeeded, "Court output capture should reject pleading document type.");
}

static void BackupSnapshotCopiesEncryptedVaultAndDatabaseWithManifest()
{
    var tempRoot = Path.Combine(Path.GetTempPath(), "WakiliDms.Tests", Guid.NewGuid().ToString("N"));
    var dbPath = Path.Combine(tempRoot, "wakili-dms.db");
    var vaultPath = Path.Combine(tempRoot, "vault");
    var backupTarget = Path.Combine(tempRoot, "backups");
    var sourcePath = Path.Combine(tempRoot, "affidavit.docx");
    var recoveryKey = "backup snapshot recovery key";

    try
    {
        Directory.CreateDirectory(tempRoot);
        CreateMinimalDocx(sourcePath, "Backup snapshot affidavit content must remain encrypted at rest.");
        ImportOneDocumentForBackup(dbPath, vaultPath, sourcePath, recoveryKey);

        var service = new BackupSnapshotService();
        var result = service.CreateSnapshotAsync(
            new BackupSnapshotRequest(vaultPath, dbPath, backupTarget, recoveryKey),
            CancellationToken.None).GetAwaiter().GetResult();

        Assert(result.Succeeded, result.Error ?? "Backup snapshot failed.");
        Assert(result.Value is not null, "Backup snapshot result should be returned.");
        Assert(Directory.Exists(result.Value!.BackupDirectory), "Backup directory should exist.");
        Assert(File.Exists(result.Value.ManifestPath), "Backup manifest should exist.");
        Assert(result.Value.BackedUpFileCount >= 3, "Backup should include vault manifest, vault object, and database.");
        Assert(File.Exists(Path.Combine(result.Value.BackupDirectory, "data", "wakili-dms.db.backup")), "Backup should include the encrypted SQLite database artifact.");
        Assert(!File.Exists(Path.Combine(result.Value.BackupDirectory, "data", "wakili-dms.db")), "Backup should not include a plain SQLite database copy.");

        var manifestText = File.ReadAllText(result.Value.ManifestPath);
        Assert(manifestText.Contains("\"kind\": \"encrypted-database\"", StringComparison.Ordinal), "Backup manifest should identify encrypted database entry.");
        Assert(manifestText.Contains("\"kind\": \"vault\"", StringComparison.Ordinal), "Backup manifest should identify vault entries.");

        var objectJson = Directory.GetFiles(Path.Combine(result.Value.BackupDirectory, "vault", "objects"), "*.json").Single();
        Assert(!File.ReadAllText(objectJson).Contains("Backup snapshot affidavit content", StringComparison.Ordinal), "Backup should not contain decrypted document text.");
    }
    finally
    {
        if (Directory.Exists(tempRoot))
        {
            Directory.Delete(tempRoot, recursive: true);
        }
    }
}

static void RestoreDrillVerifiesBackupHashesAndCopiesRestorableFiles()
{
    var tempRoot = Path.Combine(Path.GetTempPath(), "WakiliDms.Tests", Guid.NewGuid().ToString("N"));
    var dbPath = Path.Combine(tempRoot, "wakili-dms.db");
    var vaultPath = Path.Combine(tempRoot, "vault");
    var backupTarget = Path.Combine(tempRoot, "backups");
    var restoreTarget = Path.Combine(tempRoot, "restore-drill");
    var sourcePath = Path.Combine(tempRoot, "ruling.pdf");
    var recoveryKey = "restore drill recovery key";

    try
    {
        Directory.CreateDirectory(tempRoot);
        File.WriteAllText(sourcePath, "%PDF-1.7\nRestore drill ruling content\n%%EOF");
        ImportOneDocumentForBackup(dbPath, vaultPath, sourcePath, recoveryKey);

        var snapshot = new BackupSnapshotService().CreateSnapshotAsync(
            new BackupSnapshotRequest(vaultPath, dbPath, backupTarget, recoveryKey),
            CancellationToken.None).GetAwaiter().GetResult();
        Assert(snapshot.Succeeded && snapshot.Value is not null, snapshot.Error ?? "Backup snapshot failed.");

        var drill = new RestoreDrillService().RunAsync(
            new RestoreDrillRequest(snapshot.Value!.BackupDirectory, restoreTarget, recoveryKey),
            CancellationToken.None).GetAwaiter().GetResult();

        Assert(drill.Succeeded, drill.Error ?? "Restore drill failed.");
        Assert(drill.Value is not null, "Restore drill result should be returned.");
        Assert(drill.Value!.VerifiedFileCount == snapshot.Value.BackedUpFileCount, "Restore drill should verify every backed-up file.");
        Assert(File.Exists(Path.Combine(restoreTarget, "data", "wakili-dms.db.backup")), "Restore drill should copy encrypted database artifact.");
        Assert(!File.Exists(Path.Combine(restoreTarget, "data", "wakili-dms.db")), "Restore drill should not leave a plain database file.");
        Assert(Directory.GetFiles(Path.Combine(restoreTarget, "vault", "objects"), "*.json").Length == 1, "Restore drill should copy vault object files.");
    }
    finally
    {
        if (Directory.Exists(tempRoot))
        {
            Directory.Delete(tempRoot, recursive: true);
        }
    }
}

static void LocalBackupCatalogListsRestorableSnapshots()
{
    var tempRoot = Path.Combine(Path.GetTempPath(), "WakiliDms.Tests", Guid.NewGuid().ToString("N"));
    var dbPath = Path.Combine(tempRoot, "wakili-dms.db");
    var vaultPath = Path.Combine(tempRoot, "vault");
    var backupTarget = Path.Combine(tempRoot, "backups");
    var sourcePath = Path.Combine(tempRoot, "draft-ruling.docx");
    var recoveryKey = "local catalog recovery key";

    try
    {
        Directory.CreateDirectory(tempRoot);
        CreateMinimalDocx(sourcePath, "Local backup catalog fixture content.");
        ImportOneDocumentForBackup(dbPath, vaultPath, sourcePath, recoveryKey);

        var snapshot = new BackupSnapshotService().CreateSnapshotAsync(
            new BackupSnapshotRequest(vaultPath, dbPath, backupTarget, recoveryKey),
            CancellationToken.None).GetAwaiter().GetResult();
        Assert(snapshot.Succeeded && snapshot.Value is not null, snapshot.Error ?? "Backup snapshot failed.");

        var invalidDirectory = Path.Combine(backupTarget, "invalid-snapshot");
        Directory.CreateDirectory(invalidDirectory);
        File.WriteAllText(Path.Combine(invalidDirectory, "backup-manifest.json"), "{not-json");

        var snapshots = new LocalBackupCatalogService().ListSnapshotsAsync(backupTarget, CancellationToken.None).GetAwaiter().GetResult();

        Assert(snapshots.Count == 1, "Catalog should list only valid local backup snapshots.");
        Assert(snapshots[0].BackupDirectory == Path.GetFullPath(snapshot.Value!.BackupDirectory), "Catalog should return the backup directory.");
        Assert(snapshots[0].SnapshotId == Path.GetFileName(snapshot.Value.BackupDirectory), "Catalog snapshot ID should match backup folder name.");
        Assert(snapshots[0].FileCount == snapshot.Value.BackedUpFileCount, "Catalog should expose backed-up file count.");
        Assert(snapshots[0].ByteLength == snapshot.Value.BackedUpByteLength, "Catalog should expose backed-up byte length.");
    }
    finally
    {
        if (Directory.Exists(tempRoot))
        {
            Directory.Delete(tempRoot, recursive: true);
        }
    }
}

static void RestoreDrillVerifiesBackupCopiedFromAnotherMachine()
{
    var tempRoot = Path.Combine(Path.GetTempPath(), "WakiliDms.Tests", Guid.NewGuid().ToString("N"));
    var dbPath = Path.Combine(tempRoot, "wakili-dms.db");
    var vaultPath = Path.Combine(tempRoot, "vault");
    var backupTarget = Path.Combine(tempRoot, "backups");
    var copiedBackupDirectory = Path.Combine(tempRoot, "copied-from-other-machine");
    var restoreTarget = Path.Combine(tempRoot, "external-restore-workspace");
    var sourcePath = Path.Combine(tempRoot, "copied-machine-pleading.pdf");
    var recoveryKey = "external restore recovery key";

    try
    {
        Directory.CreateDirectory(tempRoot);
        File.WriteAllText(sourcePath, "%PDF-1.7\nCopied machine backup fixture\n%%EOF");
        ImportOneDocumentForBackup(dbPath, vaultPath, sourcePath, recoveryKey);

        var snapshot = new BackupSnapshotService().CreateSnapshotAsync(
            new BackupSnapshotRequest(vaultPath, dbPath, backupTarget, recoveryKey),
            CancellationToken.None).GetAwaiter().GetResult();
        Assert(snapshot.Succeeded && snapshot.Value is not null, snapshot.Error ?? "Backup snapshot failed.");

        CopyDirectory(snapshot.Value!.BackupDirectory, copiedBackupDirectory);
        Directory.Delete(backupTarget, recursive: true);

        var drill = new RestoreDrillService().RunAsync(
            new RestoreDrillRequest(copiedBackupDirectory, restoreTarget, recoveryKey),
            CancellationToken.None).GetAwaiter().GetResult();

        Assert(drill.Succeeded && drill.Value is not null, drill.Error ?? "Copied backup restore drill should pass.");
        Assert(File.Exists(Path.Combine(restoreTarget, "data", "wakili-dms.db.backup")), "Copied backup restore should include encrypted database artifact.");
        Assert(drill.Value!.VerifiedFileCount == snapshot.Value.BackedUpFileCount, "Copied backup restore should verify all files.");
    }
    finally
    {
        if (Directory.Exists(tempRoot))
        {
            Directory.Delete(tempRoot, recursive: true);
        }
    }
}

static void RestoreVerificationReportExcludesDocumentText()
{
    var tempRoot = Path.Combine(Path.GetTempPath(), "WakiliDms.Tests", Guid.NewGuid().ToString("N"));
    var reportDirectory = Path.Combine(tempRoot, "restore-workspace");
    var confidentialText = "Confidential Jane Doe affidavit text";
    var recoveryKeyValue = "super-secret-restore-key";

    try
    {
        var report = new RestoreVerificationReport(
            1,
            DateTimeOffset.UtcNow,
            "ExternalBackup",
            "copied-from-drive",
            reportDirectory,
            2,
            4096,
            "This report intentionally excludes matter names, document names, case numbers, OCR text, document text, and recovery keys.");

        var result = new RestoreVerificationReportService().WriteAsync(
            reportDirectory,
            report,
            CancellationToken.None).GetAwaiter().GetResult();

        Assert(result.Succeeded && result.Value is not null, result.Error ?? "Report write failed.");
        var json = File.ReadAllText(result.Value!);
        Assert(json.Contains("ExternalBackup", StringComparison.Ordinal), "Report should include source kind.");
        Assert(json.Contains("restore-workspace", StringComparison.OrdinalIgnoreCase), "Report should include restore workspace path.");
        Assert(!json.Contains(confidentialText, StringComparison.Ordinal), "Report must not include document text.");
        Assert(!json.Contains(recoveryKeyValue, StringComparison.Ordinal), "Report must not include recovery-key values.");
    }
    finally
    {
        if (Directory.Exists(tempRoot))
        {
            Directory.Delete(tempRoot, recursive: true);
        }
    }
}

static void BackupSnapshotRejectsTargetInsideVault()
{
    var tempRoot = Path.Combine(Path.GetTempPath(), "WakiliDms.Tests", Guid.NewGuid().ToString("N"));
    var dbPath = Path.Combine(tempRoot, "wakili-dms.db");
    var vaultPath = Path.Combine(tempRoot, "vault");
    var sourcePath = Path.Combine(tempRoot, "ruling.pdf");
    var recoveryKey = "backup target validation key";

    try
    {
        Directory.CreateDirectory(tempRoot);
        File.WriteAllText(sourcePath, "%PDF-1.7\nBackup target validation\n%%EOF");
        ImportOneDocumentForBackup(dbPath, vaultPath, sourcePath, recoveryKey);

        var result = new BackupSnapshotService().CreateSnapshotAsync(
            new BackupSnapshotRequest(vaultPath, dbPath, Path.Combine(vaultPath, "backups"), recoveryKey),
            CancellationToken.None).GetAwaiter().GetResult();

        Assert(!result.Succeeded, "Backup target inside vault should be rejected.");
        Assert(result.Error!.Contains("inside the encrypted vault", StringComparison.OrdinalIgnoreCase), "Backup target rejection should explain the vault boundary.");
    }
    finally
    {
        if (Directory.Exists(tempRoot))
        {
            Directory.Delete(tempRoot, recursive: true);
        }
    }
}

static void RestoreDrillRejectsDestructiveTargetPathsWithoutDeletingBackup()
{
    var tempRoot = Path.Combine(Path.GetTempPath(), "WakiliDms.Tests", Guid.NewGuid().ToString("N"));
    var dbPath = Path.Combine(tempRoot, "wakili-dms.db");
    var vaultPath = Path.Combine(tempRoot, "vault");
    var backupTarget = Path.Combine(tempRoot, "backups");
    var sourcePath = Path.Combine(tempRoot, "ruling.pdf");
    var recoveryKey = "restore target validation key";

    try
    {
        Directory.CreateDirectory(tempRoot);
        File.WriteAllText(sourcePath, "%PDF-1.7\nRestore target validation\n%%EOF");
        ImportOneDocumentForBackup(dbPath, vaultPath, sourcePath, recoveryKey);
        var snapshot = new BackupSnapshotService().CreateSnapshotAsync(
            new BackupSnapshotRequest(vaultPath, dbPath, backupTarget, recoveryKey),
            CancellationToken.None).GetAwaiter().GetResult();
        Assert(snapshot.Succeeded && snapshot.Value is not null, snapshot.Error ?? "Backup snapshot failed.");

        var backupDirectory = snapshot.Value!.BackupDirectory;
        var sameDirectory = new RestoreDrillService().RunAsync(
            new RestoreDrillRequest(backupDirectory, backupDirectory, recoveryKey),
            CancellationToken.None).GetAwaiter().GetResult();
        var parentDirectory = new RestoreDrillService().RunAsync(
            new RestoreDrillRequest(backupDirectory, tempRoot, recoveryKey),
            CancellationToken.None).GetAwaiter().GetResult();

        Assert(!sameDirectory.Succeeded, "Restore drill should reject target equal to backup directory.");
        Assert(!parentDirectory.Succeeded, "Restore drill should reject target that contains backup directory.");
        Assert(File.Exists(snapshot.Value.ManifestPath), "Rejected restore target must not delete backup manifest.");
        Assert(Directory.Exists(backupDirectory), "Rejected restore target must not delete backup directory.");
    }
    finally
    {
        if (Directory.Exists(tempRoot))
        {
            Directory.Delete(tempRoot, recursive: true);
        }
    }
}

static void RestoreDrillRejectsTamperedBackupHashes()
{
    var tempRoot = Path.Combine(Path.GetTempPath(), "WakiliDms.Tests", Guid.NewGuid().ToString("N"));
    var dbPath = Path.Combine(tempRoot, "wakili-dms.db");
    var vaultPath = Path.Combine(tempRoot, "vault");
    var backupTarget = Path.Combine(tempRoot, "backups");
    var restoreTarget = Path.Combine(tempRoot, "restore");
    var sourcePath = Path.Combine(tempRoot, "ruling.pdf");
    var recoveryKey = "restore tamper validation key";

    try
    {
        Directory.CreateDirectory(tempRoot);
        File.WriteAllText(sourcePath, "%PDF-1.7\nRestore tamper validation\n%%EOF");
        ImportOneDocumentForBackup(dbPath, vaultPath, sourcePath, recoveryKey);
        var snapshot = new BackupSnapshotService().CreateSnapshotAsync(
            new BackupSnapshotRequest(vaultPath, dbPath, backupTarget, recoveryKey),
            CancellationToken.None).GetAwaiter().GetResult();
        Assert(snapshot.Succeeded && snapshot.Value is not null, snapshot.Error ?? "Backup snapshot failed.");

        var backedUpVaultObject = Directory.GetFiles(Path.Combine(snapshot.Value!.BackupDirectory, "vault", "objects"), "*.json").Single();
        File.AppendAllText(backedUpVaultObject, "tampered");

        var restore = new RestoreDrillService().RunAsync(
            new RestoreDrillRequest(snapshot.Value.BackupDirectory, restoreTarget, recoveryKey),
            CancellationToken.None).GetAwaiter().GetResult();

        Assert(!restore.Succeeded, "Restore drill should reject tampered backup files.");
        Assert(restore.Error!.Contains("hash mismatch", StringComparison.OrdinalIgnoreCase), "Restore drill should report hash mismatch.");
    }
    finally
    {
        if (Directory.Exists(tempRoot))
        {
            Directory.Delete(tempRoot, recursive: true);
        }
    }
}

static void CloudBackupUploadRequiresEntitlement()
{
    var tempRoot = Path.Combine(Path.GetTempPath(), "WakiliDms.Tests", Guid.NewGuid().ToString("N"));
    var dbPath = Path.Combine(tempRoot, "wakili-dms.db");
    var vaultPath = Path.Combine(tempRoot, "vault");
    var backupTarget = Path.Combine(tempRoot, "backups");
    var cloudRoot = Path.Combine(tempRoot, "cloud");
    var sourcePath = Path.Combine(tempRoot, "Jane Doe private pleading.docx");
    var recoveryKey = "cloud entitlement recovery key";

    try
    {
        Directory.CreateDirectory(tempRoot);
        CreateMinimalDocx(sourcePath, "Cloud entitlement fixture content.");
        ImportOneDocumentForBackup(dbPath, vaultPath, sourcePath, recoveryKey);
        var snapshot = new BackupSnapshotService().CreateSnapshotAsync(
            new BackupSnapshotRequest(vaultPath, dbPath, backupTarget, recoveryKey),
            CancellationToken.None).GetAwaiter().GetResult();
        Assert(snapshot.Succeeded && snapshot.Value is not null, snapshot.Error ?? "Backup snapshot failed.");

        var settings = ValidSettings() with
        {
            InstallationId = Guid.NewGuid(),
            LicenseStatus = LicenseStatus.Active,
            CloudBackupEnabled = false
        };
        var result = new CloudBackupService().UploadSnapshotAsync(
            new CloudBackupUploadRequest(settings, snapshot.Value!.BackupDirectory, recoveryKey),
            new LocalFilesystemCloudBackupProvider(cloudRoot),
            CancellationToken.None).GetAwaiter().GetResult();

        Assert(!result.Succeeded, "Cloud backup upload should require the cloud backup feature to be enabled.");
        Assert(result.Error!.Contains("not enabled", StringComparison.OrdinalIgnoreCase), "Cloud entitlement error should explain the disabled feature.");
        Assert(!Directory.Exists(cloudRoot), "Rejected cloud backup upload must not create provider data.");
    }
    finally
    {
        if (Directory.Exists(tempRoot))
        {
            Directory.Delete(tempRoot, recursive: true);
        }
    }
}

static void CloudBackupUploadStoresEncryptedPackageAndRedactedMetadata()
{
    var tempRoot = Path.Combine(Path.GetTempPath(), "WakiliDms.Tests", Guid.NewGuid().ToString("N"));
    var dbPath = Path.Combine(tempRoot, "wakili-dms.db");
    var vaultPath = Path.Combine(tempRoot, "vault");
    var backupTarget = Path.Combine(tempRoot, "backups");
    var cloudRoot = Path.Combine(tempRoot, "cloud");
    var sourcePath = Path.Combine(tempRoot, "Jane Doe v Acme Ltd plaint.docx");
    var recoveryKey = "cloud redaction recovery key";

    try
    {
        Directory.CreateDirectory(tempRoot);
        CreateMinimalDocx(sourcePath, "Jane Doe confidential pleading text should never appear in cloud package bytes.");
        ImportOneDocumentForBackup(dbPath, vaultPath, sourcePath, recoveryKey);
        var snapshot = new BackupSnapshotService().CreateSnapshotAsync(
            new BackupSnapshotRequest(vaultPath, dbPath, backupTarget, recoveryKey),
            CancellationToken.None).GetAwaiter().GetResult();
        Assert(snapshot.Succeeded && snapshot.Value is not null, snapshot.Error ?? "Backup snapshot failed.");

        var settings = CloudEnabledSettings();
        var service = new CloudBackupService();
        var provider = new LocalFilesystemCloudBackupProvider(cloudRoot);
        var upload = service.UploadSnapshotAsync(
            new CloudBackupUploadRequest(settings, snapshot.Value!.BackupDirectory, recoveryKey),
            provider,
            CancellationToken.None).GetAwaiter().GetResult();

        Assert(upload.Succeeded && upload.Value is not null, upload.Error ?? "Cloud upload failed.");
        var stored = provider.DownloadSnapshotAsync(settings.InstallationId, upload.Value!.Metadata.SnapshotId, CancellationToken.None).GetAwaiter().GetResult();
        Assert(stored.Succeeded && stored.Value is not null, stored.Error ?? "Stored cloud snapshot should download.");

        var metadataJson = JsonSerializer.Serialize(stored.Value!.Metadata, new JsonSerializerOptions(JsonSerializerDefaults.Web));
        var packageText = System.Text.Encoding.UTF8.GetString(stored.Value.EncryptedPackageBytes);
        Assert(metadataJson.Contains(settings.InstallationId.ToString("D"), StringComparison.OrdinalIgnoreCase), "Cloud metadata should include installation ID.");
        Assert(metadataJson.Contains(upload.Value.Metadata.SnapshotId, StringComparison.Ordinal), "Cloud metadata should include snapshot ID.");
        Assert(!metadataJson.Contains("Jane Doe", StringComparison.Ordinal), "Cloud metadata must not include matter names.");
        Assert(!metadataJson.Contains("Acme", StringComparison.Ordinal), "Cloud metadata must not include party names.");
        Assert(!metadataJson.Contains("plaint.docx", StringComparison.Ordinal), "Cloud metadata must not include document filenames.");
        Assert(!packageText.Contains("Jane Doe", StringComparison.Ordinal), "Encrypted cloud package must not expose document text.");
        Assert(!packageText.Contains("plaint.docx", StringComparison.Ordinal), "Encrypted cloud package must not expose document filenames.");

        var listed = provider.ListSnapshotsAsync(settings.InstallationId, CancellationToken.None).GetAwaiter().GetResult();
        Assert(listed.Count == 1, "Provider should list one uploaded cloud snapshot.");
    }
    finally
    {
        if (Directory.Exists(tempRoot))
        {
            Directory.Delete(tempRoot, recursive: true);
        }
    }
}

static void CloudBackupDownloadRestoresSnapshotForRestoreDrill()
{
    var tempRoot = Path.Combine(Path.GetTempPath(), "WakiliDms.Tests", Guid.NewGuid().ToString("N"));
    var dbPath = Path.Combine(tempRoot, "wakili-dms.db");
    var vaultPath = Path.Combine(tempRoot, "vault");
    var backupTarget = Path.Combine(tempRoot, "backups");
    var cloudRoot = Path.Combine(tempRoot, "cloud");
    var cloudRestoreTarget = Path.Combine(tempRoot, "cloud-restore");
    var restoreDrillTarget = Path.Combine(tempRoot, "restore-drill");
    var sourcePath = Path.Combine(tempRoot, "registry notice.pdf");
    var recoveryKey = "cloud restore recovery key";

    try
    {
        Directory.CreateDirectory(tempRoot);
        File.WriteAllText(sourcePath, "%PDF-1.7\nCloud restore registry notice\n%%EOF");
        ImportOneDocumentForBackup(dbPath, vaultPath, sourcePath, recoveryKey);
        var snapshot = new BackupSnapshotService().CreateSnapshotAsync(
            new BackupSnapshotRequest(vaultPath, dbPath, backupTarget, recoveryKey),
            CancellationToken.None).GetAwaiter().GetResult();
        Assert(snapshot.Succeeded && snapshot.Value is not null, snapshot.Error ?? "Backup snapshot failed.");

        var settings = CloudEnabledSettings();
        var service = new CloudBackupService();
        var provider = new LocalFilesystemCloudBackupProvider(cloudRoot);
        var upload = service.UploadSnapshotAsync(
            new CloudBackupUploadRequest(settings, snapshot.Value!.BackupDirectory, recoveryKey),
            provider,
            CancellationToken.None).GetAwaiter().GetResult();
        Assert(upload.Succeeded && upload.Value is not null, upload.Error ?? "Cloud upload failed.");

        var downloaded = service.DownloadSnapshotAsync(
            new CloudBackupDownloadRequest(settings, upload.Value!.Metadata.SnapshotId, recoveryKey, cloudRestoreTarget),
            provider,
            CancellationToken.None).GetAwaiter().GetResult();
        Assert(downloaded.Succeeded && downloaded.Value is not null, downloaded.Error ?? "Cloud download failed.");
        Assert(File.Exists(Path.Combine(cloudRestoreTarget, "backup-manifest.json")), "Cloud restore should extract backup manifest.");
        Assert(File.Exists(Path.Combine(cloudRestoreTarget, "data", "wakili-dms.db.backup")), "Cloud restore should extract encrypted database artifact.");

        var drill = new RestoreDrillService().RunAsync(
            new RestoreDrillRequest(cloudRestoreTarget, restoreDrillTarget, recoveryKey),
            CancellationToken.None).GetAwaiter().GetResult();
        Assert(drill.Succeeded, drill.Error ?? "Restore drill should pass from cloud-downloaded snapshot.");
        Assert(drill.Value!.VerifiedFileCount == snapshot.Value.BackedUpFileCount, "Cloud-downloaded snapshot should verify every backed-up file.");

        var wrongKey = service.DownloadSnapshotAsync(
            new CloudBackupDownloadRequest(settings, upload.Value.Metadata.SnapshotId, "wrong recovery key", Path.Combine(tempRoot, "wrong-key")),
            provider,
            CancellationToken.None).GetAwaiter().GetResult();
        Assert(!wrongKey.Succeeded, "Cloud backup download should reject the wrong recovery key.");
    }
    finally
    {
        if (Directory.Exists(tempRoot))
        {
            Directory.Delete(tempRoot, recursive: true);
        }
    }
}

static void InstallationIdentityIsGeneratedAndPreserved()
{
    var service = new InstallationControlService();
    var now = DateTimeOffset.UtcNow;
    var settings = ValidSettings() with
    {
        DeviceNickname = "  Front Desk PC  ",
        LicenseKey = "WLVD-TRIAL-001"
    };

    var generated = service.EnsureInstallationIdentity(settings, now);
    var preserved = service.EnsureInstallationIdentity(generated, now.AddDays(1));

    Assert(generated.InstallationId != Guid.Empty, "Installation ID should be generated.");
    Assert(generated.DeviceNickname == "Front Desk PC", "Device nickname should be trimmed.");
    Assert(generated.InstallationCreatedAt == now, "Installation creation time should be set.");
    Assert(preserved.InstallationId == generated.InstallationId, "Existing installation ID should be preserved.");
    Assert(preserved.InstallationCreatedAt == now, "Existing installation creation time should be preserved.");
}

static void DisabledInstallationBlocksLicensedFeatureAccessWithoutDeletingData()
{
    var service = new InstallationControlService();
    var active = service.EvaluateLocalAccess(ValidSettings() with { LicenseStatus = LicenseStatus.Active });
    var disabled = service.EvaluateLocalAccess(ValidSettings() with { LicenseStatus = LicenseStatus.Disabled });
    var revoked = service.EvaluateLocalAccess(ValidSettings() with { LicenseStatus = LicenseStatus.Revoked });

    Assert(active.Allowed, "Active installation should pass the local license gate.");
    Assert(!disabled.Allowed, "Disabled installation should fail the local license gate.");
    Assert(disabled.Message.Contains("Local vault data remains", StringComparison.Ordinal), "Disabled message must preserve local vault data.");
    Assert(!revoked.Allowed, "Revoked installation should fail the local license gate.");
    Assert(revoked.Message.Contains("Local vault data remains", StringComparison.Ordinal), "Revoked message must preserve local vault data.");
}

static void InstallationCheckInPayloadExcludesDocumentAndMatterDetails()
{
    var service = new InstallationControlService();
    var settings = service.EnsureInstallationIdentity(ValidSettings() with
    {
        FirmName = "Allowed Firm Display",
        PrimaryUser = "Private Advocate Name",
        VaultPath = @"C:\ClientVaults\Jane Doe v Acme Ltd",
        ScanFolderPath = @"C:\Scans\ELC-100",
        BackupTargetPath = @"D:\Backups\Client Files",
        LicenseKey = "WLVD-TRIAL-002",
        DeviceNickname = "Office PC",
        LicenseStatus = LicenseStatus.Active
    }, DateTimeOffset.UtcNow);

    var payload = service.CreateCheckInPayload(
        settings,
        "1.0.0",
        new BackupHealthSummary(DateTimeOffset.UtcNow, null, "LocalBackupHealthy"),
        DateTimeOffset.UtcNow);
    var json = JsonSerializer.Serialize(payload, new JsonSerializerOptions(JsonSerializerDefaults.Web));

    Assert(json.Contains("Allowed Firm Display", StringComparison.Ordinal), "Payload may include firm display name.");
    Assert(json.Contains(settings.InstallationId.ToString("D"), StringComparison.OrdinalIgnoreCase), "Payload should include installation ID.");
    Assert(!json.Contains("Private Advocate Name", StringComparison.Ordinal), "Payload must not include primary user name.");
    Assert(!json.Contains("Jane Doe", StringComparison.Ordinal), "Payload must not include matter or client names from paths.");
    Assert(!json.Contains("ELC-100", StringComparison.Ordinal), "Payload must not include case numbers from paths.");
    Assert(!json.Contains("Client Files", StringComparison.Ordinal), "Payload must not include backup path details.");
}

static void AdminRegistryCanEnableAndDisableInstallationIds()
{
    var tempRoot = Path.Combine(Path.GetTempPath(), "WakiliDms.Tests", Guid.NewGuid().ToString("N"));
    var registryPath = Path.Combine(tempRoot, "admin-registry.json");

    try
    {
        var registry = new AdminInstallationRegistry(registryPath);
        var installationId = Guid.NewGuid();
        var payload = new InstallationCheckInPayload(
            installationId,
            "WLVD-ACTIVE-001",
            "Example Advocates LLP",
            "Reception PC",
            "1.0.0",
            LicenseStatus.Trial,
            true,
            new FeatureEntitlements(false),
            new BackupHealthSummary(DateTimeOffset.UtcNow, null, "Healthy"),
            DateTimeOffset.UtcNow);

        var upsert = registry.UpsertFromCheckInAsync(payload, "initial install", CancellationToken.None).GetAwaiter().GetResult();
        Assert(upsert.Succeeded && upsert.Value is not null, upsert.Error ?? "Admin registry upsert failed.");
        Assert(upsert.Value!.Created, "First check-in should create an admin registry record.");

        var disabled = registry.DisableAsync(installationId, CancellationToken.None).GetAwaiter().GetResult();
        Assert(disabled.Succeeded && disabled.Value is not null, disabled.Error ?? "Disable failed.");
        Assert(disabled.Value!.LicenseStatus == LicenseStatus.Disabled, "Disable should set status to Disabled.");

        var enabled = registry.EnableAsync(installationId, CancellationToken.None).GetAwaiter().GetResult();
        Assert(enabled.Succeeded && enabled.Value is not null, enabled.Error ?? "Enable failed.");
        Assert(enabled.Value!.LicenseStatus == LicenseStatus.Active, "Enable should set status to Active.");

        var records = registry.ListAsync(CancellationToken.None).GetAwaiter().GetResult();
        Assert(records.Count == 1, "Admin registry should contain one installation.");
        Assert(records[0].SupportNotes == "initial install", "Support notes should persist.");
    }
    finally
    {
        if (Directory.Exists(tempRoot))
        {
            Directory.Delete(tempRoot, recursive: true);
        }
    }
}

static void AdminRegistryDeleteDoesNotTouchVaultData()
{
    var tempRoot = Path.Combine(Path.GetTempPath(), "WakiliDms.Tests", Guid.NewGuid().ToString("N"));
    var registryPath = Path.Combine(tempRoot, "admin-registry.json");
    var vaultPath = Path.Combine(tempRoot, "vault");
    var markerPath = Path.Combine(vaultPath, "vault-marker.txt");

    try
    {
        Directory.CreateDirectory(vaultPath);
        File.WriteAllText(markerPath, "local vault data");
        var registry = new AdminInstallationRegistry(registryPath);
        var installationId = Guid.NewGuid();
        var payload = new InstallationCheckInPayload(
            installationId,
            "WLVD-ACTIVE-002",
            "Example Firm",
            "Office PC",
            "1.0.0",
            LicenseStatus.Active,
            true,
            new FeatureEntitlements(false),
            new BackupHealthSummary(null, null, "Unknown"),
            DateTimeOffset.UtcNow);
        registry.UpsertFromCheckInAsync(payload, string.Empty, CancellationToken.None).GetAwaiter().GetResult();

        var deleted = registry.DeleteAsync(installationId, CancellationToken.None).GetAwaiter().GetResult();

        Assert(deleted.Succeeded, deleted.Error ?? "Registry delete failed.");
        Assert(File.Exists(markerPath), "Deleting an admin registry record must not delete local vault data.");
        Assert(registry.ListAsync(CancellationToken.None).GetAwaiter().GetResult().Count == 0, "Registry record should be deleted.");
    }
    finally
    {
        if (Directory.Exists(tempRoot))
        {
            Directory.Delete(tempRoot, recursive: true);
        }
    }
}

static void ImportOneDocumentForBackup(
    string dbPath,
    string vaultPath,
    string sourcePath,
    string recoveryKey)
{
    var matterRepository = new SqliteMatterRepository(dbPath);
    var documentRepository = new SqliteDocumentRepository(dbPath);
    var documentVersionRepository = new SqliteDocumentVersionRepository(dbPath);
    var vault = new EncryptedVaultService();
    matterRepository.InitializeAsync(CancellationToken.None).GetAwaiter().GetResult();
    documentRepository.InitializeAsync(CancellationToken.None).GetAwaiter().GetResult();
    documentVersionRepository.InitializeAsync(CancellationToken.None).GetAwaiter().GetResult();
    vault.CreateVaultAsync(vaultPath, recoveryKey, CancellationToken.None).GetAwaiter().GetResult();

    var matter = Matter.Create("Backup Test Matter");
    matterRepository.AddAsync(matter, CancellationToken.None).GetAwaiter().GetResult();

    var importService = new DocumentImportService(matterRepository, documentRepository, documentVersionRepository, vault);
    var imported = importService.ImportAsync(
        new DocumentImportRequest(matter.Id, sourcePath, vaultPath, recoveryKey, DocumentType.Affidavit),
        CancellationToken.None).GetAwaiter().GetResult();
    Assert(imported.Succeeded, imported.Error ?? "Backup fixture import failed.");
}

static void CopyDirectory(string sourceDirectory, string destinationDirectory)
{
    Directory.CreateDirectory(destinationDirectory);
    foreach (var directory in Directory.EnumerateDirectories(sourceDirectory, "*", SearchOption.AllDirectories))
    {
        var relativeDirectory = Path.GetRelativePath(sourceDirectory, directory);
        Directory.CreateDirectory(Path.Combine(destinationDirectory, relativeDirectory));
    }

    foreach (var file in Directory.EnumerateFiles(sourceDirectory, "*", SearchOption.AllDirectories))
    {
        var relativeFile = Path.GetRelativePath(sourceDirectory, file);
        var destinationFile = Path.Combine(destinationDirectory, relativeFile);
        var destinationParent = Path.GetDirectoryName(destinationFile);
        if (!string.IsNullOrWhiteSpace(destinationParent))
        {
            Directory.CreateDirectory(destinationParent);
        }

        File.Copy(file, destinationFile, overwrite: false);
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

static AppSettings CloudEnabledSettings()
{
    return ValidSettings() with
    {
        InstallationId = Guid.NewGuid(),
        LicenseKey = "WLVD-CLOUD-TEST",
        LicenseStatus = LicenseStatus.Active,
        CloudBackupEnabled = true
    };
}

static void CreateMinimalDocx(string path, string bodyText)
{
    using var archive = System.IO.Compression.ZipFile.Open(path, System.IO.Compression.ZipArchiveMode.Create);
    var contentTypes = archive.CreateEntry("[Content_Types].xml");
    using (var writer = new StreamWriter(contentTypes.Open()))
    {
        writer.Write("""
            <?xml version="1.0" encoding="UTF-8"?>
            <Types xmlns="http://schemas.openxmlformats.org/package/2006/content-types">
              <Default Extension="rels" ContentType="application/vnd.openxmlformats-package.relationships+xml"/>
              <Default Extension="xml" ContentType="application/xml"/>
              <Override PartName="/word/document.xml" ContentType="application/vnd.openxmlformats-officedocument.wordprocessingml.document.main+xml"/>
            </Types>
            """);
    }

    var document = archive.CreateEntry("word/document.xml");
    using (var writer = new StreamWriter(document.Open()))
    {
        writer.Write($"""
            <?xml version="1.0" encoding="UTF-8" standalone="yes"?>
            <w:document xmlns:w="http://schemas.openxmlformats.org/wordprocessingml/2006/main">
              <w:body>
                <w:p><w:r><w:t>{System.Security.SecurityElement.Escape(bodyText)}</w:t></w:r></w:p>
              </w:body>
            </w:document>
            """);
    }
}

static void Assert(bool condition, string message)
{
    if (!condition)
    {
        throw new InvalidOperationException(message);
    }
}
