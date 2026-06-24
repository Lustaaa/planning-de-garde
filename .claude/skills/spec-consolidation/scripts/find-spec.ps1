#requires -Version 7
<#
.SYNOPSIS
  Localise la version courante de la spec (`NN-specification.md` de plus grand préfixe
  sous docs/) et calcule la cible de la prochaine version. Sortie JSON pour /5-consolidation.

.DESCRIPTION
  Convention de versionnage : la spec courante = le `NN-specification.md` de plus grand
  préfixe `NN` dans `docs/` (le pointeur « version courante » EST le plus grand numéro).
  La consolidation produit `<NN+1>-specification.md` ; l'ancienne reste figée en historique.

.OUTPUTS
  JSON : { found, currentSpec, currentVersion, nextSpec, nextVersion }
#>
[CmdletBinding()]
param(
  [string]$DocsDir = 'docs'
)

$ErrorActionPreference = 'Stop'
if (-not (Test-Path $DocsDir)) { throw "Répertoire docs introuvable : $DocsDir" }

function Get-Prefix([string]$name) {
  if ($name -match '^(\d{2})-') { return [int]$Matches[1] } else { return -1 }
}

$specs = Get-ChildItem -Path $DocsDir -Filter '*-specification.md' -File |
  Where-Object { (Get-Prefix $_.Name) -ge 0 } |
  Sort-Object { Get-Prefix $_.Name } -Descending

if (-not $specs) {
  [pscustomobject]@{ found = $false; docsDir = (Resolve-Path $DocsDir).Path } | ConvertTo-Json -Compress
  return
}

$current      = $specs[0]
$currentVer   = Get-Prefix $current.Name
$nextVer      = $currentVer + 1
$nextName     = ('{0:D2}-specification.md' -f $nextVer)

[pscustomobject]@{
  found          = $true
  currentSpec    = (Resolve-Path $current.FullName).Path
  currentVersion = ('{0:D2}' -f $currentVer)
  nextSpec       = Join-Path (Resolve-Path $DocsDir).Path $nextName
  nextVersion    = ('{0:D2}' -f $nextVer)
} | ConvertTo-Json -Compress
