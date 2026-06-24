---
name: spec-consolidation
description: À utiliser pour consolider un backlog de besoins priorisés (99-sprint<NN>-besoins-fin-itération.md, sortie de /4-retours) avec la spec courante en une nouvelle version versionnée de spec vivante (NN-specification.md) — documentation à jour de la vision et du pourquoi de l'application, source de vérité unique qui réamorce /2-make-gherkin. L'ancienne version reste figée en historique.
---

# Spec-consolidation — Backlog de besoins → spec vivante versionnée

## Vue d'ensemble

L'étage qui transforme les **besoins priorisés** d'une itération en la **prochaine
version de la spec**. Tu pars du backlog `99-sprint<NN>-besoins-fin-itération.md` (`<NN>` =
numéro du sprint = préfixe 2 chiffres du dossier, ex. `99-sprint02-besoins-fin-itération.md` ; produit par
`/4-retours`) et de la **spec courante** (`NN-specification.md` de plus grand préfixe),
et tu produis `<NN+1>-specification.md` : une **documentation vivante** de l'app — son
*pourquoi* (vision, objectif, arbitre), son périmètre courant et ses règles de gestion —
qui intègre les besoins validés et redevient la **source de vérité unique** consommée
ensuite par `/2-make-gherkin`.

**Principe central :** la nouvelle spec n'est pas un journal de diffs, c'est l'**état
courant cohérent** de la vision. Un lecteur (humain ou agent) qui ne lit qu'elle doit
comprendre l'application : pourquoi elle existe, ce qu'elle fait aujourd'hui, et les
règles qui la gouvernent. Tu **réécris** la spec dans sa forme à jour, tu n'empiles pas
des annexes.

**Position dans le pipeline :** `/4-retours` (besoins + archivage) → **`/5-consolidation`**
(cette passe) → `/2-make-gherkin` (sur la nouvelle version de spec) → `/3-tdd-implement`.

## Versionnage

- **Spec courante** = le `NN-specification.md` de plus grand préfixe dans `docs/` (le
  plus grand numéro **est** le pointeur « version courante »). Fourni par
  `find-spec.ps1` (`currentSpec`).
- **Sortie** = `<NN+1>-specification.md` (`nextSpec`). L'ancienne version **reste
  figée** comme trace historique — ne la modifie pas.
- La nouvelle spec porte, juste sous le titre, un blockquote de version :
  `> Version <NN+1> · consolide la v<NN> + le backlog <sprint>/99-sprint<NN>-besoins-fin-itération.md.`

## Entrées

- **Backlog** `99-sprint<NN>-besoins-fin-itération.md` (obligatoire) — besoins classés, arbitre,
  séquence, prochain sujet, risques/questions ouvertes (sortie de `/4-retours`).
- **Spec courante** `NN-specification.md` — la version à faire évoluer.
- **Contexte** — le sprint clos (`00-sprint<NN>-suivi.md`, `*-retours.md`) pour situer ce qui a été
  livré et ce que les retours remettent en cause.

## Format de sortie

La nouvelle spec suit le **format maison** du skill `redaction-spec` (sections, ordre,
numérotation continue des règles) — réutilise-le, n'improvise pas la structure :

1. `# <Titre> — <sous-titre>`
2. `> Version <NN+1> · consolide la v<NN> + …` (blockquote de version)
3. `## Contexte` (2-3 lignes : le produit, pour qui)
4. `## Objectif & arbitrage` (objectifs + arbitre en blockquote)
5. `## Séquence de livraison` (phases numérotées, justifiées — **alignée sur la séquence
   du backlog**)
6. `## Mécaniques de base` (invariants structurels)
7. `## Règles de gestion` (cœur — catégories `###`, numérotation continue)
8. `## Risques & questions ouvertes`

**Consolidation, pas juxtaposition :** intègre les besoins validés **dans** les bonnes
sections (une nouvelle capacité = nouvelle règle / mécanique / phase de séquence ; une
évolution = règle révisée ; un besoin qui invalide une règle existante = règle réécrite,
pas doublée). Conserve les règles encore valides de la version précédente. La
numérotation des règles reste **continue** dans la version produite.

## Processus

1. **Explore d'abord.** Lis le backlog `99-sprint<NN>-besoins-fin-itération.md` en entier, puis la
   spec courante, puis le contexte du sprint clos. Repère : ce qui est **nouveau**, ce
   qui **révise** une règle existante, ce qui **invalide** une règle.

