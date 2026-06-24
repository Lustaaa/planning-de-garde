# Scénario 3 — La case-jour prend la couleur du parent responsable de la période `@nominal`

[← Retour au suivi](00-sprint03-suivi.md)

> **Routage : backend → `tdd-auto`.** Mapping responsable → couleur au niveau
> `JourCase` par `GrilleAgendaQuery`, observable sans Blazor (la couleur est une
> donnée du read model, pas un style rendu).

> **Acceptation (BDD)** —
> `Should_Colorer_les_cases_du_lundi_22_06_au_dimanche_28_06_2026_de_la_couleur_de_Parent_A_distincte_de_celle_de_Parent_B_When_une_periode_confie_Lea_a_Parent_A_sur_cet_intervalle`
> Test unitaire de projection : set de couleurs Parent A = bleu, Parent B = orange ;
> une période confie Léa à Parent A du 22/06 au 28/06/2026 ; date de référence
> 24/06/2026 → chaque `JourCase` du 22/06 au 28/06 porte la couleur de Parent A
> (bleu), **distincte** de la couleur de Parent B (orange).

## Tests unitaires (ordonnés TPP)

| # | Test unitaire (FLFI) | TPP | Contradiction | Status |
|---|----------------------|-----|---------------|--------|
| 1 | `Should_Attribuer_la_couleur_du_parent_responsable_aux_cases_couvertes_par_sa_periode_When_une_periode_confie_Lea_a_Parent_A_sur_un_intervalle_interne` | constante neutre → valeur dérivée (mapping responsable→couleur sur les jours couverts) | Driver : les cases du Sc.1/Sc.2 portent une couleur neutre (aucune lecture des périodes) ; ce test force la lecture de `IPeriodeRepository`, le calcul des jours couverts par la période et l'attribution de la couleur du responsable. | ⏳ Pending |
| 2 | `Should_Donner_a_Parent_A_une_couleur_distincte_de_celle_de_Parent_B_When_le_set_de_couleurs_par_defaut_associe_chaque_parent_a_une_couleur` | distinction par personne (injectivité du set sur les 2 parents) | Driver : une couleur constante / partagée entre responsables (early-green naïf « tous bleus ») satisfait #1 mais contredit la distinction Parent A ≠ Parent B ; force le set acteur→couleur déterministe. | ⏳ Pending |
| 3 | `Should_Conserver_la_couleur_neutre_sur_les_cases_hors_periode_When_aucune_periode_ne_couvre_ces_jours` | présence + absence couplées (jours sans période restent neutres) | Driver : une implémentation qui colorerait toute la grille de la couleur du responsable (au lieu des seuls jours couverts) échoue ; couple coloration des jours internes (#1) et neutralité ailleurs dans la même grille. Prépare le Sc.6 (intersection partielle). | ⏳ Pending |

## Fichiers à créer / modifier

- `src/PlanningDeGarde.Infrastructure/Foyer.cs` — **ajouter** le set de couleurs par
  défaut acteur → couleur (au minimum Parent A = bleu, Parent B = orange ; étendre
  aux acteurs non-responsables aux Sc.4/Sc.8). Repli neutre (gris) — introduit au
  Sc.8, mais la **couleur neutre de case-jour sans période** est utilisée dès ici.
- `src/PlanningDeGarde.Application/GrilleAgenda.cs` — `JourCase.CouleurResponsable`.
- `src/PlanningDeGarde.Application/GrilleAgendaQuery.cs` — lecture des périodes,
  calcul des jours couverts, mapping responsable → couleur via le set.
- `tests/PlanningDeGarde.Tests/Scenario_CouleurResponsableCaseJour.cs`.

## Design notes

- **Accès au set de couleurs depuis l'Application** : `Foyer` est en Infrastructure ;
  la projection (Application) ne doit pas dépendre d'Infrastructure (sens des
  dépendances). Recommandé : un **port / dictionnaire de couleurs** injecté dans la
  projection (l'Infrastructure fournit l'implémentation depuis `Foyer`). L'agent
  d'implémentation tranche ; ne pas faire dépendre Application → Infrastructure.
- **Couleur** = donnée du read model (chaîne / value object couleur), **pas** un style
  CSS rendu : le test assert sur la valeur de `CouleurResponsable`, pas sur du DOM.
- « Jours couverts par la période » : intervalle `[Debut, Fin]` projeté sur les jours
  datés de la fenêtre. La **frontière partielle** (période débordant la fenêtre) est
  traitée au Sc.6 — ici la période est entièrement interne.
- Doubler uniquement les ports. Pas de Blazor.
