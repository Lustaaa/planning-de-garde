# Scénario 3 — L'UI d'exploration interactive de l'API liste les endpoints du canal d'écriture

`@nominal`

[← Retour au suivi](00-sprint05-suivi.md)

> **Axe : backend.** L'UI d'exploration (Scalar) est un **confort d'outillage sans observable
> métier** : on la pilote par sa **servabilité HTTP** sur l'hôte d'API détaché. Le driver est que
> la **route d'exploration répond** et que le **document OpenAPI** sous-jacent **liste les endpoints
> du canal d'écriture** (« poser un slot », « affecter une période »). Observable à la frontière HTTP
> de l'API via `WebApplicationFactory<ApiProgram>`. Aucun `.razor` ni interactivité navigateur pilotée
> ici (l'essai-en-direct côté navigateur est une feature de Scalar, pas une règle métier). Routé vers
> `tdd-auto`.
>
> **Niveau d'acceptation : intégration** (`WebApplicationFactory<ApiProgram>`), pas unit ni bUnit.

## Acceptation (BDD)

`Should_Lister_les_endpoints_poser_un_slot_et_affecter_une_periode_dans_la_description_servie_par_l_hote_d_API_When_on_ouvre_l_exploration_de_l_API`

Test d'**intégration** sur l'hôte d'API détaché (`WebApplicationFactory<ApiProgram>`) :
- **Given** l'hôte d'API est démarré seul ;
- **When** un client outillage **ouvre la page d'exploration interactive** de l'hôte d'API (la
  route Scalar) **et** récupère le document OpenAPI qu'elle référence ;
- **Then** la page d'exploration **répond** (servie) **et** le document liste les endpoints du
  **canal d'écriture**, dont « poser un slot » (`/api/canal/poser-slot`) et « affecter une période »
  (`/api/canal/affecter-periode`).

## Tests d'intégration (ordonnés)

| # | Test (FLFI) | TPP | Contradiction | Status |
|---|-------------|-----|---------------|--------|
| 1 | `Should_Servir_la_page_d_exploration_interactive_When_on_ouvre_la_route_d_exploration_de_l_hote_d_API` | {} → route qui répond | **Driver de servabilité** : aucune UI d'exploration n'est livrée sur le nouvel hôte API (seul le document OpenAPI l'était, et sur l'ancien hôte Web). Le 1er rouge force l'ajout de **Scalar** (`Scalar.AspNetCore`) sur `ApiProgram` et une route d'exploration qui **répond en succès**. | ⏳ Pending |
| 2 | `Should_Lister_les_endpoints_poser_un_slot_et_affecter_une_periode_dans_la_description_servie_par_l_hote_d_API_When_on_recupere_le_document_OpenAPI_de_l_exploration` | route servie → contenu attendu | **Driver de complétude (anti early-green)** : une UI servie mais branchée sur un document vide / sans le canal passe #1 mais échoue ici. Force le câblage OpenAPI ↔ endpoints du canal sur l'hôte API → le document **liste** « poser un slot » et « affecter une période ». ⚠️ Le document OpenAPI existait déjà sur l'hôte Web (sprint 04) ; ici c'est sa **première exposition sur l'hôte API détaché** — caractérisation du portage. | ⏳ Pending |

## Fichiers à créer / modifier

- Ajout de **Scalar** (`Scalar.AspNetCore`) + route d'exploration sur `src/PlanningDeGarde.Api`
  (`ApiProgram`), reliée au document OpenAPI natif (`AddOpenApi`/`MapOpenApi`). — scaffolding `tdd-auto`.
- Fichier de tests d'intégration de l'exploration dans `tests/PlanningDeGarde.Api.Tests/`.

## Design notes

- **Confort d'outillage, pas règle métier** : on n'asserte aucun observable de domaine, seulement la
  servabilité de l'exploration et la présence des endpoints du canal dans la description.
- **Document vs UI** : l'« API explorable » couvre **deux choses** — le document OpenAPI (description)
  **et** l'UI interactive Scalar (essai). Le test #2 observe le document (assertion stable) ; #1
  observe que la route Scalar répond.
- **Essai-en-direct** (chaque endpoint essayable depuis la page) : c'est une **capacité native de
  Scalar** une fois la route servie sur le document complet ; non re-testée en propre (pas d'observable
  métier, et hors portée d'un test d'intégration HTTP).
- **Réutiliser** la fabrique `WebApplicationFactory<ApiProgram>` du Sc.1 (même hôte API).
