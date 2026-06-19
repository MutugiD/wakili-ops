using System.IO;
using System.Windows.Input;
using System.Collections.ObjectModel;
using WakiliDms.Core.Backup;
using WakiliDms.Core.CourtOutput;
using WakiliDms.Core.Documents;
using WakiliDms.Core.Domain;
using WakiliDms.Core.Filing;
using WakiliDms.Core.Matter;
using WakiliDms.Core.Scan;
using WakiliDms.Core.Search;
using WakiliDms.Core.Setup;
using WakiliDms.Core.Vault;

namespace WakiliDms.App.ViewModels;

public sealed class MainWindowViewModel : ObservableObject
{
    private readonly string _databasePath;
    private readonly ISettingsStore _settingsStore;
    private readonly IMatterRepository _matterRepository;
    private readonly IDocumentRepository _documentRepository;
    private readonly IDocumentVersionRepository _documentVersionRepository;
    private readonly IScanInboxRepository _scanInboxRepository;
    private readonly IDocumentSearchRepository _documentSearchRepository;
    private readonly IVaultService _vaultService;
    private readonly DocumentImportService _documentImportService;
    private readonly ScanFolderService _scanFolderService;
    private readonly DocumentIndexingService _documentIndexingService;
    private readonly FilingPackExportService _filingPackExportService;
    private readonly CourtOutputCaptureService _courtOutputCaptureService;
    private readonly BackupSnapshotService _backupSnapshotService;
    private readonly RestoreDrillService _restoreDrillService;
    private string _firmName = string.Empty;
    private string _primaryUser = string.Empty;
    private string _vaultPath = string.Empty;
    private string _scanFolderPath = string.Empty;
    private string _backupTargetPath = string.Empty;
    private string _setupRecoveryKey = string.Empty;
    private string _newMatterName = string.Empty;
    private string _newMatterClientName = string.Empty;
    private string _newMatterCourtCaseNumber = string.Empty;
    private string _importSourceFilePath = string.Empty;
    private string _importRecoveryKey = string.Empty;
    private string _filingPackExportRootPath = string.Empty;
    private string _courtOutputSourceFilePath = string.Empty;
    private string _backupRecoveryKey = string.Empty;
    private string _searchQuery = string.Empty;
    private DocumentType _selectedCourtOutputType = DocumentType.FilingReceipt;
    private MatterListItemViewModel? _selectedMatter;
    private DocumentListItemViewModel? _selectedDocument;
    private DocumentType _selectedDocumentType = DocumentType.Unknown;
    private DocumentStatus _selectedDocumentStatus = DocumentStatus.Imported;
    private ScanInboxItemViewModel? _selectedScan;
    private bool _recoveryKeyConfirmed;
    private bool _isSetupComplete;
    private string _statusMessage = "Complete setup to create a local-first document vault.";

    public MainWindowViewModel(
        string databasePath,
        ISettingsStore settingsStore,
        IMatterRepository matterRepository,
        IDocumentRepository documentRepository,
        IDocumentVersionRepository documentVersionRepository,
        IScanInboxRepository scanInboxRepository,
        IDocumentSearchRepository documentSearchRepository,
        IDocumentTextExtractor documentTextExtractor,
        IVaultService vaultService)
    {
        _databasePath = databasePath;
        _settingsStore = settingsStore;
        _matterRepository = matterRepository;
        _documentRepository = documentRepository;
        _documentVersionRepository = documentVersionRepository;
        _scanInboxRepository = scanInboxRepository;
        _documentSearchRepository = documentSearchRepository;
        _vaultService = vaultService;
        _documentImportService = new DocumentImportService(_matterRepository, _documentRepository, _documentVersionRepository, _vaultService);
        _scanFolderService = new ScanFolderService(_scanInboxRepository);
        _documentIndexingService = new DocumentIndexingService(_documentRepository, _vaultService, documentTextExtractor, _documentSearchRepository);
        _filingPackExportService = new FilingPackExportService(_matterRepository, _documentRepository, _vaultService);
        _courtOutputCaptureService = new CourtOutputCaptureService(_documentImportService);
        _backupSnapshotService = new BackupSnapshotService();
        _restoreDrillService = new RestoreDrillService();
        CompleteSetupCommand = new AsyncRelayCommand(CompleteSetupAsync);
        CreateMatterCommand = new AsyncRelayCommand(CreateMatterAsync, () => IsSetupComplete);
        ImportDocumentCommand = new AsyncRelayCommand(ImportDocumentAsync, () => IsSetupComplete && SelectedMatter is not null);
        RefreshScanFolderCommand = new AsyncRelayCommand(RefreshScanFolderAsync, () => IsSetupComplete);
        ImportSelectedScanCommand = new AsyncRelayCommand(ImportSelectedScanAsync, () => IsSetupComplete && SelectedMatter is not null && SelectedScan is not null);
        UpdateDocumentClassificationCommand = new AsyncRelayCommand(UpdateDocumentClassificationAsync, () => IsSetupComplete && SelectedDocument is not null);
        IndexSelectedDocumentCommand = new AsyncRelayCommand(IndexSelectedDocumentAsync, () => IsSetupComplete && SelectedDocument is not null);
        SearchMatterCommand = new AsyncRelayCommand(SearchMatterAsync, () => IsSetupComplete && SelectedMatter is not null);
        ExportFilingPackCommand = new AsyncRelayCommand(ExportFilingPackAsync, () => IsSetupComplete && SelectedMatter is not null && Documents.Count > 0);
        CaptureCourtOutputCommand = new AsyncRelayCommand(CaptureCourtOutputAsync, () => IsSetupComplete && SelectedMatter is not null);
        RunBackupCommand = new AsyncRelayCommand(RunBackupAsync, () => IsSetupComplete);
    }

