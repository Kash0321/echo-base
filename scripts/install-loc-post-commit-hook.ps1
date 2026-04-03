$ErrorActionPreference = "Stop"

$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
$hookDir = Join-Path $repoRoot ".git/hooks"
$hookPath = Join-Path $hookDir "post-commit"

if (-not (Test-Path -LiteralPath $hookDir)) {
    throw "No existe .git/hooks. Ejecuta este script desde un clon Git valido."
}

$hookContent = @'
#!/usr/bin/env bash
set -e

LOCK_FILE=".git/.loc-post-commit.lock"

if [ -f "$LOCK_FILE" ]; then
  rm -f "$LOCK_FILE"
  exit 0
fi

touch "$LOCK_FILE"

pwsh -NoLogo -NoProfile -ExecutionPolicy Bypass -File "scripts/update-loc-report.ps1" -CommitRef HEAD || true

if git diff --quiet -- README.md; then
  rm -f "$LOCK_FILE"
  exit 0
fi

git add README.md
GIT_EDITOR=true git commit --amend --no-edit --no-verify || true
rm -f "$LOCK_FILE"
'@

Set-Content -LiteralPath $hookPath -Value $hookContent -Encoding Ascii

if (Get-Command chmod -ErrorAction SilentlyContinue) {
    & chmod +x $hookPath | Out-Null
}

Write-Host "Hook instalado en .git/hooks/post-commit"
Write-Host "Cada commit actualizara README.md con el reporte LoC y enmendara el commit automaticamente."
