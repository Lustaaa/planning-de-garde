# Scénario 4 — L'hôte d'API démarre en mode headless et sert la description et l'exploration

`@limite`

[← Retour au suivi](00-sprint05-suivi.md)

> **Axe : backend.** Pousse le Sc.1 à sa limite : l'hôte d'API démarre dans un environnement
> **headless** (aucun front déployé ni référencé) et **sert quand même** sa description OpenAPI et son
> exploration. Le driver est le **démarrage autonome** + la **servabilité de la description** sans
> aucune dépendance au front. Observable à la frontière HTTP de l'API via
> `WebApplicationFactory<ApiProgram>` (par construction, la fabrique de test **est** un environnement
> sans front). Routé vers `tdd-auto`.
>
> **Niveau d'acceptation : intégration** (`WebApplicationFactory<ApiProgram>`), pas unit ni bUnit.

## Acceptation (BDD)

`Should_Servir_le_document_de_description_OpenAPI_du_canal_d_ecriture_When_l_hote_d_API_demarre_dans_un_environnement_headless_sans_front`

Test d'**intégration** sur l'hôte d'API détaché démarré **sans front** (`WebApplicationFactory<ApiProgram>`) :
- **Given** un environnement headless où aucun front n'est déployé ni référencé ;
- **When** l'hôte d'API est démarré seul dans cet environnement ;
- **Then** l'hôte **répond** et **sert le document de description OpenAPI** de son canal d'écriture ;
  **et** la **page d'exploration interactive** des endpoints est **accessible**.

## Tests d'intégration (ordonnés)

| # | Test (FLFI) | TPP | Contradiction | Status |
|---|-------------|-----|---------------|--------|
| 1 | `Should_Servir_le_document_de_description_OpenAPI_du_canal_d_ecriture_When_l_hote_d_API_demarre_sans_front` | {} → document servi | **Driver headless** : prouve que l'hôte API démarre et sert son **document OpenAPI** sans aucune dépendance front. Le rouge force que `MapOpenApi` soit servi par `ApiProgram` y compris sans environnement de développement Web front. ⚠️ Le **démarrage sans front** est déjà acquis par le test d'architecture du Sc.1 (#1) + la fabrique `WebApplicationFactory<ApiProgram>` (qui ne charge jamais le front) ; ce test est une **caractérisation headless de la description servie**, son driver propre est la servabilité du **document** dans cet environnement nu. ⚠️ probablement early green si Sc.3 a déjà servi l'exploration sur le même hôte. | ✅ GREEN (caractérisation) |
| 2 | `Should_Rendre_la_page_d_exploration_interactive_accessible_When_l_hote_d_API_demarre_sans_front` | document servi → exploration accessible | **Caractérisation (anti early-green confirmé)** : l'exploration Scalar est déjà servie par le Sc.3 #1 sur ce même hôte API. Ici on confirme qu'elle reste **accessible en environnement headless** (aucune dépendance front au démarrage). ⚠️ **probablement early green — couvert par Sc.3 #1 (caractérisation, pas driver)** : si Scalar est monté inconditionnellement sur `ApiProgram`, ce test passe d'emblée. Garde-le comme filet headless ; `tdd-auto` le marquera `GREEN (caractérisation)`. | ✅ GREEN (caractérisation) |

> **Note de priorisation.** Ce scénario est principalement un **filet de non-régression headless** :
> son seul rouge potentiellement neuf est #1 (servabilité du document OpenAPI sur l'hôte API détaché,
> si non déjà couvert par Sc.3). #2 est une caractérisation de l'accessibilité Scalar déjà introduite
> au Sc.3. Aucune règle métier nouvelle.

## Fichiers à créer / modifier

- Fichier de tests d'intégration headless dans `tests/PlanningDeGarde.Api.Tests/` (réutilise la
  fabrique `WebApplicationFactory<ApiProgram>`).
- Le cas échéant, s'assurer que `MapOpenApi`/Scalar sont servis **inconditionnellement** (pas
  seulement sous `IsDevelopment`) pour être accessibles en environnement headless. — ajustement `tdd-auto`.

## Design notes

- **Headless = fabrique de test** : `WebApplicationFactory<ApiProgram>` ne charge jamais le projet
  front ; elle **est** l'environnement « aucun front déployé ni référencé ». Le test d'architecture du
  Sc.1 garantit le découplage structurel ; ce scénario garantit la **servabilité** dans cet environnement.
- **Environnement** : si l'OpenAPI/Scalar étaient conditionnés à `IsDevelopment` (comme sur l'ancien
  hôte Web), le démarrage headless les rendrait indisponibles → ce scénario force à lever cette
  condition sur l'hôte API (l'exploration est l'objet même de l'hôte ouvert).
- **Réutiliser** les assertions OpenAPI/Scalar du Sc.3 ; ne dupliquer que la **dimension headless**.
