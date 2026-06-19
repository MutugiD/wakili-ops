using WakiliDms.Core.Domain;
using WakiliDms.Core.Documents;
using WakiliDms.Core.Filing;
using WakiliDms.Core.Setup;
using WakiliDms.Core.Scan;
using WakiliDms.Core.Search;
using WakiliDms.Infrastructure.Documents;
using WakiliDms.Infrastructure.Matter;
using WakiliDms.Infrastructure.Scan;
using WakiliDms.Infrastructure.Search;
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
    ("Filing pack export writes decrypted copies and manifest", FilingPackExportWritesDecryptedCopiesAndManifest)
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
