param(
    [switch]$BuildAndInstallPackage,
    [switch]$FrameworkDependent,
    [switch]$UseDefaultUserAppData,
    [switch]$KeepAppOpen,
    [string]$TestRoot = (Join-Path $env:LOCALAPPDATA "WakiliDmsInteractiveE2E"),
    [string]$RecoveryKey = "wakili-e2e-recovery-key-2026"
)

$ErrorActionPreference = "Stop"

Add-Type -AssemblyName UIAutomationClient
Add-Type -AssemblyName System.IO.Compression.FileSystem

$root = Split-Path -Parent $PSScriptRoot
$installRoot = Join-Path $env:LOCALAPPDATA "Programs\WindowsLegalDocumentVault"
$installedExe = Join-Path $installRoot "WindowsLegalDocumentVault.exe"
$settingsOverrideName = "WAKILI_DMS_SETTINGS_PATH"
$databaseOverrideName = "WAKILI_DMS_DATABASE_PATH"
$previousSettingsOverride = [Environment]::GetEnvironmentVariable($settingsOverrideName, "Process")
$previousDatabaseOverride = [Environment]::GetEnvironmentVariable($databaseOverrideName, "Process")

function Assert-SafeTestRoot {
    param([string]$Path)

    $fullPath = [System.IO.Path]::GetFullPath($Path)
    $allowedParents = @(
        [System.IO.Path]::GetFullPath($env:TEMP),
        [System.IO.Path]::GetFullPath($env:LOCALAPPDATA)
    )

    foreach ($parent in $allowedParents) {
        if ($fullPath.StartsWith($parent, [System.StringComparison]::OrdinalIgnoreCase)) {
            return $fullPath
        }
    }

    throw "Refusing to use test root outside TEMP or LOCALAPPDATA: $fullPath"
}

function Wait-Until {
    param(
        [scriptblock]$Condition,
        [string]$Description,
        [int]$TimeoutSeconds = 30
    )

    $deadline = (Get-Date).AddSeconds($TimeoutSeconds)
    do {
        $result = & $Condition
        if ($result) {
            return $result
        }

        Start-Sleep -Milliseconds 250
    } while ((Get-Date) -lt $deadline)

    throw "Timed out waiting for $Description."
}

function Get-WindowForProcess {
    param([int]$ProcessId)

    $rootElement = [System.Windows.Automation.AutomationElement]::RootElement
    $processCondition = New-Object System.Windows.Automation.PropertyCondition(
        [System.Windows.Automation.AutomationElement]::ProcessIdProperty,
        $ProcessId)

    return Wait-Until -Description "main window for process $ProcessId" -TimeoutSeconds 40 -Condition {
        $rootElement.FindFirst([System.Windows.Automation.TreeScope]::Children, $processCondition)
    }
}

function Find-ElementByAutomationId {
    param(
        [System.Windows.Automation.AutomationElement]$Window,
        [string]$AutomationId,
        [int]$TimeoutSeconds = 20
    )

    $automationIdCondition = New-Object System.Windows.Automation.PropertyCondition(
        [System.Windows.Automation.AutomationElement]::AutomationIdProperty,
        $AutomationId)

    return Wait-Until -Description "element $AutomationId" -TimeoutSeconds $TimeoutSeconds -Condition {
        $Window.FindFirst([System.Windows.Automation.TreeScope]::Descendants, $automationIdCondition)
    }
}

function Find-TextContaining {
    param(
        [System.Windows.Automation.AutomationElement]$Window,
        [string]$Text,
        [int]$TimeoutSeconds = 20
    )

    return Wait-Until -Description "text containing '$Text'" -TimeoutSeconds $TimeoutSeconds -Condition {
        if ($Window.Current.Name -like "*$Text*") {
            return $Window
        }

        $all = $Window.FindAll([System.Windows.Automation.TreeScope]::Descendants, [System.Windows.Automation.Condition]::TrueCondition)
        for ($index = 0; $index -lt $all.Count; $index++) {
            $element = $all.Item($index)
            if ($element.Current.Name -like "*$Text*") {
                return $element
            }
        }

        return $null
    }
}

