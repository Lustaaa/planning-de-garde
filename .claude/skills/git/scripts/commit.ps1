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
# git émet ses chemins en UTF-8 ; sans cela PowerShell les décode dans la code page
# console et corrompt les caractères accentués (ex. dépôt « privée »), faisant échouer
# Set-Location. -LiteralPath fiabilise le cd vers les chemins à caractères spéciaux.
$OutputEncoding = [Console]::OutputEncoding = [Text.UTF8Encoding]::new($false)
Set-Location -LiteralPath (git rev-parse --show-toplevel).Trim()

$branch = (git branch --show-current).Trim()
# HEAD détaché (git branch --show-current vide) : un `git checkout <sha>` concurrent a pu
# détacher HEAD hors de la branche de travail → refuser plutôt que committer dans le vide
# (friction réelle s31 : process/agent concurrent basculé hors branche pendant un scénario).
if (-not $branch) {
    Write-Error "HEAD détaché (aucune branche courante). Rebranche-toi (git switch ia-…) avant de committer."
    exit 1
}
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

# Garde anti-index-pollué (friction réelle s31) : un process/agent concurrent a pu PRÉ-STAGER
# des fichiers étrangers dans l'index. Signale-les — le commit ci-dessous étant SCOPÉ aux
# pathspecs -Files, ils ne seront PAS happés, mais l'opérateur doit le savoir.
$dejaStages = git diff --cached --name-only
if ($dejaStages) {
    Write-Host "⚠️ Index déjà peuplé avant staging (pré-stagé par un autre process ?) :" -ForegroundColor Yellow
    $dejaStages | ForEach-Object { Write-Host "    $_" -ForegroundColor Yellow }
    Write-Host "    → le commit reste SCOPÉ à -Files ; ces fichiers ne seront committés que s'ils y figurent." -ForegroundColor Yellow
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

# Commit SCOPÉ aux pathspecs -Files (`-- @Files`) : un `git commit` sans pathspec committe
# TOUT l'index, y compris un éventuel fichier étranger pré-stagé par un process concurrent
# (friction réelle s31). Scoper garantit qu'on ne committe QUE nos fichiers.
git commit -m $finalMessage -- @Files
Write-Host ''
git log --oneline -1
