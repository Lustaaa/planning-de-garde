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
| 2 | [Poser un slot sur un lieu absent est refusé sans toucher la grille](02-poser-slot-lieu-absent-refuse.md) | `@erreur` | ⏳ Pending | 0/2 | ⏳ Pending |
| 3 | [Affecter une période colore les cases-jour couvertes](03-affecter-periode-couleur-cases.md) | `@nominal` | ⏳ Pending | 0/2 | ⏳ Pending |
| 4 | [Affecter une période sans responsable est refusée](04-affecter-periode-sans-responsable-refusee.md) | `@erreur` | ⏳ Pending | 0/2 | ⏳ Pending |

**Total** : 4 scénarios backend (acceptation **intégration** `WebApplicationFactory`) · 8 tests d'intégration.

> **Scaffolding requis (à créer par `tdd-auto`, hors périmètre de l'analyse)** :
> - endpoints HTTP du canal d'écriture sur l'hôte Web (`pose de slot`, `affectation de
>   période` ; `définir-transfert` exposé mais non scénarisé) ;
> - référence `Microsoft.AspNetCore.Mvc.Testing` ajoutée à `PlanningDeGarde.Web.Tests` ;
> - une fabrique de test (`WebApplicationFactory<Program>`) résolvant les singletons de
>   store réels pour observer la projection après l'appel.