function Set-ElementValue {
    param(
        [System.Windows.Automation.AutomationElement]$Window,
        [string]$AutomationId,
        [string]$Value
    )

    $element = Find-ElementByAutomationId -Window $Window -AutomationId $AutomationId
    $pattern = $element.GetCurrentPattern([System.Windows.Automation.ValuePattern]::Pattern)
    $pattern.SetValue($Value)
}

function Invoke-Element {
    param(
        [System.Windows.Automation.AutomationElement]$Window,
        [string]$AutomationId,
        [int]$TimeoutSeconds = 20
    )

    $element = Wait-Until -Description "enabled element $AutomationId" -TimeoutSeconds $TimeoutSeconds -Condition {
        $candidate = Find-ElementByAutomationId -Window $Window -AutomationId $AutomationId -TimeoutSeconds 1
        if ($candidate -and $candidate.Current.IsEnabled) {
            return $candidate
        }

        return $null
    }

    $pattern = $element.GetCurrentPattern([System.Windows.Automation.InvokePattern]::Pattern)
    $pattern.Invoke()
}

function Ensure-CheckboxChecked {
    param(
        [System.Windows.Automation.AutomationElement]$Window,
        [string]$AutomationId
    )

    $element = Find-ElementByAutomationId -Window $Window -AutomationId $AutomationId
    $pattern = $element.GetCurrentPattern([System.Windows.Automation.TogglePattern]::Pattern)
    if ($pattern.Current.ToggleState -ne [System.Windows.Automation.ToggleState]::On) {
        $pattern.Toggle()
    }
}

function Select-FirstListItem {
    param(
        [System.Windows.Automation.AutomationElement]$Window,
        [string]$AutomationId,
        [int]$TimeoutSeconds = 20
    )

    $list = Find-ElementByAutomationId -Window $Window -AutomationId $AutomationId -TimeoutSeconds $TimeoutSeconds
    $listItemCondition = New-Object System.Windows.Automation.PropertyCondition(
        [System.Windows.Automation.AutomationElement]::ControlTypeProperty,
        [System.Windows.Automation.ControlType]::ListItem)

    $item = Wait-Until -Description "first list item in $AutomationId" -TimeoutSeconds $TimeoutSeconds -Condition {
        $items = $list.FindAll([System.Windows.Automation.TreeScope]::Descendants, $listItemCondition)
        if ($items.Count -gt 0) {
            return $items.Item(0)
        }

        return $null
    }

    $pattern = $item.GetCurrentPattern([System.Windows.Automation.SelectionItemPattern]::Pattern)
    $pattern.Select()
    return $item
}

function Download-OnlineDocuments {
    param([string]$DocumentRoot)

    New-Item -ItemType Directory -Path $DocumentRoot -Force | Out-Null
    $documents = @(
        @{
            Name = "sample-online-pleading.docx"
            Url = "https://raw.githubusercontent.com/rounakdatta/CorrectLy/master/sample.docx"
            Source = "GitHub raw sample DOCX"
        },
        @{
            Name = "sample-online-court-output.pdf"
            Url = "https://ontheline.trincoll.edu/images/bookdown/sample-local-pdf.pdf"
            Source = "Trinity College sample PDF"
        }
    )

    foreach ($document in $documents) {
        $target = Join-Path $DocumentRoot $document.Name
        Invoke-WebRequest -Uri $document.Url -OutFile $target -UseBasicParsing
        if ((Get-Item $target).Length -le 0) {
            throw "Downloaded file is empty: $target"
        }
    }

    $documents | ConvertTo-Json -Depth 4 | Set-Content -Path (Join-Path $DocumentRoot "online-document-sources.json")
}

