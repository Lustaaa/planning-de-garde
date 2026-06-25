#requires -Version 7
<#
.SYNOPSIS
  Commit avec staging sélectif. Refuse main/master. Ajoute le trailer Co-Authored-By.
.PARAMETER Message
  Message de commit (sujet + corps). Le trailer est ajouté s'il manque.
.PARAMETER Files
  Fichiers/chemins à stager. OBLIGATOIRE — jamais de `git add -A`.
  ⚠️ Se passe **séparés par VIRGULES, sans espace** : `-Files a.cs,b.cs,c.cs`.
  La forme séparée par espaces (`-Files a.cs b.cs`) ÉCHOUE (« A positional parameter
  cannot be found ») car PowerShell traite les tokens suivants comme des arguments
  positionnels — c'est la friction du sprint 03, désormais documentée.
.EXAMPLE
  pwsh .claude/skills/git/scripts/commit.ps1 -Message "fix: corrige X" -Files a.cs,b.cs,c.cs
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory)][string]$Message,
    [Parameter(Mandatory)][string[]]$Files
)

$ErrorActionPreference = 'Stop'
Set-Location (git rev-parse --show-toplevel)

$branch = (git branch --show-current).Trim()
if ($branch -in @('main', 'master')) {
    Write-Error "Commit interdit sur '$branch'. Crée une branche : branch.ps1 -Type … -Slug …"
    exit 1
}

# Via `pwsh -File`, un [string[]] arrive comme chaîne unique : redécoupe sur virgules.
$Files = $Files | ForEach-Object { $_ -split ',' } | ForEach-Object { $_.Trim() } | Where-Object { $_ }

if (-not $Files -or $Files.Count -eq 0) {
    Write-Error "Aucun fichier fourni. Staging sélectif obligatoire (pas de git add -A)."
    exit 1
}

# Staging sélectif.
git add -- @Files

# Rien de stagé ? (fichiers inchangés)
if (-not (git diff --cached --name-only)) {
    Write-Error "Rien à committer après staging des fichiers fournis."
    exit 1
}

# Ajoute le trailer Co-Authored-By s'il est absent.
$trailer = 'Co-Authored-By: Claude Opus 4.8 (1M context) <noreply@anthropic.com>'
$finalMessage = $Message
if ($Message -notmatch [regex]::Escape('Co-Authored-By: Claude')) {
    $finalMessage = "$Message`n`n$trailer"
}

git commit -m $finalMessage
Write-Host ''
git log --oneline -1
