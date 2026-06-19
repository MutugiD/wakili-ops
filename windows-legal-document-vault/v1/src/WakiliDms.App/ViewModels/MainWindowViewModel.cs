using System.Windows.Input;
using System.Collections.ObjectModel;
using WakiliDms.Core.Domain;
using WakiliDms.Core.Matter;
using WakiliDms.Core.Setup;

namespace WakiliDms.App.ViewModels;

public sealed class MainWindowViewModel : ObservableObject
{
    private readonly ISettingsStore _settingsStore;
    private readonly IMatterRepository _matterRepository;
    private string _firmName = string.Empty;
    private string _primaryUser = string.Empty;
    private string _vaultPath = string.Empty;
    private string _scanFolderPath = string.Empty;
    private string _backupTargetPath = string.Empty;
    private string _newMatterName = string.Empty;
    private string _newMatterClientName = string.Empty;
    private string _newMatterCourtCaseNumber = string.Empty;
    private bool _recoveryKeyConfirmed;
    private bool _isSetupComplete;
    private string _statusMessage = "Complete setup to create a local-first document vault.";

    public MainWindowViewModel(ISettingsStore settingsStore, IMatterRepository matterRepository)
    {
        _settingsStore = settingsStore;
        _matterRepository = matterRepository;
        CompleteSetupCommand = new AsyncRelayCommand(CompleteSetupAsync);
        CreateMatterCommand = new AsyncRelayCommand(CreateMatterAsync, () => IsSetupComplete);
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

    public ObservableCollection<MatterListItemViewModel> Matters { get; } = [];

    public IReadOnlyList<string> NextModules { get; } =
    [
        "Setup wizard",
        "Encrypted vault",
        "Matter management",
        "Document import",
        "Classification and versioning"
    ];

    public async Task LoadAsync()
    {
        await _matterRepository.InitializeAsync(CancellationToken.None);

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

        await _settingsStore.SaveAsync(settings, CancellationToken.None);
        StatusMessage = $"Vault setup complete for {settings.FirmName}.";
        IsSetupComplete = true;
        await ReloadMattersAsync();
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
        }
        catch (ArgumentException ex)
        {
            StatusMessage = ex.Message;
        }
    }

    private async Task ReloadMattersAsync()
    {
        Matters.Clear();
        var matters = await _matterRepository.ListAsync(CancellationToken.None);
        foreach (var matter in matters)
        {
            Matters.Add(new MatterListItemViewModel(matter));
        }
    }
}
