param(
    [string]$ReadmePath = "README.md",
    [string]$CommitRef = "HEAD"
)

$ErrorActionPreference = "Stop"

$culture = [System.Globalization.CultureInfo]::GetCultureInfo("es-ES")
$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
Set-Location $repoRoot

function Format-Int([long]$value) {
    return $value.ToString("N0", $culture)
}

function Format-Pct([double]$value) {
    return ($value.ToString("N1", $culture) + "%")
}

function Get-RelativeUnixPath([string]$basePath, [string]$fullPath) {
    $relative = [System.IO.Path]::GetRelativePath($basePath, $fullPath)
    return ($relative -replace '\\', '/')
}

$sourceExt = @('.cs', '.razor', '.cshtml', '.js', '.ts', '.tsx', '.css', '.scss')

$allFiles = Get-ChildItem -Path (Join-Path $repoRoot "src"), (Join-Path $repoRoot "tests") -Recurse -File |
    Where-Object {
        $_.Extension -in $sourceExt -and
        $_.FullName -notmatch '\\bin\\|\\obj\\|\\Migrations\\|\\wwwroot\\lib\\' -and
        $_.Name -notlike '*.Designer.cs' -and
        $_.Name -notlike '*.g.cs' -and
        $_.Name -notlike '*.g.i.cs' -and
        $_.Name -notmatch '\\.min\\.'
    }

$rows = foreach ($file in $allFiles) {
    $relative = Get-RelativeUnixPath -basePath $repoRoot -fullPath $file.FullName
    $parts = $relative -split '/'
    $root = $parts[0]
    $project = if ($parts.Count -gt 1) { $parts[1] } else { "otros" }
    $kind = if ($root -eq "tests") { "Pruebas" } else { "Aplicacion" }
    $loc = (Get-Content -LiteralPath $file.FullName | Where-Object { $_.Trim() -ne "" }).Count

    [pscustomobject]@{
        Project = $project
        Kind = $kind
        LoC = [long]$loc
    }
}

$projectDirs = @()
$projectDirs += Get-ChildItem -Path (Join-Path $repoRoot "src") -Directory
$projectDirs += Get-ChildItem -Path (Join-Path $repoRoot "tests") -Directory

$projectSummary = foreach ($projectDir in $projectDirs) {
    $projectName = $projectDir.Name
    $kind = if ($projectDir.FullName -like "*\tests\*") { "Pruebas" } else { "Aplicacion" }

    $projectRows = $rows | Where-Object { $_.Project -eq $projectName -and $_.Kind -eq $kind }
    if ($projectRows.Count -eq 0) {
        [pscustomobject]@{
            Project = $projectName
            Kind = $kind
            Files = 0
            LoC = [long]0
        }
    }
    else {
        [pscustomobject]@{
            Project = $projectName
            Kind = $kind
            Files = $projectRows.Count
            LoC = [long](($projectRows | Measure-Object -Property LoC -Sum).Sum)
        }
    }
}

$appSummary = $projectSummary | Where-Object { $_.Kind -eq "Aplicacion" } | Sort-Object Project
$testSummary = $projectSummary | Where-Object { $_.Kind -eq "Pruebas" } | Sort-Object Project

$appFiles = ($appSummary | Measure-Object -Property Files -Sum).Sum
$appLoc = ($appSummary | Measure-Object -Property LoC -Sum).Sum
$testFiles = ($testSummary | Measure-Object -Property Files -Sum).Sum
$testLoc = ($testSummary | Measure-Object -Property LoC -Sum).Sum
$totalFiles = [long]$appFiles + [long]$testFiles
$totalLoc = [long]$appLoc + [long]$testLoc

$appPct = if ($totalLoc -eq 0) { 0.0 } else { ([double]$appLoc / [double]$totalLoc) * 100.0 }
$testPct = if ($totalLoc -eq 0) { 0.0 } else { ([double]$testLoc / [double]$totalLoc) * 100.0 }

$commitHash = (& git rev-parse --short $CommitRef).Trim()
$commitDate = (& git show -s --format=%cs $CommitRef).Trim()

$entryLines = [System.Collections.Generic.List[string]]::new()
$entryLines.Add("### $commitDate | commit $commitHash")
$entryLines.Add("")
$entryLines.Add("Resumen")
$entryLines.Add("")
$entryLines.Add("| Categoria | Ficheros | LoC | % sobre total |")
$entryLines.Add("|---|---:|---:|---:|")
$entryLines.Add("| Codigo de aplicacion | $(Format-Int $appFiles) | $(Format-Int $appLoc) | $(Format-Pct $appPct) |")
$entryLines.Add("| Codigo de pruebas | $(Format-Int $testFiles) | $(Format-Int $testLoc) | $(Format-Pct $testPct) |")
$entryLines.Add("| Total | $(Format-Int $totalFiles) | $(Format-Int $totalLoc) | 100% |")
$entryLines.Add("")
$entryLines.Add("Desglose de codigo de aplicacion")
$entryLines.Add("")
$entryLines.Add("| Proyecto | Ficheros | LoC |")
$entryLines.Add("|---|---:|---:|")

foreach ($row in $appSummary) {
    $entryLines.Add("| $($row.Project) | $(Format-Int $row.Files) | $(Format-Int $row.LoC) |")
}

$entryLines.Add("")
$entryLines.Add("Desglose de pruebas")
$entryLines.Add("")
$entryLines.Add("| Proyecto | Ficheros | LoC |")
$entryLines.Add("|---|---:|---:|")

foreach ($row in $testSummary) {
    $entryLines.Add("| $($row.Project) | $(Format-Int $row.Files) | $(Format-Int $row.LoC) |")
}

$entry = ($entryLines -join "`r`n")

$readmeFullPath = Join-Path $repoRoot $ReadmePath
$readmeContent = Get-Content -LiteralPath $readmeFullPath -Raw
$historyStart = "<!-- LOC_REPORT_HISTORY_START -->"
$historyEnd = "<!-- LOC_REPORT_HISTORY_END -->"

if ($readmeContent -notmatch [regex]::Escape($historyStart) -or $readmeContent -notmatch [regex]::Escape($historyEnd)) {
    throw "No se encontraron los marcadores LOC en README. Esperaba $historyStart y $historyEnd."
}

$pattern = [regex]::new("(?s)" + [regex]::Escape($historyStart) + ".*?" + [regex]::Escape($historyEnd))
$match = [regex]::Match($readmeContent, "(?s)" + [regex]::Escape($historyStart) + "(.*?)" + [regex]::Escape($historyEnd))
$existing = $match.Groups[1].Value.Trim()

$entryHeader = "### $commitDate | commit $commitHash"
if ($existing -match [regex]::Escape($entryHeader)) {
    # Reemplaza la entrada existente para ese commit
    $existing = [regex]::Replace(
        $existing,
        "(?s)" + [regex]::Escape($entryHeader) + ".*?(?=\r?\n###\s\d{4}-\d{2}-\d{2}\s\|\scommit\s[0-9a-f]+|$)",
        $entry
    ).Trim()
    $newBlock = "$historyStart`r`n`r`n$existing`r`n`r`n$historyEnd"
}
else {
    $newBody = if ([string]::IsNullOrWhiteSpace($existing)) { $entry } else { "$entry`r`n`r`n$existing" }
    $newBlock = "$historyStart`r`n`r`n$newBody`r`n`r`n$historyEnd"
}

$updated = $pattern.Replace($readmeContent, $newBlock, 1)
Set-Content -LiteralPath $readmeFullPath -Value $updated -Encoding UTF8

Write-Host "Reporte LoC actualizado en $ReadmePath para commit $commitHash"
