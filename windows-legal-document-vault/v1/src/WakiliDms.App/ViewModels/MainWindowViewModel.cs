using System.Windows.Input;
using System.Collections.ObjectModel;
using WakiliDms.Core.Documents;
using WakiliDms.Core.Domain;
using WakiliDms.Core.Matter;
using WakiliDms.Core.Scan;
using WakiliDms.Core.Setup;
using WakiliDms.Core.Vault;

namespace WakiliDms.App.ViewModels;

public sealed class MainWindowViewModel : ObservableObject
{
    private readonly ISettingsStore _settingsStore;
    private readonly IMatterRepository _matterRepository;
    private readonly IDocumentRepository _documentRepository;
    private readonly IScanInboxRepository _scanInboxRepository;
    private readonly IVaultService _vaultService;
    private readonly DocumentImportService _documentImportService;
    private readonly ScanFolderService _scanFolderService;
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
    private MatterListItemViewModel? _selectedMatter;
    private ScanInboxItemViewModel? _selectedScan;
    private bool _recoveryKeyConfirmed;
    private bool _isSetupComplete;
    private string _statusMessage = "Complete setup to create a local-first document vault.";

    public MainWindowViewModel(
        ISettingsStore settingsStore,
        IMatterRepository matterRepository,
        IDocumentRepository documentRepository,
        IScanInboxRepository scanInboxRepository,
        IVaultService vaultService)
    {
        _settingsStore = settingsStore;
        _matterRepository = matterRepository;
        _documentRepository = documentRepository;
        _scanInboxRepository = scanInboxRepository;
        _vaultService = vaultService;
        _documentImportService = new DocumentImportService(_matterRepository, _documentRepository, _vaultService);
        _scanFolderService = new ScanFolderService(_scanInboxRepository);
        CompleteSetupCommand = new AsyncRelayCommand(CompleteSetupAsync);
        CreateMatterCommand = new AsyncRelayCommand(CreateMatterAsync, () => IsSetupComplete);
        ImportDocumentCommand = new AsyncRelayCommand(ImportDocumentAsync, () => IsSetupComplete && SelectedMatter is not null);
        RefreshScanFolderCommand = new AsyncRelayCommand(RefreshScanFolderAsync, () => IsSetupComplete);
        ImportSelectedScanCommand = new AsyncRelayCommand(ImportSelectedScanAsync, () => IsSetupComplete && SelectedMatter is not null && SelectedScan is not null);
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

    public ObservableCollection<MatterListItemViewModel> Matters { get; } = [];

    public ObservableCollection<DocumentListItemViewModel> Documents { get; } = [];

    public ObservableCollection<ScanInboxItemViewModel> ScanInbox { get; } = [];

    public IReadOnlyList<string> NextModules { get; } =
    [
        "Setup wizard",
        "Encrypted vault",
        "Matter management",
        "Document import",
        "Scan inbox",
        "Classification and versioning"
    ];

    public async Task LoadAsync()
    {
        await _matterRepository.InitializeAsync(CancellationToken.None);
        await _documentRepository.InitializeAsync(CancellationToken.None);
        await _scanInboxRepository.InitializeAsync(CancellationToken.None);

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

    private async Task ReloadDocumentsForSelectionAsync()
    {
        Documents.Clear();
        if (SelectedMatter is null)
        {
            return;
        }

        var documents = await _documentRepository.ListByMatterAsync(SelectedMatter.Id, CancellationToken.None);
        foreach (var document in documents)
        {
            Documents.Add(new DocumentListItemViewModel(document));
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
