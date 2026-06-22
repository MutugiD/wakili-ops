using System.IO;
using System.Windows.Input;
using System.Collections.ObjectModel;
using WakiliDms.Core.Backup;
using WakiliDms.Core.CloudBackup;
using WakiliDms.Core.CourtOutput;
using WakiliDms.Core.Documents;
using WakiliDms.Core.Domain;
using WakiliDms.Core.Filing;
using WakiliDms.Core.Licensing;
using WakiliDms.Core.Matter;
using WakiliDms.Core.Scan;
using WakiliDms.Core.Search;
using WakiliDms.Core.Setup;
using WakiliDms.Core.Vault;
using WakiliDms.Infrastructure.CloudBackup;

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
    private readonly LocalBackupCatalogService _localBackupCatalogService;
    private readonly RestoreDrillService _restoreDrillService;
    private readonly RestoreVerificationReportService _restoreVerificationReportService;
    private readonly CloudBackupService _cloudBackupService;
    private readonly InstallationControlService _installationControlService;
    private AppSettings? _currentSettings;
    private string _firmName = string.Empty;
    private string _primaryUser = string.Empty;
    private string _licenseKey = string.Empty;
    private string _deviceNickname = string.Empty;
    private string _installationId = string.Empty;
    private LicenseStatus _licenseStatus = LicenseStatus.Trial;
    private bool _installationEnabled = true;
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
    private string _localRestoreTargetPath = string.Empty;
    private string _externalBackupDirectoryPath = string.Empty;
    private string _externalRestoreTargetPath = string.Empty;
    private string _cloudBackupProviderPath = string.Empty;
    private string _cloudRestoreTargetPath = string.Empty;
    private string _searchQuery = string.Empty;
    private DocumentType _selectedCourtOutputType = DocumentType.FilingReceipt;
    private MatterListItemViewModel? _selectedMatter;
    private DocumentListItemViewModel? _selectedDocument;
    private DocumentType _selectedDocumentType = DocumentType.Unknown;
    private DocumentStatus _selectedDocumentStatus = DocumentStatus.Imported;
    private ScanInboxItemViewModel? _selectedScan;
    private LocalBackupSnapshotViewModel? _selectedLocalBackupSnapshot;
    private CloudBackupSnapshotViewModel? _selectedCloudBackupSnapshot;
    private bool _recoveryKeyConfirmed;
    private bool _cloudBackupEnabled;
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
        _localBackupCatalogService = new LocalBackupCatalogService();
        _restoreDrillService = new RestoreDrillService();
        _restoreVerificationReportService = new RestoreVerificationReportService();
        _cloudBackupService = new CloudBackupService();
        _installationControlService = new InstallationControlService();
        CompleteSetupCommand = new AsyncRelayCommand(CompleteSetupAsync);
        CreateMatterCommand = new AsyncRelayCommand(CreateMatterAsync, () => CanUseInstall);
        ImportDocumentCommand = new AsyncRelayCommand(ImportDocumentAsync, () => CanUseInstall && SelectedMatter is not null);
        RefreshScanFolderCommand = new AsyncRelayCommand(RefreshScanFolderAsync, () => CanUseInstall);
        ImportSelectedScanCommand = new AsyncRelayCommand(ImportSelectedScanAsync, () => CanUseInstall && SelectedMatter is not null && SelectedScan is not null);
        UpdateDocumentClassificationCommand = new AsyncRelayCommand(UpdateDocumentClassificationAsync, () => CanUseInstall && SelectedDocument is not null);
        IndexSelectedDocumentCommand = new AsyncRelayCommand(IndexSelectedDocumentAsync, () => CanUseInstall && SelectedDocument is not null);
        SearchMatterCommand = new AsyncRelayCommand(SearchMatterAsync, () => CanUseInstall && SelectedMatter is not null);
        ExportFilingPackCommand = new AsyncRelayCommand(ExportFilingPackAsync, () => CanUseInstall && SelectedMatter is not null && Documents.Count > 0);
        CaptureCourtOutputCommand = new AsyncRelayCommand(CaptureCourtOutputAsync, () => CanUseInstall && SelectedMatter is not null);
        RunBackupCommand = new AsyncRelayCommand(RunBackupAsync, () => CanUseInstall);
        RefreshLocalBackupsCommand = new AsyncRelayCommand(RefreshLocalBackupsAsync, () => CanUseInstall);
        RestoreSelectedLocalBackupCommand = new AsyncRelayCommand(RestoreSelectedLocalBackupAsync, () => CanUseInstall && SelectedLocalBackupSnapshot is not null);
        VerifyExternalBackupCommand = new AsyncRelayCommand(VerifyExternalBackupAsync, () => CanUseInstall);
        EnableCloudBackupCommand = new AsyncRelayCommand(EnableCloudBackupAsync, () => CanUseInstall);
        UploadCloudBackupCommand = new AsyncRelayCommand(UploadCloudBackupAsync, () => CanUseInstall && CloudBackupEnabled);
        RefreshCloudBackupsCommand = new AsyncRelayCommand(RefreshCloudBackupsAsync, () => CanUseInstall && CloudBackupEnabled);
        RestoreSelectedCloudBackupCommand = new AsyncRelayCommand(RestoreSelectedCloudBackupAsync, () => CanUseInstall && CloudBackupEnabled && SelectedCloudBackupSnapshot is not null);
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

    public string LicenseKey
    {
        get => _licenseKey;
        set => SetProperty(ref _licenseKey, value);
    }

    public string DeviceNickname
    {
        get => _deviceNickname;
        set => SetProperty(ref _deviceNickname, value);
    }

    public string InstallationId
    {
        get => _installationId;
        private set => SetProperty(ref _installationId, value);
    }

    public string LicenseStatusText => $"License status: {LicenseStatus}";

    public LicenseStatus LicenseStatus
    {
        get => _licenseStatus;
        private set
        {
            if (SetProperty(ref _licenseStatus, value))
            {
                OnPropertyChanged(nameof(LicenseStatusText));
            }
        }
    }

    public bool InstallationEnabled
    {
        get => _installationEnabled;
        private set
        {
            if (SetProperty(ref _installationEnabled, value))
            {
                OnPropertyChanged(nameof(CanUseInstall));
                RaiseLicensedCommandStates();
            }
        }
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

    public string LocalRestoreTargetPath
    {
        get => _localRestoreTargetPath;
        set => SetProperty(ref _localRestoreTargetPath, value);
    }

    public string ExternalBackupDirectoryPath
    {
        get => _externalBackupDirectoryPath;
        set => SetProperty(ref _externalBackupDirectoryPath, value);
    }

    public string ExternalRestoreTargetPath
    {
        get => _externalRestoreTargetPath;
        set => SetProperty(ref _externalRestoreTargetPath, value);
    }

    public string CloudBackupProviderPath
    {
        get => _cloudBackupProviderPath;
        set => SetProperty(ref _cloudBackupProviderPath, value);
    }

    public string CloudRestoreTargetPath
    {
        get => _cloudRestoreTargetPath;
        set => SetProperty(ref _cloudRestoreTargetPath, value);
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
                (SearchMatterCommand as AsyncRelayCommand)?.RaiseCanExecuteChanged();
                (ExportFilingPackCommand as AsyncRelayCommand)?.RaiseCanExecuteChanged();
                (CaptureCourtOutputCommand as AsyncRelayCommand)?.RaiseCanExecuteChanged();
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

    public LocalBackupSnapshotViewModel? SelectedLocalBackupSnapshot
    {
        get => _selectedLocalBackupSnapshot;
        set
        {
            if (SetProperty(ref _selectedLocalBackupSnapshot, value))
            {
                (RestoreSelectedLocalBackupCommand as AsyncRelayCommand)?.RaiseCanExecuteChanged();
            }
        }
    }

    public CloudBackupSnapshotViewModel? SelectedCloudBackupSnapshot
    {
        get => _selectedCloudBackupSnapshot;
        set
        {
            if (SetProperty(ref _selectedCloudBackupSnapshot, value))
            {
                (RestoreSelectedCloudBackupCommand as AsyncRelayCommand)?.RaiseCanExecuteChanged();
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

    public bool CloudBackupEnabled
    {
        get => _cloudBackupEnabled;
        private set
        {
            if (SetProperty(ref _cloudBackupEnabled, value))
            {
                OnPropertyChanged(nameof(CloudBackupStatusText));
                (UploadCloudBackupCommand as AsyncRelayCommand)?.RaiseCanExecuteChanged();
                (RefreshCloudBackupsCommand as AsyncRelayCommand)?.RaiseCanExecuteChanged();
                (RestoreSelectedCloudBackupCommand as AsyncRelayCommand)?.RaiseCanExecuteChanged();
            }
        }
    }

    public string CloudBackupStatusText => CloudBackupEnabled
        ? "Cloud backup: enabled for local provider testing"
        : "Cloud backup: disabled";

    public bool IsSetupComplete
    {
        get => _isSetupComplete;
        private set
        {
            if (SetProperty(ref _isSetupComplete, value))
            {
                OnPropertyChanged(nameof(IsSetupRequired));
                OnPropertyChanged(nameof(CanUseInstall));
                RaiseLicensedCommandStates();
            }
        }
    }

    public bool IsSetupRequired => !IsSetupComplete;

    public bool CanUseInstall => IsSetupComplete && InstallationEnabled;

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

    public ICommand RefreshLocalBackupsCommand { get; }

    public ICommand RestoreSelectedLocalBackupCommand { get; }

    public ICommand VerifyExternalBackupCommand { get; }

    public ICommand EnableCloudBackupCommand { get; }

    public ICommand UploadCloudBackupCommand { get; }

    public ICommand RefreshCloudBackupsCommand { get; }

    public ICommand RestoreSelectedCloudBackupCommand { get; }

    public ObservableCollection<MatterListItemViewModel> Matters { get; } = [];

    public ObservableCollection<DocumentListItemViewModel> Documents { get; } = [];

    public ObservableCollection<DocumentVersionListItemViewModel> DocumentVersions { get; } = [];

    public ObservableCollection<ScanInboxItemViewModel> ScanInbox { get; } = [];

    public ObservableCollection<DocumentSearchResultViewModel> SearchResults { get; } = [];

    public ObservableCollection<LocalBackupSnapshotViewModel> LocalBackupSnapshots { get; } = [];

    public ObservableCollection<CloudBackupSnapshotViewModel> CloudBackupSnapshots { get; } = [];

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

        var ensuredSettings = _installationControlService.EnsureInstallationIdentity(settings, DateTimeOffset.UtcNow);
        if (ensuredSettings != settings)
        {
            await _settingsStore.SaveAsync(ensuredSettings, CancellationToken.None);
        }

        _currentSettings = ensuredSettings;
        FirmName = ensuredSettings.FirmName;
        LicenseKey = ensuredSettings.LicenseKey;
        DeviceNickname = ensuredSettings.DeviceNickname;
        InstallationId = ensuredSettings.InstallationId.ToString("D");
        LicenseStatus = ensuredSettings.LicenseStatus;
        PrimaryUser = settings.PrimaryUser;
        VaultPath = ensuredSettings.VaultPath;
        ScanFolderPath = ensuredSettings.ScanFolderPath;
        BackupTargetPath = ensuredSettings.BackupTargetPath;
        FilingPackExportRootPath = ensuredSettings.BackupTargetPath;
        LocalRestoreTargetPath = Path.Combine(ensuredSettings.BackupTargetPath, "local-restore-workspace");
        ExternalRestoreTargetPath = Path.Combine(ensuredSettings.BackupTargetPath, "external-restore-workspace");
        CloudBackupProviderPath = DefaultCloudBackupProviderPath(ensuredSettings);
        CloudRestoreTargetPath = Path.Combine(ensuredSettings.BackupTargetPath, "cloud-restore");
        CloudBackupEnabled = ensuredSettings.CloudBackupEnabled;
        RecoveryKeyConfirmed = ensuredSettings.RecoveryKeyConfirmed;
        ApplyLicenseGate(ensuredSettings);
        IsSetupComplete = true;
        await ReloadMattersAsync();
        await ReloadScanInboxAsync();
        await ReloadLocalBackupsAsync();
        await ReloadCloudBackupsAsync();
    }

    public async Task CompleteSetupAsync()
    {
        var settings = _installationControlService.EnsureInstallationIdentity(new AppSettings
        {
            FirmName = FirmName,
            PrimaryUser = PrimaryUser,
            LicenseKey = LicenseKey,
            DeviceNickname = DeviceNickname,
            VaultPath = VaultPath,
            ScanFolderPath = ScanFolderPath,
            BackupTargetPath = BackupTargetPath,
            RecoveryKeyConfirmed = RecoveryKeyConfirmed,
            CloudBackupEnabled = false,
            SetupCompletedAt = DateTimeOffset.UtcNow
        }, DateTimeOffset.UtcNow);

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
        _currentSettings = settings;
        SetupRecoveryKey = string.Empty;
        InstallationId = settings.InstallationId.ToString("D");
        DeviceNickname = settings.DeviceNickname;
        LicenseStatus = settings.LicenseStatus;
        LocalRestoreTargetPath = Path.Combine(settings.BackupTargetPath, "local-restore-workspace");
        ExternalRestoreTargetPath = Path.Combine(settings.BackupTargetPath, "external-restore-workspace");
        CloudBackupProviderPath = DefaultCloudBackupProviderPath(settings);
        CloudRestoreTargetPath = Path.Combine(settings.BackupTargetPath, "cloud-restore");
        CloudBackupEnabled = settings.CloudBackupEnabled;
        ApplyLicenseGate(settings);
        IsSetupComplete = true;
        await ReloadMattersAsync();
        await ReloadScanInboxAsync();
        await ReloadLocalBackupsAsync();
    }

    public async Task CreateMatterAsync()
    {
        if (!CanUseLicensedFeatures())
        {
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
        if (!CanUseLicensedFeatures())
        {
            return;
        }

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
        if (!CanUseLicensedFeatures())
        {
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
        if (!CanUseLicensedFeatures())
        {
            return;
        }

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
        if (!CanUseLicensedFeatures())
        {
            return;
        }

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
        if (!CanUseLicensedFeatures())
        {
            return;
        }

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
        if (!CanUseLicensedFeatures())
        {
            return;
        }

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
        if (!CanUseLicensedFeatures())
        {
            return;
        }

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
        if (!CanUseLicensedFeatures())
        {
            return;
        }

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
        if (!CanUseLicensedFeatures())
        {
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
        await ReloadLocalBackupsAsync(Path.GetFileName(snapshot.Value.BackupDirectory));
    }

    public async Task RefreshLocalBackupsAsync()
    {
        if (!CanUseLicensedFeatures())
        {
            return;
        }

        await ReloadLocalBackupsAsync();
        StatusMessage = $"Local backup list refreshed: {LocalBackupSnapshots.Count:N0} snapshot(s).";
    }

    public async Task RestoreSelectedLocalBackupAsync()
    {
        if (!CanUseLicensedFeatures())
        {
            return;
        }

        if (SelectedLocalBackupSnapshot is null)
        {
            StatusMessage = "Select a local backup snapshot before restore drill.";
            return;
        }

        if (string.IsNullOrWhiteSpace(BackupRecoveryKey))
        {
            StatusMessage = "Recovery key is required to verify a local backup restore.";
            return;
        }

        if (string.IsNullOrWhiteSpace(LocalRestoreTargetPath))
        {
            StatusMessage = "Local restore workspace folder is required.";
            return;
        }

        var restoreWorkspace = Path.Combine(
            LocalRestoreTargetPath,
            SelectedLocalBackupSnapshot.SnapshotId);
        var drill = await _restoreDrillService.RunAsync(
            new RestoreDrillRequest(
                SelectedLocalBackupSnapshot.BackupDirectory,
                restoreWorkspace,
                BackupRecoveryKey),
            CancellationToken.None);
        if (!drill.Succeeded || drill.Value is null)
        {
            StatusMessage = drill.Error ?? "Local backup restore drill failed.";
            return;
        }

        BackupRecoveryKey = string.Empty;
        var reportPath = await WriteRestoreVerificationReportAsync(
            drill.Value,
            "LocalBackup",
            SelectedLocalBackupSnapshot.SnapshotId);
        StatusMessage = $"Local backup restore workspace verified {drill.Value.VerifiedFileCount:N0} file(s) at {drill.Value.RestoreDirectory}. Report: {reportPath}.";
    }

    public async Task VerifyExternalBackupAsync()
    {
        if (!CanUseLicensedFeatures())
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(ExternalBackupDirectoryPath))
        {
            StatusMessage = "External backup folder is required.";
            return;
        }

        if (string.IsNullOrWhiteSpace(BackupRecoveryKey))
        {
            StatusMessage = "Recovery key is required to verify an external backup.";
            return;
        }

        if (string.IsNullOrWhiteSpace(ExternalRestoreTargetPath))
        {
            StatusMessage = "External restore workspace folder is required.";
            return;
        }

        var drill = await _restoreDrillService.RunAsync(
            new RestoreDrillRequest(
                ExternalBackupDirectoryPath,
                ExternalRestoreTargetPath,
                BackupRecoveryKey),
            CancellationToken.None);
        if (!drill.Succeeded || drill.Value is null)
        {
            StatusMessage = drill.Error ?? "External backup restore verification failed.";
            return;
        }

        BackupRecoveryKey = string.Empty;
        var reportPath = await WriteRestoreVerificationReportAsync(
            drill.Value,
            "ExternalBackup",
            ExternalBackupDirectoryPath);
        StatusMessage = $"External backup restore verified {drill.Value.VerifiedFileCount:N0} file(s) at {drill.Value.RestoreDirectory}. Report: {reportPath}.";
    }

    public async Task EnableCloudBackupAsync()
    {
        if (!CanUseLicensedFeatures())
        {
            return;
        }

        if (_currentSettings is null)
        {
            StatusMessage = "Complete setup before enabling cloud backup.";
            return;
        }

        if (string.IsNullOrWhiteSpace(CloudBackupProviderPath))
        {
            StatusMessage = "Cloud backup provider folder is required.";
            return;
        }

        var updatedSettings = _currentSettings with
        {
            CloudBackupEnabled = true,
            CloudBackupProviderPath = CloudBackupProviderPath.Trim()
        };
        await _settingsStore.SaveAsync(updatedSettings, CancellationToken.None);
        _currentSettings = updatedSettings;
        CloudBackupEnabled = true;
        StatusMessage = "Cloud backup enabled for local provider testing. Encrypted packages will be written to the configured provider folder.";
        await ReloadCloudBackupsAsync();
    }

    public async Task UploadCloudBackupAsync()
    {
        if (!CanUseLicensedFeatures())
        {
            return;
        }

        if (!CanUseCloudBackup())
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(BackupRecoveryKey))
        {
            StatusMessage = "Recovery key is required before uploading an encrypted cloud backup.";
            return;
        }

        var snapshot = await _backupSnapshotService.CreateSnapshotAsync(
            new BackupSnapshotRequest(VaultPath, _databasePath, BackupTargetPath, BackupRecoveryKey),
            CancellationToken.None);
        if (!snapshot.Succeeded || snapshot.Value is null)
        {
            StatusMessage = snapshot.Error ?? "Local snapshot failed before cloud upload.";
            return;
        }

        var provider = new LocalFilesystemCloudBackupProvider(CloudBackupProviderPath);
        var upload = await _cloudBackupService.UploadSnapshotAsync(
            new CloudBackupUploadRequest(_currentSettings!, snapshot.Value.BackupDirectory, BackupRecoveryKey),
            provider,
            CancellationToken.None);
        if (!upload.Succeeded || upload.Value is null)
        {
            StatusMessage = upload.Error ?? "Cloud backup upload failed.";
            return;
        }

        BackupRecoveryKey = string.Empty;
        StatusMessage = $"Cloud backup uploaded encrypted snapshot {upload.Value.Metadata.SnapshotId}.";
        await ReloadCloudBackupsAsync(upload.Value.Metadata.SnapshotId);
    }

    public async Task RefreshCloudBackupsAsync()
    {
        if (!CanUseLicensedFeatures())
        {
            return;
        }

        if (!CanUseCloudBackup())
        {
            return;
        }

        await ReloadCloudBackupsAsync();
        StatusMessage = $"Cloud backup list refreshed: {CloudBackupSnapshots.Count:N0} snapshot(s).";
    }

    public async Task RestoreSelectedCloudBackupAsync()
    {
        if (!CanUseLicensedFeatures())
        {
            return;
        }

        if (!CanUseCloudBackup())
        {
            return;
        }

        if (SelectedCloudBackupSnapshot is null)
        {
            StatusMessage = "Select a cloud backup snapshot before restore drill.";
            return;
        }

        if (string.IsNullOrWhiteSpace(BackupRecoveryKey))
        {
            StatusMessage = "Recovery key is required to download and verify a cloud backup.";
            return;
        }

        if (string.IsNullOrWhiteSpace(CloudRestoreTargetPath))
        {
            StatusMessage = "Cloud restore target folder is required.";
            return;
        }

        var provider = new LocalFilesystemCloudBackupProvider(CloudBackupProviderPath);
        var download = await _cloudBackupService.DownloadSnapshotAsync(
            new CloudBackupDownloadRequest(
                _currentSettings!,
                SelectedCloudBackupSnapshot.SnapshotId,
                BackupRecoveryKey,
                CloudRestoreTargetPath),
            provider,
            CancellationToken.None);
        if (!download.Succeeded || download.Value is null)
        {
            StatusMessage = download.Error ?? "Cloud backup download failed.";
            return;
        }

        var restoreTarget = Path.Combine(download.Value.RestoreTargetPath, "restore-drill");
        var drill = await _restoreDrillService.RunAsync(
            new RestoreDrillRequest(download.Value.RestoreTargetPath, restoreTarget, BackupRecoveryKey),
            CancellationToken.None);
        if (!drill.Succeeded || drill.Value is null)
        {
            StatusMessage = drill.Error ?? "Cloud backup restore drill failed.";
            return;
        }

        BackupRecoveryKey = string.Empty;
        var reportPath = await WriteRestoreVerificationReportAsync(
            drill.Value,
            "CloudBackup",
            SelectedCloudBackupSnapshot.SnapshotId);
        StatusMessage = $"Cloud backup restore drill verified {drill.Value.VerifiedFileCount:N0} file(s) from snapshot {SelectedCloudBackupSnapshot.SnapshotId}. Report: {reportPath}.";
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

    private async Task<string> WriteRestoreVerificationReportAsync(
        RestoreDrillResult drill,
        string sourceKind,
        string sourceIdentifier)
    {
        var report = new RestoreVerificationReport(
            ReportVersion: 1,
            CreatedAt: DateTimeOffset.UtcNow,
            SourceKind: sourceKind,
            SourceIdentifier: sourceIdentifier,
            RestoreDirectory: drill.RestoreDirectory,
            VerifiedFileCount: drill.VerifiedFileCount,
            RestoredByteLength: drill.RestoredByteLength,
            PrivacyNote: "This report intentionally excludes matter names, document names, case numbers, OCR text, document text, and recovery keys.");

        var result = await _restoreVerificationReportService.WriteAsync(
            drill.RestoreDirectory,
            report,
            CancellationToken.None);
        return result.Succeeded && result.Value is not null
            ? result.Value
            : result.Error ?? "report unavailable";
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

    private async Task ReloadLocalBackupsAsync(string? selectedSnapshotId = null)
    {
        var previousSnapshotId = selectedSnapshotId ?? SelectedLocalBackupSnapshot?.SnapshotId;
        LocalBackupSnapshots.Clear();
        if (string.IsNullOrWhiteSpace(BackupTargetPath))
        {
            return;
        }

        var snapshots = await _localBackupCatalogService.ListSnapshotsAsync(BackupTargetPath, CancellationToken.None);
        foreach (var snapshot in snapshots)
        {
            LocalBackupSnapshots.Add(new LocalBackupSnapshotViewModel(snapshot));
        }

        if (!string.IsNullOrWhiteSpace(previousSnapshotId))
        {
            SelectedLocalBackupSnapshot = LocalBackupSnapshots.FirstOrDefault(snapshot => snapshot.SnapshotId == previousSnapshotId);
        }
        else
        {
            SelectedLocalBackupSnapshot = LocalBackupSnapshots.FirstOrDefault();
        }

        (RestoreSelectedLocalBackupCommand as AsyncRelayCommand)?.RaiseCanExecuteChanged();
    }

    private async Task ReloadCloudBackupsAsync(string? selectedSnapshotId = null)
    {
        var previousSnapshotId = selectedSnapshotId ?? SelectedCloudBackupSnapshot?.SnapshotId;
        CloudBackupSnapshots.Clear();
        if (_currentSettings is null || !CloudBackupEnabled || string.IsNullOrWhiteSpace(CloudBackupProviderPath))
        {
            return;
        }

        var provider = new LocalFilesystemCloudBackupProvider(CloudBackupProviderPath);
        var snapshots = await provider.ListSnapshotsAsync(_currentSettings.InstallationId, CancellationToken.None);
        foreach (var snapshot in snapshots)
        {
            CloudBackupSnapshots.Add(new CloudBackupSnapshotViewModel(snapshot));
        }

        if (!string.IsNullOrWhiteSpace(previousSnapshotId))
        {
            SelectedCloudBackupSnapshot = CloudBackupSnapshots.FirstOrDefault(snapshot => snapshot.SnapshotId == previousSnapshotId);
        }
        else
        {
            SelectedCloudBackupSnapshot = CloudBackupSnapshots.FirstOrDefault();
        }
    }

    private void ApplyLicenseGate(AppSettings settings)
    {
        var gate = _installationControlService.EvaluateLocalAccess(settings);
        InstallationEnabled = gate.Allowed;
        StatusMessage = gate.Allowed
            ? $"Vault setup complete for {settings.FirmName}. {gate.Message}"
            : gate.Message;
    }

    private void RaiseLicensedCommandStates()
    {
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
        (RefreshLocalBackupsCommand as AsyncRelayCommand)?.RaiseCanExecuteChanged();
        (RestoreSelectedLocalBackupCommand as AsyncRelayCommand)?.RaiseCanExecuteChanged();
        (VerifyExternalBackupCommand as AsyncRelayCommand)?.RaiseCanExecuteChanged();
        (EnableCloudBackupCommand as AsyncRelayCommand)?.RaiseCanExecuteChanged();
        (UploadCloudBackupCommand as AsyncRelayCommand)?.RaiseCanExecuteChanged();
        (RefreshCloudBackupsCommand as AsyncRelayCommand)?.RaiseCanExecuteChanged();
        (RestoreSelectedCloudBackupCommand as AsyncRelayCommand)?.RaiseCanExecuteChanged();
    }

    private bool CanUseLicensedFeatures()
    {
        if (!IsSetupComplete)
        {
            StatusMessage = "Complete setup before using this feature.";
            return false;
        }

        if (!InstallationEnabled)
        {
            StatusMessage = "This installation ID is disabled or revoked. Local vault data remains on this computer.";
            return false;
        }

        return true;
    }

    private bool CanUseCloudBackup()
    {
        if (_currentSettings is null)
        {
            StatusMessage = "Complete setup before using cloud backup.";
            return false;
        }

        if (!CloudBackupEnabled)
        {
            StatusMessage = "Enable cloud backup before uploading or restoring cloud snapshots.";
            return false;
        }

        if (string.IsNullOrWhiteSpace(CloudBackupProviderPath))
        {
            StatusMessage = "Cloud backup provider folder is required.";
            return false;
        }

        return true;
    }

    private static string DefaultCloudBackupProviderPath(AppSettings settings)
    {
        return string.IsNullOrWhiteSpace(settings.CloudBackupProviderPath)
            ? Path.Combine(settings.BackupTargetPath, "cloud-provider")
            : settings.CloudBackupProviderPath;
    }
}
