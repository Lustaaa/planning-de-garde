# Scénario 5 — Le front sur une origine distincte est autorisé par le CORS de l'API distante

`@limite`

[← Retour au suivi](00-sprint05-suivi.md)

> **Axe : backend.** Le découplage distant introduit le **cross-origin** : le front sur
> `https://app.planning.local` écrit vers l'API sur `https://api.planning.local`. Le driver est la
> **politique CORS de l'hôte d'API** qui **autorise l'origine du front** : une requête cross-origin
> portant l'`Origin` du front est **acceptée** et confirme l'effet. Observable à la frontière HTTP de
> l'API via `WebApplicationFactory<ApiProgram>` (en émettant la requête avec l'en-tête `Origin` et en
> vérifiant l'en-tête de réponse CORS + le succès + l'effet sur le store réel). Routé vers `tdd-auto`.
>
> **Niveau d'acceptation : intégration** (`WebApplicationFactory<ApiProgram>`), pas unit ni bUnit.

## Acceptation (BDD)

`Should_Accepter_l_ecriture_cross_origin_et_faire_apparaitre_le_slot_ecole_dans_la_case_du_mercredi_24_06_2026_When_le_front_d_une_origine_autorisee_pose_un_slot_vers_l_API_distante` — ✅ GREEN

Test d'**intégration** sur l'hôte d'API détaché (`WebApplicationFactory<ApiProgram>`) :
- **Given** l'hôte d'API démarré seul ; le front depuis l'origine `https://app.planning.local`, **autorisée
  par le CORS** de l'API ; le foyer connaît « école » ; aucun slot pour le mercredi 24/06/2026 ;
- **When** le front, depuis `https://app.planning.local`, émet vers l'API une **pose de slot** cross-origin
  (Léa, « école », 24/06/2026 08:30→16:30) — requête portant l'en-tête `Origin` du front ;
- **Then** la requête cross-origin est **acceptée** par l'API (en-tête CORS autorisant l'origine), qui
  **confirme l'effet** ; **et** dans la grille projetée à la semaine du lundi 22/06/2026, la case du
  mercredi 24/06/2026 porte un slot « école » de 08:30 à 16:30 (store réel).

## Tests d'intégration (ordonnés)

| # | Test (FLFI) | TPP | Contradiction | Status |
|---|-------------|-----|---------------|--------|
| 1 | `Should_Autoriser_l_origine_du_front_When_une_requete_cross_origin_du_front_atteint_le_canal_d_ecriture_de_l_API` | nil → en-tête CORS autorisant l'origine | **Driver CORS** : sans politique CORS, une requête portant l'`Origin` du front n'obtient pas l'en-tête d'autorisation cross-origin. Le rouge force l'ajout d'une politique CORS sur `ApiProgram` autorisant l'origine du front (`https://app.planning.local`), vérifiée par l'en-tête `Access-Control-Allow-Origin` de la réponse. | ✅ GREEN |
| 2 | `Should_Faire_apparaitre_le_slot_ecole_08h30_16h30_dans_la_case_du_mercredi_24_06_2026_dans_la_projection_reelle_When_la_pose_cross_origin_du_front_a_abouti_via_l_API` | origine autorisée → effet observé en bout de chaîne | **Driver de bout en bout (anti early-green)** : une autorisation CORS qui n'aboutirait pas à l'écriture passe #1 mais échoue ici. Confirme que la pose cross-origin **transite réellement** jusqu'au store réel lu par la projection. ⚠️ La pose « slot dans la case du jour » est **déjà verte** (Application + Sc.1 sprint 04 + Sc.2 ce sprint) ; ici c'est sa **première observation sous contrainte cross-origin** — caractérisation du chemin CORS. | ✅ GREEN (caractérisation) |

## Fichiers à créer / modifier

- Politique **CORS** sur `src/PlanningDeGarde.Api` (`ApiProgram`) autorisant l'origine du front
  (origine configurable). — scaffolding `tdd-auto`.
- Fichier de tests d'intégration CORS dans `tests/PlanningDeGarde.Api.Tests/` (émission avec en-tête
  `Origin`, vérification de l'en-tête de réponse + effet store réel).

## Design notes

- **Origine configurable** : l'origine autorisée du front est une **donnée de config** de l'hôte API
  (pas figée), cohérente avec l'URL d'API configurable côté front (Sc.2).
- **Observable double** : l'acceptation CORS (en-tête de réponse) **et** l'effet métier (slot dans la
  case via store réel). Un test qui n'asserterait que l'en-tête CORS « mentirait au vert » sur l'écriture.
- **Préflight** : le cas nominal piloté est la requête simple acceptée ; le préflight `OPTIONS` est un
  détail de la politique CORS (couvert mécaniquement par la configuration), non scénarisé en propre.
- **Réutiliser** la fabrique `WebApplicationFactory<ApiProgram>` + la projection réelle du Sc.1.
