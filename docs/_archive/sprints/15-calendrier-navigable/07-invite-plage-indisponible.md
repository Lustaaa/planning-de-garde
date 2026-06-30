# Scénario 7 — Sélection de plage indisponible en consultation seule

`@erreur` · **🖥️ scénario IHM** — **Routé vers `ihm-builder`** · **acceptation RUNTIME**. ⚠️ **early green
(gate `EstParent` + trigger de plage Sc.5)** — gating règle 9 mutualisé, **gate vérifié présent** sur
`PlanningPartage`.

[← Retour au suivi](00-sprint15-suivi.md)

Un Invité (ou acteur « Autre » incarné) **navigue librement** (clic « Semaine suivante » → lundi 15/06)
mais **ne peut affecter aucune période par plage** : tenter de sélectionner une plage **n'ouvre aucun
déclencheur d'écriture ni dialog**, et **aucune période n'est enregistrée**.

## Acceptation (BDD) — niveau RUNTIME — ✅ GREEN (early green ANTICIPÉ, caractérisation)

`Should_Permettre_la_navigation_au_lundi_15_06_mais_n_ouvrir_aucun_declencheur_ni_dialog_d_affectation_et_n_enregistrer_aucune_periode_When_un_Invite_tente_de_selectionner_une_plage_de_cases_sur_l_app_reellement_cablee`
(`tests/PlanningDeGarde.Web.Tests/FrontWasmInvitePlageIndisponibleTempsReelTests.cs`)
— sur l'app réellement câblée sous identité Invité (sélecteur de rôle réel) : la navigation fonctionne
(« Semaine suivante » → fenêtre au lundi 15/06/2026) ; toute tentative de sélection de plage est inerte
(bouton `mode-plage` absent, aucun menu, aucune dialog, aucune case sélectionnée), **zéro POST d'écriture**
émis (espion de transport sur `/api/canal/`).

**Non-vacuité (RED signifiant)** : contrôle positif AVANT le négatif — en **Parent**, le déclencheur de plage
`mode-plage` est **présent** (sans cette borne, l'absence vérifiée en Invité serait un faux vert). Aucun
changement de production : le gate `EstParent` + le trigger de plage posés par Sc.5 rendent l'écriture par
plage morte par construction en consultation ; ce test **caractérise** l'inertie du gating mutualisé, sans
rien tirer en avant.

## Inner-loop (boucle rapide `ihm-builder`)

| # | Test inner-loop (gating de la plage) | Contradiction | Status |
|---|--------------------------------------|---------------|--------|
| 1 | `Should_Ne_pas_ouvrir_le_declencheur_de_plage_When_l_acteur_n_est_pas_Parent_ni_Admin` | ⚠️ early green CONFIRMÉ (gate partagé) — le déclencheur de plage de Sc.5 est gardé `Session.EstParent` (gate **présent** sur `PlanningPartage`, `OuvrirMenu`/`BasculerModePlage`/`@if EstParent`) ; en consultation, le geste de plage est inerte par construction. Caractérisation du gating mutualisé, **pas** un driver — couvert par l'acceptation runtime. | ✅ GREEN |

## Fichiers à créer / modifier

- `src/PlanningDeGarde.Web/Components/Pages/PlanningPartage.razor` (+ `.razor.cs`) — le déclencheur de
  sélection de plage (Sc.5) hérite du gating `Session.EstParent` ; navigation **non** gardée (l'Invité
  navigue).

## Design notes

- **Contrôle de cohérence du gating (leçon s13)** : le gate `EstParent` est **vérifié présent** sur l'écran
  cible (`PlanningPartage` — `OuvrirMenu` et `@if Session.EstParent`). Le trigger de plage de Sc.5 réutilise
  ce même gate → **early green** une fois Sc.5 bâti gardé. À **batcher** comme caractérisation, pas un
  early-green inattendu.
- **Navigation NON gardée** : seul l'**écriture** (plage / déclencheurs) est réservée Parent/Admin (règle
  9) ; la lecture/navigation reste ouverte à l'Invité (règle 14, lecture seule).
- L'identité effective (Invité, ou « Autre » incarné — s14) pilote `EstParent` ; le gating lit l'identité
  effective sans changement. → remonter au CP si un cas d'« Autre » incarné devait, à l'inverse, ouvrir un
  sous-ensemble d'écriture (non prévu ici).
