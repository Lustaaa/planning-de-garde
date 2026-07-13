#requires -Version 7
<#
.SYNOPSIS
  Exécute la suite de tests et renvoie un JSON COMPACT — au lieu de la sortie verbeuse de
  `dotnet test` (qui inonde le contexte de l'agent à chaque cycle TDD).

.DESCRIPTION
  Pensé pour l'agent dev-team : le chemin vert ne renvoie qu'un objet
  `{ green, total, passed, failed, skipped, assemblies }`, pas des milliers de lignes.
  Sur rouge, ajoute `failures` (lignes d'échec, plafonnées) pour orienter sans tout déverser.

  Non-régression = suite COMPLÈTE, build COMPLET (jamais --no-build ni filtre projet partiel,
  cf. Sc.1 s07 : sinon le vert ment). C'est le défaut. `-Filter` est réservé à un cycle RED
  ciblé, jamais à une preuve de non-régression.

.PARAMETER Filter
  Filtre xUnit optionnel (`dotnet test --filter`). RED ciblé uniquement.

.PARAMETER Serial
  Sérialise l'exécution des assemblies (`-- RunConfiguration.MaxCpuCount=1`) : les projets de
  test tournent l'un APRÈS l'autre au lieu d'en parallèle. Reste une suite COMPLÈTE / build
  COMPLET (non-régression valide, ne masque AUCUNE régression : un rouge déterministe reste
  rouge en série). Sert au GATE quand le run parallèle montre un rouge *TempsReel* isolé-vert
  (flake de charge SignalR/I/O, dette backlog) : plutôt que re-tourner toute la suite en
  parallèle en espérant un vert (coût 2e/3e run), la relancer en `-Serial` supprime le
  blast-radius de charge parallèle. Le run parallèle reste le défaut (plus rapide).

.PARAMETER Solution
  Chemin de la solution/projet (défaut : PlanningDeGarde.slnx à la racine).

.EXAMPLE
  pwsh -NoProfile -File .claude/skills/dotnet/scripts/test.ps1
.EXAMPLE
  pwsh -NoProfile -File .claude/skills/dotnet/scripts/test.ps1 -Serial
#>
[CmdletBinding()]
param(
    [string]$Filter,
    [switch]$Serial,
    [string]$Solution = 'PlanningDeGarde.slnx'
)

$ErrorActionPreference = 'Stop'
# Chemin du dépôt accentué (…/source/privée/…) : forcer l'UTF-8 sinon le repositionnement
# et le parse des résumés localisés cassent (même précaution que les scripts git).
$OutputEncoding = [Console]::OutputEncoding = [Text.UTF8Encoding]::new($false)
Set-Location -LiteralPath (git rev-parse --show-toplevel).Trim()

# Arrête les hôtes Api/Web résiduels d'un run précédent (lancement de l'app, gate visuel) avant
# `dotnet test` : un hôte zombie verrouille les DLL de sortie et fait échouer le build de la suite
# (MSB3027). On ne tue que les dotnet pointant l'un des projets, pas tous les dotnet.
$zombies = Get-CimInstance Win32_Process -Filter "Name = 'PlanningDeGarde.Api.exe' OR Name = 'PlanningDeGarde.Web.exe' OR Name = 'dotnet.exe'" -ErrorAction SilentlyContinue |
    Where-Object { $_.CommandLine -match 'PlanningDeGarde\.(Api|Web)' }
foreach ($z in $zombies) {
    Stop-Process -Id $z.ProcessId -Force -ErrorAction SilentlyContinue
}
if ($zombies) { Start-Sleep -Milliseconds 500 }  # laisse l'OS relâcher les verrous DLL

$dotnetArgs = @('test', $Solution, '--nologo')
if ($Filter) { $dotnetArgs += @('--filter', $Filter) }
# Sérialisation des assemblies : MaxCpuCount=1 côté RunSettings (après `--`) fait tourner les
# projets de test l'un après l'autre → supprime le blast-radius des flakes *TempsReel* de charge
# parallèle (SignalR/I/O). Ne masque pas une régression : un rouge déterministe reste rouge.
if ($Serial) { $dotnetArgs += @('--', 'RunConfiguration.MaxCpuCount=1') }

$raw = & dotnet @dotnetArgs 2>&1
$exit = $LASTEXITCODE
$lines = $raw -split "`r?`n"

$passed = 0; $failed = 0; $skipped = 0; $total = 0
$assemblies = @()

# Résumé par assembly — FR : « échec : N, réussite : N, ignorée(s) : N, total : N … - X.dll »
# EN : « Failed: N, Passed: N, Skipped: N, Total: N … - X.dll ».
$reFr = 'échec\s*:\s*(\d+).*?réussite\s*:\s*(\d+).*?ignorée\(s\)\s*:\s*(\d+).*?total\s*:\s*(\d+).*?-\s*(\S+\.dll)'
$reEn = 'Failed:\s*(\d+).*?Passed:\s*(\d+).*?Skipped:\s*(\d+).*?Total:\s*(\d+).*?-\s*(\S+\.dll)'

foreach ($line in $lines) {
    $m = [regex]::Match($line, $reFr)
    if (-not $m.Success) { $m = [regex]::Match($line, $reEn) }
    if ($m.Success) {
        $f = [int]$m.Groups[1].Value; $p = [int]$m.Groups[2].Value
        $s = [int]$m.Groups[3].Value; $t = [int]$m.Groups[4].Value
        $failed += $f; $passed += $p; $skipped += $s; $total += $t
        $assemblies += [ordered]@{ dll = $m.Groups[5].Value; passed = $p; failed = $f; skipped = $s; total = $t }
    }
}

$green = ($exit -eq 0 -and $failed -eq 0 -and $total -gt 0)

$result = [ordered]@{
    green      = $green
    total      = $total
    passed     = $passed
    failed     = $failed
    skipped    = $skipped
    exitCode   = $exit
    assemblies = $assemblies
}

if (-not $green) {
    # Sur rouge : remonter les lignes d'échec (plafonnées) pour orienter, pas tout déverser.
    $fail = $lines | Where-Object { $_ -match '(?i)\b(échec|failed|erreur|error)\b' -and $_ -notmatch $reFr -and $_ -notmatch $reEn }
    $result.failures = @($fail | Select-Object -First 40)
    if ($total -eq 0) { $result.note = 'Aucun test exécuté (build cassé ou filtre trop strict ?) — voir failures.' }
}

$result | ConvertTo-Json -Depth 5 -Compress
exit 0
