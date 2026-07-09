#requires -Version 7
<#
.SYNOPSIS
  Gate anti-bypass de la rétrospective. Détecte le dernier sprint CLOS (incrément livré,
  besoins /4-retours écrits) qui n'a PAS encore de 98-retrospective.md.

.DESCRIPTION
  Garde-fou d'amélioration continue : un nouveau cycle (/2-make-gherkin) ne doit pas
  démarrer tant que la rétrospective du sprint précédent n'a pas tourné.

  Un sprint est « clos non-rétrospecté » si son dossier docs/sprints/<NN>-<sujet>/ contient :
    - un 99-sprint<NN>-besoins-fin-itération.md REMPLI (sortie de /4-retours, pas le
      placeholder vide scaffolddé par tdd-analyse), preuve que l'itération est close,
    - MAIS PAS de 98-retrospective.md (la rétro n'a pas tourné).

  Le « dernier » = plus grand préfixe NN. found=true ⇒ il existe un sprint à rétrospecter
  AVANT de continuer ; le thread principal doit lancer retro-sprint sur retroPath.

.OUTPUTS
  JSON : { gateOpen, lastClosedSprint, hasRetro, retroPath, besoinsPath, reason }
    gateOpen=true  → aucun sprint clos en attente de rétro : on peut continuer.
    gateOpen=false → un sprint clos n'a pas de 98-retrospective.md : BLOQUER.
#>
[CmdletBinding()]
param(
  [string]$SprintsDir = 'docs/sprints'
)

$ErrorActionPreference = 'Stop'
# git émet ses chemins en UTF-8 ; sans cela PowerShell les décode dans la code page
# console et corrompt les caractères accentués (ex. dépôt « privée »), faisant échouer
# Set-Location. -LiteralPath fiabilise le cd vers les chemins à caractères spéciaux.
$OutputEncoding = [Console]::OutputEncoding = [Text.UTF8Encoding]::new($false)
Set-Location -LiteralPath (git rev-parse --show-toplevel).Trim()

function Get-Prefix([string]$name) {
  if ($name -match '^(\d{2})-') { return [int]$Matches[1] } else { return -1 }
}

if (-not (Test-Path $SprintsDir)) {
  [pscustomobject]@{ gateOpen = $true; reason = "Aucun dossier $SprintsDir." } | ConvertTo-Json -Compress
  return
}

# Dossiers de sprint (préfixe NN-), du plus récent au plus ancien.
$dirs = Get-ChildItem -Path $SprintsDir -Directory |
  Where-Object { (Get-Prefix $_.Name) -ge 0 } |
  Sort-Object { Get-Prefix $_.Name } -Descending

foreach ($d in $dirs) {
  $nn = '{0:D2}' -f (Get-Prefix $d.Name)
  # Le backlog (et la rétro) peuvent vivre à la racine du dossier OU sous `archive/` :
  # /6-cloture-sprint archive en fin de course tous les .md de pilotage SAUF le suivi.
  # On cherche donc en récursif (-Recurse couvre racine + archive/).
  $besoins = Get-ChildItem -Path $d.FullName -Filter '99-sprint*-besoins-fin-itération.md' -File -Recurse -ErrorAction SilentlyContinue | Select-Object -First 1
  if (-not $besoins) { continue }  # itération pas encore close (pas passée par /4-retours)

  # Placeholder vide (scaffolddé par tdd-analyse) ≠ backlog rempli par /4-retours.
  $content = Get-Content $besoins.FullName -Raw
  $isPlaceholder = $content -match 'Placeholder\s+—\s+\*\*rempli par'
  if ($isPlaceholder) { continue }  # /4-retours pas encore passé : itération non close

  # Sprint clos : a-t-il sa rétro ? (racine ou archive/ après clôture)
  $retro = Join-Path $d.FullName '98-retrospective.md'
  $retroArchive = Join-Path $d.FullName 'archive/98-retrospective.md'
  $hasRetro = (Test-Path $retro) -or (Test-Path $retroArchive)

  [pscustomobject]@{
    gateOpen         = $hasRetro
    lastClosedSprint = $d.Name
    hasRetro         = $hasRetro
    retroPath        = (Join-Path $d.FullName '98-retrospective.md')
    besoinsPath      = (Resolve-Path $besoins.FullName).Path
    reason           = if ($hasRetro) {
        "Sprint clos $($d.Name) déjà rétrospecté (98-retrospective.md présent)."
      } else {
        "BLOQUER : sprint clos $($d.Name) sans 98-retrospective.md. Lancer retro-sprint avant tout nouveau cycle /2-make-gherkin."
      }
  } | ConvertTo-Json -Compress
  return
}

# Aucun sprint clos trouvé.
[pscustomobject]@{ gateOpen = $true; reason = 'Aucun sprint clos (avec besoins /4-retours remplis) en attente de rétro.' } | ConvertTo-Json -Compress
