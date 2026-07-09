# Workflow Claude — planning-de-garde

Ce dépôt est piloté par un **pipeline d'agents Claude Code** en boucle de sprint SCRUM.
Chaque étape est une *slash command* qui agit en **relais pur** : le thread principal ne
raisonne pas, il dispatche des **subagents** spécialisés, relaie leurs questions au PO via
`AskUserQuestion`, et présente les checkpoints. Tout le raisonnement vit dans les agents — le
contexte du main reste propre.

> Refonte 2026-06-29 (design : `docs/superpowers/specs/2026-06-29-refonte-pipeline-agile-design.md`).
> Objectif : un sprint en **moitié moins de temps/tokens**. L'ancien pipeline (6 commands,
> ~10 agents) est archivé sous `docs/_archive/claude-pipeline-v1/` — source d'inspiration, plus actif.

## Les acteurs SCRUM

| Rôle SCRUM | Incarnation | Rôle |
|---|---|---|
| **Product Owner** | **toi (humain)** | Vision, priorités. Valide G2, G3, git. |
| **Scrum Master** | agent `scrum-master` | Décide (ex-CP) + orchestre la méthode. 3 chapeaux : planning / décision / clôture. Ne code jamais. |
| **Dev team** | agent `dev-team` | **Seul** à coder : TDD / DDD / BDD / CQRS / hexagonale. Backend puis IHM Blazor + SignalR réel. |
| **Architecte** | agent `architecte` | **Hors-sprint.** Bypass méthodo : fait exactement la consigne technique du PO, puis resynchronise la doc. Exclusif avec `dev-team`. |

Un **acteur = un chapeau**, pas un dispatch par rôle : peu d'agents → moins de contextes
rechargés → plus rapide et moins cher.

## Le cycle de sprint

```text
   ┌──────────────────────────────────────────────────────────────┐
   ▼                                                                │
 BACKLOG ─▶ /planning ─▶ /sprint ─▶ (gate visuel G3) ─▶ /cloture ──┘
 (vivant)   goal G2 +     dev-team :      PO teste        SM : retours→backlog,
            fichier       BDD+TDD +        la livraison    spec en diff,
            de sprint     IHM, commits                     rétro conditionnelle,
                                                           git push/PR/merge
```

### Vocabulaire SCRUM ↔ pipeline

| Concept SCRUM | Dans le pipeline |
|---|---|
| Product backlog | `docs/BACKLOG.md` (vivant : retours persistants, source des goals) |
| Sprint Planning | `/planning` — 3-4 goals candidats (G2) + fichier de sprint léger |
| Sprint goal | le **goal** tranché par le PO en G2 (~1h IA, tranche verticale) |
| Sprint (dev) | `/sprint` — `dev-team` implémente tous les scénarios |
| Increment + DoD | le **gate visuel G3** de `/sprint` (back + IHM up, testé par le PO) |
| Sprint Review | la validation PO du gate G3 |
| Retours produit | `/cloture` — fusion dans le backlog vivant (à chaque sprint) |
| Sprint Retrospective | `/cloture` — rétro **méthode conditionnelle** (« amélioration ou rien ») |

## Détail des étages

| Command | Rôle | Agent | Sortie |
|---|---|---|---|
| `/planning` | Goals candidats (G2) → fichier de sprint léger | `scrum-master` (planning) | `docs/sprints/NN-<slug>.md` (tableau en tête + Gherkin) |
| `/sprint` | BDD+TDD backend + IHM, commits, gate visuel (G3) | `dev-team` (+ `scrum-master` pour les décisions) | code + tests + tableau d'avancement à jour |
| `/cloture` | Retours→backlog, spec en diff, rétro conditionnelle, git | `scrum-master` (clôture) | `docs/BACKLOG.md`, `docs/specs/`, branche mergée |

L'`architecte` est **hors de cette boucle** : dispatché manuellement par le PO pour une tâche
technique, entre deux sprints.

## Artefacts

- `docs/specs/` — **spec vivante éclatée par sujet** + `index.md` navigable, éditée **en diff**
  (fin du monolithe et du ×10). **Migration intégrale faite** : c'est la source complète et
  courante ; les monolithes `docs/NN-specification.md` sont figés en historique.
- `docs/BACKLOG.md` — **backlog produit vivant** : retours persistants (rien ne se perd), source
  des goals.
- `docs/sprints/NN-<slug>.md` — **1 fichier par sprint** : tableau d'avancement en tête (X/N,
  ⏳/🔴/✅) + scénarios Gherkin (`@back`/`@ihm`) + section `# Retours produit (PO)`.
- `docs/sprints/JOURNAL-METHODE.md` — 1 ligne par amélioration de rétro (pas de doc dédié).

## Conventions

- **Relais pur** : le thread principal ne lit ni n'écrit code/spec/scénarios — il délègue.
- **`AskUserQuestion`** : appelé **uniquement** par le thread principal (un subagent ne peut pas).
- **Portes PO — deux seulement** (+ git sortant) :
  - **G2** — choix du sprint goal (`/planning`) : le SM propose 3-4 goals (bullets de scope),
    le PO tranche (peut en injecter un).
  - **G3** — gate visuel (`/sprint`) : le PO valide la livraison après test.
  - **Git sortant** (`/cloture`) : push / PR / merge confirmés par le PO.
  Tout le reste est tranché par le `scrum-master` (escalade G1 seulement sur un vrai trou métier).
- **Scripts adossés** : `dotnet` (restore/build/test, JSON compact), `git` (garde-fous),
  `run` (lancer l'app). Jamais de commande brute.
- **Backend d'abord, IHM en fin** ; **acceptation runtime obligatoire** (câblage/store réel,
  jamais bUnit comme preuve d'un scénario IHM).
- **Branche** : `ia-{type}/{slug}` ; jamais de commit sur `main` ; jamais de `git add -A`.

## Suivi d'un sprint en cours

Le **tableau d'avancement en tête** de `docs/sprints/NN-<slug>.md` (compte `X/N` + statut par
scénario ⏳/🔴/✅) + les tags `@pending`/`@rouge`/`@vert` des scénarios. Plus de dossier de
suivi ni de fichier-par-scénario.
