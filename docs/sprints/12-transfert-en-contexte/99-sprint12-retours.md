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
| 2026-06-28 | `/5` consolidation v13 — Q1 : comment cadrer le sort des **cases orphelines** d'un acteur supprimé (slots/périodes/transferts) ? Vrai trou métier (G1) ou tranché par règle structurante ? | **Décision (pas G1) — cadrage déterministe par la chaîne de résolution existante.** v13 cadre : la suppression d'un acteur est **autorisée** (option « refus si références » écartée — contredirait l'additivité et le repli neutre déjà actés) ; ses slots/périodes orphelins sont **neutralisés** = la surcharge orpheline **cesse de primer**, la case **retombe sur le fond** (le cycle reprend, règles 12/15), ou sur le **neutre** si l'index n'est pas mappé / non résolu, **sans nom fantôme** (règles 15/19) ; un acteur **mappé au cycle de fond** rend son index **non mappé → neutre** (règles 11/19) ; **message non bloquant** à l'utilisateur (registre avertissement-à-part, règles 16/28) ; **pas** de réaffectation auto (serait une règle neuve, hors périmètre). **Transferts** : dérivés du planning (règle 24) et **invisibles jusqu'au palier 14** → pas d'orphelin observable séparé ; un transfert **ponctuel** explicite (règle 25) suit la même neutralisation — détail à **scénariser au make-gherkin**, pas un pré-arbitrage v13. Variantes refus/réaffectation = **révisions de règle hors boucle** ; **G1 seulement si** le make-gherkin révèle un vrai trou (ex. empêcher la suppression du dernier responsable d'un enfant). Borne anti-cliquet : slots/périodes/transferts **restent InMemory** (règle 30). | spec v12 règles 11/12/15/18/19/24/25/30 ; arbitre d'usage (repli neutre = état « responsable indéterminé », jamais bloquant) ; cohérence registre message règles 16/28 ; ligne CP précédente (orphelines renvoyées au make-gherkin) |
| 2026-06-28 | `/5` consolidation v13 — Q2 : recaler le **n° de palier** (suppression = É2, pas « palier 9 ») et le **séquencement** (v12 plaçait Calendrier navigable AVANT CRUD acteurs ; besoins s12 place la suppression AVANT le calendrier) | **Décision (CP, structure/convention) — re-séquencement + renumérotation actés en v13.** Le `/4` PO (besoins s12) fait foi : **CRUD acteurs (suppression) passe DEVANT le calendrier navigable**. v13, séquence continue (palier 7 « écriture en contexte » désormais **livré complet**, 3ᵉ dialog transfert incluse) : **palier 8 = CRUD acteurs — suppression** (ex-9, tiré devant), découpé en deux make-gherkin (suppression `crud-acteurs-suppression` **puis** amorce `impersonation-bornee`, besoins rangs 1→2) ; **palier 9 = Calendrier navigable** (ex-8) ; paliers suivants décalés en conséquence. **Épic** : suppression = **É2 (Modèle & configuration d'acteurs)**, tranche impersonation = **É10** (couplage É2↔É10 à expliciter au palier auth) — réserve CP levée. **Action consolidation** : répercuter le swap dans la table *À faire* du `BACKLOG.md` (palier 8 ⇄ ex-CRUD), mappings d'épics inchangés ; v13 = référence unique de numérotation. Aucune porte PO (le cap suppression-d'abord est déjà l'arbitrage `/4` du PO). | besoins s12 (`99-sprint12-besoins-fin-itération.md` séquence rangs 1→4) ; BACKLOG (`docs/BACKLOG.md` É2/É10, rang +4, dépendance É2↔É10 §301-302) ; spec v12 paliers 8/9 + note « Numérotation réalignée » (référence unique) ; CLAUDE.md (séquencement = convention, tranché CP) |
| 2026-06-28 | Validation de l'écriture de la spec vivante **v13** (consolidation) : la synthèse est-elle cohérente pour ordonner l'écriture sans déranger le PO, ou un conflit de valeur subsiste-t-il (G1) ? | **Écriture v13 autorisée — synthèse cohérente, aucune porte PO ouverte.** Q1 (règle 6 réécrite : suppression autorisée + neutralisation par repli surcharge>fond>neutre, message non bloquant, pas de réaffectation auto) et Q2 (palier 8 = CRUD suppression `crud-acteurs-suppression` rang1 → `impersonation-bornee` rang2 ; palier 9 = calendrier navigable ; v13 = référence unique ; swap répercuté au BACKLOG en `/6`) **reconduisent fidèlement** les 2 décisions CP déjà journalisées (lignes ci-dessus) — pas de réouverture. Cibles d'édition **vérifiées exactes** : blockquotes « prochain sujet » (§390/§430), Mécaniques d'écriture (§444/§447), **règle 14** (« seul chemin d'écriture pour le slot et la période » → +transfert, 3 dialogs) et **règle 25** (« 3ᵉ dialog = prochain incrément » → **livré**, épic refermé) sont bien les règles transfert/écriture-en-contexte. v12 reste figée. **Réserve non bloquante** : la table *À faire* du BACKLOG porte encore une numérotation distincte de la *Séquence* spec (écart préexistant, déjà acté « résorbé en v13 = référence unique ») ; sa réconciliation reste **différée en `/6`** comme prévu — gap connu et journalisé, pas un conflit neuf. Le swap 8⇄9 est une transposition **locale** (paliers aval inchangés dans la séquence spec) ; le « paliers suivants décalés » de la synthèse est une formulation large, sans incidence de valeur. **Aucun conflit de valeur → pas de G1.** | spec v12 (`docs/12-specification.md` §385-437 séquence, §439-449 mécaniques, règles 6/14/15/24/25/30) ; besoins s12 + 2 décisions CP Q1/Q2 ci-dessus (reconduites) ; CLAUDE.md (spec vivante = source de vérité, consolidation tranchée CP, numérotation = convention) ; BACKLOG (réconciliation /6) |
| 2026-06-28 | `retro-sprint` s12 — prioriser 4 actions de rétro méthode (A1–A4) : lesquelles appliquer en autonomie, lesquelles escalader (G1) ? | **Les 4 actions retenues — appliquer (aucune n'est structurelle/risquée → pas de G1).** Toutes sont des tweaks **non destructifs**, ciblés, sans refonte d'agent ni suppression de gate : **A1** (`4-retours.md` ét.2&4) documente un fast-path déjà pratiqué de fait par le CP (retours produit vides s11→s12, cf. ligne journal `/4` ci-dessus) — formalise l'existant, n'altère pas l'arbitrage G2 PO. **A2** (`ihm-builder.md` VERIFY) cadre un flake **connu** *TempsReel* (vert en isolation/re-run) : re-run isolé avant blocage — **borne explicite** : ne vaut que pour ce flake identifié, ne lève **pas** l'acceptation runtime ni le rempart anti vert-qui-ment, le vrai fix (retrofit P2) **reste au backlog**. **A3** (`test-count.ps1`) = hygiène d'outillage (kill hôtes résiduels avant `dotnet test`, snippet `run.ps1`) contre MSB3027/DLL verrouillées — purement opérationnel. **A4** (`6-cloture-sprint.md` ét.4bis) installe le check « spec vivante = référence unique de numérotation » — **résorbe** précisément l'écart BACKLOG↔spec laissé en réserve `/5` (lignes Q2/v13 ci-dessus), au lieu de le re-différer. Palier 0 respecté : tweaks réversibles, traçables en diff, aucun arbitrage de valeur PO engagé. | retours s12 (retours produit vides → A1) ; lignes journal `/4` et Q2/v13 ci-dessus (A1 fast-path déjà pratiqué, A4 résorbe la réserve numérotation) ; CLAUDE.md (acceptation runtime obligatoire = garde-fou A2 ; spec vivante = source de vérité = A4 ; séquencement/méthode tranchés CP) ; `run.ps1` (snippet A3) |
| 2026-06-28 | `retro-sprint` s12 — exécution des 4 actions retenues : application bloquée pour le subagent (garde-fou anti-auto-modification refuse l'édition `.claude/`). | **Garde-fou levé par aval PO explicite ; éditions appliquées par le thread principal.** Le PO a tranché en `AskUserQuestion` « Appliquer les 4 » ; le classifieur bloquant les écritures `.claude/` pour les subagents, le **thread principal a appliqué lui-même** A1→A4 (`4-retours.md`, `ihm-builder.md`, `test-count.ps1`, `6-cloture-sprint.md`). `retro-sprint` a **vérifié la présence effective** des 4 éditions (grep ciblé) avant d'acter — pas de vert-qui-ment méthode — puis a écrit `98-retrospective.md`. Question ouverte consignée dans la rétro : retrofit P2 anti-flake `*TempsReel*` récurrent s10→s12, à arbitrer avant le prochain scénario temps-réel. | aval PO (AskUserQuestion « Appliquer les 4 ») ; garde-fou self-modification (subagent refusé) ; vérification grep des 4 cibles `.claude/` ; `98-retrospective.md` |
