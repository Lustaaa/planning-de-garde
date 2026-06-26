# Scénario 7 — Un slot hors fenêtre est exclu tandis qu'un slot interne du même jour-semaine est rendu `@erreur`

[← Retour au suivi](00-sprint03-suivi.md)

> **Routage : backend → `tdd-auto`.** Exclusion des slots hors fenêtre par
> `GrilleAgendaQuery`, couplée à la présence d'un slot interne (anti early-green).

> **Acceptation (BDD)** —
> `Should_Faire_apparaitre_le_slot_ecole_du_mardi_23_06_2026_et_n_inclure_aucune_case_ni_aucun_slot_pour_le_03_08_2026_When_un_slot_interne_et_un_slot_hors_fenetre_sont_enregistres`
> Test unitaire de projection : deux slots « école » de Léa 08:00→17:00, l'un le
> 23/06/2026 (interne), l'autre le 03/08/2026 (hors fenêtre) ; date de réf 24/06/2026
> → la `JourCase` du 23/06 contient le slot « école 08h00–17h00 », **aucune** des 35
> cases n'est datée du 03/08, et le slot du 03/08 n'apparaît dans **aucune** case.
> Couple présence (interne) + absence (hors fenêtre) dans la même grille.

## Tests unitaires (ordonnés TPP)

| # | Test unitaire (FLFI) | TPP | Contradiction | Status |
|---|----------------------|-----|---------------|--------|
| 1 | `Should_Exclure_le_slot_du_03_08_2026_tout_en_rendant_le_slot_du_mardi_23_06_2026_When_un_slot_interne_et_un_slot_hors_fenetre_partagent_le_meme_jour_semaine_et_le_meme_horaire` | présence + absence couplées (filtrage par appartenance à la fenêtre) | Driver : une implémentation qui rattache un slot à une case par jour-de-semaine ou par heure (sans vérifier la **date** d'appartenance à la fenêtre) ferait apparaître le slot du 03/08 dans une case interne (ex. le mardi) — la double assertion (interne présent + 03/08 absent partout) le débusque. Force le filtrage strict `slot.Date ∈ fenêtre` + rattachement par date complète. | ✅ GREEN (caractérisation) |
| 2 | `Should_Ne_creer_aucune_case_datee_du_03_08_2026_When_la_fenetre_s_arrete_au_dimanche_26_07_2026` | absence de case hors borne (couplée à la cardinalité 35) | Driver : couple l'absence de case 03/08 à la présence des 35 cases internes ; une grille qui s'étendrait jusqu'au slot le plus lointain (au lieu de 35 jours fixes) échoue. Caractérisation de la borne du Sc.1 renforcée par la donnée hors fenêtre. | ✅ GREEN (caractérisation) |

## Fichiers à créer / modifier

- `src/PlanningDeGarde.Application/GrilleAgendaQuery.cs` — filtrage des slots par
  appartenance de leur **date** à la fenêtre (les slots hors [22/06..26/07] sont
  ignorés).
- `tests/PlanningDeGarde.Tests/Scenario_SlotHorsFenetreExclu.cs`.

## Design notes

- **Anticipation early-green** : si l'implémentation du Sc.2 rattache déjà les slots
  par **date complète** (`JourCase.Date == slot.Debut.Date`) — réflexe naturel — alors
  un slot hors fenêtre n'a simplement **aucune case d'accueil** et le test #1 passe
  d'emblée → marquer `✅ GREEN (caractérisation)`. C'est **voulu** : ce scénario
  documente et verrouille l'exclusion. Le risque réel qu'il pilote est l'implémentation
  **paresseuse** par jour-de-semaine/heure ; le choix des deux slots **même jour-semaine
  + même horaire** (mardi 23/06 vs lundi 03/08, 08:00–17:00) est l'**anti early-green**
  qui rend la contradiction réelle si ce raccourci est pris.
- **Couplage présence + absence** imposé par le fichier source : ne jamais asserter
  uniquement l'absence — une grille vide passerait. Le slot interne **doit** être
  présent dans la même grille.
- Le slot « franchissant minuit hors fenêtre » a été **absorbé** ici (cf. risques du
  fichier source) : pas de scénario distinct.
- Doubler uniquement les ports. Pas de Blazor.
