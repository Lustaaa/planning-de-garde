# Retours — Sprint 15 (Calendrier navigable)

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

## IHM - /planning/poser-slot

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
| 2026-06-29 | `tdd-auto` Sc.8 — 3 tests d'accept. Mongo s09 (`ConfigurationFoyerMongoSeedOnceTests`, `...DurabiliteTests`, `SupprimerActeurMongoIntegrationTests`) cassent car ils s'appuient sur le seed-once Mongo, retiré par Sc.8 | **Option 1 retenue.** Dans le commit Sc.8 : **supprimer** `ConfigurationFoyerMongoSeedOnceTests` (asserte un comportement supprimé — plus de raison d'être). **Réécrire** `...DurabiliteTests` et `SupprimerActeurMongoIntegrationTests` pour **ajouter explicitement** les acteurs (Alicia/bleu, parent-a/parent-b) **avant** de prouver durabilité/suppression — la preuve de durabilité s09 reste sur store Mongo réel, re-câblée sur ajout explicite (zéro appui sur le seed). **Pas de G1 / pas de trou métier** : la sémantique de durabilité est préservée, seul l'amorçage des données de preuve change (écart d'implémentation, pas de valeur). Refusé : Option 2 (suppr. sèche = trou de couverture durabilité entre Sc.8 et Sc.9) ; Option 3 (seed gated = contredit l'asymétrie actée). | Plan Sc.8 (« Mongo ne seede **jamais** », asymétrie assumée — décision PO) ; CLAUDE.md règle R4 / acceptation runtime sur store réel (rempart anti vert-qui-ment, à **préserver**, d'où réécriture plutôt que suppression) ; principe TDD (un test sans contrepartie comportementale disparaît avec son comportement) |
| 2026-06-29 | `tdd-analyse` — valider le plan d'impl. (9 sc. / 19 tests, routage backend/IHM, scope multi-couche, early-green/caractérisations) | **Plan VALIDÉ.** Routage backend (Sc.2/3 read model unit `GrilleAgendaQuery` vue/span ; Sc.8/9 intégration Mongo réel `MongoRequisFact`) vs IHM runtime (Sc.1/4/5/6/7 `ihm-builder`) cohérent. Caractérisations vs drivers correctement étiquetés : Sc.3 re-résolution fond + Sc.5 write `AffecterPeriode` + Sc.7 gate `EstParent` = early-green **anticipés/batchables**, pas inattendus. Bascule défaut 5→4 sem. = re-pointage structurel attendu, pas régression. Asymétrie seed (Mongo jamais seedé / InMemory gardé) assumée. **Pas d'angle mort de gating partiel** : sur `PlanningPartage` les écritures (`OuvrirMenu`/affectation) sont déjà gardées `EstParent` ; le trigger de plage Sc.5 réutilise le même gate → couverture d'écran uniforme (≠ vécu s13 Sc.7). | Skill CP §5 (gating partiel) ; CLAUDE.md (backend d'abord/IHM en fin, acceptation runtime, canal req/rép vs SignalR) ; règle 9 (gating écriture) / 14 (lecture ouverte Invité) ; révision PO hors-process (borne anti-cliquet règle 30 levée, palier 14 absorbé) consignée au plan |