    public string Title { get; } = "Windows Legal Document Vault";

    public string Subtitle { get; } = "Local-first legal document management for Kenyan advocates and small firms.";

    public string FirmName
    {
        get => _firmName;
        set => SetProperty(ref _firmName, value);
    }

    public string PrimaryUser
    {
        get => _primaryUser;
        set => SetProperty(ref _primaryUser, value);
    }

    public string VaultPath
    {
        get => _vaultPath;
        set => SetProperty(ref _vaultPath, value);
    }

    public string ScanFolderPath
    {
        get => _scanFolderPath;
        set => SetProperty(ref _scanFolderPath, value);
    }

    public string BackupTargetPath
    {
        get => _backupTargetPath;
        set => SetProperty(ref _backupTargetPath, value);
    }

    public string SetupRecoveryKey
    {
        get => _setupRecoveryKey;
        set => SetProperty(ref _setupRecoveryKey, value);
    }

    public string NewMatterName
    {
        get => _newMatterName;
        set => SetProperty(ref _newMatterName, value);
    }

    public string NewMatterClientName
    {
        get => _newMatterClientName;
        set => SetProperty(ref _newMatterClientName, value);
    }

    public string NewMatterCourtCaseNumber
    {
        get => _newMatterCourtCaseNumber;
        set => SetProperty(ref _newMatterCourtCaseNumber, value);
    }

    public string ImportSourceFilePath
    {
        get => _importSourceFilePath;
        set => SetProperty(ref _importSourceFilePath, value);
    }

    public string ImportRecoveryKey
    {
        get => _importRecoveryKey;
        set => SetProperty(ref _importRecoveryKey, value);
    }

    public string SearchQuery
    {
        get => _searchQuery;
        set => SetProperty(ref _searchQuery, value);
    }

    public string FilingPackExportRootPath
    {
        get => _filingPackExportRootPath;
        set => SetProperty(ref _filingPackExportRootPath, value);
    }

    public string CourtOutputSourceFilePath
    {
        get => _courtOutputSourceFilePath;
        set => SetProperty(ref _courtOutputSourceFilePath, value);
    }

    public DocumentType SelectedCourtOutputType
    {
        get => _selectedCourtOutputType;
        set => SetProperty(ref _selectedCourtOutputType, value);
    }

    public string BackupRecoveryKey
    {
        get => _backupRecoveryKey;
        set => SetProperty(ref _backupRecoveryKey, value);
    }

    public MatterListItemViewModel? SelectedMatter
    {
        get => _selectedMatter;
        set
        {
            if (SetProperty(ref _selectedMatter, value))
            {
                (ImportDocumentCommand as AsyncRelayCommand)?.RaiseCanExecuteChanged();
                (ImportSelectedScanCommand as AsyncRelayCommand)?.RaiseCanExecuteChanged();
                _ = ReloadDocumentsForSelectionAsync();
            }
        }
    }

    public ScanInboxItemViewModel? SelectedScan
    {
        get => _selectedScan;
        set
        {
            if (SetProperty(ref _selectedScan, value))
            {
                (ImportSelectedScanCommand as AsyncRelayCommand)?.RaiseCanExecuteChanged();
            }
        }
    }

