# Scénario 9 — Chaque item du domaine survit au redémarrage (Mongo)

`@nominal` · **backend — intégration sur Mongo RÉEL** — **BOUCLE EXTERNE** pilotant les **4 adaptateurs
Mongo** (un type d'item par ligne). Acceptation runtime sur store réel (rempart anti vert-qui-ment).
`MongoRequisFact` → skip propre si Docker absent.

[← Retour au suivi](00-sprint15-suivi.md)

Un item saisi en mode Mongo (sur store vierge, après création de l'acteur « Alice » + cycle N=2 mappé
index 0 → Alice) **persiste après redémarrage de l'hôte d'API** : l'item est toujours présent et projeté
dans la grille, et l'acteur + le cycle de fond sont toujours là. Le redémarrage = **nouvelle instance**
d'hôte sur la **même base Mongo** (façon `ConfigurationFoyerMongoDurabiliteTests`).

## Acceptation (BDD) — niveau INTÉGRATION (Mongo réel)

`Should_Retrouver_l_item_saisi_projete_dans_la_grille_ainsi_que_l_acteur_Alice_et_le_cycle_de_fond_When_l_hote_d_API_redemarre_en_persistance_Mongo_sur_la_meme_base` — Scenario Outline, **une ligne par
item** (slot / période / transfert / cycle de fond), chacune pilotant son adaptateur Mongo.

## Tests (intégration — un driver par adaptateur Mongo)

| # | Test d'intégration (Mongo réel) | Contradiction | Status |
|---|---------------------------------|---------------|--------|
| 1 | `Should_Conserver_un_slot_enregistre_enfant_lieu_date_apres_le_redemarrage_de_l_hote_When_les_slots_sont_persistes_sur_le_store_Mongo_reel` | Les slots vivent en `InMemorySlotRepository` (volatil) → perdus au redémarrage. Force un **`MongoSlotRepository`** (write-through + relecture). **Driver adaptateur slots.** | ✅ GREEN |
| 2 | `Should_Conserver_une_periode_affectee_a_Alice_sur_2_jours_apres_le_redemarrage_de_l_hote_When_les_periodes_sont_persistees_sur_le_store_Mongo_reel` | `InMemoryPeriodeRepository` volatil → période perdue. Force un **`MongoPeriodeRepository`** (dont `Modifier` optimiste). **Driver adaptateur périodes.** | ✅ GREEN |
| 3 | `Should_Conserver_un_transfert_depositaire_recuperateur_lieu_date_heure_apres_le_redemarrage_de_l_hote_When_les_transferts_sont_persistes_sur_le_store_Mongo_reel` | `InMemoryTransfertRepository` volatil → transfert perdu. Force un **`MongoTransfertRepository`**. **Driver adaptateur transferts.** | ✅ GREEN |
| 4 | `Should_Conserver_le_cycle_de_fond_de_2_semaines_apres_le_redemarrage_de_l_hote_When_le_cycle_est_persiste_sur_le_store_Mongo_reel` | `CycleDeFondEnMemoire` volatil → cycle perdu (le fond ne se résout plus). Force un **`CycleDeFondMongo`** + commutation DI du port cycle. **Driver adaptateur cycle.** | ✅ GREEN |

## Fichiers à créer / modifier

- `src/PlanningDeGarde.AdapterDroite.Mongo/Classes/MongoSlotRepository.cs` — réalise `ISlotRepository`
  (write-through Mongo, relecture au démarrage ; sérialisation `SlotSnapshot`).
- `src/PlanningDeGarde.AdapterDroite.Mongo/Classes/MongoPeriodeRepository.cs` — réalise `IPeriodeRepository`
  (dont `Modifier` optimiste sur l'état persisté).
- `src/PlanningDeGarde.AdapterDroite.Mongo/Classes/MongoTransfertRepository.cs` — réalise `ITransfertRepository`.
- `src/PlanningDeGarde.AdapterDroite.Mongo/Classes/CycleDeFondMongo.cs` — réalise `IReferentielCycleDeFond`.
- `src/PlanningDeGarde.Infrastructure/ServiceCollectionExtensions.cs` — **DI généralisée** : le flag
  `Foyer:Persistance` commute **tout** le domaine droite (slots / périodes / transferts / cycle) vers Mongo
  en runtime, InMemory sinon — au lieu de la seule config foyer (s09).
- `tests/PlanningDeGarde.Api.Tests/ItemDomaineSurvitRedemarrageMongoTests.cs` — `MongoRequisFact` par item,
  base isolée par `Guid`, supprimée au teardown ; redémarrage = nouvelle instance d'hôte sur la même base.

## Design notes

- **Boucle externe pilotant 4 adaptateurs** : chaque ligne est un **driver réel** (l'adaptateur Mongo
  n'existe pas). Les scénarios fonctionnels existants (slots/périodes/transferts/cycle, déjà verts) **ne
  changent pas de comportement** — ils sont seulement **re-pointés** sur le store durable selon la DI ; ils
  restent verts en **InMemory seedé** (garde-fou de séparation, sans scénario codant dédié).
- **DI commutée = cœur transverse** : généraliser le flag `Foyer:Persistance` à tous les ports de droite.
  En runtime → Mongo ; sous l'environnement de test → InMemory (la suite reste InMemory seedé) ; Sc.8/Sc.9
  **forcent Mongo** explicitement via `UseSetting("Foyer:Persistance","Mongo")`. → remonter au CP si le
  **mécanisme de configuration runtime** (appsettings vs variable d'environnement) doit être tranché.
- **Pattern de durabilité** : write-through + relecture d'une **instance fraîche** = un redémarrage (relit
  l'état persisté), exactement comme `ConfigurationFoyerMongo`. Sérialiser les snapshots du domaine
  (`SlotSnapshot` / `PeriodeSnapshot` / `TransfertSnapshot`) et le `CycleDeFond` (N + mapping index→id).
- **Acceptation runtime sur store réel obligatoire** (R4) : jamais de doublure « qui ment au vert ». `Skip`
  propre si Docker absent.
