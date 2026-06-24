#requires -Version 7
<#
.SYNOPSIS
  Clôt une itération : déplace les fichiers de scénario (NN-slug.md) d'un dossier de
  suivi dans un sous-dossier `archive/`, ne laissant à la racine que les artefacts de
  pilotage : `00-suivi.md`, le(s) `*-retours.md`, et `99-besoins-fin-itération.md`.

.DESCRIPTION
  Appelé en fin de /4-retours, une fois le backlog écrit. Met aussi à jour les liens
  Markdown de `00-suivi.md` qui pointaient vers les scénarios déplacés (`](NN-slug.md)`
  → `](archive/NN-slug.md)`) pour garder le tableau de bord navigable. Idempotent : si
  rien n'est à archiver, ne fait rien.

.OUTPUTS
  JSON : { dossier, archiveDir, archived[], kept[], suiviLinksUpdated }
#>
[CmdletBinding()]
param(
  [Parameter(Mandatory)] [string]$Dossier
)

$ErrorActionPreference = 'Stop'
if (-not (Test-Path $Dossier)) { throw "Dossier introuvable : $Dossier" }
$Dossier = (Resolve-Path $Dossier).Path

# Artefacts de pilotage gardés à la racine
function Test-Kept([string]$name) {
  return ($name -ieq '00-suivi.md') -or
         ($name -ilike '*-retours.md') -or
         ($name -ieq '99-besoins-fin-itération.md')
}

$archiveDir = Join-Path $Dossier 'archive'

$toArchive = Get-ChildItem -Path $Dossier -Filter '*.md' -File |
  Where-Object { -not (Test-Kept $_.Name) }

$archived = @()
if ($toArchive) {
  if (-not (Test-Path $archiveDir)) { New-Item -ItemType Directory -Path $archiveDir | Out-Null }
  foreach ($f in $toArchive) {
    Move-Item -Path $f.FullName -Destination (Join-Path $archiveDir $f.Name) -Force
    $archived += $f.Name
  }
}

# Réécrit les liens de 00-suivi.md vers les fichiers déplacés
$suiviLinksUpdated = $false
$suivi = Join-Path $Dossier '00-suivi.md'
if ($archived.Count -gt 0 -and (Test-Path $suivi)) {
  $content = Get-Content -Path $suivi -Raw
  $updated = $content
  foreach ($name in $archived) {
    $updated = $updated -replace "\]\($([regex]::Escape($name))\)", "](archive/$name)"
  }
  if ($updated -ne $content) {
    Set-Content -Path $suivi -Value $updated -NoNewline
    $suiviLinksUpdated = $true
  }
}

$kept = Get-ChildItem -Path $Dossier -Filter '*.md' -File | ForEach-Object { $_.Name }

[pscustomobject]@{
  dossier           = $Dossier
  archiveDir        = $archiveDir
  archived          = @($archived)
  kept              = @($kept)
  suiviLinksUpdated = $suiviLinksUpdated
} | ConvertTo-Json -Compress