    public DocumentListItemViewModel? SelectedDocument
    {
        get => _selectedDocument;
        set
        {
            if (SetProperty(ref _selectedDocument, value))
            {
                if (value is not null)
                {
                    SelectedDocumentType = value.RawDocumentType;
                    SelectedDocumentStatus = value.RawStatus;
                }

                (UpdateDocumentClassificationCommand as AsyncRelayCommand)?.RaiseCanExecuteChanged();
                (IndexSelectedDocumentCommand as AsyncRelayCommand)?.RaiseCanExecuteChanged();
                _ = ReloadVersionsForSelectionAsync();
            }
        }
    }

    public DocumentType SelectedDocumentType
    {
        get => _selectedDocumentType;
        set => SetProperty(ref _selectedDocumentType, value);
    }

    public DocumentStatus SelectedDocumentStatus
    {
        get => _selectedDocumentStatus;
        set => SetProperty(ref _selectedDocumentStatus, value);
    }

    public bool RecoveryKeyConfirmed
    {
        get => _recoveryKeyConfirmed;
        set => SetProperty(ref _recoveryKeyConfirmed, value);
    }

    public bool IsSetupComplete
    {
        get => _isSetupComplete;
        private set
        {
            if (SetProperty(ref _isSetupComplete, value))
            {
                OnPropertyChanged(nameof(IsSetupRequired));
                (CreateMatterCommand as AsyncRelayCommand)?.RaiseCanExecuteChanged();
                (ImportDocumentCommand as AsyncRelayCommand)?.RaiseCanExecuteChanged();
                (RefreshScanFolderCommand as AsyncRelayCommand)?.RaiseCanExecuteChanged();
                (ImportSelectedScanCommand as AsyncRelayCommand)?.RaiseCanExecuteChanged();
                (UpdateDocumentClassificationCommand as AsyncRelayCommand)?.RaiseCanExecuteChanged();
                (IndexSelectedDocumentCommand as AsyncRelayCommand)?.RaiseCanExecuteChanged();
                (SearchMatterCommand as AsyncRelayCommand)?.RaiseCanExecuteChanged();
                (ExportFilingPackCommand as AsyncRelayCommand)?.RaiseCanExecuteChanged();
                (CaptureCourtOutputCommand as AsyncRelayCommand)?.RaiseCanExecuteChanged();
                (RunBackupCommand as AsyncRelayCommand)?.RaiseCanExecuteChanged();
            }
        }
    }

    public bool IsSetupRequired => !IsSetupComplete;

    public string StatusMessage
    {
        get => _statusMessage;
        private set => SetProperty(ref _statusMessage, value);
    }

    public ICommand CompleteSetupCommand { get; }

    public ICommand CreateMatterCommand { get; }

    public ICommand ImportDocumentCommand { get; }

    public ICommand RefreshScanFolderCommand { get; }

    public ICommand ImportSelectedScanCommand { get; }

    public ICommand UpdateDocumentClassificationCommand { get; }

    public ICommand IndexSelectedDocumentCommand { get; }

    public ICommand SearchMatterCommand { get; }

    public ICommand ExportFilingPackCommand { get; }

    public ICommand CaptureCourtOutputCommand { get; }

    public ICommand RunBackupCommand { get; }

    public ObservableCollection<MatterListItemViewModel> Matters { get; } = [];

    public ObservableCollection<DocumentListItemViewModel> Documents { get; } = [];

    public ObservableCollection<DocumentVersionListItemViewModel> DocumentVersions { get; } = [];

    public ObservableCollection<ScanInboxItemViewModel> ScanInbox { get; } = [];

    public ObservableCollection<DocumentSearchResultViewModel> SearchResults { get; } = [];

    public IReadOnlyList<DocumentType> DocumentTypeOptions { get; } = Enum.GetValues<DocumentType>();

    public IReadOnlyList<DocumentStatus> DocumentStatusOptions { get; } = Enum.GetValues<DocumentStatus>();

    public IReadOnlyList<DocumentType> CourtOutputTypeOptions { get; } =
    [
        DocumentType.FilingReceipt,
        DocumentType.PaymentReceipt,
        DocumentType.CourtOrder,
        DocumentType.Ruling,
        DocumentType.Judgment,
        DocumentType.Notice
    ];

