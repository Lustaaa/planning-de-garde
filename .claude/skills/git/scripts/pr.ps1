#requires -Version 7
<#
.SYNOPSIS
  Crée une pull request via gh, base main. Pousse d'abord si nécessaire.
  Ajoute le trailer "Generated with Claude Code" au corps.
.PARAMETER Title
  Titre de la PR.
.PARAMETER Body
  Corps de la PR (markdown). Le trailer est ajouté s'il manque.
.PARAMETER Draft
  Crée la PR en brouillon.
.EXAMPLE
  pwsh .claude/skills/git/scripts/pr.ps1 -Title "Skill git" -Body "Ajoute le skill git."
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory)][string]$Title,
    [Parameter(Mandatory)][string]$Body,
    [switch]$Draft
)

$ErrorActionPreference = 'Stop'
# git émet ses chemins en UTF-8 ; sans cela PowerShell les décode dans la code page
# console et corrompt les caractères accentués (ex. dépôt « privée »), faisant échouer
# Set-Location. -LiteralPath fiabilise le cd vers les chemins à caractères spéciaux.
$OutputEncoding = [Console]::OutputEncoding = [Text.UTF8Encoding]::new($false)
Set-Location -LiteralPath (git rev-parse --show-toplevel).Trim()

# Résout gh : PATH d'abord, sinon emplacements d'installation usuels.
$gh = (Get-Command gh -ErrorAction SilentlyContinue)?.Source
if (-not $gh) {
    $candidates = @(
        "$env:ProgramFiles\GitHub CLI\gh.exe",
        "${env:ProgramFiles(x86)}\GitHub CLI\gh.exe",
        "$env:LOCALAPPDATA\Programs\GitHub CLI\gh.exe"
    )
    $gh = $candidates | Where-Object { Test-Path $_ } | Select-Object -First 1
}
if (-not $gh) {
    Write-Error "gh (GitHub CLI) introuvable (ni dans PATH ni aux emplacements usuels)."
    exit 1
}

$branch = (git branch --show-current).Trim()
if ($branch -in @('main', 'master')) {
    Write-Error "Une PR se crée depuis une branche de travail, pas '$branch'."
    exit 1
}

# S'assure que la branche est poussée (upstream défini).
git rev-parse --abbrev-ref --symbolic-full-name '@{u}' 2>$null | Out-Null
if ($LASTEXITCODE -ne 0) {
    git push -u origin $branch
}

# Ajoute le trailer Claude Code s'il manque.
$trailer = "🤖 Generated with [Claude Code](https://claude.com/claude-code)"
$finalBody = $Body
if ($Body -notmatch [regex]::Escape('Generated with [Claude Code]')) {
    $finalBody = "$Body`n`n$trailer"
}

$ghArgs = @('pr', 'create', '--base', 'main', '--title', $Title, '--body', $finalBody)
if ($Draft) { $ghArgs += '--draft' }

& $gh @ghArgs
