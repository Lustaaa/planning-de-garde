# Scénario 4 — Affecter une période sans responsable est refusée et laisse les cases en couleur neutre

`@erreur`

[← Retour au suivi](00-sprint04-suivi.md)

> **Axe : backend.** Canal d'écriture (endpoint HTTP) → `AffecterPeriodeHandler` →
> `PeriodeDeGarde.Affecter` (invariant « un responsable requis ») → store réel → projection
> réelle. Routé vers `tdd-auto`. **Niveau d'acceptation : intégration**
> (`WebApplicationFactory`).
>
> **Garde conditionnelle dès le 1er test `@erreur`** : le nominal Sc.3 (affectation réussie
> via le canal) est vert avant ce scénario ; un refus inconditionnel le régresserait. La
> garde « responsable requis » est **conditionnelle** dès le départ (déjà portée par
> `PeriodeDeGarde.Affecter`, motif « Un responsable est requis pour la période de garde. »).

## Acceptation (BDD)

`Should_Refuser_l_affectation_pour_responsable_manquant_et_laisser_les_cases_du_lundi_22_au_vendredi_26_06_2026_en_couleur_neutre_dans_la_projection_reelle_When_la_commande_d_affectation_sans_responsable_est_emise_via_le_canal` — ✅ GREEN

Test d'**intégration de bout en bout** (`WebApplicationFactory<Program>`, store réel) :
- **Given** aucune période sur la semaine (cases neutres) ;
- **When** la commande d'affectation (du lundi 22 au vendredi 26/06/2026, **sans
  responsable**) est émise via le canal requête/réponse ;
- **Then** le canal renvoie une **réponse d'échec pour responsable manquant** ; **et** la
  projection réelle `Projeter(22/06/2026)` laisse les cases du lundi 22 au vendredi 26/06 en
  **couleur neutre** (aucune couleur de responsable appliquée).

## Tests d'intégration (ordonnés)

| # | Test (FLFI) | TPP | Contradiction | Status |
|---|-------------|-----|---------------|--------|
| 1 | `Should_Renvoyer_une_reponse_d_echec_pour_responsable_manquant_When_la_commande_d_affectation_d_une_periode_sans_responsable_est_emise_via_le_canal` | endpoint succès → endpoint qui propage le refus | **Driver du canal d'erreur** : un canal qui acquitterait toujours en succès (Sc.3) passe le nominal mais échoue ici. Force la **propagation du `Result.Echec`** (motif « responsable requis ») par le canal d'affectation. ⚠️ Le refus « responsable requis » est déjà vert en unit (`Scenario8_PeriodeSansResponsable`) — driver = la **propagation par le canal**. | ✅ GREEN |
| 2 | `Should_Laisser_les_cases_du_lundi_22_au_vendredi_26_06_2026_en_couleur_neutre_dans_la_projection_reelle_When_l_affectation_sans_responsable_a_ete_refusee_via_le_canal` | refus propagé → absence d'effet de bord coloré observée en bout de chaîne | **Anti early-green / vert qui ment** : un canal qui répondrait « échec » mais aurait persisté une période (ou store doublé) passe le #1 mais échoue ici. Force l'observation sur le **store réel** : cases concernées restent à la couleur neutre, aucune couleur de responsable. | ✅ GREEN (caractérisation) |

## Fichiers à créer

- Test d'intégration `tests/PlanningDeGarde.Web.Tests/` (cas d'erreur du canal d'affectation).
- (Endpoint « affecter une période » déjà introduit au Sc.3 — réutilisé.)

## Design notes

- **Convention d'erreur** : `Result<T>.Echec` propagé en réponse d'échec porteuse du motif
  métier ; le mapping de statut est laissé à `tdd-auto`.
- **Absence d'effet coloré** : observer la **projection réelle** (couleur neutre =
  `FoyerPaletteCouleurs.CouleurNeutre` / `gris`) après le canal, pas l'accusé du canal.
- **Aucune notification** : un refus ne déclenche pas la diffusion temps réel.
