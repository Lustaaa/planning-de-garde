# Scénario 2 — Un slot enregistré apparaît dans la case de son jour avec son horaire `@nominal`

[← Retour au suivi](00-sprint03-suivi.md)

> **Routage : backend → `tdd-auto`.** Positionnement d'un slot dans la `JourCase` de
> sa date par `GrilleAgendaQuery`, observable sans Blazor.

> **Acceptation (BDD)** —
> `Should_Placer_le_slot_ecole_08h00_17h00_de_Lea_dans_la_seule_case_du_mardi_23_06_2026_When_un_Parent_consulte_la_grille_le_24_06_2026`
> Test unitaire de projection : un slot « école » de Léa enregistré le 23/06/2026
> 08:00→17:00 dans `FakeSlotRepository`, date de référence 24/06/2026 → la `JourCase`
> du **mardi 23/06/2026** contient un `SlotCase` libellé « école » 08h00–17h00, et
> **aucune autre** des 35 cases ne le contient. (Couplage présence + unicité : une
> projection qui ignore le slot, ou le duplique sur plusieurs jours, échoue.)
>
> **Statut : ✅ GREEN** — acceptation + 3 tests unitaires verts.

## Tests unitaires (ordonnés TPP)

| # | Test unitaire (FLFI) | TPP | Contradiction | Status |
|---|----------------------|-----|---------------|--------|
| 1 | `Should_Faire_apparaitre_le_slot_de_Lea_dans_la_case_du_mardi_23_06_2026_When_ce_slot_est_enregistre_dans_la_fenetre` | tableau vide → tableau peuplé (présence d'un slot) | Driver : la grille du Sc.1 ne lit **pas** les slots (toutes cases sans slot) ; ce test force la lecture de `ISlotRepository` et le rattachement du slot à la case de sa date. | ✅ GREEN |
| 2 | `Should_Exposer_le_libelle_ecole_et_l_horaire_08h00_a_17h00_du_slot_When_le_slot_est_place_dans_sa_case` | présence → valeurs (libellé acteur/lieu + bornes horaires) | Driver : un slot rattaché mais sans libellé/horaire (ou bornes erronées) contredit l'assertion sur « école 08h00–17h00 » ; force le mapping `SlotCase { Libelle, Debut, Fin }` depuis le snapshot. | ✅ GREEN |
| 3 | `Should_Ne_rattacher_le_slot_a_aucune_autre_case_que_celle_de_son_jour_When_la_grille_est_projetee` | unicité (anti-duplication) | Driver : une implémentation naïve qui placerait tout slot dans toutes les cases (ou dans la mauvaise) échoue ; force le rattachement **exact** à `JourCase.Date == slot.Debut.Date`. Couplé au #1 (présence) pour qu'une grille vide ne passe pas. | ✅ GREEN |

## Fichiers à créer / modifier

- `src/PlanningDeGarde.Application/GrilleAgenda.cs` — enrichir `JourCase` d'une
  collection `SlotCase[]` et créer le record `SlotCase { Libelle, Debut, Fin }`
  (la couleur acteur arrive au Sc.4 — ne pas l'anticiper ici).
- `src/PlanningDeGarde.Application/GrilleAgendaQuery.cs` — lecture des slots et
  rattachement à la case de leur jour.
- `tests/PlanningDeGarde.Tests/Scenario_SlotDansCaseDuJour.cs`.

## Design notes

- **Libellé** : le snapshot porte `LieuId` (et `EnfantId`), pas de notion d'« acteur »
  explicite. Le libellé « école » vient du `LieuId`. Trancher en implémentation si le
  libellé = `LieuId` brut (cas Sc.2/Sc.5) ; le set de couleurs (Sc.3-4-8) introduira
  la notion d'acteur — garder cohérent.
- **Horaire** : `SlotSnapshot.Debut`/`Fin` sont des `DateTime` ; le `SlotCase` n'expose
  que la **partie horaire** (08h00–17h00) une fois la case datée fixée.
- Le test #3 réalise l'**anti early-green** : présence (#1) **et** absence ailleurs,
  dans la même grille — prépare le terrain au Sc.7 (hors fenêtre).
- Doubler uniquement `ISlotRepository`/`IPeriodeRepository`. Pas de Blazor.
