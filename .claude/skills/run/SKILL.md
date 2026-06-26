---
name: run
description: À utiliser pour lancer l'application planning-de-garde en local — démarre l'hôte d'API détaché (PlanningDeGarde.Api) puis le front Blazor WebAssembly (PlanningDeGarde.Web) qui le consomme à distance. Adossé au script PowerShell run.ps1 (options build/watch/navigateur). Préférer ce skill à un `dotnet run` manuel.
---

# run — Lancer l'application

Démarre `planning-de-garde` en local via le script
`.claude/skills/run/scripts/run.ps1` (PowerShell 7). Depuis le sprint 05,
l'architecture est **découplée en deux hôtes** :

- **`PlanningDeGarde.Api`** (`Sdk.Web`) — hôte d'**API détaché** : canal d'écriture
  HTTP, canal de lecture (grille), hub SignalR, OpenAPI + UI Scalar. Démarre **seul**,
  sans le front. URL : `http://localhost:5180` (exploration Scalar :
  `http://localhost:5180/scalar/v1`).
- **`PlanningDeGarde.Web`** (`Sdk.BlazorWebAssembly`) — front **WASM réel**, exécuté
  dans le navigateur. Il consomme l'**API distante** (`Api:BaseUrl` =
  `http://localhost:5180/`, défini dans `wwwroot/appsettings.json`) et le hub SignalR
  distant. Servi sur `http://localhost:5292` (origine autorisée par le CORS de l'API,
  clé `Front:Origine`).

Le script lance d'abord l'**API en arrière-plan**, puis le **front WASM au premier
plan** (Ctrl+C arrête le front ; l'API d'arrière-plan est arrêtée à la sortie).

> Lance le script avec `pwsh .claude/skills/run/scripts/run.ps1 [options]`.

## Quand l'utiliser

Dès qu'on veut **voir tourner l'appli** : validation visuelle d'un scénario,
démonstration, debug manuel de l'IHM. Préférer ce skill à un `dotnet run` tapé à la
main (il cible les bons projets et orchestre les deux hôtes).

## Commande

```
pwsh .claude/skills/run/scripts/run.ps1            # lance API + front WASM, ouvre le navigateur
pwsh .claude/skills/run/scripts/run.ps1 -Watch     # front via dotnet watch (hot reload)
pwsh .claude/skills/run/scripts/run.ps1 -NoBuild   # lance sans rebuild
pwsh .claude/skills/run/scripts/run.ps1 -NoBrowser # ne pas ouvrir le navigateur
```

- `-NoBuild` : saute le build préalable.
- `-Watch` : hot reload du front (`dotnet watch`), pratique pendant la phase IHM.
- `-NoBrowser` : ne lance pas le navigateur (CI, environnement headless).

## Notes

- **Projets requis** : le script échoue clairement si `PlanningDeGarde.Api` ou
  `PlanningDeGarde.Web` n'existe pas (solution non scaffoldée).
- Le front WASM ne porte aucun store : il lit la grille et écrit ses commandes **via
  l'API distante**. Sans l'API démarrée, le front s'affiche mais la grille reste vide
  et toute écriture signale « service injoignable » (comportement attendu, Sc.6).
- Le processus front reste au premier plan (Ctrl+C pour arrêter, ce qui arrête aussi
  l'API d'arrière-plan).
