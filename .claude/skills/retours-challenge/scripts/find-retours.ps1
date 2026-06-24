#requires -Version 7
<#
.SYNOPSIS
  Localise le fichier de retours (NN-retours.md) d'un dossier de scénarios et détecte
  la présence des sections IHM / Tech. Sortie JSON consommée par la command /4-retours.

.DESCRIPTION
  - Si -Dossier est fourni : cherche le *-retours.md de plus grand préfixe NN dans ce dossier.
  - Sinon : balaie docs/sprints/*/ et retient le dossier dont le *-retours.md est le plus
    récemment modifié.
  Détecte les sections via les en-têtes markdown `## ...` : un en-tête contenant « IHM »
  → hasIHM ; contenant « Tech » → hasTech (le bypass AskUser s'appuie sur hasTech=false).
  Renvoie aussi le chemin du backlog à écrire : `99-sprint<NN>-besoins-fin-itération.md`
  (<NN> = numéro du sprint = préfixe 2 chiffres du dossier de sprint, ex. dossier
  `02-...` -> `99-sprint02-besoins-fin-itération.md` ; préfixe 99 = tri en fin de dossier,
  un backlog de fin d'itération), et le prochain préfixe libre pour info.

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

# Le glob `*-retours.md` ci-dessous cherche le RETOURS PRODUIT du PO (NN-retours.md).
# Or le dossier de sprint contient aussi un JOURNAL MÉTHODE `99-sprint<NN>-retours.md`
# (retours sur les agents/skills/commands, consommé par retro-sprint) qui matche le même
# glob mais n'est PAS un retours produit. On l'exclut explicitement, ainsi que par
# sécurité tout `99-*-retours.md`, pour ne jamais le prendre pour le retours produit.
function Test-IsRetoursProduit([string]$name) {
  if ($name -ilike '99-sprint*-retours.md') { return $false }
  if ($name -ilike '99-*-retours.md')       { return $false }
  return $true
}

# 1. Résoudre le dossier + le fichier de retours
$retoursFile = $null
if ($Dossier) {
  if (-not (Test-Path $Dossier)) { throw "Dossier introuvable : $Dossier" }
  $retoursFile = Get-ChildItem -Path $Dossier -Filter '*-retours.md' -File |
    Where-Object { Test-IsRetoursProduit $_.Name } |
    Sort-Object { Get-Prefix $_.Name } -Descending | Select-Object -First 1
} else {
  $candidates = Get-ChildItem -Path 'docs/sprints' -Directory -ErrorAction SilentlyContinue |
    ForEach-Object {
      Get-ChildItem -Path $_.FullName -Filter '*-retours.md' -File -ErrorAction SilentlyContinue
    } | Where-Object { Test-IsRetoursProduit $_.Name }
  $retoursFile = $candidates | Sort-Object LastWriteTime -Descending | Select-Object -First 1
  if ($retoursFile) { $Dossier = $retoursFile.Directory.FullName }
}

if (-not $retoursFile) {
  [pscustomobject]@{ found = $false; dossier = $Dossier } | ConvertTo-Json -Compress
  return
}

# 2. Détecter les sections IHM / Tech via les en-têtes markdown
$content  = Get-Content -Path $retoursFile.FullName -Raw
$headers  = [regex]::Matches($content, '(?m)^##\s+(.+?)\s*$') | ForEach-Object { $_.Groups[1].Value.Trim() }
$hasIHM   = [bool]($headers | Where-Object { $_ -match '(?i)IHM' })
$hasTech  = [bool]($headers | Where-Object { $_ -match '(?i)tech' })

# 3. Prochain préfixe libre du dossier (pour NN-besoins.md)
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