    public IReadOnlyList<string> NextModules { get; } =
    [
        "Setup wizard",
        "Encrypted vault",
        "Matter management",
        "Document import",
        "Scan inbox",
        "Classification and versioning",
        "OCR and search",
        "Filing-pack builder",
        "Receipt and court-output capture",
        "Backup and restore drill"
    ];

    public async Task LoadAsync()
    {
        await _matterRepository.InitializeAsync(CancellationToken.None);
        await _documentRepository.InitializeAsync(CancellationToken.None);
        await _documentVersionRepository.InitializeAsync(CancellationToken.None);
        await _scanInboxRepository.InitializeAsync(CancellationToken.None);
        await _documentSearchRepository.InitializeAsync(CancellationToken.None);

        var settings = await _settingsStore.LoadAsync(CancellationToken.None);
        if (settings is null || settings.SetupCompletedAt is null)
        {
            IsSetupComplete = false;
            return;
        }

        FirmName = settings.FirmName;
        PrimaryUser = settings.PrimaryUser;
        VaultPath = settings.VaultPath;
        ScanFolderPath = settings.ScanFolderPath;
        BackupTargetPath = settings.BackupTargetPath;
        FilingPackExportRootPath = settings.BackupTargetPath;
        RecoveryKeyConfirmed = settings.RecoveryKeyConfirmed;
        StatusMessage = $"Vault setup complete for {settings.FirmName}.";
        IsSetupComplete = true;
        await ReloadMattersAsync();
        await ReloadScanInboxAsync();
    }

    public async Task CompleteSetupAsync()
    {
        var settings = new AppSettings
        {
            FirmName = FirmName,
            PrimaryUser = PrimaryUser,
            VaultPath = VaultPath,
            ScanFolderPath = ScanFolderPath,
            BackupTargetPath = BackupTargetPath,
            RecoveryKeyConfirmed = RecoveryKeyConfirmed,
            CloudBackupEnabled = false,
            SetupCompletedAt = DateTimeOffset.UtcNow
        };

        var validation = SetupValidator.Validate(settings);
        if (!validation.Succeeded)
        {
            StatusMessage = validation.Error ?? "Setup details are invalid.";
            return;
        }

        if (string.IsNullOrWhiteSpace(SetupRecoveryKey))
        {
            StatusMessage = "Recovery key is required to create the encrypted vault.";
            return;
        }

        var vault = await _vaultService.CreateVaultAsync(settings.VaultPath, SetupRecoveryKey, CancellationToken.None);
        if (!vault.Succeeded)
        {
            StatusMessage = vault.Error ?? "Encrypted vault could not be created.";
            return;
        }

        await _settingsStore.SaveAsync(settings, CancellationToken.None);
        SetupRecoveryKey = string.Empty;
        StatusMessage = $"Vault setup complete for {settings.FirmName}.";
        IsSetupComplete = true;
        await ReloadMattersAsync();
        await ReloadScanInboxAsync();
    }

    public async Task CreateMatterAsync()
    {
        if (!IsSetupComplete)
        {
            StatusMessage = "Complete setup before creating matters.";
            return;
        }

        try
        {
            var matter = Matter.Create(
                NewMatterName,
                courtCaseNumber: NewMatterCourtCaseNumber,
                clientName: NewMatterClientName);

            await _matterRepository.AddAsync(matter, CancellationToken.None);
            NewMatterName = string.Empty;
            NewMatterClientName = string.Empty;
            NewMatterCourtCaseNumber = string.Empty;
            StatusMessage = $"Matter created: {matter.Name}.";
            await ReloadMattersAsync();
            SelectedMatter = Matters.FirstOrDefault(candidate => candidate.Id == matter.Id);
        }
        catch (ArgumentException ex)
        {
            StatusMessage = ex.Message;
        }
    }

    public async Task ImportDocumentAsync()
    {
        if (SelectedMatter is null)
        {
            StatusMessage = "Select a matter before importing a document.";
            return;
        }

        var request = new DocumentImportRequest(
            SelectedMatter.Id,
            ImportSourceFilePath,
            VaultPath,
            ImportRecoveryKey);

        var result = await _documentImportService.ImportAsync(request, CancellationToken.None);
        if (!result.Succeeded || result.Value is null)
        {
            StatusMessage = result.Error ?? "Document import failed.";
            return;
        }

        StatusMessage = $"Imported {result.Value.OriginalFileName} into {SelectedMatter.Name}.";
        ImportSourceFilePath = string.Empty;
        await ReloadDocumentsForSelectionAsync();
    }

