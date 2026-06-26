# Sc.3 — Fenêtre sans aucune affectation : légende masquée

`@limite` `🖥️ IHM`

↩ Retour : [00-sprint07-suivi.md](00-sprint07-suivi.md)

**Routage** : le **driver réel est IHM** (masquage du bloc légende dans le `.razor`, routé
`ihm-builder`). Le backend ne porte qu'une **caractérisation** (`tdd-auto`, ⚠️ early green).

## Acceptation (BDD) — niveau **runtime/IHM** (routé `ihm-builder`)

`Should_N_afficher_aucun_nom_dans_les_cases_et_masquer_entierement_le_bloc_legende_When_la_grille_reellement_cablee_est_affichee_sans_aucune_periode_dans_la_fenetre`

- **Niveau** : E2E/runtime sur l'app câblée.
- **Observable** : aucune case ne porte de nom de responsable **et** le bloc légende est
  **absent du rendu** (pas seulement vide visuellement) — driver de **présentation** que
  bUnit-sur-app-réelle / E2E attrape (le `.razor` doit masquer le bloc quand la légende est
  vide).

## Tests unitaires backend (boucle interne, `tdd-auto` sur `GrilleAgendaQuery`)

| # | Test unitaire (FLFI) | TPP | Contradiction | Status |
|---|----------------------|-----|---------------|--------|
| 1 | `Should_Produire_une_legende_vide_et_aucune_case_nommee_When_aucune_periode_ne_couvre_la_fenetre` | collection vide (cas dégénéré) | ⚠️ probablement early green — couvert par Sc.1 #1/#2 : la légende est dérivée des **présents** (aucun → liste **vide**) et le nom n'est résolu que sur les **cases couvertes** (aucune → pas de nom). **Caractérisation, pas driver.** Le **vrai driver** est le **masquage du bloc** côté `.razor` (rendu), routé `ihm-builder`. | ✅ GREEN (caractérisation) |

## Fichiers à créer / modifier

- *(Aucun fichier backend — caractérisation sur le read model existant.)*
- **Driver IHM** (routé `ihm-builder`) : `src/PlanningDeGarde.Web/Components/Pages/PlanningPartage.razor` — rendre le bloc Légende **conditionnellement** (absent si la légende est vide).

## Design notes

- Le skill prévient : un scénario dont **tous** les tests backend sont des caractérisations
  n'apporte aucun rouge backend. Ici c'est le cas — la **valeur** du scénario est le
  **masquage** (IHM). Le test #1 reste comme **filet** (la légende ne doit jamais inventer
  d'entrée sur fenêtre vide ; on ne « ment » pas en suggérant que personne ne garde via une
  légende fantôme).
- Distinguer **légende vide** (masquée, conforme) d'un **échec de lecture** (« planning
  indisponible ») — ce dernier est **hors sujet** (candidat backlog règle 25 côté read,
  cf. analyse technique).
