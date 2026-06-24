#requires -Version 7
<#
.SYNOPSIS
  Localise le fichier de retours produit (PO) d'un dossier de scénarios et détecte la
  présence des sections IHM / Tech. Sortie JSON consommée par la command /4-retours.

.DESCRIPTION
  Le retours produit du PO vit désormais dans le fichier UNIFIÉ `99-sprint<NN>-retours.md`
  (<NN> = préfixe 2 chiffres du dossier de sprint). Ce fichier porte à la fois le retours
  produit (section `# Retours produit (PO)`, lue par /4-retours) ET la partie méthode +
  `## IA` (lue par retro-sprint). Ce script CIBLE ce fichier (il ne l'exclut plus).

  - Si -Dossier est fourni : construit/trouve `99-sprint<NN>-retours.md` dans ce dossier
    (NN = préfixe 2 chiffres du dossier). Si absent, fallback legacy : plus grand préfixe
    `NN-retours.md` (anciens sprints).
  - Sinon : balaie docs/sprints/*/ et retient le dossier dont le fichier de retours est le
    plus récemment modifié (en privilégiant `99-sprint<NN>-retours.md`).

  Détection des sections (bypass Tech) : restreinte à la section `# Retours produit (PO)`
  du fichier unifié — un en-tête `## ...` contenant « IHM » → hasIHM ; « Tech » → hasTech.
  Les sections `# Méthode (agents)`, `## IA`, `## Notes de contexte` sont IGNORÉES (elles
  relèvent de retro-sprint, pas du retours produit). Pour un legacy `NN-retours.md` sans
  section `# Retours produit (PO)`, on retombe sur tout le fichier.

  Renvoie aussi le chemin du backlog à écrire : `99-sprint<NN>-besoins-fin-itération.md`.

.OUTPUTS
  JSON : { found, retoursPath, dossier, hasIHM, hasTech, sections[], nextPrefix, nextBesoins }
#>
[CmdletBinding()]
param(
  [string]$Dossier
)

$ErrorActionPreference = 'Stop'

function Get-Prefix([string]$name) {
  if ($name -match '^(\d{2})-') { return [int]$Matches[1] } else { return -1 }
}

# Cible primaire : le fichier unifié `99-sprint<NN>-retours.md` (NN = préfixe du dossier).
# Fallback legacy : un ancien `NN-retours.md` produit (hors `99-sprint*`) encore présent.
function Resolve-RetoursFile([string]$dir) {
  $dirName = Split-Path -Leaf (Resolve-Path $dir).Path
  if ($dirName -match '^(\d{2})-') {
    $unified = Join-Path $dir "99-sprint$($Matches[1])-retours.md"
    if (Test-Path $unified) { return (Get-Item $unified) }
  }
  # Fallback : tout `99-sprint*-retours.md` présent (autre numérotation), puis legacy.
  $unifiedAny = Get-ChildItem -Path $dir -Filter '99-sprint*-retours.md' -File -ErrorAction SilentlyContinue |
    Sort-Object { Get-Prefix $_.Name } -Descending | Select-Object -First 1
  if ($unifiedAny) { return $unifiedAny }
  # Legacy : ancien retours produit `NN-retours.md` (préfixe à 2 chiffres, hors 99-sprint).
  return Get-ChildItem -Path $dir -Filter '*-retours.md' -File -ErrorAction SilentlyContinue |
    Where-Object { $_.Name -notlike '99-sprint*-retours.md' } |
    Sort-Object { Get-Prefix $_.Name } -Descending | Select-Object -First 1
}

# 1. Résoudre le dossier + le fichier de retours
$retoursFile = $null
if ($Dossier) {
  if (-not (Test-Path $Dossier)) { throw "Dossier introuvable : $Dossier" }
  $retoursFile = Resolve-RetoursFile $Dossier
} else {
  $candidates = Get-ChildItem -Path 'docs/sprints' -Directory -ErrorAction SilentlyContinue |
    ForEach-Object { Resolve-RetoursFile $_.FullName } |
    Where-Object { $_ }
  $retoursFile = $candidates | Sort-Object LastWriteTime -Descending | Select-Object -First 1
  if ($retoursFile) { $Dossier = $retoursFile.Directory.FullName }
}

if (-not $retoursFile) {
  [pscustomobject]@{ found = $false; dossier = $Dossier } | ConvertTo-Json -Compress
  return
}

# 2. Détecter les sections IHM / Tech via les en-têtes `## ...`, RESTREINT à la section
#    produit `# Retours produit (PO)` du fichier unifié. On découpe le contenu : depuis le
#    H1 `# Retours produit (PO)` jusqu'au prochain H1 (`# Méthode ...`). Pour un legacy sans
#    cette section, on retombe sur tout le fichier.
$content  = Get-Content -Path $retoursFile.FullName -Raw
$produit  = $content
$startM   = [regex]::Match($content, '(?m)^#\s+Retours produit \(PO\)\s*$')
if ($startM.Success) {
  $rest   = $content.Substring($startM.Index + $startM.Length)
  $nextH1 = [regex]::Match($rest, '(?m)^#\s+(?!#)')   # prochain H1 (pas H2/H3)
  $produit = if ($nextH1.Success) { $rest.Substring(0, $nextH1.Index) } else { $rest }
}

$headers  = [regex]::Matches($produit, '(?m)^##\s+(.+?)\s*$') | ForEach-Object { $_.Groups[1].Value.Trim() }
$hasIHM   = [bool]($headers | Where-Object { $_ -match '(?i)IHM' })
$hasTech  = [bool]($headers | Where-Object { $_ -match '(?i)tech' })

# 3. Prochain préfixe libre du dossier (pour info)
$maxPrefix = (Get-ChildItem -Path $Dossier -Filter '*.md' -File |
  ForEach-Object { Get-Prefix $_.Name } | Measure-Object -Maximum).Maximum
if ($null -eq $maxPrefix -or $maxPrefix -lt 0) { $maxPrefix = 0 }
$nextPrefix = '{0:D2}' -f ([int]$maxPrefix + 1)

# 4. Numéro du sprint = préfixe 2 chiffres du dossier de sprint (ex. `02-...` -> `02`).
#    Le backlog est nommé `99-sprint<NN>-besoins-fin-itération.md` pour éviter les
#    collisions d'onglets éditeur entre sprints. Fallback sans préfixe si introuvable.
$dossierName = Split-Path -Leaf (Resolve-Path $Dossier).Path
if ($dossierName -match '^(\d{2})-') {
  $besoinsName = "99-sprint$($Matches[1])-besoins-fin-itération.md"
} else {
  $besoinsName = '99-besoins-fin-itération.md'
}

[pscustomobject]@{
  found       = $true
  retoursPath = (Resolve-Path $retoursFile.FullName).Path
  dossier     = (Resolve-Path $Dossier).Path
  hasIHM      = $hasIHM
  hasTech     = $hasTech
  sections    = @($headers)
  nextPrefix  = $nextPrefix
  nextBesoins = Join-Path (Resolve-Path $Dossier).Path $besoinsName
} | ConvertTo-Json -Compress
