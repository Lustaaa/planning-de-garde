---
name: make-gherkin
description: À utiliser pour transformer une spec fonctionnelle (docs/init/01-specification.md) en un fichier d'analyse technique légère + scénarios Gherkin numérotés, prêt à amorcer une implémentation BDD+TDD — fait émerger cas nominaux/limites/erreur avec résultat observable, puis écrit le fichier de sortie.
---

# Make Gherkin

## Vue d'ensemble

Une passe de **testabilité + amorce d'implémentation**. Tu pars d'une spec
fonctionnelle déjà challengée (sortie de `/spec`) et tu produis **un seul fichier**
qui mêle : une **analyse technique légère** (orientée implémentation) et des
**scénarios Gherkin numérotés** (nominal / limite / erreur).

**Principe central :** une règle de gestion n'est pas un test. Pour chaque
comportement, extrais le triplet observable *état initial → action → résultat
constatable*. Un `Then` non vérifiable (« ça marche ») est refusé. L'analyse
technique reste **légère** : juste de quoi amorcer `tdd-implement`.

## Quand l'utiliser

- Après `/spec`, pour préparer l'implémentation BDD+TDD.
- Quand une règle de gestion est trop vague pour être testée telle quelle.

## Processus

1. **Lis la spec d'abord.** Charge `docs/init/01-specification.md` (ou le chemin
   fourni). Ne génère jamais de scénarios depuis une page blanche.

2. **Nomme les tensions de testabilité — avant de poser quoi que ce soit.** Sonde :

   | Angle | La question dure |
   |---|---|
   | Cas nominal | Quel est le chemin heureux exact et observable ? |
   | Cas limite | Bornes : zéro, vide, max, simultané, frontière de durée ? |
   | Cas d'erreur | Que se passe-t-il quand l'invariant est violé, et quel comportement est attendu ? |
   | Données d'exemple | Quelles valeurs concrètes rendent le scénario vérifiable ? |
   | Observabilité | Comment sait-on que le `Then` est satisfait ? Quelle sortie observable ? |
   | Amorce technique | Quels composants .NET/Blazor/SignalR sont impactés, quels contrats de données ? |

3. **Pose une question à la fois.** Choix multiple quand c'est possible, hypothèse
   par défaut. Couvre au minimum : périmètre des scénarios, cas limites, erreurs,
   et le grain de l'analyse technique.

4. **Force le résultat observable.** Refuse un `Then` non vérifiable.

5. **Synthétise puis écris.** Une fois tranché, produis le fichier de sortie au
   format ci-dessous.

## Format du fichier de sortie

`docs/init/scenarios/<sujet>.md`, un fichier par sujet :

```markdown
# <Sujet> — Analyse & scénarios

## Analyse technique

- **Composants impactés** — … (côté .NET / Blazor / SignalR)
- **Contrats de données** — … (entrées/sorties, entités cœur)
- **Points d'attention TDD** — … (ordre de test, dépendances, cas délicats)

## Scénarios

Feature: <titre>
  <courte description de la valeur métier>

@nominal
Scenario 1: <titre>
  Given <état initial>
  When <action>
  Then <résultat observable>

@limite
Scenario 2: <titre>
  ...
```

Règles de forme :
- **Numérotation continue** : `Scenario 1`, `Scenario 2`… sans recommencer.
- Tag de type au-dessus de chaque scénario : `@nominal` / `@limite` / `@erreur`.
- `Scenario Outline` + `Examples:` quand le scénario varie selon des données.
- Le bloc Gherkin reste **fonctionnel/observable** ; la technique vit dans
  `## Analyse technique`, légère (3 puces max par sous-section).

## Mode agent (orchestré)

Quand ce skill est exécuté par un **subagent**, l'agent **ne pose pas** les
questions — il **ne peut pas** appeler `AskUserQuestion`. Il **renvoie** les
questions au thread principal (round-trip), puis, une fois le cadrage tranché,
**écrit lui-même** le fichier de sortie.

**Phase challenge** — à chaque appel, renvoie **uniquement** :

```json
{
  "tensions": ["angle de testabilité nommé", "..."],
  "questions": [
    {
      "question": "Question complète, finissant par ?",
      "header": "≤12 car",
      "multiSelect": false,
      "options": [
        { "label": "Choix 1 (Recommandé)", "description": "implication / tradeoff" },
        { "label": "Choix 2", "description": "..." }
      ]
    }
  ],
  "synthese": null,
  "done": false
}
```

Règles : **une question par tour**, 2-4 options, défaut en 1ʳᵉ option suffixé
` (Recommandé)`. `tensions` rempli au 1er tour, `[]` ensuite. Quand tranché :
`done: true`, `questions: []`, et `synthese` rempli :

```json
{
  "sujet": "<slug-kebab>",
  "feature": "<titre>",
  "analyse_technique": { "composants": ["..."], "contrats": ["..."], "points_tdd": ["..."] },
  "scenarios": [
    { "id": 1, "titre": "<court>", "type": "nominal", "given": "<état>", "when": "<action>", "then": "<résultat observable>" }
  ],
  "risques": ["..."]
}
```

**Phase écriture** — quand le thread principal renvoie l'ordre d'écrire (avec le
chemin cible), l'agent écrit `docs/init/scenarios/<sujet>.md` au format ci-dessus
et renvoie **uniquement** :

```json
{ "path": "docs/init/scenarios/<sujet>.md", "scenarios": <n>, "notes": "<bref>" }
```

Aucun texte hors du JSON dans chaque phase.

## Signaux d'alarme

- « Ça marche » / « c'est correct » comme `Then` → pas observable → extrais la sortie.
- Uniquement le cas nominal → manque limites et erreurs → nomme-les.
- Analyse technique qui déborde en conception complète → garde-la légère (amorce).

## Erreurs fréquentes

- **Poser avant d'avoir nommé les tensions.**
- **Plusieurs questions d'un coup.**
- **`Then` non observable accepté.**
- **Sur-spécifier l'analyse technique** — c'est une amorce, pas la conception finale.
