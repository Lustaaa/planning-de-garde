# Rétrospective — Sprint 06 (saisie-visible)

> Rétro de la **méthode** (pipeline d'agents/skills/commands) · produite par `retro-sprint`.
> Distincte de `99-sprint06-besoins-fin-itération.md` (rétro produit).
>
> **Statut clôture :** les 5 actions ci-dessous sont **proposées — NON appliquées**. Le PO
> a choisi de réécrire lui-même les agents dans un chantier dédié (intégration du
> `chef-de-projet`, actions 3/4 et au-delà). Aucun fichier `.claude/` n'a été modifié par
> cette rétro.

## Ce qui a bien marché

- **Anti-blocage early-green ACQUIS.** `tdd-analyse` a annoté Sc.4/5/7
  « ⚠️ probablement early green — caractérisation » (`.claude/agents/tdd-analyse.md`
  l.101-103) et `tdd-auto` les a marqués « ✅ GREEN (caractérisation) » **sans** suspendre via
  une question early-green (`.claude/agents/tdd-auto.md` l.64-68). 4 tests backend verts
  d'emblée, zéro round-trip parasite — mécanisme nominal, sprint **8/8** fluide.
- **Fallback `ihm-builder` → `general-purpose` en régime nominal documenté.** Sc.1/2/3/6/8
  menés sans friction via le fallback prévu (`.claude/commands/3-tdd-implement.md` l.42-45).
  Réutilisation du même agent de fond via `SendMessage` pour enchaîner les scénarios IHM →
  contexte du port `IDateTimeProvider` conservé entre scénarios, gain d'efficacité réel.
- **Routage backend/IHM net dès l'analyse.** `00-sprint06-suivi.md` sépare explicitement
  5 IHM/runtime (`ihm-builder`, acceptation E2E réelle) et 3 backend caractérisation
  (`tdd-auto`), avec garde-fou « jamais un bUnit composant comme preuve » — aucune dérive de
  niveau de test ce sprint.

## Ce qui a coincé

- **Acquis early-green non consolidé comme invariant méthode** — l'anticipation n'est
  constatée que dans le suivi du sprint, jamais gravée dans `tdd-analyse.md` comme cas-type
  récurrent. Preuve : `.claude/agents/tdd-analyse.md` l.83-120 anticipe les early greens via
  des exemples vécus s03, mais le motif vécu s06 (Sc.4/5/7 = invariants `GrilleAgendaQuery`
  inchangés sur un sprint de scaffolding d'adaptateurs) n'y figure pas. Le prochain sprint de
  même nature pourrait re-découvrir le motif.
- **Réutilisation d'un agent IHM de fond non codifiée** — l'enchaînement des scénarios IHM
  par le même agent (`SendMessage`) en conservant le contexte (`IDateTimeProvider`) est une
  bonne pratique improvisée, pas une instruction du pipeline. Preuve :
  `.claude/commands/3-tdd-implement.md` étape 7 et l.100-105 décrivent le dispatch par
  scénario mais pas la persistance/réutilisation d'un agent IHM de fond entre scénarios
  partageant un même scaffolding.
- **`chef-de-projet` non câblé dans `/5` + journal absent** — le type d'agent est apparu dans
  le registre en cours de sprint mais reste **non câblé dans `/5-consolidation`** (grep vide)
  alors que `/3` et `/4` le dispatchent ; et la section « # Décisions autonomes (chef de
  projet) » que le CP doit appender (`chef-de-projet.md` l.34-36) n'existe pas dans
  `99-sprint06-retours.md`. Le CP n'a donc rien tranché ce sprint — tout a été relayé au PO.
  Preuve : `.claude/commands/5-consolidation.md` (aucune mention CP / Protocole d'escalade)
  vs `3-tdd-implement.md` l.20-28 et `4-retours.md` l.20-31.
- **Warnings d'encodage LF→CRLF récurrents** sur les commits docs : bénins mais bruit répété
  à chaque sprint, jamais neutralisés. Le chemin accentué « privée » est OK (réparé s05) mais
  la normalisation des fins de ligne reste non gérée. Preuve : aucun `.gitattributes`
  normalisant `*.md` à la racine du dépôt.

## Actions sur le pipeline

> **Toutes proposées — NON appliquées.** Déléguées au chantier `chef-de-projet` du PO
> (réécriture des agents dans un prompt dédié).

| # | Cible (fichier) | Édition | Statut |
|---|---|---|---|
| 1 | `.claude/agents/tdd-analyse.md` | Documenter le cas-type récurrent « sprint de scaffolding d'adaptateurs sur projection CQRS figée → les scénarios ré-exerçant un invariant déjà vert (fenêtre 35j, exclusion hors-fenêtre, repli neutre de `GrilleAgendaQuery`, s03) sont mécaniquement des caractérisations à annoter `⚠️ probablement early green` sans investigation. Exemple vécu s06 : Sc.4/5/7. » | ⏸️ proposée — NON appliquée (déléguée au chantier chef-de-projet du PO) |
| 2 | `.claude/commands/3-tdd-implement.md` | Étape 7 / dispatch `ihm-builder` (l.100-105) : noter que lorsque plusieurs scénarios IHM partagent un scaffolding commun (port injecté, sélecteurs, seed — ex. `IDateTimeProvider` s06), réutiliser le même agent `ihm-builder` de fond (`SendMessage`) pour les enchaîner et conserver le contexte. | ⏸️ proposée — NON appliquée (déléguée au chantier chef-de-projet du PO) |
| 3 | `.claude/commands/5-consolidation.md` | Ajouter le bloc « Protocole d'escalade — chef de projet (CP) » comme dans `3-tdd-implement.md` (l.20-31) et `4-retours.md` (l.20-31) : dispatcher d'abord `chef-de-projet` sur une `question` de `spec-consolidation` ; relayer sa `decision`, n'appeler `AskUserQuestion` que sur une `escalate` (G1/G2) ; fallback type absent → `general-purpose`. | ⏸️ proposée — NON appliquée (déléguée au chantier chef-de-projet du PO) |
| 4 | `.claude/agents/tdd-analyse.md` (template `99-…-retours.md`) | Au moment où `tdd-analyse` génère le squelette du fichier unifié, ajouter la section vide « # Décisions autonomes (chef de projet) » avec en-tête `\| Date \| Cible \| Décision \| Rationale \| Sources \|`, pour donner au CP une cible où journaliser (`chef-de-projet.md` l.34-36). | ⏸️ proposée — NON appliquée (déléguée au chantier chef-de-projet du PO) |
| 5 | `.gitattributes` (racine du dépôt) | Créer/compléter un `.gitattributes` normalisant les fins de ligne des sources texte (ex. `* text=auto` + `*.md text eol=lf`/`eol=crlf` selon convention Windows) pour neutraliser les warnings LF→CRLF récurrents sur les commits docs. | ⏸️ proposée — NON appliquée (déléguée au chantier chef-de-projet du PO) |

## Questions ouvertes (méthode)

- **Périmètre du chantier `chef-de-projet` du PO** : au-delà du câblage dans `/5` (action 3)
  et de la section de journal (action 4), faut-il que le CP tranche aussi les escalades de
  **cap (G2)** — choix du prochain sprint goal — et non seulement les questions des agents
  dev ? À arbitrer dans le chantier de réécriture des agents.
- **Actions 1/2/5 hors chantier CP** : early-green invariant, réutilisation d'agent IHM et
  normalisation EOL ne touchent pas le `chef-de-projet`. À reprendre lors d'une rétro
  ultérieure si le chantier PO ne les absorbe pas.