2. **Nomme les points de consolidation — avant de poser quoi que ce soit.** Pour chaque
   besoin du backlog : section cible de la spec, et règle créée / révisée / supprimée.
   Signale les **collisions** (un besoin qui contredit une règle en vigueur — ex. un
   transfert dérivé automatiquement vs la règle « transferts explicites »).

   **Contrôle « besoin vs couverture existante » (avant de créer une règle/un sujet).**
   Pour chaque besoin, vérifie qu'il n'est pas **déjà couvert** par le code/les commits
   existants : `Grep` le comportement dans `src/`, relis les règles déjà présentes dans
   la spec courante et les scénarios `@vert` du sprint clos. Un besoin **déjà livré** ne
   devient ni une nouvelle règle ni un sujet make-gherkin — signale-le
   (`couverture: "déjà livré — <fichier/commit>"`) au lieu d'ordonner la réparation de ce
   qui existe. C'est le garde-fou contre le sprint à vide (réparer du déjà-fait).

3. **Pose une question à la fois** pour chaque collision ou question ouverte non tranchée
   (round-trip). Reprends en priorité les **questions ouvertes** héritées du backlog.
   Choix multiple, hypothèse par défaut en 1ʳᵉ option. Ne devine pas une réécriture de
   règle structurante.

4. **Garde la spec vivante et cohérente.** Pas de section « changelog » qui raconte les
   diffs : l'état courant se lit directement. L'historique vit dans les fichiers de
   version précédents, figés.

5. **Synthétise puis écris** `<NN+1>-specification.md`.

## Mode agent (orchestré)

Exécuté par un **subagent**, l'agent **ne pose pas** les questions (il ne peut pas
appeler `AskUserQuestion`) : il les **renvoie** au thread principal (round-trip), puis,
une fois tranché, **écrit** la nouvelle spec.

**Phase consolidation** — à chaque appel, renvoie **uniquement** :

```json
{
  "plan_consolidation": [
    { "besoin": "<résumé>", "section_cible": "Règles de gestion / …", "action": "nouvelle règle|règle révisée|règle supprimée|nouvelle mécanique|phase de séquence|déjà couvert (aucune)", "collision": "<règle en vigueur contredite, ou null>", "couverture": "<\"déjà livré — fichier/commit\" si le besoin est déjà couvert par le code existant, sinon null>" }
  ],
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
` (Recommandé)`. `plan_consolidation` rempli au **1er tour**, `[]` ensuite. Une collision
avec une règle structurante **doit** être tranchée avant `done`. Quand tout est tranché :
`done: true`, `questions: []`, et `synthese` rempli :

```json
{
  "titre": "<titre de la spec>",
  "version": "<NN+1>",
  "remplace": "<NN>",
  "contexte": "<2-3 lignes>",
  "objectif": "<objectifs>",
  "arbitre": "<règle de départage>",
  "sequence": ["phase 1 — justif", "..."],
  "mecaniques": ["..."],
  "regles": [ { "categorie": "<###>", "nom": "<court>", "regle": "<une phrase>", "origine": "conservée|révisée|nouvelle" } ],
  "risques": ["..."]
}
```

**Phase écriture** — quand le thread principal renvoie l'ordre d'écrire (avec le chemin
`nextSpec`), l'agent écrit la spec au format maison et renvoie **uniquement** :

```json
{ "path": "docs/NN-specification.md", "version": "<NN>", "remplace": "<NN-1>", "regles": <n>, "notes": "<bref>" }
```

Aucun texte hors du JSON dans chaque phase.

## Signaux d'alarme

- **Juxtaposition au lieu de consolidation** — coller les besoins en annexe au lieu de
  les fondre dans les bonnes sections → la spec cesse d'être lisible d'un trait.
- **Collision tue** — un besoin qui contredit une règle en vigueur (transfert auto vs
  explicite) passé sous silence → règle incohérente. Tranche-la en round-trip.
- **Changelog déguisé** — une section qui raconte « ce qui a changé » → la spec vivante
  décrit l'**état**, pas l'historique (qui vit dans les versions figées).
- **Fuite technique** — une règle qui parle d'implémentation → coupe (cf. `redaction-spec`).
- **Numérotation cassée** — règles renumérotées par catégorie → garde-la continue.
- **Besoin déjà livré ordonné à nouveau** — un besoin couvert par le code/les commits
  existants transformé en règle/sujet de sprint → contrôle « besoin vs couverture
  existante » sauté → sprint à vide sur du déjà-fait. Signale `couverture`, ne réordonne pas.

## Erreurs fréquentes

- **Modifier l'ancienne version** au lieu d'en créer une nouvelle figée.
- **Conclure malgré une collision non tranchée** avec une règle structurante.
- **Plusieurs questions d'un coup** — une seule par tour.
- **Perdre une règle encore valide** de la version précédente en réécrivant.
