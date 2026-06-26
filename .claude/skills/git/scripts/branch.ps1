#requires -Version 7
<#
.SYNOPSIS
  Crée une branche de travail ia-{TYPE}/{slug} depuis main à jour.
.PARAMETER Type
  fix | feat | refactor | test | chore
.PARAMETER Slug
  Description courte en kebab-case (sera normalisée).
.EXAMPLE
  pwsh .claude/skills/git/scripts/branch.ps1 -Type refactor -Slug "git skill scripts"
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory)][ValidateSet('fix', 'feat', 'refactor', 'test', 'chore')]
    [string]$Type,
    [Parameter(Mandatory)][string]$Slug
)

$ErrorActionPreference = 'Stop'
# git émet ses chemins en UTF-8 ; sans cela PowerShell les décode dans la code page
# console et corrompt les caractères accentués (ex. dépôt « privée »), faisant échouer
# Set-Location. -LiteralPath fiabilise le cd vers les chemins à caractères spéciaux.
$OutputEncoding = [Console]::OutputEncoding = [Text.UTF8Encoding]::new($false)
Set-Location -LiteralPath (git rev-parse --show-toplevel).Trim()

$main = 'main'

if (git status --porcelain) {
    Write-Error "Arbre non propre — commit ou stash avant de créer une branche."
    exit 1
}

# Normalise le slug : minuscules, espaces/_ -> -, supprime le reste, compacte les tirets.
$norm = $Slug.ToLowerInvariant() -replace '[\s_]+', '-' -replace '[^a-z0-9-]', '' -replace '-+', '-'
$norm = $norm.Trim('-')
if (-not $norm) { Write-Error "Slug vide après normalisation."; exit 1 }

$name = "ia-$Type/$norm"

# Met main à jour avant de brancher.
git fetch origin --prune
git fetch origin "${main}:${main}" 2>$null

if (git rev-parse --verify --quiet $name) {
    Write-Error "La branche $name existe déjà."
    exit 1
}

git checkout -b $name $main
Write-Host "Branche créée : $name (depuis $main à jour)."
