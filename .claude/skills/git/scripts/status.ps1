#requires -Version 7
<#
.SYNOPSIS
  État du dépôt : branche courante, ahead/behind origin, fichiers modifiés.
.EXAMPLE
  pwsh .claude/skills/git/scripts/status.ps1
#>
[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
# git émet ses chemins en UTF-8 ; sans cela PowerShell les décode dans la code page
# console et corrompt les caractères accentués (ex. dépôt « privée »), faisant échouer
# Set-Location. -LiteralPath fiabilise le cd vers les chemins à caractères spéciaux.
$OutputEncoding = [Console]::OutputEncoding = [Text.UTF8Encoding]::new($false)
Set-Location -LiteralPath (git rev-parse --show-toplevel).Trim()

$branch = (git branch --show-current).Trim()
Write-Host "Branche : $branch"

# ahead/behind vs upstream (si défini)
$upstream = git rev-parse --abbrev-ref --symbolic-full-name '@{u}' 2>$null
if ($LASTEXITCODE -eq 0 -and $upstream) {
    $counts = git rev-list --left-right --count "$upstream...HEAD" 2>$null
    if ($counts) {
        $behind, $ahead = $counts -split '\s+'
        Write-Host "Upstream : $upstream  (ahead $ahead / behind $behind)"
    }
} else {
    Write-Host "Upstream : (aucun — branche jamais poussée)"
}

Write-Host ''
$changes = git status --short
if ($changes) {
    Write-Host 'Modifications :'
    $changes | ForEach-Object { Write-Host "  $_" }
} else {
    Write-Host 'Arbre propre.'
}
