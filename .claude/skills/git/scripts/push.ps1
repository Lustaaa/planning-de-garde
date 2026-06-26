#requires -Version 7
<#
.SYNOPSIS
  Pousse la branche courante. -u origin si jamais poussée, sinon push simple.
  Refuse main/master.
.PARAMETER ReturnToMain
  Revient sur main après le push.
.EXAMPLE
  pwsh .claude/skills/git/scripts/push.ps1
  pwsh .claude/skills/git/scripts/push.ps1 -ReturnToMain
#>
[CmdletBinding()]
param(
    [switch]$ReturnToMain
)

$ErrorActionPreference = 'Stop'
# git émet ses chemins en UTF-8 ; sans cela PowerShell les décode dans la code page
# console et corrompt les caractères accentués (ex. dépôt « privée »), faisant échouer
# Set-Location. -LiteralPath fiabilise le cd vers les chemins à caractères spéciaux.
$OutputEncoding = [Console]::OutputEncoding = [Text.UTF8Encoding]::new($false)
Set-Location -LiteralPath (git rev-parse --show-toplevel).Trim()

$branch = (git branch --show-current).Trim()
if ($branch -in @('main', 'master')) {
    Write-Error "Push direct de '$branch' interdit. Pousse une branche de travail ia-*."
    exit 1
}

# Upstream déjà défini ?
git rev-parse --abbrev-ref --symbolic-full-name '@{u}' 2>$null | Out-Null
if ($LASTEXITCODE -eq 0) {
    git push
} else {
    git push -u origin $branch
}

Write-Host "Branche $branch poussée."

if ($ReturnToMain) {
    if (git status --porcelain) {
        Write-Warning "Arbre non propre — reste sur $branch."
    } else {
        git checkout main
        Write-Host "Retour sur main."
    }
}
