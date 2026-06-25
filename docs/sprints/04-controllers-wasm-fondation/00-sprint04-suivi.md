# Suivi — Sprint 04 · controllers-wasm-fondation

> **Cadrage scaffolding.** Décision PO : **API HTTP réelle**. Le « canal requête/réponse »
> (adaptateur de gauche) est matérialisé par des **endpoints HTTP** (minimal API ou
> contrôleurs) sur l'hôte Web réel, invoquant les handlers d'écriture **inchangés**
> (`PoserSlotCommand`, `AffecterPeriodeCommand`). Le test d'acceptation de chaque scénario
> est un **test d'intégration de bout en bout** via `WebApplicationFactory` (ajouter le
> package `Microsoft.AspNetCore.Mvc.Testing` à `PlanningDeGarde.Web.Tests`) :
> `POST` commande → handler → **store réel** (`InMemory*Repository` singletons) →
> projection **réelle** `GrilleAgendaQuery.Projeter(dateReference)`. **Aucune doublure sur
> le chemin observé** (anti « vert qui ment » / early-green).
>
> **Niveau = intégration, pas unit.** Les comportements métier (handler refuse lieu absent,
> agrégat exige un responsable, projection colore/positionne) sont **déjà verts** au niveau
> Application/Domain (Sc.1/4/7/8 + tests `GrilleAgendaQuery`) : ce sprint ne ré-ouvre aucune
> règle de gestion, il **branche le canal** et l'observe de bout en bout. Les drivers réels
> sont donc les **endpoints HTTP** ; les comportements métier ré-assertés en bout de chaîne
> sont des **caractérisations** (filet de non-régression du câblage réel).
>
> **Invariants NON-codants** (garde-fous structure, jamais pilotés en Gherkin) : exécution
> côté navigateur (WASM), code-behind systématique, API explorable/documentée (swagger),
> séparation canal écriture (requête/réponse) vs diffusion (lecture seule) en tant que
> câblage. **Hors périmètre codant** : commande `définir-transfert` (exposée par le canal
> mais sans observable dans `GrilleAgendaQuery` → caractérisation pure, non scénarisée).
>
> **Migration front WASM** : invariant **non-codant** (hors périmètre de ce dossier ;
> aucun `.razor` ni câblage SignalR client n'est piloté ici). La notification temps réel
> déclenchée par une écriture aboutie se vérifie par un **Spy** sur `INotificateurPlanning`,
> jamais par le canal de diffusion.

| # | Scénario | Tag | Acceptation | Tests | Statut |
|---|----------|-----|-------------|-------|--------|
| 1 | [Poser un slot via le canal le rend visible dans sa case](01-poser-slot-canal-visible.md) | `@nominal` | ✅ GREEN | 2/2 | ✅ GREEN |
| 2 | [Poser un slot sur un lieu absent est refusé sans toucher la grille](02-poser-slot-lieu-absent-refuse.md) | `@erreur` | ✅ GREEN | 2/2 | ✅ GREEN |
| 3 | [Affecter une période colore les cases-jour couvertes](03-affecter-periode-couleur-cases.md) | `@nominal` | ✅ GREEN | 2/2 | ✅ GREEN |
| 4 | [Affecter une période sans responsable est refusée](04-affecter-periode-sans-responsable-refusee.md) | `@erreur` | ✅ GREEN | 2/2 | ✅ GREEN |

**Total** : 4 scénarios backend (acceptation **intégration** `WebApplicationFactory`) · 8 tests d'intégration.

## Phase IHM finale (ihm-builder)

Tous les scénarios `@vert` → phase IHM. Le front existant a été **câblé au canal d'écriture HTTP** :

- **Vues d'écriture migrées au canal** : `PoserSlot` et `AffecterPeriode` n'appellent plus les
  handlers en DI direct ; elles **POSTent leurs commandes via `/api/canal/poser-slot` et
  `/api/canal/affecter-periode`** (adaptateur de gauche), reçoivent l'accusé succès/échec et
  affichent le motif métier propagé. `HttpClient` (BaseAddress = hôte) enregistré dans `Program.cs`.
- **Convention code-behind** (invariant) appliquée : `@code` inline retiré, logique déplacée dans
  `PoserSlot.razor.cs` / `AffecterPeriode.razor.cs`.
- **API explorable** (invariant) : `AddOpenApi()` + `MapOpenApi()` → document OpenAPI en dev
  (`/openapi/v1.json`), listant les deux endpoints du canal.
- **SignalR conservé en lecture seule** (invariant) : `PlanningHub` n'expose aucune méthode
  d'écriture ; `/planning` (`PlanningPartage`) ne fait qu'écouter `MiseAJour` et recharger la
  projection. Jamais d'écriture par la diffusion ; la grille se rafraîchit après une écriture
  HTTP aboutie (notification via `INotificateurPlanning`).
- **Migration WASM** : traitée en **invariant non-codant** (cf. cadrage suivi). Le front reste
  rendu côté serveur ; seul le **canal d'écriture est découplé** (HTTP requête/réponse). Le même
  `HttpClient` ciblera l'hôte distant après bascule WASM, sans toucher les vues.
- **Tests de composant** réécrits (bUnit) : la vue est vérifiée comme **émettrice via le canal HTTP**
  (stub `FakeCanalHttpHandler`), le bout en bout du canal restant couvert par les tests d'intégration.

> **Note infra** : `nuget.config` local ajouté (sources scopées nuget.org + offline) — le feed privé
> CNDO (Azure DevOps) hérité faisait échouer la restauration (401) hors réseau d'entreprise.

**Vérification** : `dotnet build` vert + suite complète **82 verts** (63 + 19). Runtime validé :
canal valide → 200, refus → 400 + motif, `/openapi/v1.json` → 200, `/planning` → 200.

> **Scaffolding requis (à créer par `tdd-auto`, hors périmètre de l'analyse)** :
> - endpoints HTTP du canal d'écriture sur l'hôte Web (`pose de slot`, `affectation de
>   période` ; `définir-transfert` exposé mais non scénarisé) ;
> - référence `Microsoft.AspNetCore.Mvc.Testing` ajoutée à `PlanningDeGarde.Web.Tests` ;
> - une fabrique de test (`WebApplicationFactory<Program>`) résolvant les singletons de
>   store réels pour observer la projection après l'appel.
