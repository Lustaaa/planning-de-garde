#requires -Version 7
<#
.SYNOPSIS
  Build puis lance l'application Blazor Server (PlanningDeGarde.Web).
.DESCRIPTION
  Hôte unique Blazor Server (Sdk.Web) qui référence Application + Infrastructure.
  Par défaut : build de la solution puis `dotnet run` sur le projet Web.
.PARAMETER NoBuild
  Saute le build préalable (lance directement le projet Web).
.PARAMETER Watch
  Lance via `dotnet watch` (hot reload) au lieu de `dotnet run`.
.PARAMETER NoBrowser
  N'ouvre pas le navigateur automatiquement.
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
Set-Location (git rev-parse --show-toplevel)

$web = 'src/PlanningDeGarde.Web/PlanningDeGarde.Web.csproj'
if (-not (Test-Path $web)) {
    throw "Projet Web introuvable ($web). La solution n'est peut-être pas encore scaffoldée."
}

if (-not $NoBuild -and -not $Watch) {
    Write-Host 'Build de la solution…' -ForegroundColor Cyan
    dotnet build PlanningDeGarde.slnx
    if ($LASTEXITCODE -ne 0) { throw 'Build en échec — lancement annulé.' }
}

if (-not $NoBrowser) { $env:ASPNETCORE_ENVIRONMENT = 'Development' }

Write-Host 'Lancement de PlanningDeGarde.Web…' -ForegroundColor Green
if ($Watch) {
    dotnet watch --project $web run
} else {
    $launch = if ($NoBrowser) { @('--no-launch-profile') } else { @() }
    dotnet run --project $web @launch
}
