#requires -Version 7
<#
.SYNOPSIS
  Met à jour la branche par défaut (main) depuis origin, sans la checkouter.
  Option : rebase la branche courante sur main mis à jour.
.PARAMETER RebaseCurrent
  Après mise à jour de main, rebase la branche de travail courante dessus.
.EXAMPLE
  pwsh .claude/skills/git/scripts/sync.ps1
  pwsh .claude/skills/git/scripts/sync.ps1 -RebaseCurrent
#>
[CmdletBinding()]
param(
    [switch]$RebaseCurrent
)

$ErrorActionPreference = 'Stop'
Set-Location (git rev-parse --show-toplevel)

$main = 'main'
$branch = (git branch --show-current).Trim()

# Arbre propre exigé pour toute opération de sync.
if (git status --porcelain) {
    Write-Error "Arbre non propre — commit ou stash avant de synchroniser."
    exit 1
}

git fetch origin --prune

if ($branch -eq $main) {
    git pull --rebase origin $main
} else {
    # Fast-forward du ref local main sans le checkouter.
    git fetch origin "${main}:${main}"
    Write-Host "Branche $main mise à jour depuis origin/$main."
    if ($RebaseCurrent) {
        Write-Host "Rebase de $branch sur $main…"
        git rebase $main
    }
}

Write-Host 'Sync terminé.'
