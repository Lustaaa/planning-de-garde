# Harnais E2E navigateur (Playwright / Chromium) — s49

bUnit est **structurellement aveugle** au geste souris natif (drag de sélection de plage sur la
grille) : il invoque les handlers Blazor en C# sans reproduire `pointerdown`/`pointermove`/`pointerup`
natifs, `document.elementFromPoint`, ni l'exécution du module JS `window.pdgPointeur`. Ce projet
pilote un **vrai Chromium** contre l'app **réellement servie** pour observer et **prouver** le geste.

## Hors de la suite de non-régression (par conception)

Ce projet **n'est pas dans `PlanningDeGarde.slnx`** : il exige un navigateur installé ET l'app en
cours d'exécution. Il n'entre donc **jamais** dans
`pwsh -NoProfile -File .claude/skills/dotnet/scripts/test.ps1` (suite hermétique, reproductible).

## Pré-requis

1. **L'app servie** — front `http://localhost:5292`, API `http://localhost:5180`, avec le **compte
   de démo** (`deveaux.cyril@gmail.com` / `Toto123@`). Via docker compose (`docker compose up`) ou
   le skill `run` (`pwsh .claude/skills/run/scripts/run.ps1 -SeedDemo`).
2. **Chromium Playwright** installé une fois :
   ```
   dotnet build tests/PlanningDeGarde.Web.E2E/PlanningDeGarde.Web.E2E.csproj
   pwsh -NoProfile -File tests/PlanningDeGarde.Web.E2E/bin/Debug/net10.0/playwright.ps1 install chromium
   ```

## ATTENTION — build servi via docker (piège du gate G3 s49)

Le `docker-compose.yml` a un service `build` **one-shot** qui compile Api+Web dans le volume nommé
`build-artifacts` ; les services `api`/`web` servent ensuite `--no-build` **depuis ce volume**.
Modifier la source **ne se voit pas** au navigateur tant qu'on n'a pas **re-lancé le build** :

```
docker compose up build --force-recreate      # recompile la source courante dans le volume
docker compose up -d --force-recreate web api  # ressert les binaires frais
```

Recharger l'onglet ne suffit **pas** : le conteneur ressert l'ancien WASM. *(C'est la cause réelle
des 3 échecs du gate G3 s49 : code drag correct, mais build servi périmé.)*

## Lancer les smoke tests

```
dotnet test tests/PlanningDeGarde.Web.E2E/PlanningDeGarde.Web.E2E.csproj --filter "FullyQualifiedName~SmokeDragPlage"
```

- `Drag_de_J1_a_J3_...` : drag J1→J3, cases surlignées **pendant** le geste + dialog « Affecter une
  période » (s06) ouverte **pré-remplie** `[J1..J3]` au relâchement.
- `Clic_simple_...` : clic sans déplacement → **menu clic-case**, jamais la dialog plage.

## Diagnostic (observation brute)

`DiagnosticDragPlage` consigne console/erreurs/DOM/trace pointeur dans un fichier (défaut : `%TEMP%`,
surchargeable par `PDG_E2E_RAPPORT`). Utile pour ré-observer un futur symptôme au navigateur réel.

## Variables d'environnement

- `PDG_E2E_BASEURL` (défaut `http://localhost:5292`)
- `PDG_E2E_HEADED=1` (fenêtre visible au lieu de headless)
- `PDG_E2E_EMAIL` / `PDG_E2E_MOTDEPASSE` (défaut : compte de démo)
