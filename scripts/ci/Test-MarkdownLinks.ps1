$ErrorActionPreference = "Stop"

$root = (Get-Location).Path
$markdownFiles = Get-ChildItem -Path $root -Recurse -File -Filter "*.md" |
  Where-Object { $_.FullName -notmatch "\\bin\\|\\obj\\" }

$errors = New-Object System.Collections.Generic.List[string]
$linkPattern = [regex]'\[[^\]]+\]\(([^)]+)\)'

foreach ($file in $markdownFiles) {
  $content = Get-Content -LiteralPath $file.FullName -Raw
  $matches = $linkPattern.Matches($content)

  foreach ($match in $matches) {
    $target = $match.Groups[1].Value.Trim()

    if ($target -match '^(https?|mailto):') {
      continue
    }

    if ($target.StartsWith("#")) {
      continue
    }

    $targetWithoutAnchor = $target.Split("#")[0]
    if ([string]::IsNullOrWhiteSpace($targetWithoutAnchor)) {
      continue
    }

    $decoded = [Uri]::UnescapeDataString($targetWithoutAnchor)
    $baseDirectory = Split-Path -Parent $file.FullName
    $candidate = [System.IO.Path]::GetFullPath((Join-Path $baseDirectory $decoded))

    if (-not $candidate.StartsWith($root, [StringComparison]::OrdinalIgnoreCase)) {
      $errors.Add("$($file.FullName): link escapes repository root: $target")
      continue
    }

    if (-not (Test-Path -LiteralPath $candidate)) {
      $relativeFile = $file.FullName.Substring($root.Length).TrimStart("\", "/")
      $errors.Add("${relativeFile}: missing markdown link target: $target")
    }
  }
}

if ($errors.Count -gt 0) {
  Write-Error ("Markdown link check failed:`n" + ($errors -join "`n"))
}

Write-Host "PASS Markdown links resolve."
