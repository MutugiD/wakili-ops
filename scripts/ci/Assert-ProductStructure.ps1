$ErrorActionPreference = "Stop"

$requiredPaths = @(
  "README.md",
  "wakili-ops-documentation/README.md",
  "windows-legal-document-vault/v1/documentation/README.md",
  "windows-legal-document-vault/v1/src",
  "windows-legal-document-vault/v1/tests",
  "windows-legal-document-vault/v1/WakiliDms.sln",
  "local-matter-rag-connector/v1/documentation/README.md",
  "local-matter-rag-connector/v1/app/README.md",
  "wakili-mkononi-matter-ai-integration/v1/documentation/README.md",
  "wakili-mkononi-matter-ai-integration/v1/app/README.md"
)

$missing = @()
foreach ($path in $requiredPaths) {
  if (-not (Test-Path -LiteralPath $path)) {
    $missing += $path
  }
}

if ($missing.Count -gt 0) {
  Write-Error ("Missing required repository paths:`n" + ($missing -join "`n"))
}

Write-Host "PASS Product/version scaffold is valid."

