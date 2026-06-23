---
name: run
description: À utiliser pour lancer l'application planning-de-garde en local — build de la solution .NET puis démarrage de l'hôte Blazor Server (PlanningDeGarde.Web, front + back en un seul processus). Adossé au script PowerShell run.ps1 (options build/watch/navigateur). Préférer ce skill à un `dotnet run` manuel.
---

# run — Lancer l'application

Démarre `planning-de-garde` en local via le script
`.claude/skills/run/scripts/run.ps1` (PowerShell 7). L'application est un **hôte
Blazor Server unique** (`PlanningDeGarde.Web`, `Sdk.Web`) qui référence
`Application` + `Infrastructure` : front et back tournent dans le même processus,
SignalR inclus. Pas de backend séparé à démarrer.

> Lance le script avec `pwsh .claude/skills/run/scripts/run.ps1 [options]`.

## Quand l'utiliser

Dès qu'on veut **voir tourner l'appli** : validation visuelle d'un scénario,
démonstration, debug manuel de l'IHM. Préférer ce skill à un `dotnet run` tapé à la
main (il build d'abord et cible le bon projet).

## Commande

```
pwsh .claude/skills/run/scripts/run.ps1            # build + lance, ouvre le navigateur
pwsh .claude/skills/run/scripts/run.ps1 -Watch     # dotnet watch (hot reload)
pwsh .claude/skills/run/scripts/run.ps1 -NoBuild   # lance sans rebuild
pwsh .claude/skills/run/scripts/run.ps1 -NoBrowser # ne pas ouvrir le navigateur
```

- `-NoBuild` : saute le build préalable.
- `-Watch` : hot reload (`dotnet watch`), pratique pendant la phase IHM.
- `-NoBrowser` : ne lance pas le navigateur (CI, environnement headless).

## Notes

- **Solution requise** : le script échoue clairement si `PlanningDeGarde.Web` n'existe
  pas encore (solution non scaffoldée). Le scaffolding est posé au 1er scénario par
  `tdd-auto` (cf. skill `tdd-implement`).
- Le processus reste au premier plan (Ctrl+C pour arrêter). Pour un lancement
  non bloquant, le run en arrière-plan est du ressort de l'appelant.