function Install-PackageIfNeeded {
    if ((Test-Path $installedExe) -and -not $BuildAndInstallPackage) {
        return
    }

    $packageArgs = @("-NoProfile", "-ExecutionPolicy", "Bypass", "-File", ".\scripts\Build-WindowsPackage.ps1")
    if ($FrameworkDependent) {
        $packageArgs += "-FrameworkDependent"
    }

    Push-Location $root
    try {
        powershell @packageArgs
        if ($LASTEXITCODE -ne 0) {
            throw "Package build failed with exit code $LASTEXITCODE."
        }

        $packageRoot = Join-Path $root "artifacts\package\windows-legal-document-vault-v1-win-x64"
        powershell -NoProfile -ExecutionPolicy Bypass -File (Join-Path $packageRoot "install-local.ps1") -SkipShortcuts
        if ($LASTEXITCODE -ne 0) {
            throw "Package install failed with exit code $LASTEXITCODE."
        }
    }
    finally {
        Pop-Location
    }
}

$testRootFull = Assert-SafeTestRoot -Path $TestRoot
Get-Process -Name WindowsLegalDocumentVault -ErrorAction SilentlyContinue | Stop-Process -Force

if (Test-Path $testRootFull) {
    Remove-Item -LiteralPath $testRootFull -Recurse -Force
}

$documentsRoot = Join-Path $testRootFull "online-documents"
$vaultPath = Join-Path $testRootFull "Vault"
$scanPath = Join-Path $testRootFull "WatchedScan"
$backupPath = Join-Path $testRootFull "BackupTarget"
$localRestorePath = Join-Path $testRootFull "LocalRestore"
$externalBackupPath = Join-Path $testRootFull "ExternalMachineBackup"
$externalRestorePath = Join-Path $testRootFull "ExternalRestore"
$cloudProviderPath = Join-Path $testRootFull "CloudProvider"
$cloudRestorePath = Join-Path $testRootFull "CloudRestore"
$filingExportPath = Join-Path $testRootFull "FilingExports"
$appDataPath = Join-Path $testRootFull "AppData"

New-Item -ItemType Directory -Path $vaultPath, $scanPath, $backupPath, $localRestorePath, $externalBackupPath, $externalRestorePath, $cloudProviderPath, $cloudRestorePath, $filingExportPath, $appDataPath -Force | Out-Null
Download-OnlineDocuments -DocumentRoot $documentsRoot
Install-PackageIfNeeded

if (-not (Test-Path $installedExe)) {
    throw "Installed executable was not found: $installedExe"
}

if (-not $UseDefaultUserAppData) {
    [Environment]::SetEnvironmentVariable($settingsOverrideName, (Join-Path $appDataPath "settings.json"), "Process")
    [Environment]::SetEnvironmentVariable($databaseOverrideName, (Join-Path $appDataPath "wakili-dms.db"), "Process")
}

