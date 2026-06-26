# Scénario 5 — Plusieurs slots d'un même jour sont empilés dans l'ordre horaire `@limite`

[← Retour au suivi](00-sprint03-suivi.md)

> **Routage : backend → `tdd-auto`.** Ordre des `SlotCase[]` d'une `JourCase` par
> heure de début, par `GrilleAgendaQuery`.

> **Acceptation (BDD)** — ✅ GREEN —
> `Should_Lister_les_trois_slots_du_vendredi_26_06_2026_dans_l_ordre_domicile_A_07h00_puis_ecole_08h30_puis_nounou_16h30_When_trois_slots_de_Lea_sont_enregistres_ce_jour_la`
> Test unitaire de projection : trois slots de Léa le 26/06/2026 (domicile A
> 07:00→08:30, école 08:30→16:30, nounou 16:30→18:30) enregistrés **dans le désordre**
> dans `FakeSlotRepository` ; date de réf 24/06/2026 → la `JourCase` du vendredi 26/06
> liste les `SlotCase` dans l'ordre horaire : domicile A, puis école, puis nounou.

## Tests unitaires (ordonnés TPP)

| # | Test unitaire (FLFI) | TPP | Contradiction | Status |
|---|----------------------|-----|---------------|--------|
| 1 | `Should_Empiler_les_trois_slots_du_vendredi_26_06_2026_dans_l_ordre_des_heures_de_debut_When_ils_sont_enregistres_dans_le_desordre` | unconditional → ordre (tri par heure de début) | Driver : enregistrer les slots **désordonnés** dans le fake et vérifier l'ordre rendu ; une projection qui conserve l'ordre d'insertion (early-green naïf) échoue. Force le tri par `Debut`. | ✅ GREEN |
| 2 | `Should_Conserver_les_trois_slots_distincts_dans_la_meme_case_When_ils_partagent_le_meme_jour` | présence + cardinalité (3 slots, pas de fusion/perte) | Driver : couple l'ordre (#1) au fait que les **trois** slots coexistent dans la case (cardinalité 3, libellés distincts) ; une implémentation qui n'en garderait qu'un (ou les fusionnerait) échoue. Anti early-green sur le tri. | ✅ GREEN (caractérisation) |

## Fichiers à créer / modifier

- `src/PlanningDeGarde.Application/GrilleAgendaQuery.cs` — tri des `SlotCase` d'une
  case par heure de début (`OrderBy(s => s.Debut)`).
- `tests/PlanningDeGarde.Tests/Scenario_SlotsEmpilesOrdreHoraire.cs`.

## Design notes

- **Anticipation early-green** : si l'implémentation du Sc.2 a déjà trié les slots par
  `Debut` (réflexe naturel, cf. `Charger()` existant qui fait `OrderBy(s => s.Debut)`),
  le test #1 peut être **early green**. Dans ce cas le marquer
  `✅ GREEN (caractérisation)` — le driver réel reste le couplage cardinalité (#2). Le
  tri **doit** néanmoins être piloté explicitement par un cas désordonné (≠ ordre
  d'insertion) pour que la non-régression soit réelle.
- Pas de chevauchement métier ici (slots contigus 07:00→08:30→16:30→18:30) : pur ordre
  d'affichage, pas de règle de validation.
- Doubler uniquement les ports. Pas de Blazor.
