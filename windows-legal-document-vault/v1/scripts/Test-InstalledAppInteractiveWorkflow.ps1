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
Add-Type -AssemblyName System.Windows.Forms
Add-Type @"
using System;
using System.Runtime.InteropServices;
public static class NativeMouse {
    [DllImport("user32.dll")]
    public static extern void mouse_event(int dwFlags, int dx, int dy, int cButtons, int dwExtraInfo);
}
"@

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

function Invoke-ConfirmationButton {
    param(
        [int]$ProcessId,
        [string]$Title,
        [string]$ButtonName,
        [string]$ButtonAutomationId,
        [int]$TimeoutSeconds = 20
    )

    $rootElement = [System.Windows.Automation.AutomationElement]::RootElement
    $processCondition = New-Object System.Windows.Automation.PropertyCondition(
        [System.Windows.Automation.AutomationElement]::ProcessIdProperty,
        $ProcessId)
    $buttonCondition = New-Object System.Windows.Automation.PropertyCondition(
        [System.Windows.Automation.AutomationElement]::ControlTypeProperty,
        [System.Windows.Automation.ControlType]::Button)

    $dialog = Wait-Until -Description "confirmation dialog '$Title'" -TimeoutSeconds $TimeoutSeconds -Condition {
        $windows = $rootElement.FindAll([System.Windows.Automation.TreeScope]::Descendants, $processCondition)
        for ($index = 0; $index -lt $windows.Count; $index++) {
            $candidate = $windows.Item($index)
            if ($candidate.Current.ControlType -ne [System.Windows.Automation.ControlType]::Window) {
                continue
            }

            if ($candidate.Current.Name -like "*$Title*" -or $candidate.Current.Name -like "*Delete*" -or $candidate.Current.Name -like "*cleanup*") {
                return $candidate
            }
        }

        return $null
    }

    $confirmationButton = Wait-Until -Description "$ButtonName button for '$Title'" -TimeoutSeconds $TimeoutSeconds -Condition {
        $buttons = $dialog.FindAll([System.Windows.Automation.TreeScope]::Descendants, $buttonCondition)
        for ($index = 0; $index -lt $buttons.Count; $index++) {
            $button = $buttons.Item($index)
            if ($button.Current.Name -eq $ButtonName -or $button.Current.AutomationId -eq $ButtonAutomationId) {
                return $button
            }
        }

        return $null
    }

    $pattern = $confirmationButton.GetCurrentPattern([System.Windows.Automation.InvokePattern]::Pattern)
    $pattern.Invoke()
}

