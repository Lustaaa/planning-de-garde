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
  JSON : { found, currentSpec, currentVersion, nextSpec, nextVersion, readmePropagated }

.PARAMETER PropagateReadme
  Réécrit MÉCANIQUEMENT, dans README.md, le pointeur « Spec courante » vers la version
  courante (plus grand NN) et la ligne « versions précédentes » vers NN-1. Sert l'étape
  Propagation de /5-consolidation, pour qu'elle ne soit jamais oubliée (cf. rétro sprint 03).
#>
[CmdletBinding()]
param(
  [string]$DocsDir = 'docs',
  [switch]$PropagateReadme
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
$currentName  = ('{0:D2}-specification.md' -f $currentVer)
$prevName     = ('{0:D2}-specification.md' -f ($currentVer - 1))

# --- Propagation README (étape mécanique de /5-consolidation) ---
$readmePropagated = $false
if ($PropagateReadme) {
  $readme = 'README.md'
  if (Test-Path $readme) {
    $lines = Get-Content $readme
    $out = foreach ($l in $lines) {
      if ($l -match '^📄 Spec courante :') {
        "📄 Spec courante : [``docs/$currentName``](docs/$currentName)"
      } elseif ($l -match 'restent figées en historique') {
        "*(les versions précédentes, ex. [``docs/$prevName``](docs/$prevName), restent figées en historique)*"
      } else { $l }
    }
    Set-Content -Path $readme -Value $out -Encoding utf8NoBOM
    $readmePropagated = $true
  }
}

[pscustomobject]@{
  found            = $true
  currentSpec      = (Resolve-Path $current.FullName).Path
  currentVersion   = ('{0:D2}' -f $currentVer)
  nextSpec         = Join-Path (Resolve-Path $DocsDir).Path $nextName
  nextVersion      = ('{0:D2}' -f $nextVer)
  readmePropagated = $readmePropagated
} | ConvertTo-Json -Compress