    public async Task RefreshScanFolderAsync()
    {
        if (!IsSetupComplete)
        {
            StatusMessage = "Complete setup before scanning the watched folder.";
            return;
        }

        var result = await _scanFolderService.ScanOnceAsync(ScanFolderPath, CancellationToken.None);
        if (!result.Succeeded || result.Value is null)
        {
            StatusMessage = result.Error ?? "Scan folder refresh failed.";
            return;
        }

        StatusMessage = $"Scan folder refreshed: {result.Value.QueuedCount} queued, {result.Value.DuplicateCount} duplicates, {result.Value.IgnoredCount} ignored.";
        await ReloadScanInboxAsync();
    }

    public async Task ImportSelectedScanAsync()
    {
        if (SelectedMatter is null)
        {
            StatusMessage = "Select a matter before importing a pending scan.";
            return;
        }

        if (SelectedScan is null)
        {
            StatusMessage = "Select a pending scan before importing.";
            return;
        }

        var request = new DocumentImportRequest(
            SelectedMatter.Id,
            SelectedScan.SourcePath,
            VaultPath,
            ImportRecoveryKey);

        var result = await _documentImportService.ImportAsync(request, CancellationToken.None);
        if (!result.Succeeded || result.Value is null)
        {
            StatusMessage = result.Error ?? "Pending scan import failed.";
            return;
        }

        await _scanInboxRepository.MarkImportedAsync(
            SelectedScan.Id,
            result.Value.Id,
            DateTimeOffset.UtcNow,
            CancellationToken.None);

        StatusMessage = $"Imported scan {result.Value.OriginalFileName} into {SelectedMatter.Name}.";
        await ReloadScanInboxAsync();
        await ReloadDocumentsForSelectionAsync();
    }

    public async Task UpdateDocumentClassificationAsync()
    {
        if (SelectedDocument is null)
        {
            StatusMessage = "Select a document before updating classification.";
            return;
        }

        var document = await _documentRepository.GetAsync(SelectedDocument.Id, CancellationToken.None);
        if (document is null)
        {
            StatusMessage = "Selected document was not found.";
            return;
        }

        try
        {
            var updated = document.WithClassification(SelectedDocumentType, SelectedDocumentStatus);
            await _documentRepository.UpdateClassificationAsync(updated, CancellationToken.None);
            StatusMessage = $"Updated {updated.OriginalFileName} to {updated.DocumentType} / {updated.Status}.";
            await ReloadDocumentsForSelectionAsync(updated.Id);
        }
        catch (InvalidOperationException ex)
        {
            StatusMessage = ex.Message;
        }
    }

    public async Task IndexSelectedDocumentAsync()
    {
        if (SelectedDocument is null)
        {
            StatusMessage = "Select a document before indexing text.";
            return;
        }

        var result = await _documentIndexingService.IndexDocumentAsync(
            SelectedDocument.Id,
            VaultPath,
            ImportRecoveryKey,
            CancellationToken.None);
        if (!result.Succeeded)
        {
            StatusMessage = result.Error ?? "Document text indexing failed.";
            return;
        }

        StatusMessage = $"Indexed {result.Value:N0} searchable characters from {SelectedDocument.OriginalFileName}.";
    }

    public async Task SearchMatterAsync()
    {
        SearchResults.Clear();
        if (SelectedMatter is null)
        {
            StatusMessage = "Select a matter before searching.";
            return;
        }

        var results = await _documentSearchRepository.SearchAsync(
            SelectedMatter.Id,
            SearchQuery,
            CancellationToken.None);
        foreach (var result in results)
        {
            SearchResults.Add(new DocumentSearchResultViewModel(result));
        }

        StatusMessage = $"Search returned {results.Count:N0} result(s).";
    }

    public async Task ExportFilingPackAsync()
    {
        if (SelectedMatter is null)
        {
            StatusMessage = "Select a matter before exporting a filing pack.";
            return;
        }

        var documentIds = Documents.Select(document => document.Id).ToList();
        var result = await _filingPackExportService.ExportAsync(
            new FilingPackExportRequest(
                SelectedMatter.Id,
                documentIds,
                VaultPath,
                ImportRecoveryKey,
                FilingPackExportRootPath),
            CancellationToken.None);

        if (!result.Succeeded || result.Value is null)
        {
            StatusMessage = result.Error ?? "Filing pack export failed.";
            return;
        }

        StatusMessage = $"Filing pack exported {result.Value.ExportedDocumentCount:N0} document(s) to {result.Value.ExportDirectory}.";
    }

