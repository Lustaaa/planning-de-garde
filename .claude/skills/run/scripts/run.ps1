#requires -Version 7
<#
.SYNOPSIS
  Lance l'application planning-de-garde en local : hôte d'API détaché + front Blazor WebAssembly.
.DESCRIPTION
  Depuis le sprint 05, l'architecture est DÉCOUPLÉE :
    - `PlanningDeGarde.Api` (Sdk.Web) — hôte d'API détaché : canal d'écriture HTTP, canal de
      lecture (grille), hub SignalR, OpenAPI + UI Scalar. Démarre SEUL, sans le front. URL :
      http://localhost:5180 (exploration Scalar : http://localhost:5180/scalar/v1).
    - `PlanningDeGarde.Web` (Sdk.BlazorWebAssembly) — front WASM RÉEL, exécuté dans le navigateur.
      Il consomme l'API distante (Api:BaseUrl = http://localhost:5180/, défini dans
      wwwroot/appsettings.json) et le hub SignalR distant. Servi par le dev server WASM sur
      http://localhost:5292 (origine autorisée par le CORS de l'API, clé Front:Origine).

  Ce script démarre d'abord l'hôte d'API EN ARRIÈRE-PLAN, puis le front WASM AU PREMIER PLAN
  (Ctrl+C arrête le front ; l'API en arrière-plan est arrêtée à la sortie du script).

  On lance chaque projet directement (`dotnet run --project …`) plutôt que de builder la
  solution entière : construire la solution (qui inclut le projet de test bUnit référençant le
  front WASM) peut empoisonner le manifest des static web assets. `dotnet run` ne construit que
  ce qu'il faut.

  Avant le run, le script arrête les instances résiduelles d'un lancement précédent (API + front) :
  un hôte zombie verrouille les DLL de sortie et fait échouer le build (MSB3027).
.PARAMETER NoBuild
  Lance sans rebuild (`--no-build`) : suppose une sortie déjà compilée.
.PARAMETER Watch
  Lance le front via `dotnet watch run` (hot reload), pratique pendant la phase IHM.
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
# Racine du dépôt dérivée du chemin du script (4 niveaux au-dessus de .claude/skills/run/scripts/)
# plutôt que `git rev-parse`, dont la sortie UTF-8 est mal décodée quand le chemin contient un accent.
Set-Location -LiteralPath (Resolve-Path -LiteralPath "$PSScriptRoot/../../../..")

$api = 'src/PlanningDeGarde.Api/PlanningDeGarde.Api.csproj'
$web = 'src/PlanningDeGarde.Web/PlanningDeGarde.Web.csproj'
foreach ($proj in @($api, $web)) {
    if (-not (Test-Path $proj)) {
        throw "Projet introuvable ($proj). La solution n'est peut-être pas encore scaffoldée."
    }
}

# Arrête les instances résiduelles (API + front) d'un lancement précédent : un hôte zombie
# verrouille les DLL de sortie et fait échouer le build (MSB3027). On cible les processus dont la
# ligne de commande pointe l'un des deux projets, pas tous les dotnet.
$zombies = Get-CimInstance Win32_Process -Filter "Name = 'PlanningDeGarde.Api.exe' OR Name = 'PlanningDeGarde.Web.exe' OR Name = 'dotnet.exe'" -ErrorAction SilentlyContinue |
    Where-Object { $_.CommandLine -match 'PlanningDeGarde\.(Api|Web)' }
foreach ($z in $zombies) {
    Write-Host "Arrêt d'une instance résiduelle (PID $($z.ProcessId))…" -ForegroundColor Yellow
    Stop-Process -Id $z.ProcessId -Force -ErrorAction SilentlyContinue
}
if ($zombies) { Start-Sleep -Milliseconds 500 }  # laisse l'OS relâcher les verrous DLL

# --- Hôte d'API détaché (arrière-plan) -------------------------------------------------------
$apiArgs = @('run', '--project', $api, '--launch-profile', 'http')
if ($NoBuild) { $apiArgs += '--no-build' }
Write-Host "Démarrage de l'hôte d'API détaché (http://localhost:5180, Scalar : /scalar/v1)…" -ForegroundColor Green
$apiProc = Start-Process -FilePath 'dotnet' -ArgumentList $apiArgs -PassThru -NoNewWindow

# À la sortie du script (Ctrl+C sur le front), on arrête aussi l'API d'arrière-plan.
$stopApi = {
    if ($apiProc -and -not $apiProc.HasExited) {
        Write-Host "`nArrêt de l'hôte d'API détaché (PID $($apiProc.Id))…" -ForegroundColor Yellow
        Stop-Process -Id $apiProc.Id -Force -ErrorAction SilentlyContinue
    }
}

try {
    # --- Front Blazor WebAssembly (premier plan) ---------------------------------------------
    $webArgs = @('--project', $web)
    if ($NoBrowser) {
        $webArgs += '--no-launch-profile'
        $env:ASPNETCORE_URLS = 'http://localhost:5292'
    } else {
        $webArgs += @('--launch-profile', 'http')
    }
    if ($NoBuild) { $webArgs += '--no-build' }

    Write-Host 'Lancement du front WASM (http://localhost:5292)…' -ForegroundColor Green
    if ($Watch) {
        dotnet watch run @webArgs
    } else {
        dotnet run @webArgs
    }
}
finally {
    & $stopApi
}
