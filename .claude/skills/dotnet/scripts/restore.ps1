#requires -Version 7
<#
.SYNOPSIS
  Restaure les paquets NuGet de la solution, sortie JSON COMPACTE.

.DESCRIPTION
  Wrapper `dotnet restore` renvoyant `{ green, exitCode }` (+ `messages` plafonnées sur rouge).
  Utile au scaffolding et après changement de dépendances avant un build/test.

.PARAMETER Solution
  Chemin de la solution/projet (défaut : PlanningDeGarde.slnx à la racine).

.EXAMPLE
  pwsh -NoProfile -File .claude/skills/dotnet/scripts/restore.ps1
#>
[CmdletBinding()]
param(
    [string]$Solution = 'PlanningDeGarde.slnx'
)

$ErrorActionPreference = 'Stop'
$OutputEncoding = [Console]::OutputEncoding = [Text.UTF8Encoding]::new($false)
Set-Location -LiteralPath (git rev-parse --show-toplevel).Trim()

$raw = & dotnet restore $Solution --nologo 2>&1
$exit = $LASTEXITCODE
$lines = $raw -split "`r?`n"

$green = ($exit -eq 0)
$result = [ordered]@{ green = $green; exitCode = $exit }
if (-not $green) {
    $errLines = $lines | Where-Object { $_ -match '(?i)\b(error|erreur)\b' }
    $result.messages = @($errLines | Select-Object -First 30)
}

$result | ConvertTo-Json -Depth 3 -Compress
exit 0
