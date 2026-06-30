# Scénario 8 — Premier lancement sur store Mongo vierge : application vide

`@limite` · **backend — intégration sur Mongo RÉEL** (`WebApplicationFactory<ApiProgram>`,
`Foyer:Persistance=Mongo`, base vierge isolée). `MongoRequisFact` → **skip propre** si Docker absent (jamais
un faux vert). Façon `ConfigurationFoyerMongoDurabiliteTests` (s09).

[← Retour au suivi](00-sprint15-suivi.md)

Au **tout premier lancement** sur une base vierge, **rien n'est seedé** : aucun acteur listé, grille sans
slot/période/transfert, aucun cycle de fond défini. Drive le **retrait du seed runtime**.

## Acceptation (BDD) — niveau INTÉGRATION (Mongo réel)

`Should_N_amorcer_aucun_acteur_aucun_slot_aucune_periode_aucun_transfert_ni_aucun_cycle_de_fond_When_l_application_demarre_en_persistance_Mongo_sur_un_store_vierge`
— hôte API câblé `Foyer:Persistance=Mongo` sur base vierge : `GET /api/foyer/acteurs` → liste vide ;
`GET /api/grille/{aujourd'hui}` → cases sans slot/période/transfert et sans fond (aucun cycle).

## Tests (intégration — boucle externe)

| # | Test d'intégration (Mongo réel) | Contradiction | Status |
|---|---------------------------------|---------------|--------|
| 1 | `Should_Ouvrir_une_application_totalement_vide_acteurs_grille_cycle_When_le_store_Mongo_est_vierge_et_le_seed_runtime_est_retire` | Aujourd'hui l'hôte **amorce** (`AmorcerDonneesDemo`) et le store Mongo **seede-once** les acteurs → l'app n'est jamais vide. Force le **retrait** de `AmorcerDonneesDemo` **et** du seed-once acteurs côté Mongo. **Driver.** | ✅ GREEN |

## Fichiers à créer / modifier

- `src/PlanningDeGarde.Api/Program.cs` — **retrait** de l'appel `AmorcerDonneesDemo` au démarrage runtime.
- `src/PlanningDeGarde.Api/Classes/SeedDonneesDemo.cs` — retiré / neutralisé (l'amorçage de démo disparaît).
- `src/PlanningDeGarde.AdapterDroite.Mongo/Classes/ConfigurationFoyerMongo.cs` — **retrait du seed-once**
  des acteurs (`if Count == 0 → InsertMany(SeedDepuisFoyer())`) : Mongo ne seede **jamais** (asymétrie
  assumée — l'InMemory garde son seed pour les tests).
- `tests/PlanningDeGarde.Api.Tests/PremierLancementMongoVideTests.cs` — `MongoRequisFact`, base isolée par
  `Guid`, supprimée au teardown.

## Design notes

- **Asymétrie seed assumée** : Mongo = **aucun seed** (vide → durable) ; InMemory = **garde le seed**
  (la suite InMemory reste verte). Le retrait du seed-once Mongo **inverse** exactement la logique s09
  (qui seedait-once si vide) — surface de bug principale, à prouver sur store réel.
- **Dépend des adaptateurs Mongo du domaine (Sc.9)** : une grille vide sur Mongo suppose que slots /
  périodes / transferts / cycle sont lus **depuis Mongo** (et non InMemory seedé). Ordonner Sc.8 **après**
  (ou conjointement avec) la pose des 4 adaptateurs et la DI commutée de Sc.9.
- **Acceptation runtime sur store réel** (rempart anti vert-qui-ment, règle R4 spec) : prouver l'app vide
  sur Mongo réel, jamais par doublure. → remonter au CP si l'« app vide » doit afficher un état d'accueil
  particulier (hors scope : aucune règle ne le prévoit, l'app ouvre simplement vide).
