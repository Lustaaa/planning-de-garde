# Scénario 3 — Affecter une période via le canal colore les cases-jour couvertes à la couleur du responsable

`@nominal`

[← Retour au suivi](00-sprint04-suivi.md)

> **Axe : backend.** Canal d'écriture (endpoint HTTP) → `AffecterPeriodeHandler` **inchangé**
> → store réel → projection réelle `GrilleAgendaQuery`. Routé vers `tdd-auto`.
> **Niveau d'acceptation : intégration** (`WebApplicationFactory`).

## Acceptation (BDD)

`Should_Colorer_les_cases_jour_du_lundi_22_au_vendredi_26_06_2026_de_la_couleur_bleue_de_Parent_A_dans_la_projection_reelle_When_la_commande_d_affectation_de_periode_est_emise_via_le_canal` — ✅ GREEN

Test d'**intégration de bout en bout** (`WebApplicationFactory<Program>`, store réel) :
- **Given** le foyer connaît « Parent A » de couleur par défaut bleu (palette réelle) ;
  aucune période sur la semaine (cases neutres) ;
- **When** la commande d'affectation (Parent A, du lundi 22 au vendredi 26/06/2026) est émise
  via le canal requête/réponse ;
- **Then** le canal renvoie une **réponse de succès** ; **et** la projection réelle
  `Projeter(22/06/2026)` porte la couleur bleue de Parent A sur les cases du lundi 22 au
  vendredi 26/06.

> **Note palette** : la couleur par défaut de « Parent A » est associée par la palette réelle
> (`FoyerPaletteCouleurs` → clé `parent-a` = `bleu`). Vérifier la **clé** attendue par
> l'endpoint (libellé « Parent A » vs id `parent-a`) lors du câblage ; cohérence à confirmer
> en bout de chaîne, sans dupliquer la règle de coloration (déjà verte).

## Tests d'intégration (ordonnés)

| # | Test (FLFI) | TPP | Contradiction | Status |
|---|-------------|-----|---------------|--------|
| 1 | `Should_Confirmer_l_affectation_par_une_reponse_de_succes_When_la_commande_d_affectation_d_une_periode_avec_responsable_est_emise_via_le_canal_requete_reponse` | nil → endpoint d'affectation qui acquitte | **Driver du canal d'affectation** : aucun endpoint d'affectation de période n'existe sur le canal. Le rouge force le câblage HTTP → `AffecterPeriodeHandler` + acquittement de succès. ⚠️ Le handler « affectation réussie » est déjà vert (`Scenario7_AffecterPeriode`) — driver = le **câblage du canal**, pas la règle. | ✅ GREEN |
| 2 | `Should_Colorer_les_cases_jour_du_lundi_22_au_vendredi_26_06_2026_de_la_couleur_de_Parent_A_dans_la_projection_reelle_When_l_affectation_a_abouti_via_le_canal` | endpoint acquitté → effet coloré observé en bout de chaîne | **Driver de bout en bout (anti early-green)** : un canal qui acquitterait sans persister dans le **store réel** passe le #1 mais échoue ici. Force le chemin réel canal → handler → store périodes singleton → coloration par la projection réelle. ⚠️ La coloration des cases couvertes est déjà verte en isolé (`Scenario_CouleurResponsableCaseJour`) ; ici **première observation sur le store réel après le canal** (caractérisation de la chaîne). | ✅ GREEN (caractérisation) |

## Fichiers à créer

- Test d'intégration `tests/PlanningDeGarde.Web.Tests/` (canal d'affectation de période,
  niveau `WebApplicationFactory`).
- Endpoint HTTP du canal d'écriture « affecter une période » sur l'hôte Web (production) —
  scaffolding créé par `tdd-auto`.

## Design notes

- **Store réel + palette réelle** : observer la projection réelle (`GrilleAgendaQuery` +
  `FoyerPaletteCouleurs`) après le canal ; pas de doublure de palette sur le chemin observé.
- **Date de référence injectée** : `22/06/2026`.
- **`AffecterPeriodeHandler` inchangé** : il ne valide pas l'existence du responsable (seul
  l'invariant « responsable non vide » est porté par `PeriodeDeGarde.Affecter`) — voir Sc.4.
