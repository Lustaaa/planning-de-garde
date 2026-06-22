# Design — Flow `make-gherkin` (2ᵉ pipeline BDD)

> Date : 2026-06-22 · Statut : design validé, prêt pour plan d'implémentation

## Contexte

Le dépôt possède une pipeline de cadrage produit `/spec` qui enchaîne deux
agents orchestrés : `challenge-po` (challenge fonctionnel, boucle de questions
remontées au thread principal) puis `redaction-spec` (écriture de la spec maison
dans `docs/init/`). La spec produite est riche en **règles de gestion**
fonctionnelles.

Ce design ajoute une **2ᵉ pipeline autonome**, `make-gherkin`, qui part de cette
spec fonctionnelle et produit une analyse de testabilité sous forme de **Gherkin
à scénarios numérotés**. Cet artefact alimentera plus tard une 3ᵉ pipeline
d'implémentation BDD + TDD (hors scope ici).

Décompte des pipelines : (1) `/spec` — cadrage produit (`brainstorm` →
`redaction-spec`) ; (2) `make-gherkin` — ce design ; (3) `tdd-implement` —
implémentation, à venir.

## Objectif

Transformer les règles de gestion d'une spec en **scénarios testables clairs et
numérotés**, en restant strictement fonctionnel (aucun détail technique dans le
Gherkin). Le flow réutilise le pattern éprouvé du duo agent + round-trip de
`/spec` : un agent challenge la testabilité, un agent rédige, le thread principal
est le seul à poser les questions via `AskUserQuestion`.

Stack cible de la future implémentation (contexte transmis, **pas** codée dans le
Gherkin) : `.NET` pour le backend, `Blazor` + `SignalR` pour le front.

## Architecture

Miroir strict de `/spec` (approche A retenue). Trois nouvelles unités, chacune
avec une responsabilité unique, testable indépendamment.

| Unité | Type | Rôle | Tools |
|---|---|---|---|
| `make-gherkin` | command | Orchestre le flow ; **seul** à appeler `AskUserQuestion` ; dispatche les agents | — |
| `challenge-gherkin` | skill + agent | Sonde la **testabilité** de chaque règle (nominal / limite / erreur + données d'exemple) ; boucle de questions remontées au thread | Read, Grep, Glob |
| `redaction-gherkin` | skill + agent | Écrit le fichier `.feature` ; renvoie un récap JSON | Read, Write, Edit, Glob |

### Séparation des préoccupations

- **`challenge-gherkin`** ne pose jamais les questions lui-même (ne peut pas
  appeler `AskUserQuestion`). Il renvoie à chaque tour **un objet JSON**
  `{ tensions, questions, synthese, done }`. Read-only.
- **`redaction-gherkin`** reçoit les décisions tranchées (scénarios à couvrir),
  écrit **uniquement** le fichier cible, renvoie
  `{ path, features, scenarios, notes }`.
- **`make-gherkin`** (command) ne fait que : lire le contexte, dispatcher,
  rendre les questions via `AskUserQuestion`, relancer l'agent via `SendMessage`,
  proposer le commit.

## Flux de données

1. **Contexte.** La command repère la spec fonctionnelle (`docs/init/01-specification.md`)
   et passe son chemin à l'agent.
2. **Challenge (round-trip).** `challenge-gherkin` énonce les tensions de
   testabilité, puis pose **une** question à la fois. Le thread principal la rend
   via `AskUserQuestion` (option recommandée en premier), renvoie la réponse via
   `SendMessage`. Boucle tant que `done` est faux.
3. **Validation.** Quand `done: true`, le thread présente la `synthese` (liste des
   scénarios à couvrir : nominaux, limites, erreurs) et demande l'accord.
4. **Rédaction.** `redaction-gherkin` écrit `docs/init/scenarios/<sujet>.feature`
   et renvoie le récap JSON.
5. **Commit.** Le thread propose un commit (jamais de push sauf demande explicite).

## Format de sortie

- Emplacement : `docs/init/scenarios/`, un fichier `.feature` par feature si le
  périmètre grossit.
- Structure Gherkin standard :
  - `Feature: <titre>` + courte description.
  - `Scenario N: <titre>` — **numérotation continue simple** (1, 2, 3… à travers
    tout le fichier), sans tag de traçabilité vers les règles.
  - `Given / When / Then` ; `Scenario Outline` + `Examples` pour les jeux de
    données.
- **Fonctionnel uniquement** : aucun détail d'implémentation dans le Gherkin.

## Rename `challenge-po → brainstorm`

Inclus dans ce chantier pour cohérence de nommage. Périmètre (5 fichiers) :

- `.claude/skills/challenge-po/SKILL.md` → `.claude/skills/brainstorm/SKILL.md`
  (mise à jour du champ `name`).
- `.claude/agents/challenge-po.md` → `.claude/agents/brainstorm.md` (mise à jour
  `name`, `description`, référence au skill).
- `.claude/commands/spec.md` — référence au dispatch de l'agent et au nom du skill.
- `.claude/skills/redaction-spec/SKILL.md` — mention en prose « après une passe
  challenge-po ».
- `docs/exemples/_rapport-test-skills.md` — référence historique, mise à jour pour
  cohérence.

Note : pas de collision avec le skill `superpowers:brainstorming` (namespace
distinct).

## Hors scope

- La 3ᵉ pipeline d'**implémentation** BDD + TDD qui consommera les `.feature` —
  chantier ultérieur, prévu sous forme d'un agent `tdd-implement`.
- Tout choix d'outil d'exécution BDD (SpecFlow / Reqnroll) : le `.feature` est
  écrit en Gherkin standard, le runner sera décidé au moment de l'implémentation.

## Décisions verrouillées

| Sujet | Décision |
|---|---|
| Placement du flow | Pipeline autonome séparée (nouvelle command) |
| Entrée du challenger | La spec produite par `challenge-po` / `/spec` |
| Contenu sortie | Gherkin à scénarios numérotés (pour future impl BDD+TDD) |
| Rôle du challenger | Testabilité / critères d'acceptation (reste fonctionnel) |
| Numérotation | Continue simple, sans tag de traçabilité |
| Emplacement sortie | `docs/init/scenarios/` (un `.feature` par feature) |
| Approche | A — miroir strict de `/spec` |
| Noms | command `make-gherkin`, agents `challenge-gherkin` + `redaction-gherkin` |
| Rename | `challenge-po → brainstorm`, inclus dans ce chantier |
