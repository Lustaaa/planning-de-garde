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

**Deux exigences non négociables sur chaque ligne Gherkin :**
- **Valeurs concrètes** — jamais `un montant`, toujours `un montant de 150,37 €` ;
  jamais `une date`, toujours `le 14/07 à 22 h`. Un scénario sans valeur d'exemple
  vérifiable n'est pas testable.
- **Langage métier, zéro fuite technique** — le bloc `Given/When/Then` parle le
  vocabulaire du domaine (`Given un agent de garde`), jamais l'implémentation
  (`Given le repository est mocké`, `When l'API est appelée`). La technique vit
  **uniquement** dans `## Analyse technique`.

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
   | Invariant | Quelle règle doit rester vraie après **chaque** action ? Sa violation alimente directement les cas limites et erreur. |
   | Cas limite | Bornes : zéro, vide, max, simultané, frontière de durée ? |
   | Concurrence | Deux acteurs touchent-ils la **même** unité de cohérence au même instant (double-clic, latence) ? Si oui → scénario de conflit : la 2ᵉ écriture périmée est rejetée proprement. |
   | Cas d'erreur | Que se passe-t-il quand l'invariant est violé, et quel comportement est attendu ? |
   | Données d'exemple | Quelles valeurs concrètes rendent le scénario vérifiable ? |
   | Observabilité | Comment sait-on que le `Then` est satisfait ? Quelle sortie observable ? |
   | Amorce technique | Quels composants .NET/Blazor/SignalR sont impactés, quels contrats de données ? |

3. **Pose une question à la fois.** Choix multiple quand c'est possible, hypothèse
   par défaut. Couvre au minimum : périmètre des scénarios, cas limites, erreurs,
   et le grain de l'analyse technique.

4. **Force le résultat observable.** Refuse un `Then` non vérifiable.

5. **Matrice de couverture — avant de conclure.** Pour chaque comportement /
   règle de gestion de la spec, vérifie qu'il existe un scénario **nominal**, au
   moins un **limite** et un **erreur**. Un trou (ex. règle sans cas d'erreur) →
   ajoute un scénario candidat ou pose une question. Ne conclus jamais sur le seul
   chemin heureux.

   | Règle / comportement | @nominal | @limite | @erreur |
   |---|---|---|---|
   | RG1 — … | ✅ | ✅ | ❌ → à combler |

6. **Synthétise puis écris.** Une fois tranché, produis le fichier de sortie au
   format ci-dessous.

## Format du fichier de sortie

`docs/init/scenarios/<sujet>.md`, un fichier par sujet :

```markdown
# <Sujet> — Analyse & scénarios

## Analyse technique

- **Composants impactés** — … (côté .NET / Blazor / SignalR)
- **Couches & dépendances** — situe chaque composant (Domain / Application /
  Infrastructure–Blazor/SignalR) ; les dépendances pointent **vers l'intérieur**, le
  domaine sans `using` de framework. Litmus : testable sans framework ? infra
  remplaçable sans toucher au domaine ?
- **Contrats de données** — … (entrées/sorties, entités cœur)
- **Write vs read (CQRS)** — un comportement de **modification** passe par un agrégat
  (invariants protégés) ; un besoin de **lecture/affichage** par une projection
  dédiée, jamais par un getter de vue sur l'agrégat. Test : « cette donnée sert-elle
  un invariant ? » Non → modèle de lecture.
- **Invariants** — … (règles vraies après chaque action ; qui les garde — l'agrégat,
  pas le use case)
- **Points d'attention TDD** — … (ordre de test, ce qu'on double — ports
  seulement —, cas délicats)

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
- `Background:` quand un même `Given` est partagé par **tous** les scénarios de la
  feature — factorise-le. S'il ne vaut que pour une partie, garde-le dans chaque
  scénario concerné (pas de pollution du `Background`).
- **Valeurs concrètes** dans chaque ligne (cf. principe central), jamais de
  variable vague.
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
  "analyse_technique": { "composants": ["..."], "contrats": ["..."], "invariants": ["..."], "points_tdd": ["..."] },
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
- **Action multi-acteurs sans scénario de concurrence** (lire-décider-écrire sur une
  donnée partagée) → ajoute un scénario de conflit (rejet observable de la 2ᵉ écriture).
- **Valeur vague** (`un montant`, `une date`, `un utilisateur`) → exige une valeur
  d'exemple concrète.
- **Fuite technique** (`mock`, `repository`, `API`, `base de données` dans un
  `Given/When/Then`) → reformule en langage métier ; déplace la technique dans
  `## Analyse technique`.
- Analyse technique qui déborde en conception complète → garde-la légère (amorce).

## Erreurs fréquentes

- **Poser avant d'avoir nommé les tensions.**
- **Plusieurs questions d'un coup.**
- **`Then` non observable accepté.**
- **Scénario sans valeur d'exemple concrète.**
- **Vocabulaire d'implémentation dans le Gherkin** au lieu du langage métier.
- **Conclure sans matrice de couverture** — règle laissée sans cas limite/erreur.
- **Sur-spécifier l'analyse technique** — c'est une amorce, pas la conception finale.
