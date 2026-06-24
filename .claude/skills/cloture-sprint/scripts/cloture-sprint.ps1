#requires -Version 7
<#
.SYNOPSIS
  Clôture de sprint : pousse la branche courante et prépare la Pull Request vers la base
  (main). gh-optionnel : si `gh` est présent, propose la commande de création ; sinon
  émet le titre, le corps de PR (prêt à coller) et l'URL de comparaison GitHub.

.DESCRIPTION
  Étape mécanique du rituel de clôture (command /6-cloture-sprint). Ne merge JAMAIS tout
  seul ni ne crée la PR sans confirmation — il pousse la branche et produit le matériel
  de PR. Le corps est assemblé depuis les commits `base..HEAD` + un résumé du sprint clos.

.OUTPUTS
  JSON : { branch, base, pushed, ahead, commits[], compareUrl, ghPresent, bodyPath, title }
  + le corps de PR écrit dans bodyPath (et affiché si -Print).
#>
[CmdletBinding()]
param(
  [string]$Base = 'main',
  [string]$Sprint,                     # nom du sprint (défaut : déduit du dernier docs/sprints/*)
  [switch]$NoPush,
  [string]$BodyOut                     # chemin de sortie du corps de PR (défaut : scratch)
)

$ErrorActionPreference = 'Stop'

$branch = (git rev-parse --abbrev-ref HEAD).Trim()
if ($branch -eq $Base) { throw "Déjà sur '$Base' — la clôture part d'une branche de sprint, pas de la base." }

# Slug du dépôt depuis origin
$originUrl = (git remote get-url origin).Trim()
$slug = $null
if ($originUrl -match 'github\.com[:/](.+?)(?:\.git)?$') { $slug = $Matches[1] }
$compareUrl = if ($slug) { 'https://github.com/' + $slug + '/compare/' + $Base + '...' + $branch + '?expand=1' } else { $null }

# Commits en avance sur la base
$ahead = @(git log --oneline "$Base..HEAD" 2>$null)
$aheadCount = $ahead.Count

# Sprint clos : dernier dossier docs/sprints/* avec un *-retours.md
if (-not $Sprint) {
  $sprintDir = Get-ChildItem 'docs/sprints' -Directory -ErrorAction SilentlyContinue |
    Where-Object { Get-ChildItem $_.FullName -Filter '*-retours.md' -File -ErrorAction SilentlyContinue } |
    Sort-Object Name -Descending | Select-Object -First 1
  if ($sprintDir) { $Sprint = $sprintDir.Name }
}

# Spec courante
$specName = (Get-ChildItem 'docs' -Filter '*-specification.md' -File |
  Where-Object { $_.Name -match '^\d{2}-' } |
  Sort-Object Name -Descending | Select-Object -First 1).Name

# Push
$pushed = $false
if (-not $NoPush) {
  git push -u origin $branch | Out-Null
  $pushed = $true
}

# Titre + corps de PR
$title = if ($Sprint) { "Sprint $Sprint — clôture" } else { "Clôture de sprint — $branch" }

$commitsBlock = if ($aheadCount -gt 0) { ($ahead | ForEach-Object { "- $_" }) -join "`n" } else { "- (aucun commit en avance sur $Base)" }
$specLine = if ($specName) { "Version courante de la spec : `docs/$specName`." } else { "—" }
$sprintLine = if ($Sprint) { "Sprint clos : `docs/sprints/$Sprint/` (suivi, retours, besoins ; scénarios archivés)." } else { "—" }

$body = @"
## Clôture de sprint$([string]::IsNullOrEmpty($Sprint) ? '' : " — $Sprint")

$sprintLine
$specLine

### Commits ($aheadCount)

$commitsBlock

🤖 Generated with [Claude Code](https://claude.com/claude-code)
"@

if (-not $BodyOut) {
  $scratch = $env:TEMP
  $BodyOut = Join-Path $scratch "pr-body-$branch.md".Replace('/', '-')
}
Set-Content -Path $BodyOut -Value $body -NoNewline

$ghPresent = [bool](Get-Command gh -ErrorAction SilentlyContinue)

[pscustomobject]@{
  branch     = $branch
  base       = $Base
  pushed     = $pushed
  ahead      = $aheadCount
  commits    = @($ahead)
  sprint     = $Sprint
  spec       = $specName
  compareUrl = $compareUrl
  ghPresent  = $ghPresent
  bodyPath   = (Resolve-Path $BodyOut).Path
  title      = $title
} | ConvertTo-Json -Compress
