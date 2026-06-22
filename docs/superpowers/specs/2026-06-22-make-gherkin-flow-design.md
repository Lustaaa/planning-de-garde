# Design — Flow `make-gherkin` (2ᵉ pipeline BDD)

> Date : 2026-06-22 · Statut : design validé (révisé), prêt pour exécution

## Contexte

Le dépôt possède une pipeline de cadrage produit `/spec` qui enchaîne deux
subagents orchestrés : `brainstorm` (challenge fonctionnel, round-trip de
questions) puis `redaction-spec` (écriture de la spec maison dans `docs/init/`).
La spec produite est riche en **règles de gestion** fonctionnelles.

Ce design ajoute une **2ᵉ pipeline**, `make-gherkin`, qui part de cette spec et
produit **un seul fichier** mêlant une **analyse technique légère** et des
**scénarios Gherkin numérotés**. Ce fichier sera l'entrée de la 3ᵉ pipeline
d'implémentation.

Décompte des pipelines, chaînées par **fichiers** :
1. `/spec` — prompt user → `docs/init/01-specification.md` (cadrage produit)
2. `make-gherkin` — `01-specification.md` → `docs/init/scenarios/<sujet>.md`
3. `tdd-implement` — ce `.md` → implémentation **scénario par scénario** (à venir)

## Objectif

Transformer les règles de gestion d'une spec en **scénarios testables numérotés**,
accompagnés d'une **analyse technique légère orientée implémentation** (composants
impactés, contrats de données, points d'attention TDD), de quoi amorcer
`tdd-implement`. Cible technique : `.NET` backend, `Blazor` + `SignalR` front.

## Architecture

`make-gherkin` reprend **exactement le pattern de `brainstorm`** : un skill + un
**seul** subagent + une command fine qui le lance et gère le round-trip de
questions. On **abandonne** l'idée d'un duo challenge/rédaction : un seul agent
fait tout (lit la spec, challenge la testabilité en round-trip, écrit le fichier).

| Unité | Type | Rôle | Tools |
|---|---|---|---|
| `make-gherkin` | command | Lance l'agent ; **seul** à appeler `AskUserQuestion` ; relaie les réponses via `SendMessage` ; propose le commit | — |
| `make-gherkin` | skill | Le processus : challenge de testabilité + format du fichier de sortie (analyse + scénarios) | — |
| `make-gherkin` | agent | Subagent autonome : lit `01-specification.md`, renvoie les questions en JSON (round-trip), puis **écrit** `docs/init/scenarios/<sujet>.md` | Read, Grep, Glob, Write, Edit |

### Pourquoi un seul agent (et pas un duo)

`brainstorm` ne challenge que (il n'écrit pas — `redaction-spec` écrit). Ici on
veut un étage **autonome** de bout en bout : le même agent challenge **et** écrit.
La command reste fine, son seul rôle est le round-trip de questions (contrainte
plateforme : un subagent ne peut pas appeler `AskUserQuestion`).

## Flux de données

1. **Contexte.** La command repère `docs/init/01-specification.md` et le passe à
   l'agent `make-gherkin`.
2. **Challenge (round-trip).** L'agent renvoie `{ tensions, questions, synthese, done }`.
   La command rend chaque question via `AskUserQuestion`, relaie via `SendMessage`.
   Boucle tant que `done` est faux.
3. **Validation.** À `done: true`, la command présente la `synthese` (scénarios à
   couvrir + analyse technique pressentie) et demande l'accord.
4. **Écriture.** La command **relance le même agent** avec la consigne d'écrire le
   fichier ; l'agent écrit `docs/init/scenarios/<sujet>.md` et renvoie le récap
   `{ path, scenarios, notes }`.
5. **Commit.** La command propose un commit (jamais de push sauf demande explicite).

## Format de sortie

`docs/init/scenarios/<sujet>.md`, un fichier par sujet. Structure :

1. `# <Sujet> — Analyse & scénarios`
2. `## Analyse technique` — **légère**, orientée implémentation :
   - Composants impactés (côté `.NET` / `Blazor` / `SignalR`).
   - Contrats de données clés (entrées/sorties, entités cœur).
   - Points d'attention TDD (ordre de test, dépendances, cas délicats).
3. `## Scénarios` — bloc Gherkin :
   - `Feature: <titre>` + courte description.
   - `Scenario N: <titre>` — **numérotation continue simple** (1, 2, 3…).
   - Tags de type `@nominal` / `@limite` / `@erreur` au-dessus de chaque scénario.
   - `Given / When / Then` ; `Scenario Outline` + `Examples` pour les données.

**Note de portée** : contrairement à la spec (`/spec`, fonctionnel pur), ce
fichier porte **volontairement** une analyse technique — c'est son rôle de
préparer l'implémentation. Les pas Given/When/Then restent comportementaux
(observables), la technique vit dans la section `## Analyse technique`.

## Rename effectué : `challenge-po → brainstorm`

Réalisé (Task 1, commit `4671de8`). 5 fichiers : skill, agent, `/spec`, mention
dans `redaction-spec`, rapport d'exemple. `git grep challenge-po` vide.

## Hors scope

- La 3ᵉ pipeline `tdd-implement` (implémentation scénario par scénario) — chantier
  ultérieur.
- Tout choix d'outil d'exécution BDD (SpecFlow / Reqnroll) : décidé au moment de
  l'implémentation.

## Décisions verrouillées

| Sujet | Décision |
|---|---|
| Chaînage | Par fichiers : prompt → spec.md → scénarios.md → impl |
| Réalisation | Pattern `brainstorm` : skill + 1 agent + command fine (round-trip) |
| Nombre d'agents | **Un seul** agent autonome (pas de duo) ; il challenge ET écrit |
| Entrée | `docs/init/01-specification.md` |
| Sortie | `docs/init/scenarios/<sujet>.md`, un fichier par sujet |
| Contenu sortie | Analyse technique légère + scénarios Gherkin numérotés |
| Numérotation | Continue simple, tags `@nominal/@limite/@erreur` |
| Portée technique | Analyse technique autorisée dans ce fichier (cible .NET/Blazor/SignalR) |
| `/spec` existant | Laissé tel quel |
| Questions | Round-trip via la command (subagent ne peut pas `AskUserQuestion`) |
