---
name: dotnet
description: "À utiliser pour les opérations .NET du projet planning-de-garde (restore, build, test) — chaque commande est adossée à un script PowerShell qui renvoie un JSON compact (au lieu de la sortie verbeuse de dotnet), recompile TOUTE la solution (jamais --no-build ni filtre projet partiel) et tue les hôtes Api/Web zombies avant build. Préférer ces scripts à un dotnet brut."
---

# dotnet — restore / build / test adossés à des scripts

Toutes les opérations .NET passent par les scripts de `.claude/skills/dotnet/scripts/`
(PowerShell 7). Ils renvoient un **JSON compact** (économie de contexte) au lieu de la
sortie brute de `dotnet`, recompilent **toute** la solution et tuent les hôtes `Api`/`Web`
résiduels (verrous DLL) avant un build/test.

> Lance chaque script avec `pwsh -NoProfile -File .claude/skills/dotnet/scripts/<x>.ps1 …`.
> Solution par défaut = `PlanningDeGarde.slnx` à la racine.

## Quand l'utiliser

Dès qu'un cycle TDD ou une vérification a besoin de `dotnet` : non-régression (suite
complète), build de contrôle, restore après changement de dépendances. L'agent `dev-team`
l'utilise à chaque GREEN ; le gate visuel s'en sert pour prouver back + IHM up.

## Commandes

### `test` — suite de tests, JSON compact
Non-régression = suite COMPLÈTE, build COMPLET. C'est le défaut. Renvoie
`{ green, total, passed, failed, skipped, assemblies }` ; sur rouge ajoute `failures`.
```
pwsh -NoProfile -File .claude/skills/dotnet/scripts/test.ps1
pwsh -NoProfile -File .claude/skills/dotnet/scripts/test.ps1 -Filter "FullyQualifiedName~Reservation"
pwsh -NoProfile -File .claude/skills/dotnet/scripts/test.ps1 -Serial
```
- `-Filter` : cycle **RED ciblé uniquement**, **jamais** une preuve de non-régression.
- `-Serial` : suite **COMPLÈTE** mais **assemblies sérialisées** (`RunConfiguration.MaxCpuCount=1`).
  **Mode gate — ceinture + bretelles** (défaut au gate `/sprint`, s37). La dette flake de charge
  SignalR/I/O (`*TempsReel*`) qui motivait ce mode est **soldée à la cause s39** : les 55 classes
  `FrontWasm*TempsReel*` sont désormais **sérialisées entre elles** par la collection xUnit
  `SignalRTempsReelCollection` (`DisableParallelization=true`), et le parallèle est **fiable (0 %
  rouge sur 12 runs full-suite)**. `-Serial` reste au gate à coût quasi nul (supprime tout résidu
  de course cross-assembly sous machine chargée), mais **le parallèle nu n'est plus rougi par le
  flake** — la dev-team l'exerce au cycle TDD pour éprouver la concurrence réelle. **Ne masque aucune
  régression** : un rouge déterministe reste rouge en série (triage flake — re-run isolé x2-3,
  `N/N rouge = régression` — reste la règle). Défaut hors gate = parallèle (rapide).

### `build` — build solution, JSON compact
Recompile TOUS les projets. Renvoie `{ green, errors, warnings, exitCode }` (+ `messages`
sur rouge).
```
pwsh -NoProfile -File .claude/skills/dotnet/scripts/build.ps1
```

### `restore` — restore NuGet, JSON compact
Renvoie `{ green, exitCode }` (+ `messages` sur rouge).
```
pwsh -NoProfile -File .claude/skills/dotnet/scripts/restore.ps1
```

## Garde-fous (portés par les scripts)

- **Suite/solution complète** — jamais `--no-build` ni filtre projet partiel pour une
  non-régression (un projet de prod non recompilé fait **mentir le vert**).
- **`-Filter` = RED ciblé seulement** — interdit comme preuve de non-régression.
- **Hôtes zombies tués** — les `dotnet` pointant `Api`/`Web` sont arrêtés avant build (verrous
  DLL / MSB3027) ; on ne tue pas les autres `dotnet`.
- **Chemin accentué** — comme les scripts `git`, chaque script force l'encodage UTF-8 et se
  repositionne via `Set-Location -LiteralPath` en tête (dépôt sous `source/privée/`) — **ne jamais
  retirer ces lignes** en éditant un script.
- **Sortie compacte** — JSON court sur le chemin vert ; détails plafonnés sur rouge.

## Lancer l'app

Le **lancement** de l'app (Api + front WASM) n'est pas ici : c'est le skill `run`
(`pwsh .claude/skills/run/scripts/run.ps1`).
