# Retours — Sprint 12 (transfert en contexte)

> **Fichier unifié.** Il porte deux choses, consommées par deux étapes différentes :
> - **Retours produit (PO)** ci-dessous → lus par `/4-retours` (challenge + besoins).
> - **Méthode (agents)** + **`## IA`** plus bas → lus par `retro-sprint` en fin de sprint.
>
> Créé à l'analyse `/3` (par `tdd-analyse`). La partie produit est préparée vide ici et
> remplie par le PO après le gate visuel ; la partie méthode est appendée au fil de l'eau
> par le thread principal. Lancement de l'app : `pwsh .claude/skills/run/scripts/run.ps1`.

# Retours produit (PO)

> Le code et les tests unitaires sont **hors scope** ici (revus en revue de code).
> Ces retours portent sur l'**usage de l'IHM** : ce qui marche, ce qui coince, ce qui
> manque à l'écran. Remplis les puces, puis lance `/4-retours`.

## IHM - général

-

## IHM - /planning

-

## Tech (optionnel)

- (contraintes techniques éventuelles ; laisser vide si aucune → bypass dans `/4-retours`)

# Idée pour la suite

> Idées produit que le PO veut verser au backlog pour de futurs sprints (pas forcément le
> prochain). Consommées par `/4-retours` (classées/séquencées) puis replacées dans les épics
> du BACKLOG. Laisser vide si aucune.

-

# Consigne pour la suite

> Consignes directes du PO sur l'orientation à donner à la suite (priorité, cap, contrainte
> de séquencement). Pèsent sur le choix du prochain sujet en `/4-retours` (G2). Laisser vide
> si aucune.

-

# Méthode (agents) — pour retro-sprint

> Retours à la volée du PO sur la **méthode** (agents/skills/commands), appendés par le
> thread principal pendant le sprint. Ne PAS confondre avec les retours produit ci-dessus.

| Date | Cible (agent/skill/command) | Retour | Décision prise |
|------|-----------------------------|--------|----------------|

## IA

> Observations méthode relevées par l'IA (non demandées par le PO), candidates pour `retro-sprint`.

| Date | Cible (agent/skill/command) | Observation | Recommandation |
|------|-----------------------------|-------------|----------------|

## Notes de contexte (décisions produit, hors méthode)

-

# Décisions autonomes (chef de projet)

> Journal des décisions tranchées **seul** par le `chef-de-projet` pendant le sprint (sans
> déranger le PO). **Le PO le relit en rétro** pour piloter a posteriori et, le cas échéant,
> faire monter le palier d'autonomie du CP. Appendé par l'agent `chef-de-projet` ; lu par
> `retro-sprint`. Ne pas confondre avec `# Méthode (agents)` (retours méthode du PO).

| Date | Question (agent dev) | Décision du CP | Fondement (spec/convention/principe) |
|------|----------------------|----------------|--------------------------------------|
| 2026-06-28 | Validation du plan d'implémentation s12 (tdd-analyse, 6 sc / 9 tests, 100% IHM) | **Plan validé — implémentation autorisée.** Routage 6 sc 🖥️ → `ihm-builder` confirmé ; acceptation **runtime** (front WASM + API distante + store réel) = rempart anti vert-qui-ment, bUnit jamais preuve seule. Scope « Web only » cohérent : aucun handler/contrat de réponse neuf, réutilisation `DefinirTransfert` + canal HTTP + SignalR LS. Drivers Sc.1/Sc.5 ; Sc.2/3/4/6 early-green batchables. Ordre Sc.5 **après** Sc.1 vert (borne P1) respecté. Aucun trou métier → pas de G1. | spec v12 (`docs/12-specification.md`) ; CLAUDE.md (backend d'abord/IHM en fin, acceptation runtime obligatoire, CQRS write canal / read+diffusion) ; règles 9/14/17/19/27/28/30 ; patterns s11 (menu d'actions, accusé à part Sc.7) + invariant transfert s01 |
| 2026-06-28 | Validation de l'écriture du backlog fin-itération s12 : la prioritisation est-elle dérivable sans déranger le PO (G2 sujet déjà tranché) ? | **Écriture autorisée — prioritisation dérivable, aucune porte PO ouverte.** Retours produit **vide** → pilotage au catalogue (arbitre « l'usage tranche »), pas de bypass Tech. Séquence dérivée de l'existant + scission G2 PO : [0] fix dropdown « Acteur du foyer » hors make-gherkin (dette déjà « en tête de file ») ; [1] make-gherkin **CRUD acteurs — suppression (Delete)** (É2 « suppression derrière », règle 6) ; [2] impersonation bornée en **suite** (rang +4 scindé par le PO, pas de re-brainstorm) ; [3] calendrier navigable palier 8 ; [4] queue technique P2 flakes SignalR → P3 édition concurrente (déjà au backlog). **Politique des cases orphelines = renvoyée au make-gherkin (candidat G1 à ce moment), pas un blocant d'écriture maintenant.** Acceptation runtime (Mongo réel) rappelée pour la suppression. Réserve : numéro de palier (synthèse dit « palier 9 ») à recaler en `/5-consolidation` (suppression relève d'É2) — détail de consolidation, non bloquant. | BACKLOG (`docs/BACKLOG.md` : rangs +4/P2/P3, dettes §287/§289, É2 ajout/édition/suppression) ; G2 PO clôture s11 + scission suppression/impersonation ; `99-sprint12-retours.md` (retours produit vide) ; CLAUDE.md (acceptation runtime obligatoire, « l'usage tranche ») ; spec v12 règle 6 |
