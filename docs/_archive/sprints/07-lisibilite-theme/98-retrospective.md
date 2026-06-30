# Rétrospective de la méthode — Sprint 07 (lisibilite-theme)

> **Sprint retrospective SCRUM** sur la **méthode** (pipeline d'agents / skills / commands),
> distincte de `/4-retours` (rétro produit). Dernier maillon de la boucle : les améliorations
> retenues partent dans la PR du sprint. Actions priorisées par le PO (multiSelect).

## Bilan

### Ce qui a marché (avec preuve)

- **`chef-de-projet` — autonomie sûre.** 9 décisions tranchées sans escalade PO, toutes
  journalisées dans `99-sprint07-retours.md` (§ Décisions autonomes) → le PO pilote a
  posteriori. Palier conservateur + garde-fou découpe tenus (thème subordonné à la lisibilité,
  pas de scénario à `Then` non vérifiable).
- **`tdd-analyse` — dette symétrique assumée.** Scaffolding du port nom
  (`IReferentielResponsables`) en **miroir** de `IPaletteCouleurs` : aucune persistance
  prématurée, pas de « vert qui ment » sur le référentiel semé.
- **Garde-fou G4 efficace.** L'early-green inattendu de Sc.2 a bien **stoppé avant commit** et
  est remonté direct au PO (`gate:G4`), conformément à `tdd-auto.md`.
- **Routage scénario IHM.** Sc.4 (suivi temps réel SignalR) mené par `ihm-builder` en
  acceptation runtime réellement câblée (anti-flaky), pas en bUnit menteur → 6/6 @vert runtime
  + thème.
- **`make-gherkin` — discipline de périmètre.** Matrice fermée sur nominal + limites **sans
  `@erreur` forcé** ; read-robustness différée en candidat backlog plutôt que bolt-on.

### À améliorer (avec preuve)

1. **Angle mort de minimalité GREEN.** Aucun maillon ne revoyait le diff d'implémentation pour
   vérifier qu'il ne généralise pas au-delà du rouge.
   *Preuve :* `99-sprint07-retours.md` § Méthode + IA — l'early-green **inattendu** de Sc.2
   (dédoublonnage légende) venait d'un `.Distinct()` écrit dès Sc.1 GREEN, non exigé par le
   rouge de Sc.1. Le CP tranche les questions, ne revoit pas le diff GREEN (hors boucle
   d'implémentation par conception) → cause racine détectée seulement au scénario suivant.

2. **Passe de non-régression menteuse.** Un `--no-build` (ou filtre projet partiel) laisse un
   projet de prod non recompilé et fait passer un vert sur du code qui ne compile pas.
   *Preuve :* commit Sc.1 `48243d6` a livré `PlanningDeGarde.Web` **non compilable**
   (`PlanningPartage.razor.cs` construisait `GrilleAgenda` sans l'argument `Légende`), masqué
   par un `dotnet test --no-build` sur Web.Tests. Détecté/corrigé seulement au commit Sc.2.

3. **Race de build concurrent au gate visuel.** `run.ps1` lançait l'API (arrière-plan, qui
   rebuild) puis le front WASM (qui rebuild aussi) → les deux recompilent simultanément
   `Domain.dll` / `Application.dll` partagés → verrou de fichier.
   *Preuve :* friction vécue au gate `/run` du sprint 07 — build échoué 2× sur `CS2012`
   « used by another process ». Contourné par `dotnet build-server shutdown` + build séquentiel
   des deux projets + `run.ps1 -NoBuild`.

## Actions appliquées (validées PO — les 3)

| # | Cible | Édition appliquée |
|--:|-------|-------------------|
| 1 | `.claude/agents/tdd-auto.md` (SCENARIO_DONE + garde-fous) **+** `.claude/skills/tdd-implement/SKILL.md` (étape 6 + signaux d'alarme) | **Auto-revue de minimalité GREEN obligatoire avant commit** : relire le diff, confirmer que chaque construction neuve (`.Distinct()`, branche, boucle) a été forcée par un rouge de ce scénario ; sinon la retirer, ou STOP `gate:G4` si déjà couverte. `chef-de-projet.md` **non touché** (l'angle mort reste assumé : le CP ne fait pas de revue de code). |
| 2 | `.claude/skills/tdd-implement/SKILL.md` (étape 6 + signaux) **+** `.claude/agents/tdd-auto.md` (GREEN_PHASE + garde-fous) | **Interdiction de `--no-build` / filtre projet partiel sur la non-régression** : la garde recompile **tous** les projets de la solution. Signal d'alarme ajouté (« vert qui ment », cf. Sc.1 s07). |
| 3 | `.claude/skills/run/scripts/run.ps1` | **Durcissement anti-race CS2012** : `dotnet build-server shutdown` + build séquentiel API puis Web avant de lancer les deux hôtes, qui tournent ensuite en `--no-build` (sauf `-Watch` côté front). Param `-NoBuild` conservé pour sauter la phase ; en-tête `.DESCRIPTION` / `.PARAMETER` mis à jour. |

> **Note de process.** Les éditions sous `.claude/` (self-modification du pipeline) ont été
> refusées au sous-agent `retro-sprint` (autorité utilisateur requise, non relayable par le
> coordinateur). Appliquées par le thread principal après **autorisation explicite du PO**
> (réponse directe à un `AskUserQuestion`). Garde-fou respecté, pas contourné.

## Suite

- Améliorations embarquées dans la PR du sprint 07 (`/6-cloture-sprint`).
- Prochain sujet (backlog `/4-retours`) : **écran de config foyer — édition des acteurs
  (noms + couleurs), volatile**, à amorcer en `/2-make-gherkin` sur la spec v08.