$process = $null
try {
    $process = Start-Process -FilePath $installedExe -PassThru
    $window = Get-WindowForProcess -ProcessId $process.Id
    Find-TextContaining -Window $window -Text "Windows Legal Document Vault" -TimeoutSeconds 30 | Out-Null

    $setupButton = $window.FindFirst(
        [System.Windows.Automation.TreeScope]::Descendants,
        (New-Object System.Windows.Automation.PropertyCondition(
            [System.Windows.Automation.AutomationElement]::AutomationIdProperty,
            "CompleteSetupButton")))

    if ($setupButton) {
        Set-ElementValue -Window $window -AutomationId "SetupFirmName" -Value "Wakili Interactive E2E Advocates"
        Set-ElementValue -Window $window -AutomationId "SetupPrimaryUser" -Value "Admin E2E User"
        Set-ElementValue -Window $window -AutomationId "SetupDeviceNickname" -Value "Windows E2E Workstation"
        Set-ElementValue -Window $window -AutomationId "SetupLicenseKey" -Value "TRIAL-E2E-LOCAL"
        Set-ElementValue -Window $window -AutomationId "SetupVaultPath" -Value $vaultPath
        Set-ElementValue -Window $window -AutomationId "SetupScanFolderPath" -Value $scanPath
        Set-ElementValue -Window $window -AutomationId "SetupBackupTargetPath" -Value $backupPath
        Set-ElementValue -Window $window -AutomationId "SetupRecoveryKey" -Value $RecoveryKey
        Ensure-CheckboxChecked -Window $window -AutomationId "SetupRecoveryKeyConfirmed"
        Invoke-Element -Window $window -AutomationId "CompleteSetupButton"
    }

    Find-TextContaining -Window $window -Text "Vault setup complete" -TimeoutSeconds 45 | Out-Null
    if (-not (Test-Path (Join-Path $vaultPath "vault.manifest.json"))) {
        throw "Vault manifest was not created."
    }

    Set-ElementValue -Window $window -AutomationId "NewMatterName" -Value "Republic v Online Sample Documents"
    Set-ElementValue -Window $window -AutomationId "NewMatterClientName" -Value "Wakili E2E Client"
    Set-ElementValue -Window $window -AutomationId "NewMatterCourtCaseNumber" -Value "HC-E2E-001-2026"
    Invoke-Element -Window $window -AutomationId "CreateMatterButton"
    Find-TextContaining -Window $window -Text "Matter created" -TimeoutSeconds 30 | Out-Null
    Select-FirstListItem -Window $window -AutomationId "MattersList" | Out-Null

    $docxPath = Join-Path $documentsRoot "sample-online-pleading.docx"
    Set-ElementValue -Window $window -AutomationId "ImportSourceFilePath" -Value $docxPath
    Set-ElementValue -Window $window -AutomationId "ImportRecoveryKey" -Value $RecoveryKey
    Invoke-Element -Window $window -AutomationId "ImportMatterDocumentButton"
    Find-TextContaining -Window $window -Text "Imported sample-online-pleading.docx" -TimeoutSeconds 45 | Out-Null
    Select-FirstListItem -Window $window -AutomationId "DocumentsList" | Out-Null

    Invoke-Element -Window $window -AutomationId "IndexSelectedDocumentButton"
    Find-TextContaining -Window $window -Text "Indexed" -TimeoutSeconds 45 | Out-Null
    Set-ElementValue -Window $window -AutomationId "SearchQuery" -Value "ocmputer"
    Invoke-Element -Window $window -AutomationId "SearchMatterButton"
    Find-TextContaining -Window $window -Text "Search returned 1" -TimeoutSeconds 30 | Out-Null

    $pdfPath = Join-Path $documentsRoot "sample-online-court-output.pdf"
    $scanPdfPath = Join-Path $scanPath "queued-online-scan.pdf"
    Copy-Item -LiteralPath $pdfPath -Destination $scanPdfPath -Force
    Invoke-Element -Window $window -AutomationId "RefreshScanFolderButton"
    Find-TextContaining -Window $window -Text "Scan folder refreshed: 1 queued" -TimeoutSeconds 45 | Out-Null
    Select-FirstListItem -Window $window -AutomationId "ScanInboxList" | Out-Null
    Set-ElementValue -Window $window -AutomationId "ImportRecoveryKey" -Value $RecoveryKey
    Invoke-Element -Window $window -AutomationId "ImportSelectedScanButton"
    Find-TextContaining -Window $window -Text "Imported scan queued-online-scan.pdf" -TimeoutSeconds 45 | Out-Null
    Select-FirstListItem -Window $window -AutomationId "DocumentsList" | Out-Null

    Set-ElementValue -Window $window -AutomationId "FilingPackExportRootPath" -Value $filingExportPath
    Set-ElementValue -Window $window -AutomationId "ImportRecoveryKey" -Value $RecoveryKey
    Invoke-Element -Window $window -AutomationId "ExportFilingPackButton"
    Find-TextContaining -Window $window -Text "Filing pack exported" -TimeoutSeconds 45 | Out-Null
    $filingManifests = @(Get-ChildItem -Path $filingExportPath -Recurse -Filter "filing-pack-manifest.json")
    if ($filingManifests.Count -eq 0) {
        throw "Filing-pack manifest was not created."
    }

    Set-ElementValue -Window $window -AutomationId "CourtOutputSourceFilePath" -Value $pdfPath
    Set-ElementValue -Window $window -AutomationId "ImportRecoveryKey" -Value $RecoveryKey
    Invoke-Element -Window $window -AutomationId "CaptureCourtOutputButton"
    Find-TextContaining -Window $window -Text "Captured FilingReceipt" -TimeoutSeconds 45 | Out-Null

    Set-ElementValue -Window $window -AutomationId "BackupRecoveryKey" -Value $RecoveryKey
    Invoke-Element -Window $window -AutomationId "RunBackupButton"
    Find-TextContaining -Window $window -Text "Backup created and restore drill verified" -TimeoutSeconds 60 | Out-Null
    $backupManifests = @(Get-ChildItem -Path $backupPath -Recurse -Filter "backup-manifest.json")
    if ($backupManifests.Count -eq 0) {
        throw "Backup manifest was not created."
    }
    Invoke-Element -Window $window -AutomationId "RefreshLocalBackupsButton"
    Find-TextContaining -Window $window -Text "Local backup list refreshed" -TimeoutSeconds 30 | Out-Null
    Find-TextContaining -Window $window -Text "Backup health: Healthy: local backup snapshot is available" -TimeoutSeconds 30 | Out-Null
    Find-TextContaining -Window $window -Text "Last local backup:" -TimeoutSeconds 30 | Out-Null
    Select-FirstListItem -Window $window -AutomationId "LocalBackupsList" | Out-Null
    Set-ElementValue -Window $window -AutomationId "LocalRestoreTargetPath" -Value $localRestorePath
    Set-ElementValue -Window $window -AutomationId "BackupRecoveryKey" -Value $RecoveryKey
    Invoke-Element -Window $window -AutomationId "RestoreSelectedLocalBackupButton"
    Find-TextContaining -Window $window -Text "Local backup restore workspace verified" -TimeoutSeconds 60 | Out-Null
    $localRestoreDatabaseBackups = @(Get-ChildItem -Path $localRestorePath -Recurse -Filter "wakili-dms.db.backup")
    if ($localRestoreDatabaseBackups.Count -eq 0) {
        throw "Local restore workspace encrypted database backup was not created."
    }
    $localRestoreReports = @(Get-ChildItem -Path $localRestorePath -Recurse -Filter "restore-verification-report.json")
    if ($localRestoreReports.Count -eq 0) {
        throw "Local restore verification report was not created."
    }
    $sourceBackupDirectory = Split-Path -Parent $backupManifests[0].FullName
    Copy-Item -Path (Join-Path $sourceBackupDirectory "*") -Destination $externalBackupPath -Recurse -Force
    Set-ElementValue -Window $window -AutomationId "ExternalBackupDirectoryPath" -Value $externalBackupPath
    Set-ElementValue -Window $window -AutomationId "ExternalRestoreTargetPath" -Value $externalRestorePath
    Set-ElementValue -Window $window -AutomationId "BackupRecoveryKey" -Value $RecoveryKey
    Invoke-Element -Window $window -AutomationId "VerifyExternalBackupButton"
    Find-TextContaining -Window $window -Text "External backup restore verified" -TimeoutSeconds 60 | Out-Null
    $externalRestoreDatabaseBackups = @(Get-ChildItem -Path $externalRestorePath -Recurse -Filter "wakili-dms.db.backup")
    if ($externalRestoreDatabaseBackups.Count -eq 0) {
        throw "External restore workspace encrypted database backup was not created."
    }
    $externalRestoreReports = @(Get-ChildItem -Path $externalRestorePath -Recurse -Filter "restore-verification-report.json")
    if ($externalRestoreReports.Count -eq 0) {
        throw "External restore verification report was not created."
    }

    Set-ElementValue -Window $window -AutomationId "CloudBackupProviderPath" -Value $cloudProviderPath
    Invoke-Element -Window $window -AutomationId "EnableCloudBackupButton"
    Find-TextContaining -Window $window -Text "Cloud backup enabled" -TimeoutSeconds 30 | Out-Null
    Set-ElementValue -Window $window -AutomationId "BackupRecoveryKey" -Value $RecoveryKey
    Invoke-Element -Window $window -AutomationId "UploadCloudBackupButton"
    Find-TextContaining -Window $window -Text "Cloud backup uploaded encrypted snapshot" -TimeoutSeconds 60 | Out-Null
    Find-TextContaining -Window $window -Text "Backup health: Healthy: local and cloud backup snapshots are available" -TimeoutSeconds 30 | Out-Null
    Find-TextContaining -Window $window -Text "Last cloud backup:" -TimeoutSeconds 30 | Out-Null
    Invoke-Element -Window $window -AutomationId "RefreshCloudBackupsButton"
    Find-TextContaining -Window $window -Text "Cloud backup list refreshed" -TimeoutSeconds 30 | Out-Null
    Select-FirstListItem -Window $window -AutomationId "CloudBackupsList" | Out-Null
    Set-ElementValue -Window $window -AutomationId "CloudRestoreTargetPath" -Value $cloudRestorePath
    Set-ElementValue -Window $window -AutomationId "BackupRecoveryKey" -Value $RecoveryKey
    Invoke-Element -Window $window -AutomationId "RestoreSelectedCloudBackupButton"
    Find-TextContaining -Window $window -Text "Cloud backup restore drill verified" -TimeoutSeconds 60 | Out-Null
    $cloudRestoreReports = @(Get-ChildItem -Path $cloudRestorePath -Recurse -Filter "restore-verification-report.json")
    if ($cloudRestoreReports.Count -eq 0) {
        throw "Cloud restore verification report was not created."
    }
    $cloudPackages = @(Get-ChildItem -Path $cloudProviderPath -Recurse -Filter "snapshot.package")
    if ($cloudPackages.Count -eq 0) {
        throw "Cloud backup package was not created."
    }

    $packageText = Get-Content -LiteralPath $cloudPackages[0].FullName -Raw
    if ($packageText.Contains("Republic v Online Sample Documents") -or $packageText.Contains("sample-online-pleading.docx")) {
        throw "Cloud backup package exposed matter or document details in plain text."
    }

    $settingsPath = if ($UseDefaultUserAppData) {
        Join-Path $env:LOCALAPPDATA "WakiliDms\settings.json"
    } else {
        Join-Path $appDataPath "settings.json"
    }
    if (-not (Test-Path $settingsPath)) {
        throw "Settings file was not created: $settingsPath"
    }

    [pscustomobject]@{
        Result = "PASS"
        InstalledExe = $installedExe
        TestRoot = $testRootFull
        SettingsPath = $settingsPath
        VaultManifest = Join-Path $vaultPath "vault.manifest.json"
        OnlineDocuments = $documentsRoot
        FilingPackManifestCount = $filingManifests.Count
        BackupManifestCount = $backupManifests.Count
        LocalRestoreDatabaseBackupCount = $localRestoreDatabaseBackups.Count
        LocalRestoreReportCount = $localRestoreReports.Count
        ExternalRestoreDatabaseBackupCount = $externalRestoreDatabaseBackups.Count
        ExternalRestoreReportCount = $externalRestoreReports.Count
        CloudRestoreReportCount = $cloudRestoreReports.Count
        CloudBackupPackageCount = $cloudPackages.Count
        UsedDefaultUserAppData = [bool]$UseDefaultUserAppData
    } | ConvertTo-Json -Depth 4
}
finally {
    if (-not $KeepAppOpen -and $process -and -not $process.HasExited) {
        Stop-Process -Id $process.Id -Force
    }

    [Environment]::SetEnvironmentVariable($settingsOverrideName, $previousSettingsOverride, "Process")
    [Environment]::SetEnvironmentVariable($databaseOverrideName, $previousDatabaseOverride, "Process")
}
