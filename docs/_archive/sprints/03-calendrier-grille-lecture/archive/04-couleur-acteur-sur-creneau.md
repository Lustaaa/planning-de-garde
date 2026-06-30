# Scénario 4 — Le slot d'un acteur non-responsable porte sa propre couleur sur son créneau `@nominal`

[← Retour au suivi](00-sprint03-suivi.md)

> **Routage : backend → `tdd-auto`.** Deuxième niveau de couleur (couleur propre du
> slot/acteur, distincte de la couleur de la case-jour) par `GrilleAgendaQuery`.

> **Acceptation (BDD)** —
> `Should_Porter_la_couleur_de_Parent_A_sur_la_case_du_jeudi_25_06_2026_et_la_couleur_de_Nounou_sur_le_creneau_nounou_17h00_19h00_a_l_interieur_de_cette_case_When_une_periode_confie_Lea_a_Parent_A_ce_jour_et_un_slot_nounou_y_est_enregistre`
> Test unitaire de projection : set Parent A = bleu, Nounou = vert ; période confiant
> Léa à Parent A le 25/06/2026 + slot « nounou » 17:00→19:00 ce jour ; date de réf
> 24/06/2026 → la `JourCase` du jeudi 25/06 porte `CouleurResponsable` = bleu (Parent A)
> **et** son `SlotCase` « nounou » porte `CouleurActeur` = vert (Nounou). Deux niveaux
> de couleur **coexistants et distincts** dans la même case.
>
> **Statut : ✅ GREEN** — acceptation + 2 tests unitaires verts.

## Tests unitaires (ordonnés TPP)

| # | Test unitaire (FLFI) | TPP | Contradiction | Status |
|---|----------------------|-----|---------------|--------|
| 1 | `Should_Attribuer_au_creneau_nounou_la_couleur_propre_de_Nounou_When_un_slot_nounou_est_place_dans_la_case` | constante → valeur dérivée (mapping acteur du slot → couleur) | Driver : le `SlotCase` du Sc.2 n'a **pas** de couleur (ou hérite de la case-jour) ; ce test force le mapping de l'**acteur du slot** vers sa propre couleur via le set, indépendamment du responsable de la journée. | ✅ GREEN |
| 2 | `Should_Faire_coexister_la_couleur_de_journee_de_Parent_A_et_la_couleur_de_creneau_de_Nounou_dans_la_meme_case_When_une_periode_de_Parent_A_couvre_le_jour_du_slot_nounou` | coexistence de deux niveaux distincts | Driver : une implémentation qui ferait porter au slot la couleur de la case-jour (un seul niveau) satisfait Sc.3 mais échoue ici (le créneau doit être vert, la journée bleue) ; force la **séparation** des deux niveaux de couleur. | ✅ GREEN |

## Fichiers à créer / modifier

- `src/PlanningDeGarde.Infrastructure/Foyer.cs` — **étendre** le set : Nounou = vert
  (et autres acteurs non-responsables : école, grands-parents…).
- `src/PlanningDeGarde.Application/GrilleAgenda.cs` — `SlotCase.CouleurActeur`.
- `src/PlanningDeGarde.Application/GrilleAgendaQuery.cs` — mapping de l'acteur du slot
  vers sa couleur (set), distinct du mapping responsable→case.
- `tests/PlanningDeGarde.Tests/Scenario_CouleurActeurSurCreneau.cs`.

## Design notes

- **Acteur d'un slot** : le `SlotSnapshot` porte `LieuId` (« nounou », « école »… qui
  sont à la fois lieux ET acteurs selon le fichier source — cf. note risques). Le
  mapping couleur s'applique sur cette clé d'acteur. Cohérence avec le libellé du Sc.2.
- Les **deux niveaux** (règles 14-15) : `JourCase.CouleurResponsable` (responsabilité)
  vs `SlotCase.CouleurActeur` (localisation) — orthogonaux, lus du **même** set.
- Le repli neutre (acteur absent du set) est piloté au **Sc.8**, pas ici (Nounou est
  dans le set). Ne pas anticiper le repli.
- Doubler uniquement les ports. Pas de Blazor.
