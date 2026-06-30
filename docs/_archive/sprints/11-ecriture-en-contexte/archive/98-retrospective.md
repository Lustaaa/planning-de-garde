# Rétrospective — Sprint 11 (écriture-en-contexte)

> Rétro de la **méthode** (pipeline d'agents/skills/commands) · produite par `retro-sprint`.
> Distincte de `99-sprint11-besoins-fin-itération.md` (rétro produit).
>
> **Source méthode** : sections `# Méthode (agents)` et `## IA` du fichier unifié
> `99-sprint11-retours.md` — **toutes deux vides** ce sprint (aucun retour PO/IA consigné).
> Les actions dérivent donc des **frictions vécues par le thread principal** et du **journal
> des décisions CP** (`# Décisions autonomes (chef de projet)` du même fichier).

## Ce qui a bien marché

- **Levier « lot de caractérisations early-green » (tdd-auto)** — Sc.5/6/7 menés en un seul
  run/commit autorisé par le CP, suivi tenu scénario par scénario, garde-fous de non-vacuité
  respectés. Vélocité validée sans avaler le signal.
- **Contrôle positif sur zone sécurité (Sc.6)** — le gating Invité a porté un contrôle
  positif Parent en regard du négatif Invité : empêche un faux vert vacuous si le clic avait
  été cassé pour tous.
- **Routage early-green inattendu vers le CP** (plus de porte G4 directe au PO) — Sc.3/4/5/6
  chacun arbitrés par le CP (filet conservé, pas de trou) sans déranger le PO ; aucun G1 indu.
- **Exception de scope bornée Sc.7** — bien encadrée par le CP (bornes strictes + STOP si
  recalcul métier requis) : surfaçage d'un acquis (`AvertissementChevauchement`, read model
  existant) sans règle/handler neuf, CQRS préservé.
- **Acceptation runtime anti-vert-qui-ment** — transport réellement coupé (API injoignable,
  store distant resté vide), spy de canal à 0 écriture (annulation), discrimination
  25/06≠15/06 (ancrage date) : preuves sur câblage réel, pas par doublures.
- **Pattern « design note → remonter au CP si ambigu »** — l'ergonomie clic-case (Sc.2) a
  déclenché la bonne escalade et un arbitrage net (menu d'actions) plutôt qu'une devinette.

## Ce qui a coincé

- **Vague d'early-greens IHM en cascade non anticipée** — `tdd-analyse`
  (`00-sprint11-suivi.md`) n'avait flaggé que Sc.7 comme vrai driver ; Sc.4/5/6 sont tombés
  early-green **INATTENDUS** une fois les dialogs Sc.1/Sc.2 bâties (issue par dialog, plomberie
  `OnAnnule`/`FermerDialog`, gating mutualisé partagés) → **3 round-trips CP** (journal CP
  2026-06-27). La section §4 « Anticipe les early greens » ne couvrait que les invariants
  backend / ports / scénarios combinés, pas le **câblage IHM partagé**.
- **Scope « couche unique = Web » contredit par un de ses propres slugs** — le Cadrage
  scaffolding (`00-sprint11-suivi.md` l.3-10) déclarait « couche unique touchée = Web », mais
  le slug Sc.7 prévoyait « avertissement **renvoyé par le retour de commande** » → exception
  de scope CP en cours de route (canal poser-slot renvoyant `PoserSlotReponse`). Note CP
  /5-consolidation : « Web » → « **Web + contrat de réponse du canal poser-slot** ».
- **Flakes temps-réel SignalR préexistants non purgés (dette de test)** — famille
  `FrontWasmConfigCycle*TempsReel*` verte en isolation mais flaky sous charge parallèle (cause
  timing SignalR/Docker). La convention anti-flake (ihm-builder ACCEPT_RED) ne visait que les
  tests **neufs** ; les existants n'ont pas été rétrofités. Le CP (2026-06-28) a acté : (ii)
  édition concurrente **DIFFÉRÉE** derrière cette stabilisation = action méthode retro-sprint
  (tests, pas `src/`).

## Actions sur le pipeline

| # | Cible (fichier) | Édition | Statut |
|---|---|---|---|
| 1 | `.claude/agents/tdd-analyse.md` (§4 ORDER) | Puce « Early-greens IHM en cascade » : prédire les scénarios `🖥️ IHM` dépendants d'un câblage partagé comme `⚠️ probablement early green (câblage IHM partagé)`, batchables. | ✅ appliquée |
| 2 | `.claude/agents/tdd-auto.md` (Lot de caractérisations) | Condition 5 : toute caractérisation batchée touchant accès/sécurité (gating de rôle) DOIT porter un contrôle positif en regard du négatif (réf. Sc.6 s11). | ✅ appliquée (après aval direct du user — l'auto-mode bloque l'auto-modification de config d'agent relayée par le CP) |
| 3 | `.claude/agents/tdd-analyse.md` (§2bis ROUTE) | Contrôle de cohérence de scope : avant « couche unique = Web », vérifier qu'aucun slug n'exige un contrat de réponse d'API ; sinon déclarer « Web + contrat de réponse du canal `<…>` » (réf. Sc.7). | ✅ appliquée |
| 4 | `.claude/agents/ihm-builder.md` (anti-flake SignalR) | Convention anti-flake étendue aux tests `*TempsReel*` **préexistants** (attente déterministe + isolation d'état, jamais délai fixe), rétrofit AVANT de driver un nouveau scénario temps-réel. Exécution = passe dev P2, non tirée ici. | ✅ appliquée |
| 5 | `.claude/agents/tdd-analyse.md` (§6 WRITE / Design notes) | Systématiser l'annotation « → remonter au CP si ambigu » sur toute design note d'ergonomie/convention non tranchée par la spec (réf. Sc.2). | ✅ appliquée |

## Questions ouvertes (méthode)

- **Action 2 (tdd-auto)** — d'abord refusée par le classifieur d'auto-mode (modification d'une
  config d'agent que l'agent charge = autorisation **directe du user** requise, un ordre relayé
  par le CP ne porte pas cette autorité), puis **appliquée après aval direct du user**. Les 5
  éditions sont donc passées. Constat de méthode : les actions de rétro touchant `.claude/agents`
  exigent un aval PO direct, à anticiper (le relais CP ne couvre pas l'auto-modification).
- **Flakes SignalR (P2)** — la convention est désormais codifiée (action 4) ; le **rétrofit
  effectif** des tests `FrontWasmConfigCycle*TempsReel*` reste une passe dev séquencée, à
  mener avant le scénario d'édition concurrente différé.
