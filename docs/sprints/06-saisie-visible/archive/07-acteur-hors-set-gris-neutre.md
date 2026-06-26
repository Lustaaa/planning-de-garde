# Scénario 7 — Acteur hors set retombe sur le neutre (gris assumé)

`@limite`

[← Retour au suivi](00-sprint06-suivi.md)

> **Axe : backend (caractérisation).** Routé vers `tdd-auto`. Un acteur **légitimement absent du
> set** (id stable `grand-pere`, pas encore colorié) reçoit le **repli neutre (gris)** — c'est
> conforme à la règle 17, pas un défaut. Cet invariant est **déjà vert**
> (`IPaletteCouleurs.CouleurDe` renvoie `CouleurNeutre` sur clé absente ; couvert par
> `Scenario_CouleurResponsableCaseJour` / `AffecterPeriodeCanalApiTests` refus). Aucun nouveau
> driver : ce test **caractérise le gris assumé** pour le **distinguer du gris-bug du Sc.8**.
>
> **Niveau d'acceptation : test unitaire** (projection + palette, sans Blazor).

## Acceptation (BDD)

`Should_Colorer_la_case_du_24_06_2026_en_gris_neutre_conforme_When_une_periode_est_affectee_a_un_acteur_d_identifiant_stable_absent_du_set` — ✅ GREEN

- **Given** le set de couleurs **ne contient pas** l'identifiant `grand-pere` ; une période est
  affectée au responsable d'**identifiant stable** « grand-pere » le **24/06/2026** ;
- **When** la grille est projetée ;
- **Then** la **case du 24/06/2026 est grise** par **repli neutre conforme** ; **et** ce gris
  traduit un **acteur non encore colorié**, pas un défaut de résolution.

## Tests

| # | Test unitaire (FLFI) | TPP | Contradiction | Status |
|---|----------------------|-----|---------------|--------|
| 1 | `Should_Replier_la_case_du_24_06_2026_sur_la_couleur_neutre_When_l_acteur_grand_pere_est_absent_du_set_de_couleurs_mais_bien_affecte` | valeur dérivée (mapping) → repli neutre | ⚠️ probablement early green — couvert par le **contrat** d'`IPaletteCouleurs.CouleurDe` (renvoie `CouleurNeutre` sur clé absente, impl réelle **et** `FakePaletteCouleurs`) — caractérisation, pas driver. Discriminance : la période est **bien présente** (id `grand-pere` affecté, case **couverte**) — donc le gris vient du **repli légitime**, pas d'une absence de période (sinon ce serait le même gris que les jours non couverts). Asserter que la case **couverte** par `grand-pere` est grise **et** distincte d'un parent du set (ex. `parent-a`=bleu coexistant). | ✅ GREEN (caractérisation) |

## Fichiers à créer

- **`tests/PlanningDeGarde.Tests/Scenario_ActeurHorsSetGris.cs`** (ou nom équivalent) — test
  xUnit de projection + palette doublée (`FakePaletteCouleurs` sans `grand-pere`). Réutilise
  `FakePeriodeRepository`, `PeriodeDeGarde.Affecter`.

## Design notes

- **Caractérisation, pas driver** : `tdd-auto` doit s'attendre à **GREEN dès le 1er passage**
  (`✅ GREEN (caractérisation)`). Le repli neutre sur clé absente est garanti par le **contrat du
  port** — aucun code neuf.
- **Gris assumé ≠ gris-bug** : ici l'acteur est **légitimement hors set** (id stable valide mais
  non colorié) → gris **conforme** (règle 17). Le Sc.8 montre le gris **provoqué par un libellé**
  fourni à la place de l'identifiant — même couleur, cause opposée.
- **Discriminance** : asserter que la case **couverte par la période** (id `grand-pere`) est
  grise, en présence d'un autre responsable du set (`parent-a`=bleu) coexistant, pour ne pas
  confondre avec le gris d'une case **non couverte**.
- **Pas d'IHM ici** : projection pure, palette doublée à la main.
