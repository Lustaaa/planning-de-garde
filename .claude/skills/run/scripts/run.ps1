#requires -Version 7
<#
.SYNOPSIS
  Lance l'application Blazor Server (PlanningDeGarde.Web).
.DESCRIPTION
  Hôte unique Blazor Server (Sdk.Web) qui référence Application + Infrastructure.
  Front et back tournent dans le même processus (SignalR inclus).

  IMPORTANT — ne JAMAIS faire `dotnet build PlanningDeGarde.slnx` avant le run.
  Construire la solution entière (qui inclut le projet de test bUnit
  `PlanningDeGarde.Web.Tests` référençant le Web) empoisonne le manifest des
  static web assets en mode dev : le bundle CSS scoped (`*.styles.css`), les
  `*.razor.js` et `_framework/blazor.web.js` renvoient alors 500 et l'IHM
  s'affiche sans style ni interactivité. On lance donc directement le projet Web :
  `dotnet run` ne construit que ce qu'il faut et produit un manifest cohérent.
.PARAMETER NoBuild
  Lance sans rebuild (`--no-build`) : suppose une sortie déjà compilée.
.PARAMETER Watch
  Lance via `dotnet watch run` (hot reload), pratique pendant la phase IHM.
.PARAMETER NoBrowser
  N'ouvre pas le navigateur automatiquement (profil sans launchBrowser).
.EXAMPLE
  pwsh .claude/skills/run/scripts/run.ps1
.EXAMPLE
  pwsh .claude/skills/run/scripts/run.ps1 -Watch
#>
[CmdletBinding()]
param(
    [switch]$NoBuild,
    [switch]$Watch,
    [switch]$NoBrowser
)

$ErrorActionPreference = 'Stop'
# Racine du dépôt dérivée du chemin du script (4 niveaux au-dessus de
# .claude/skills/run/scripts/) plutôt que de `git rev-parse`, dont la sortie UTF-8
# est mal décodée par PowerShell quand le chemin contient un accent (ex. « privée »).
Set-Location -LiteralPath (Resolve-Path -LiteralPath "$PSScriptRoot/../../../..")

$web = 'src/PlanningDeGarde.Web/PlanningDeGarde.Web.csproj'
if (-not (Test-Path $web)) {
    throw "Projet Web introuvable ($web). La solution n'est peut-être pas encore scaffoldée."
}

# Profil de lancement : 'http' ouvre le navigateur (env Development, port 5292) ;
# en mode -NoBrowser on force Development + port 5000 sans profil.
$runArgs = @('--project', $web)
if ($NoBrowser) {
    $runArgs += '--no-launch-profile'
    $env:ASPNETCORE_ENVIRONMENT = 'Development'
    $env:ASPNETCORE_URLS = 'http://localhost:5000'
} else {
    $runArgs += @('--launch-profile', 'http')
}
if ($NoBuild) { $runArgs += '--no-build' }

Write-Host 'Lancement de PlanningDeGarde.Web…' -ForegroundColor Green
if ($Watch) {
    dotnet watch run @runArgs
} else {
    dotnet run @runArgs
}
