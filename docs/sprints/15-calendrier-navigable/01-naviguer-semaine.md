# Scénario 1 — Naviguer d'une semaine vers le futur ou le passé

`@nominal` · **🖥️ scénario IHM** — **Routé vers `ihm-builder`** · **acceptation RUNTIME** (app réellement
câblée : render mode interactif, état de session, re-requête de l'API distante). **Pas** un test backend à
doublures.

[← Retour au suivi](00-sprint15-suivi.md)

Cliquer « Semaine suivante / précédente » **décale la fenêtre** d'une semaine et re-projette ; le fond se
re-résout à la date naviguée ; **aucune écriture** n'est émise (lecture seule).

**Ancrage** : aujourd'hui = mercredi 10/06/2026, semaine en cours lundi 08/06/2026, vue par défaut 4
semaines. Suivante → lundi 15/06 (ISO 25 impaire → Bruno) ; précédente → lundi 01/06 (ISO 23 impaire →
Bruno).

## Acceptation (BDD) — niveau RUNTIME — ✅ GREEN

`Should_Decaler_la_fenetre_d_une_semaine_au_lundi_15_06_puis_au_lundi_01_06_et_re_resoudre_le_fond_sur_Bruno_sans_emettre_d_ecriture_When_un_acteur_clique_Semaine_suivante_puis_Semaine_precedente_sur_l_app_reellement_cablee`
(`tests/PlanningDeGarde.Web.Tests/FrontWasmNavigationSemaineTempsReelTests.cs`)
— sur l'app **réellement câblée** (front WASM + API distante), un clic « Semaine suivante » re-requête
`GET /api/grille/2026/6/15?vue=4semaines` et affiche la fenêtre démarrant au lundi 15/06 (jours en ISO 25,
fond Bruno) ; « Semaine précédente » ramène au lundi 01/06 (ISO 23, fond Bruno) ; le canal d'écriture
**n'est jamais sollicité**. bUnit seul ne prouverait pas la re-requête réelle ni le render mode.

## Inner-loop (boucle rapide pilotée par `ihm-builder` — PAS la preuve d'acceptation)

| # | Test inner-loop (état de navigation) | Contradiction | Status |
|---|--------------------------------------|---------------|--------|
| 1 | `Should_Avancer_l_ancre_au_lundi_suivant_When_l_utilisateur_demande_la_semaine_suivante` | L'ancre fixe (semaine de référence) ne se décale pas ; force un état d'ancre mutable `SemaineSuivante()`. **Driver nav.** | ✅ GREEN |
| 2 | `Should_Reculer_l_ancre_au_lundi_precedent_When_l_utilisateur_demande_la_semaine_precedente` | Symétrique : force `SemainePrecedente()` (décalage de −7 j). **Driver nav.** | ✅ GREEN |

> Inner-loop dans `tests/PlanningDeGarde.Web.Tests/SessionPlanningNavigationTests.cs`.

## Fichiers à créer / modifier

- `src/PlanningDeGarde.Web/Components/Pages/PlanningPartage.razor` (+ `.razor.cs`) — contrôles **Semaine
  précédente / suivante** ; re-requête de l'API distante à l'ancre décalée ; render mode interactif vérifié.
- `src/PlanningDeGarde.Web/State/…` (état de session du planning) — **ancre courante + vue** en
  session/mémoire (ne persiste pas) ; `SemaineSuivante()` / `SemainePrecedente()`.
- *(prérequis backend)* endpoint vue + `GrilleAgendaQuery` vue/span (Sc.2/Sc.3) — la navigation re-requête
  `GET /api/grille/{date}?vue=…`.

## Design notes

- **Navigation = re-projection front, AUCUN backend neuf.** Le décalage d'ancre se traduit par une
  re-requête à une date différente ; `GrilleAgendaQuery` re-résout déjà le fond **par date**. Tout « le
  fond affiche Bruno » est une **caractérisation** du backend déjà acquis, jamais un driver ici.
- **Dépend de Sc.2/Sc.3** (endpoint vue + span) : ordonner la navigation **après** la livraison du read
  model vue/span. Le `ihm-builder` câble la re-requête sur ce read model.
- **État de navigation en session/mémoire seulement** (ancre + vue) — **ne persiste pas** au redémarrage
  (distinct de la persistance domaine du bloc C).
- **Lecture seule** : la navigation ne sollicite jamais le canal d'écriture (assertion runtime « aucune
  écriture émise »). → remonter au CP si l'ergonomie des contrôles (placement, libellés) est contestée au
  gate visuel.
