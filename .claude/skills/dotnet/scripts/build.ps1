#requires -Version 7
<#
.SYNOPSIS
  Build de la solution complète, sortie JSON COMPACTE — au lieu de la sortie verbeuse de
  `dotnet build`.

.DESCRIPTION
  Recompile TOUS les projets de la solution (jamais un sous-ensemble : un projet de prod
  non recompilé peut être cassé et le vert ment, cf. Sc.1 s07). Renvoie
  `{ green, errors, warnings, exitCode }`. Sur rouge, ajoute `messages` (lignes d'erreur
  plafonnées).

.PARAMETER Solution
  Chemin de la solution/projet (défaut : PlanningDeGarde.slnx à la racine).

.EXAMPLE
  pwsh -NoProfile -File .claude/skills/dotnet/scripts/build.ps1
#>
[CmdletBinding()]
param(
    [string]$Solution = 'PlanningDeGarde.slnx'
)

$ErrorActionPreference = 'Stop'
$OutputEncoding = [Console]::OutputEncoding = [Text.UTF8Encoding]::new($false)
Set-Location -LiteralPath (git rev-parse --show-toplevel).Trim()

# Hôtes Api/Web résiduels → verrous DLL (MSB3027). Même ciblage que test.ps1.
$zombies = Get-CimInstance Win32_Process -Filter "Name = 'PlanningDeGarde.Api.exe' OR Name = 'PlanningDeGarde.Web.exe' OR Name = 'dotnet.exe'" -ErrorAction SilentlyContinue |
    Where-Object { $_.CommandLine -match 'PlanningDeGarde\.(Api|Web)' }
foreach ($z in $zombies) { Stop-Process -Id $z.ProcessId -Force -ErrorAction SilentlyContinue }
if ($zombies) { Start-Sleep -Milliseconds 500 }

$raw = & dotnet build $Solution --nologo 2>&1
$exit = $LASTEXITCODE
$lines = $raw -split "`r?`n"

# Résumé FR : « N Avertissement(s) » / « N Erreur(s) » — EN : « N Warning(s) » / « N Error(s) ».
$errors = 0; $warnings = 0
foreach ($line in $lines) {
    if ($line -match '(\d+)\s+(Erreur|Error)') { $errors = [int]$Matches[1] }
    if ($line -match '(\d+)\s+(Avertissement|Warning)') { $warnings = [int]$Matches[1] }
}

$green = ($exit -eq 0)
$result = [ordered]@{
    green    = $green
    errors   = $errors
    warnings = $warnings
    exitCode = $exit
}

if (-not $green) {
    $errLines = $lines | Where-Object { $_ -match '(?i):\s*(error|erreur)\b' }
    $result.messages = @($errLines | Select-Object -First 40)
}

$result | ConvertTo-Json -Depth 4 -Compress
exit 0