function Invoke-ElementAndConfirm {
    param(
        [System.Windows.Automation.AutomationElement]$Window,
        [string]$AutomationId,
        [int]$ProcessId,
        [string]$ConfirmationTitle,
        [string]$ButtonName,
        [string]$ButtonAutomationId,
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
    Invoke-ConfirmationButton -ProcessId $ProcessId -Title $ConfirmationTitle -ButtonName $ButtonName -ButtonAutomationId $ButtonAutomationId -TimeoutSeconds $TimeoutSeconds
}

function Invoke-ElementAndConfirmYes {
    param(
        [System.Windows.Automation.AutomationElement]$Window,
        [string]$AutomationId,
        [int]$ProcessId,
        [string]$ConfirmationTitle,
        [int]$TimeoutSeconds = 20
    )

    Invoke-ElementAndConfirm -Window $Window -AutomationId $AutomationId -ProcessId $ProcessId -ConfirmationTitle $ConfirmationTitle -ButtonName "Yes" -ButtonAutomationId "6" -TimeoutSeconds $TimeoutSeconds
}

function Invoke-ElementAndConfirmNo {
    param(
        [System.Windows.Automation.AutomationElement]$Window,
        [string]$AutomationId,
        [int]$ProcessId,
        [string]$ConfirmationTitle,
        [int]$TimeoutSeconds = 20
    )

    Invoke-ElementAndConfirm -Window $Window -AutomationId $AutomationId -ProcessId $ProcessId -ConfirmationTitle $ConfirmationTitle -ButtonName "No" -ButtonAutomationId "7" -TimeoutSeconds $TimeoutSeconds
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

function Select-ComboBoxItemByName {
    param(
        [System.Windows.Automation.AutomationElement]$Window,
        [string]$AutomationId,
        [string]$ItemName,
        [int]$TimeoutSeconds = 20
    )

    $combo = Find-ElementByAutomationId -Window $Window -AutomationId $AutomationId -TimeoutSeconds $TimeoutSeconds
    try {
        $expand = $combo.GetCurrentPattern([System.Windows.Automation.ExpandCollapsePattern]::Pattern)
        if ($expand.Current.ExpandCollapseState -ne [System.Windows.Automation.ExpandCollapseState]::Expanded) {
            $expand.Expand()
        }
    }
    catch {
        $combo.SetFocus()
        [System.Windows.Forms.SendKeys]::SendWait("%{DOWN}")
    }

    $listItemCondition = New-Object System.Windows.Automation.PropertyCondition(
        [System.Windows.Automation.AutomationElement]::ControlTypeProperty,
        [System.Windows.Automation.ControlType]::ListItem)

    $item = Wait-Until -Description "combo item $ItemName in $AutomationId" -TimeoutSeconds $TimeoutSeconds -Condition {
        $items = [System.Windows.Automation.AutomationElement]::RootElement.FindAll(
            [System.Windows.Automation.TreeScope]::Descendants,
            $listItemCondition)
        for ($index = 0; $index -lt $items.Count; $index++) {
            $candidate = $items.Item($index)
            if ($candidate.Current.Name -eq $ItemName) {
                return $candidate
            }
        }

        return $null
    }

    try {
        $selection = $item.GetCurrentPattern([System.Windows.Automation.SelectionItemPattern]::Pattern)
        $selection.Select()
    }
    catch {
        try {
            $invoke = $item.GetCurrentPattern([System.Windows.Automation.InvokePattern]::Pattern)
            $invoke.Invoke()
        }
        catch {
            $rect = $item.Current.BoundingRectangle
            if ($rect.Width -le 0 -or $rect.Height -le 0) {
                throw "Combo item $ItemName has no clickable bounds."
            }

            [System.Windows.Forms.Cursor]::Position = New-Object System.Drawing.Point(
                [int]($rect.X + ($rect.Width / 2)),
                [int]($rect.Y + ($rect.Height / 2)))
            [NativeMouse]::mouse_event(0x0002, 0, 0, 0, 0)
            Start-Sleep -Milliseconds 100
            [NativeMouse]::mouse_event(0x0004, 0, 0, 0, 0)
        }
    }
}

function Download-OnlineDocuments {
    param([string]$DocumentRoot)

    New-Item -ItemType Directory -Path $DocumentRoot -Force | Out-Null
    $documents = @(
        @{
            Name = "sample-online-pleading.docx"
            Url = "https://raw.githubusercontent.com/rounakdatta/CorrectLy/master/sample.docx"
            Source = "GitHub raw sample DOCX for Word drafting coverage"
            Category = "Word draft pleading"
        },
        @{
            Name = "judiciary-coa-registry-manual.pdf"
            Url = "https://judiciary.go.ke/wp-content/uploads/2023/07/COA-REG-Manual.pdf"
            Source = "Kenya Judiciary Court of Appeal Registry Manual"
            Category = "Registry/authority material"
        },
        @{
            Name = "judiciary-coa-automation-guide.pdf"
            Url = "https://judiciary.go.ke/wp-content/uploads/2023/07/coa-guide-for-print-3.pdf"
            Source = "Kenya Judiciary Court of Appeal Automation Guide"
            Category = "Court automation/e-filing guide"
        },
        @{
            Name = "supreme-court-general-practice-directions-2020.pdf"
            Url = "https://supremecourt.judiciary.go.ke/wp-content/uploads/2022/11/Supreme-Court-General-Practice-Directions-of-2020.pdf"
            Source = "Supreme Court of Kenya General Practice Directions 2020"
            Category = "Practice directions"
        },
        @{
            Name = "supreme-court-virtual-session-practice-directions-2023.pdf"
            Url = "https://supremecourt.judiciary.go.ke/wp-content/uploads/2024/07/Supreme-Court-Virtual-Session-Practice-Directions-2023.pdf"
            Source = "Supreme Court of Kenya Virtual Session Practice Directions 2023"
            Category = "Virtual court practice directions"
        },
        @{
            Name = "supreme-court-self-representing-litigants-e-guide.pdf"
            Url = "https://supremecourt.judiciary.go.ke/wp-content/uploads/2025/06/SUPREME-COURT-OF-KENYA-SELF-REPRESENTING-LITIGANTS-E-GUIDE.pdf"
            Source = "Supreme Court of Kenya Self Representing Litigants E-Guide"
            Category = "Litigant guide"
        },
        @{
            Name = "supreme-court-bia-tosha-judgment.pdf"
            Url = "https://supremecourt.judiciary.go.ke/wp-content/uploads/2026/06/Bia-Tosha-Distributors-Limited-Vs-Kenya-Breweries-Limited-6-Others-SC-Petition-No-15-of-2020-Judgment-17th-February-2023.pdf"
            Source = "Supreme Court of Kenya judgment PDF"
            Category = "Judgment"
        },
        @{
            Name = "supreme-court-charles-kanjama-ruling.pdf"
            Url = "https://supremecourt.judiciary.go.ke/wp-content/uploads/2026/06/Charles-Kanjama-Vs-the-AG-82-others-SC-Petition-Application-E017-of-2021-Ruling-19th-May-2022.pdf"
            Source = "Supreme Court of Kenya ruling PDF"
            Category = "Ruling"
        }
    )

    foreach ($document in $documents) {
        $target = Join-Path $DocumentRoot $document.Name
        Invoke-WebRequest -Uri $document.Url -OutFile $target -UseBasicParsing
        if ((Get-Item $target).Length -le 0) {
            throw "Downloaded file is empty: $target"
        }
        if ($document.Name.EndsWith(".pdf", [System.StringComparison]::OrdinalIgnoreCase)) {
            $header = [System.Text.Encoding]::ASCII.GetString([System.IO.File]::ReadAllBytes($target), 0, 4)
            if ($header -ne "%PDF") {
                throw "Downloaded file is not a PDF: $target"
            }
        }
    }

    $documents | ConvertTo-Json -Depth 4 | Set-Content -Path (Join-Path $DocumentRoot "online-document-sources.json")
}

function Import-MatterDocument {
    param(
        [System.Windows.Automation.AutomationElement]$Window,
        [string]$SourcePath,
        [string]$DocumentType,
        [string]$RecoveryKey,
        [int]$TimeoutSeconds = 60
    )

    Select-ComboBoxItemByName -Window $Window -AutomationId "DocumentTypeCombo" -ItemName $DocumentType
    Set-ElementValue -Window $Window -AutomationId "ImportSourceFilePath" -Value $SourcePath
    Set-ElementValue -Window $Window -AutomationId "ImportRecoveryKey" -Value $RecoveryKey
    Invoke-Element -Window $Window -AutomationId "ImportMatterDocumentButton"
    Find-TextContaining -Window $Window -Text "Imported $(Split-Path -Leaf $SourcePath)" -TimeoutSeconds $TimeoutSeconds | Out-Null
}

function Capture-CourtOutput {
    param(
        [System.Windows.Automation.AutomationElement]$Window,
        [string]$SourcePath,
        [string]$OutputType,
        [string]$RecoveryKey,
        [int]$TimeoutSeconds = 60
    )

    Select-ComboBoxItemByName -Window $Window -AutomationId "CourtOutputTypeCombo" -ItemName $OutputType
    Set-ElementValue -Window $Window -AutomationId "CourtOutputSourceFilePath" -Value $SourcePath
    Set-ElementValue -Window $Window -AutomationId "ImportRecoveryKey" -Value $RecoveryKey
    Invoke-Element -Window $Window -AutomationId "CaptureCourtOutputButton"
    Find-TextContaining -Window $Window -Text "Captured ${OutputType}" -TimeoutSeconds $TimeoutSeconds | Out-Null
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
$restoreReportExportPath = Join-Path $testRootFull "RestoreReportExports"
$filingExportPath = Join-Path $testRootFull "FilingExports"
$appDataPath = Join-Path $testRootFull "AppData"

New-Item -ItemType Directory -Path $vaultPath, $scanPath, $backupPath, $localRestorePath, $externalBackupPath, $externalRestorePath, $cloudProviderPath, $cloudRestorePath, $restoreReportExportPath, $filingExportPath, $appDataPath -Force | Out-Null
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
    Import-MatterDocument -Window $window -SourcePath $docxPath -DocumentType "Pleading" -RecoveryKey $RecoveryKey
    Select-FirstListItem -Window $window -AutomationId "DocumentsList" | Out-Null

    Invoke-Element -Window $window -AutomationId "IndexSelectedDocumentButton"
    Find-TextContaining -Window $window -Text "Indexed" -TimeoutSeconds 45 | Out-Null
    Set-ElementValue -Window $window -AutomationId "SearchQuery" -Value "ocmputer"
    Invoke-Element -Window $window -AutomationId "SearchMatterButton"
    Find-TextContaining -Window $window -Text "Search returned 1" -TimeoutSeconds 30 | Out-Null

    Import-MatterDocument -Window $window -SourcePath (Join-Path $documentsRoot "judiciary-coa-registry-manual.pdf") -DocumentType "Authority" -RecoveryKey $RecoveryKey
    Import-MatterDocument -Window $window -SourcePath (Join-Path $documentsRoot "supreme-court-general-practice-directions-2020.pdf") -DocumentType "Notice" -RecoveryKey $RecoveryKey

    $pdfPath = Join-Path $documentsRoot "judiciary-coa-automation-guide.pdf"
    $scanPdfPath = Join-Path $scanPath "queued-online-scan.pdf"
    Copy-Item -LiteralPath $pdfPath -Destination $scanPdfPath -Force
    Invoke-Element -Window $window -AutomationId "RefreshScanFolderButton"
    Find-TextContaining -Window $window -Text "Scan folder refreshed: 1 queued" -TimeoutSeconds 45 | Out-Null
    Select-FirstListItem -Window $window -AutomationId "ScanInboxList" | Out-Null
    Select-ComboBoxItemByName -Window $window -AutomationId "DocumentTypeCombo" -ItemName "Annexure"
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

    Capture-CourtOutput -Window $window -SourcePath (Join-Path $documentsRoot "supreme-court-virtual-session-practice-directions-2023.pdf") -OutputType "Notice" -RecoveryKey $RecoveryKey
    Capture-CourtOutput -Window $window -SourcePath (Join-Path $documentsRoot "supreme-court-charles-kanjama-ruling.pdf") -OutputType "Ruling" -RecoveryKey $RecoveryKey
    Capture-CourtOutput -Window $window -SourcePath (Join-Path $documentsRoot "supreme-court-bia-tosha-judgment.pdf") -OutputType "Judgment" -RecoveryKey $RecoveryKey

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
    Find-TextContaining -Window $window -Text "Backup snapshots: local 1, cloud 0" -TimeoutSeconds 30 | Out-Null
    Find-TextContaining -Window $window -Text "Last local backup:" -TimeoutSeconds 30 | Out-Null
    Select-FirstListItem -Window $window -AutomationId "LocalBackupsList" | Out-Null
    Set-ElementValue -Window $window -AutomationId "LocalRestoreTargetPath" -Value $localRestorePath
    Set-ElementValue -Window $window -AutomationId "BackupRecoveryKey" -Value $RecoveryKey
    Invoke-Element -Window $window -AutomationId "RestoreSelectedLocalBackupButton"
    Find-TextContaining -Window $window -Text "Local backup restore workspace verified" -TimeoutSeconds 60 | Out-Null
    Find-TextContaining -Window $window -Text "Last restore report: LocalBackup" -TimeoutSeconds 30 | Out-Null
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
    Find-TextContaining -Window $window -Text "Last restore report: ExternalBackup" -TimeoutSeconds 30 | Out-Null
    $externalRestoreDatabaseBackups = @(Get-ChildItem -Path $externalRestorePath -Recurse -Filter "wakili-dms.db.backup")
    if ($externalRestoreDatabaseBackups.Count -eq 0) {
        throw "External restore workspace encrypted database backup was not created."
    }
    $externalRestoreReports = @(Get-ChildItem -Path $externalRestorePath -Recurse -Filter "restore-verification-report.json")
    if ($externalRestoreReports.Count -eq 0) {
        throw "External restore verification report was not created."
    }
    Set-ElementValue -Window $window -AutomationId "BackupRecoveryKey" -Value $RecoveryKey
    Invoke-Element -Window $window -AutomationId "RunBackupButton"
    Find-TextContaining -Window $window -Text "Backup created and restore drill verified" -TimeoutSeconds 60 | Out-Null
    Invoke-Element -Window $window -AutomationId "RefreshLocalBackupsButton"
    Find-TextContaining -Window $window -Text "Local backup list refreshed: 2 snapshot" -TimeoutSeconds 30 | Out-Null
    Set-ElementValue -Window $window -AutomationId "RetentionKeepLatestCount" -Value "1"
    Set-ElementValue -Window $window -AutomationId "RetentionDeleteOlderThanDays" -Value "0"
    Invoke-Element -Window $window -AutomationId "PreviewLocalBackupRetentionButton"
    Find-TextContaining -Window $window -Text "Local backup retention preview: 1 snapshot" -TimeoutSeconds 30 | Out-Null
    Invoke-ElementAndConfirmNo -Window $window -AutomationId "ApplyLocalBackupRetentionButton" -ProcessId $process.Id -ConfirmationTitle "Apply local backup retention cleanup?"
    Find-TextContaining -Window $window -Text "Local backup retention cleanup cancelled" -TimeoutSeconds 30 | Out-Null
    $backupManifestsAfterRetentionCancel = @(Get-ChildItem -Path $backupPath -Recurse -Filter "backup-manifest.json")
    if ($backupManifestsAfterRetentionCancel.Count -ne 2) {
        throw "Retention cleanup cancellation should leave two local backup manifests; found $($backupManifestsAfterRetentionCancel.Count)."
    }
    Invoke-ElementAndConfirmYes -Window $window -AutomationId "ApplyLocalBackupRetentionButton" -ProcessId $process.Id -ConfirmationTitle "Apply local backup retention cleanup?"
    Find-TextContaining -Window $window -Text "Local backup retention cleanup deleted 1 snapshot" -TimeoutSeconds 30 | Out-Null
    $backupManifestsAfterRetention = @(Get-ChildItem -Path $backupPath -Recurse -Filter "backup-manifest.json")
    if ($backupManifestsAfterRetention.Count -ne 1) {
        throw "Retention cleanup should leave exactly one local backup manifest; found $($backupManifestsAfterRetention.Count)."
    }
    $remainingBackupDirectory = Split-Path -Parent $backupManifestsAfterRetention[0].FullName

    Set-ElementValue -Window $window -AutomationId "CloudBackupProviderPath" -Value (Join-Path $vaultPath "unsafe-cloud-provider")
    Invoke-Element -Window $window -AutomationId "EnableCloudBackupButton"
    Find-TextContaining -Window $window -Text "Cloud backup provider folder must be separate from the encrypted vault folder" -TimeoutSeconds 30 | Out-Null
    Set-ElementValue -Window $window -AutomationId "CloudBackupProviderPath" -Value $cloudProviderPath
    Invoke-Element -Window $window -AutomationId "EnableCloudBackupButton"
    Find-TextContaining -Window $window -Text "Cloud backup enabled" -TimeoutSeconds 30 | Out-Null
    Set-ElementValue -Window $window -AutomationId "BackupRecoveryKey" -Value $RecoveryKey
    Invoke-Element -Window $window -AutomationId "UploadCloudBackupButton"
    Find-TextContaining -Window $window -Text "Cloud backup uploaded encrypted snapshot" -TimeoutSeconds 60 | Out-Null
    Find-TextContaining -Window $window -Text "Backup health: Healthy: local and cloud backup snapshots are available" -TimeoutSeconds 30 | Out-Null
    Find-TextContaining -Window $window -Text "Backup snapshots: local 2, cloud 1" -TimeoutSeconds 30 | Out-Null
    Find-TextContaining -Window $window -Text "Last cloud backup:" -TimeoutSeconds 30 | Out-Null
    Invoke-Element -Window $window -AutomationId "RefreshCloudBackupsButton"
    Find-TextContaining -Window $window -Text "Cloud backup list refreshed" -TimeoutSeconds 30 | Out-Null
    Select-FirstListItem -Window $window -AutomationId "CloudBackupsList" | Out-Null
    Set-ElementValue -Window $window -AutomationId "CloudRestoreTargetPath" -Value $cloudRestorePath
    Set-ElementValue -Window $window -AutomationId "BackupRecoveryKey" -Value $RecoveryKey
    Invoke-Element -Window $window -AutomationId "RestoreSelectedCloudBackupButton"
    Find-TextContaining -Window $window -Text "Cloud backup restore drill verified" -TimeoutSeconds 60 | Out-Null
    Find-TextContaining -Window $window -Text "Last restore report: CloudBackup" -TimeoutSeconds 30 | Out-Null
    $cloudRestoreReports = @(Get-ChildItem -Path $cloudRestorePath -Recurse -Filter "restore-verification-report.json")
    if ($cloudRestoreReports.Count -eq 0) {
        throw "Cloud restore verification report was not created."
    }
    Invoke-Element -Window $window -AutomationId "CopyLastRestoreReportPathButton"
    Find-TextContaining -Window $window -Text "Latest restore verification report path copied" -TimeoutSeconds 30 | Out-Null
    $clipboardText = (Get-Clipboard -Raw).Trim()
    if ($clipboardText -ne $cloudRestoreReports[0].FullName) {
        throw "Clipboard did not contain the latest restore report path. Expected '$($cloudRestoreReports[0].FullName)' but found '$clipboardText'."
    }
    Set-ElementValue -Window $window -AutomationId "RestoreReportExportFolderPath" -Value $restoreReportExportPath
    Invoke-Element -Window $window -AutomationId "ExportLastRestoreReportButton"
    Find-TextContaining -Window $window -Text "Latest restore verification report exported" -TimeoutSeconds 30 | Out-Null
    $exportedRestoreReports = @(Get-ChildItem -Path $restoreReportExportPath -Filter "restore-verification-report-*.json")
    if ($exportedRestoreReports.Count -ne 1) {
        throw "Expected one exported restore report; found $($exportedRestoreReports.Count)."
    }
    $sourceReportHash = (Get-FileHash -LiteralPath $cloudRestoreReports[0].FullName -Algorithm SHA256).Hash
    $exportedReportHash = (Get-FileHash -LiteralPath $exportedRestoreReports[0].FullName -Algorithm SHA256).Hash
    if ($sourceReportHash -ne $exportedReportHash) {
        throw "Exported restore report hash did not match the source report."
    }
    $cloudPackages = @(Get-ChildItem -Path $cloudProviderPath -Recurse -Filter "snapshot.package")
    if ($cloudPackages.Count -eq 0) {
        throw "Cloud backup package was not created."
    }

    $packageText = Get-Content -LiteralPath $cloudPackages[0].FullName -Raw
    if ($packageText.Contains("Republic v Online Sample Documents") -or $packageText.Contains("sample-online-pleading.docx")) {
        throw "Cloud backup package exposed matter or document details in plain text."
    }
    Invoke-ElementAndConfirmNo -Window $window -AutomationId "DeleteSelectedCloudBackupButton" -ProcessId $process.Id -ConfirmationTitle "Delete cloud backup package?"
    Find-TextContaining -Window $window -Text "Cloud backup snapshot delete cancelled" -TimeoutSeconds 30 | Out-Null
    $cloudPackagesAfterCancel = @(Get-ChildItem -Path $cloudProviderPath -Recurse -Filter "snapshot.package")
    if ($cloudPackagesAfterCancel.Count -ne 1) {
        throw "Cloud backup delete cancellation should leave one package; found $($cloudPackagesAfterCancel.Count)."
    }
    Invoke-ElementAndConfirmYes -Window $window -AutomationId "DeleteSelectedCloudBackupButton" -ProcessId $process.Id -ConfirmationTitle "Delete cloud backup package?"
    Find-TextContaining -Window $window -Text "Cloud backup snapshot deleted" -TimeoutSeconds 30 | Out-Null
    Find-TextContaining -Window $window -Text "Last restore report: CloudBackup" -TimeoutSeconds 30 | Out-Null
    $cloudPackagesAfterDelete = @(Get-ChildItem -Path $cloudProviderPath -Recurse -Filter "snapshot.package")
    if ($cloudPackagesAfterDelete.Count -ne 0) {
        throw "Cloud backup package remained after delete."
    }

    Invoke-Element -Window $window -AutomationId "RefreshLocalBackupsButton"
    Find-TextContaining -Window $window -Text "Local backup list refreshed" -TimeoutSeconds 30 | Out-Null
    Select-FirstListItem -Window $window -AutomationId "LocalBackupsList" | Out-Null
    $localBackupManifestsBeforeDelete = @(Get-ChildItem -Path $backupPath -Recurse -Filter "backup-manifest.json")
    Invoke-ElementAndConfirmNo -Window $window -AutomationId "DeleteSelectedLocalBackupButton" -ProcessId $process.Id -ConfirmationTitle "Delete local backup snapshot?"
    Find-TextContaining -Window $window -Text "Local backup snapshot delete cancelled" -TimeoutSeconds 30 | Out-Null
    $localBackupManifestsAfterCancel = @(Get-ChildItem -Path $backupPath -Recurse -Filter "backup-manifest.json")
    if ($localBackupManifestsAfterCancel.Count -ne $localBackupManifestsBeforeDelete.Count) {
        throw "Local backup delete cancellation changed backup count from $($localBackupManifestsBeforeDelete.Count) to $($localBackupManifestsAfterCancel.Count)."
    }
    Invoke-ElementAndConfirmYes -Window $window -AutomationId "DeleteSelectedLocalBackupButton" -ProcessId $process.Id -ConfirmationTitle "Delete local backup snapshot?"
    Find-TextContaining -Window $window -Text "Local backup snapshot deleted" -TimeoutSeconds 30 | Out-Null
    $localBackupManifestsAfterDelete = @(Get-ChildItem -Path $backupPath -Recurse -Filter "backup-manifest.json")
    if ($localBackupManifestsAfterDelete.Count -ne ($localBackupManifestsBeforeDelete.Count - 1)) {
        throw "Local backup delete should remove one manifest; before $($localBackupManifestsBeforeDelete.Count), after $($localBackupManifestsAfterDelete.Count)."
    }
    if (-not (Test-Path (Join-Path $vaultPath "vault.manifest.json"))) {
        throw "Live vault manifest was deleted during backup cleanup."
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
        BackupManifestCountAfterRetention = $backupManifestsAfterRetention.Count
        CloudRestoreReportCount = $cloudRestoreReports.Count
        ExportedRestoreReportCount = $exportedRestoreReports.Count
        CloudBackupPackageCount = $cloudPackages.Count
        CloudBackupPackageCountAfterDelete = $cloudPackagesAfterDelete.Count
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
