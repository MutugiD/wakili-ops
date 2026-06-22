$ErrorActionPreference = "Stop"

$legacyPrefix = "Sol" + "ution"
$legacySlugPrefix = "sol" + "ution"
$patterns = @(
  "$legacyPrefix 1",
  "$legacyPrefix 2",
  "$legacyPrefix 3",
  "$legacySlugPrefix-1",
  "$legacySlugPrefix-2",
  "$legacySlugPrefix-3"
)

$files = Get-ChildItem -Path . -Recurse -File |
  Where-Object {
    $_.FullName -notmatch "\\.git\\|\\bin\\|\\obj\\" -and
    $_.FullName -notmatch "\\artifacts\\" -and
    $_.Extension -in @(".md", ".cs", ".xaml", ".yml", ".yaml", ".ps1", ".toml", ".csproj", ".sln")
  }

$hits = New-Object System.Collections.Generic.List[string]

foreach ($file in $files) {
  $content = Get-Content -LiteralPath $file.FullName -Raw
  foreach ($pattern in $patterns) {
    if ($content.IndexOf($pattern, [StringComparison]::OrdinalIgnoreCase) -ge 0) {
      $root = (Get-Location).Path
      $relative = $file.FullName.Substring($root.Length).TrimStart("\", "/")
      $hits.Add("${relative}: contains legacy label '$pattern'")
    }
  }
}

if ($hits.Count -gt 0) {
  Write-Error ("Legacy numbered product labels found:`n" + ($hits -join "`n"))
}

Write-Host "PASS No legacy numbered product labels found."
