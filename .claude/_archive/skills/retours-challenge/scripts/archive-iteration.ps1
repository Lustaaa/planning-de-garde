#requires -Version 7
<#
.SYNOPSIS
  Clôt une itération : déplace les fichiers de scénario (NN-slug.md) d'un dossier de
  suivi dans un sous-dossier `archive/`, ne laissant à la racine que les artefacts de
  pilotage : `00-sprint<NN>-suivi.md`, le fichier UNIFIÉ `99-sprint<NN>-retours.md` (retours
  produit du PO + partie méthode + `## IA`), et `99-sprint<NN>-besoins-fin-itération.md`
  (<NN> = numéro du sprint = préfixe 2 chiffres du dossier de sprint). Les anciens noms
  `00-suivi.md` / `99-besoins-fin-itération.md` et les legacy `NN-retours.md` restent
  reconnus pour compatibilité.

.DESCRIPTION
  Appelé en fin de /4-retours, une fois le backlog écrit. Met aussi à jour les liens
  Markdown du fichier de suivi (`00-sprint<NN>-suivi.md`, ancien nom `00-suivi.md`) qui
  pointaient vers les scénarios déplacés (`](NN-slug.md)` → `](archive/NN-slug.md)`) pour
  garder le tableau de bord navigable. Idempotent : si rien n'est à archiver, ne fait rien.

.OUTPUTS
  JSON : { dossier, archiveDir, archived[], kept[], suiviLinksUpdated }

.PARAMETER Closure
  Mode CLÔTURE (appelé par /6-cloture-sprint, après la rétro). Archive AUSSI les artefacts
  de pilotage du sprint clos (`99-sprint<NN>-retours.md`, `99-sprint<NN>-besoins-fin-itération.md`,
  `98-retrospective.md`), ne laissant à la racine que le seul fichier de suivi
  `00-sprint<NN>-suivi.md` (lisible par les agents). Sans ce switch (mode /4-retours), seuls
  les fichiers de scénario `NN-slug.md` sont déplacés et les artefacts de pilotage restent à
  la racine. Les scripts de détection (find-retro.ps1, cloture-sprint.ps1) cherchent ces
  fichiers à la racine ET sous archive/ pour rester cohérents avec ce mode.
#>
[CmdletBinding()]
param(
  [Parameter(Mandatory)] [string]$Dossier,
  [switch]$Closure
)

$ErrorActionPreference = 'Stop'
if (-not (Test-Path $Dossier)) { throw "Dossier introuvable : $Dossier" }
$Dossier = (Resolve-Path $Dossier).Path

# Artefacts de pilotage gardés à la racine.
# Le suivi et le backlog portent désormais le numéro de sprint dans leur nom
# (`00-sprint<NN>-suivi.md`, `99-sprint<NN>-besoins-fin-itération.md`) ; les anciens
# noms sans sprint restent reconnus pour compatibilité.
# Le fichier UNIFIÉ `99-sprint<NN>-retours.md` porte à la fois le retours produit du PO
# (section `# Retours produit (PO)`, lue par /4-retours) ET la partie méthode + `## IA`
# (lue par retro-sprint) — il doit rester à la racine. Le glob `*-retours.md` couvre
# aussi les legacy `NN-retours.md` d'anciens sprints ; la règle explicite
# `99-sprint<NN>-retours.md` est conservée pour la lisibilité et la robustesse.
function Test-Kept([string]$name) {
  # Le fichier de suivi reste TOUJOURS à la racine (lisible par les agents).
  $isSuivi = ($name -ieq '00-suivi.md') -or ($name -imatch '^00-sprint\d{2}-suivi\.md$')
  if ($Closure) { return $isSuivi }  # mode clôture : on n'archive QUE le suivi en moins
  # Mode /4-retours : on garde aussi les artefacts de pilotage de l'itération.
  return $isSuivi -or
         ($name -ilike '*-retours.md') -or
         ($name -imatch '^99-sprint\d{2}-retours\.md$') -or
         ($name -ieq '99-besoins-fin-itération.md') -or
         ($name -imatch '^99-sprint\d{2}-besoins-fin-itération\.md$')
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

# Réécrit les liens du fichier de suivi vers les fichiers déplacés.
# Le suivi est `00-sprint<NN>-suivi.md` (nouveau) ou `00-suivi.md` (ancien) : on le
# localise par motif, en privilégiant le nom versionné s'il existe.
$suiviLinksUpdated = $false
$suivi = Get-ChildItem -Path $Dossier -Filter '*.md' -File |
  Where-Object { $_.Name -imatch '^00-sprint\d{2}-suivi\.md$' -or $_.Name -ieq '00-suivi.md' } |
  Sort-Object { $_.Name -ieq '00-suivi.md' } |   # $false (versionné) avant $true (ancien)
  Select-Object -First 1 -ExpandProperty FullName
if ($archived.Count -gt 0 -and $suivi -and (Test-Path $suivi)) {
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