    public async Task CaptureCourtOutputAsync()
    {
        if (SelectedMatter is null)
        {
            StatusMessage = "Select a matter before capturing a receipt or court output.";
            return;
        }

        var result = await _courtOutputCaptureService.CaptureAsync(
            new CourtOutputCaptureRequest(
                SelectedMatter.Id,
                CourtOutputSourceFilePath,
                VaultPath,
                ImportRecoveryKey,
                SelectedCourtOutputType),
            CancellationToken.None);

        if (!result.Succeeded || result.Value is null)
        {
            StatusMessage = result.Error ?? "Receipt or court-output capture failed.";
            return;
        }

        CourtOutputSourceFilePath = string.Empty;
        StatusMessage = $"Captured {result.Value.DocumentType}: {result.Value.OriginalFileName}.";
        await ReloadDocumentsForSelectionAsync(result.Value.Id);
    }

    public async Task RunBackupAsync()
    {
        if (!IsSetupComplete)
        {
            StatusMessage = "Complete setup before running a backup.";
            return;
        }

        var snapshot = await _backupSnapshotService.CreateSnapshotAsync(
            new BackupSnapshotRequest(VaultPath, _databasePath, BackupTargetPath, BackupRecoveryKey),
            CancellationToken.None);
        if (!snapshot.Succeeded || snapshot.Value is null)
        {
            StatusMessage = snapshot.Error ?? "Backup snapshot failed.";
            return;
        }

        var restoreTarget = Path.Combine(snapshot.Value.BackupDirectory, "restore-drill");
        var drill = await _restoreDrillService.RunAsync(
            new RestoreDrillRequest(snapshot.Value.BackupDirectory, restoreTarget, BackupRecoveryKey),
            CancellationToken.None);
        if (!drill.Succeeded || drill.Value is null)
        {
            StatusMessage = drill.Error ?? "Restore drill failed.";
            return;
        }

        StatusMessage = $"Backup created and restore drill verified {drill.Value.VerifiedFileCount:N0} file(s) at {snapshot.Value.BackupDirectory}.";
        BackupRecoveryKey = string.Empty;
    }


    private async Task ReloadMattersAsync()
    {
        var selectedMatterId = SelectedMatter?.Id;
        Matters.Clear();
        var matters = await _matterRepository.ListAsync(CancellationToken.None);
        foreach (var matter in matters)
        {
            Matters.Add(new MatterListItemViewModel(matter));
        }

        if (selectedMatterId is not null)
        {
            SelectedMatter = Matters.FirstOrDefault(matter => matter.Id == selectedMatterId.Value);
        }
    }

    private async Task ReloadDocumentsForSelectionAsync(Guid? selectedDocumentId = null)
    {
        var previousSelectedDocumentId = selectedDocumentId ?? SelectedDocument?.Id;
        Documents.Clear();
        DocumentVersions.Clear();
        if (SelectedMatter is null)
        {
            return;
        }

        var documents = await _documentRepository.ListByMatterAsync(SelectedMatter.Id, CancellationToken.None);
        foreach (var document in documents)
        {
            Documents.Add(new DocumentListItemViewModel(document));
        }

        (ExportFilingPackCommand as AsyncRelayCommand)?.RaiseCanExecuteChanged();

        if (previousSelectedDocumentId is not null)
        {
            SelectedDocument = Documents.FirstOrDefault(document => document.Id == previousSelectedDocumentId.Value);
        }
    }

    private async Task ReloadVersionsForSelectionAsync()
    {
        DocumentVersions.Clear();
        if (SelectedDocument is null)
        {
            return;
        }

        var versions = await _documentVersionRepository.ListByDocumentAsync(SelectedDocument.Id, CancellationToken.None);
        foreach (var version in versions)
        {
            DocumentVersions.Add(new DocumentVersionListItemViewModel(version));
        }
    }

    private async Task ReloadScanInboxAsync()
    {
        var selectedScanId = SelectedScan?.Id;
        ScanInbox.Clear();
        var items = await _scanInboxRepository.ListPendingAsync(CancellationToken.None);
        foreach (var item in items)
        {
            ScanInbox.Add(new ScanInboxItemViewModel(item));
        }

        if (selectedScanId is not null)
        {
            SelectedScan = ScanInbox.FirstOrDefault(item => item.Id == selectedScanId.Value);
        }
    }
}
