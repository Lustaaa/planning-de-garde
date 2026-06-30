# Sc.3 — L'ajout et l'édition survivent au redémarrage du serveur

`@nominal` `🖥️ pivot durabilité`

↩ Retour : [00-sprint09-suivi.md](00-sprint09-suivi.md)

**Routage** : **PIVOT de durabilité** — niveau **intégration/E2E sur Mongo RÉEL (conteneur
Docker)**, routé `ihm-builder` + intégration. **JAMAIS** un test backend à doublures : le
symptôme (« après redémarrage, l'acteur ajouté/édité est toujours là ») vit au niveau
**persistance/runtime** ; une doublure « mentirait au vert » (anti vert-qui-ment, R4). La logique
**seed-once** (seed si vide, sinon relire l'état persisté **sans re-seeder par-dessus les
éditions**) est la **principale surface de bug** — c'est l'**inversion exacte de `Scenario10`**
(re-seed volatile assumé). **Aucun test unitaire** ici (anti-règle : pas d'infra/persistance dans
une liste unit).

## Acceptation (BDD) — niveau **intégration / E2E sur Mongo réel** (routé `ihm-builder`)

> Le symptôme PO est un fait de **persistance runtime** : Carla (rose, garde Léa 8–12 juin) a été
> ajoutée et Alice renommée « Alicia » (garde Léa 1er–5 juin) ; le **serveur est redémarré** ;
> l'écran de config **réellement câblé** liste **toujours** Alicia et Carla **sans ressaisie**, et
> la grille affiche les cases nommées+colorées (case **comme** légende). Prouvé contre un **store
> Mongo RÉEL tournant** (conteneur Docker) via la **grille réellement câblée** (front WASM + API
> distante) — preuve la plus forte. Lecture via le port sur **instance fraîche** acceptable en
> complément. **Skip propre** si Docker indisponible, **jamais** un faux vert.

`Should_Lister_toujours_Alicia_et_Carla_et_afficher_leurs_cases_nommees_et_colorees_en_case_comme_en_legende_apres_un_redemarrage_du_serveur_sans_ressaisie_When_l_etat_a_ete_persiste_sur_le_store_Mongo_reel` — ✅ GREEN
*(intégration Mongo réel — `tests/PlanningDeGarde.Api.Tests/ConfigurationFoyerMongoDurabiliteTests.cs`, hôte API câblé sur Mongo réel via Docker ; redémarrage = nouvelle instance d'hôte sur la même base persistée. Skip propre si Docker/Mongo indisponible.)*

- **Observable 1 (config)** : après redémarrage, l'énumération du store liste **Alicia** (id seed
  `parent-a` renommé) **et Carla** (id neuf), sans ressaisie.
- **Observable 2 (grille)** : cases du 1er–5 juin = « Alicia » bleu (case + légende) ; cases du
  8–12 juin = « Carla » rose (case + légende).
- **Niveau** : intégration sur Mongo réel (Docker). Le redémarrage = **nouvelle instance** de
  l'adaptateur durable contre **le même store persisté**.

## Tests d'intégration (boucle externe — Mongo réel, routé `ihm-builder` + intégration)

> Hors compte « tests unitaires » (anti-règle). Listés pour cadrer la **surface seed-once**.

| # | Test d'intégration (FLFI) | Surface de bug | Status |
|---|---------------------------|----------------|--------|
| A | `Should_Seeder_le_referentiel_depuis_le_Foyer_au_demarrage_When_le_store_durable_est_vide` (`ConfigurationFoyerMongoSeedOnceTests`) | seed-once : amorçage initial | ✅ GREEN |
| B | `Should_Relire_l_etat_persiste_sans_re_seeder_par_dessus_les_editions_When_le_store_durable_est_deja_peuple_au_redemarrage` (`ConfigurationFoyerMongoSeedOnceTests`) | **seed-once cardinal** — un re-seed à chaque démarrage écraserait Alicia→Alice / supprimerait Carla (inversion de `Scenario10`) | ✅ GREEN |
| C | `Should_Lister_toujours_Alicia_et_Carla_…_apres_un_redemarrage_…_When_l_etat_a_ete_persiste_sur_le_store_Mongo_reel` (`ConfigurationFoyerMongoDurabiliteTests`, acceptation via hôte API câblé) | survie bout-en-bout (ajout **et** édition) sur Mongo réel, case comme légende | ✅ GREEN |

## Fichiers à créer / modifier

- **Adaptateur durable Mongo (Infrastructure, NEUF)** — réalise les **3 ports inchangés**
  (`IReferentielResponsables` / `IPaletteCouleurs` / `IEditeurConfigurationFoyer`) + l'**ajout**
  (Sc.1) + l'**énumération** (Sc.1), **singleton**. Chaîne de connexion **configurable**
  (`mongodb://localhost:27017`). **Seed-au-démarrage durable** : seed depuis `Foyer` **seulement
  si le store est vide** ; sinon relit l'état persisté **sans re-seeder**.
- **DI (`ServiceCollectionExtensions.cs:28-31`)** — bind les 3 ports + ajout/énumération sur
  l'adaptateur Mongo (remplace/double `ConfigurationFoyerEnMemoire` singleton). Le reste du domaine
  (slots/périodes/transferts) **reste InMemory** (borne anti-cliquet, règle 30).
- **Outillage Docker (garde-fou, sans observable métier)** — `docker-compose` (service Mongo) ;
  `run.ps1` démarre le conteneur Mongo **avant** l'API (ou documente le prérequis). Démarrage /
  teardown du conteneur autour du test d'intégration (ou Mongo de test dédié) ; **skip propre** si
  Docker indisponible.
- **Volet runtime IHM (routé `ihm-builder`)** — grille + écran de config câblés sur l'adaptateur
  durable ; le redémarrage rejoue sur le **même store persisté**.

## Design notes

- **Seed-once = cœur du pivot.** Le bug à ne pas commettre : re-seeder depuis `Foyer` à **chaque**
  démarrage (comme la volatilité assumée de `Scenario10`) — cela **écraserait** les éditions et
  **supprimerait** les acteurs ajoutés. La logique « seed si vide, sinon relire » est l'**inversion
  exacte** de `Scenario10`. C'est la principale surface de bug du sprint.
- **Mongo réel obligatoire (R4).** Une doublure afficherait un vert qui ment : la durabilité ne se
  prouve que contre un store qui **survit réellement** au cycle de vie de l'instance.
- **Borne stricte (règle 30).** Seule la config foyer (référentiel acteurs) passe durable. Ne pas
  tirer la persistance des slots/périodes/transferts au prétexte que l'adaptateur Mongo existe.
- **Ports inchangés.** Le domaine et `GrilleAgendaQuery` ne bougent pas ; seul un **adaptateur de
  droite** neuf est posé derrière les ports existants.
