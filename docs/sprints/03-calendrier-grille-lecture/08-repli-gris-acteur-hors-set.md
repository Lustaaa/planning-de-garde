# Scénario 8 — Un acteur absent du set reçoit le repli gris quand un acteur du set garde sa couleur `@erreur`

[← Retour au suivi](00-sprint03-suivi.md)

> **Routage : backend → `tdd-auto`.** Couleur de repli neutre (gris) déterministe pour
> un acteur absent du set, par `GrilleAgendaQuery`, couplée à la couleur d'un acteur
> couvert (anti early-green).

> **Acceptation (BDD)** —
> `Should_Attribuer_le_repli_gris_au_creneau_Grand_mere_et_le_vert_au_creneau_nounou_dans_la_case_du_mardi_23_06_2026_When_le_set_de_couleurs_couvre_Nounou_mais_pas_Grand_mere`
> Test unitaire de projection : set Nounou = vert mais **sans** « Grand-mère » ; deux
> slots de Léa le 23/06/2026 (nounou 09:00→12:00, Grand-mère 14:00→18:00) ; date de réf
> 24/06/2026 → dans la `JourCase` du 23/06, le `SlotCase` « nounou » porte vert et le
> `SlotCase` « Grand-mère » porte la couleur de repli (gris), **distincte** du vert.
> Couple acteur couvert (vert) + acteur non couvert (repli) dans la même case.

## Tests unitaires (ordonnés TPP)

| # | Test unitaire (FLFI) | TPP | Contradiction | Status |
|---|----------------------|-----|---------------|--------|
| 1 | `Should_Attribuer_la_couleur_de_repli_gris_au_creneau_Grand_mere_When_l_acteur_Grand_mere_est_absent_du_set_de_couleurs` | valeur du set → repli (branche par défaut du mapping) | Driver : le mapping acteur→couleur du Sc.4 n'a **pas** de cas par défaut ; un acteur absent du set produirait une couleur nulle/vide/exception. Ce test force la **branche de repli déterministe** (gris) quand l'acteur n'est pas dans le set. | ⏳ Pending |
| 2 | `Should_Conserver_le_vert_de_Nounou_distinct_du_gris_de_repli_dans_la_meme_case_When_un_acteur_couvert_et_un_acteur_non_couvert_partagent_le_jour` | présence + distinction couplées (couvert vs repli) | Driver : couple l'acteur **couvert** (Nounou → vert, déjà piloté Sc.4) à l'acteur **non couvert** (Grand-mère → gris) dans la même case ; une implémentation qui mettrait **tout** en gris (ou tout en vert) échoue. L'assertion vert ≠ gris empêche un repli qui écraserait les couleurs valides. | ⏳ Pending |

## Fichiers à créer / modifier

- `src/PlanningDeGarde.Infrastructure/Foyer.cs` — formaliser la **couleur de repli
  neutre** (gris) du set (constante / valeur par défaut renvoyée pour tout acteur
  inconnu). Le set couvre Nounou = vert mais **délibérément pas** « Grand-mère ».
- `src/PlanningDeGarde.Application/GrilleAgendaQuery.cs` — branche de repli dans le
  mapping acteur→couleur (acteur absent → gris déterministe).
- `tests/PlanningDeGarde.Tests/Scenario_RepliGrisActeurHorsSet.cs`.

## Design notes

- **Défense déterministe** (règle 15) : le repli n'est pas une erreur mais une valeur
  par défaut stable — toujours le **même** gris pour tout acteur inconnu (pas de
  hash/random). Le test asserte une couleur de repli **constante et distincte** des
  couleurs du set.
- **Anti early-green imposé** : ne jamais asserter uniquement « Grand-mère est grise »
  — coupler avec « Nounou reste verte » dans la même case, sinon un mapping qui
  renverrait gris pour **tout le monde** passerait.
- Le repli s'applique au **même endroit** que le mapping acteur→couleur du Sc.4
  (couleur de créneau). Si un repli de **case-jour** (responsable absent du set) est
  souhaité, il dérive du même mécanisme — non couvert par un scénario distinct ici.
- Doubler uniquement les ports. Pas de Blazor.
