---
name: retours-challenge
description: À utiliser pour transformer des retours utilisateur sur un incrément livré (fichier NN-retours.md — retours IHM et/ou Tech) en un backlog priorisé de besoins (NN-besoins.md) qui réamorce le pipeline — challenge les retours sous pression, classe chaque item (bug / évolution / nouveau besoin), force la priorisation, et désigne le prochain sujet à passer à make-gherkin.
---

# Retours-challenge — Boucler retours → besoins

## Vue d'ensemble

Une passe critique qui **ferme la boucle** d'itération : après un incrément livré et
testé (IHM puis revue), l'utilisateur dépose ses retours dans un `NN-retours.md`. Ce
skill les **challenge** (comme `brainstorm`, mais en partant de retours concrets, pas
d'une idée), les **classe**, force une **priorisation** réelle, et produit un backlog
`NN-besoins.md` dont le **sujet prioritaire** réamorce `/2-make-gherkin`.

**Principe central :** un retour n'est pas un besoin. « J'aime pas le thème » est une
gêne, pas une spec. Ton rôle est d'extraire, de chaque retour, le **besoin sous-jacent
observable**, de le **classer**, puis de **séquencer** — sans accepter « tout d'un
coup ». Tu refuses l'égalité plate et tu extrais l'arbitre, exactement comme la passe
de challenge produit.

**Position dans le pipeline :** `/1-spec` → `/2-make-gherkin` → `/3-tdd-implement`
(+ IHM + validation visuelle) → **`/4-retours`** (besoins + archivage de l'itération) →
`/5-consolidation` (nouvelle version de spec vivante) → retour à `/2-make-gherkin`.

**Clôture d'itération :** une fois le backlog écrit, la command `/4-retours` archive les
fichiers de scénario (`NN-slug.md`) dans `<dossier>/archive/` (script
`archive-iteration.ps1`), ne laissant à la racine que `00-suivi.md`, le(s) `*-retours.md`
et `99-besoins-fin-itération.md`.

## Entrées

- **`NN-retours.md`** (obligatoire) — retours manuels de l'utilisateur, organisés en
  sections `## IHM - <zone>` et/ou `## Tech`. Les retours IHM portent sur l'ergonomie,
  les écrans, les parcours, les bugs constatés à l'usage ; les retours Tech sur la
  dette, la perf, l'archi, les dépendances.
- **Retours Tech — bypass** : si le `NN-retours.md` **ne contient pas** de section Tech
  (détecté par le script `find-retours.ps1`, `hasTech=false`), le thread principal
  **demande à l'utilisateur** (AskUserQuestion) s'il existe des contraintes techniques à
  injecter (ex. issues d'une revue de code GitHub) ou s'il n'y en a pas. Tu reçois le
  résultat de ce bypass dans le contexte — ne l'invente pas.
- **Contexte** — l'état livré : `00-suivi.md` (scénarios verts), la spec
  (`docs/01-specification.md`), le dossier de scénarios. À explorer pour situer chaque
  retour par rapport à l'existant.

## Classification (un retour → un type)

Pour **chaque** item de retour, attribue exactement un type :

| Type | Sens | Exemple typique |
|---|---|---|
| `bug` | comportement existant cassé / non conforme à un scénario vert | « j'ai invariablement l'erreur X quoi que je saisisse » |
| `évolution` | un comportement existant doit changer de forme/ergonomie | « ce tableau, je l'imaginais en calendrier navigable » |
| `nouveau besoin` | une capacité absente, à spécifier de zéro | « je veux une landing + connexion email Gmail/Apple/MS » |
| `question ouverte` | un retour qui pose une question produit non tranchée | « est-ce que ça ne devrait pas être un workflow de config ? » |

Un `bug` répare un scénario existant (re-fait `/3` ciblé) ; une `évolution` ou un
`nouveau besoin` alimente un **nouveau** passage `/2-make-gherkin`. Une `question
ouverte` doit être tranchée par le challenge avant de devenir l'un des trois autres.

## Processus

1. **Explore le contexte d'abord.** Lis le `NN-retours.md` en entier, puis l'état
   livré (`00-suivi.md`, spec, scénarios concernés). Ne challenge jamais à partir d'une
   page blanche : situe chaque retour par rapport au comportement déjà couvert.

2. **Classe et nomme les tensions — avant de poser quoi que ce soit.** Produis la
   table de classification (chaque retour → type → besoin sous-jacent), puis énonce
   crûment les angles morts :

   | Angle | La question dure |
   |---|---|
   | Bug vs évolution | Est-ce un défaut à réparer ou un changement de cap à spécifier ? |
   | Besoin réel | Le retour de surface (« j'aime pas ») cache quel besoin observable ? |
   | Ampleur cachée | Ce « petit » retour rouvre-t-il un pan entier (auth, rôles, modèle de données) ? |
   | Dépendance | Quel besoin en bloque d'autres (ex. modèle d'acteurs avant config parents) ? |
   | Risque mortel | Quel retour, ignoré, condamne l'adoption ? |
   | Coût / valeur | Quel est le ratio effort/valeur de chaque besoin ? |

3. **Pose une question à la fois.** Choix multiple quand c'est possible, hypothèse par
   défaut. Couvre au minimum : la **règle d'arbitrage** (quand deux besoins s'opposent,
   lequel gagne ?), le **séquencement** (quel besoin devient le prochain sujet ?), et la
   levée des **questions ouvertes** de la classification.

4. **Accepte les combinaisons — mais force un départage.** Comme `brainstorm` :
   l'utilisateur peut vouloir plusieurs choses, mais pas à égalité plate. Une
   combinaison priorisée (« A puis B, mais sécurité gagne ») est consignée et on avance ;
   un « tout pareil » sans règle → repose **une** question qui extrait l'arbitre.

5. **Désigne le prochain sujet.** La sortie pointe **un** sujet prioritaire, prêt à
   être passé à `/2-make-gherkin` (slug + intitulé), le reste séquencé derrière.

6. **Synthétise puis écris.** Une fois tranché, produis le `NN-besoins.md`.

## Format du fichier de sortie

`<dossier-du-retours>/99-besoins-fin-itération.md` (préfixe `99` = tri en fin de dossier,
un backlog de fin d'itération ; chemin fourni par `find-retours.ps1`, champ `nextBesoins`).

````markdown
# Besoins priorisés — <sujet de l'incrément>

> Source : `NN-retours.md` · produit par `/4-retours` (retours-challenge).
> Réamorce `/2-make-gherkin` sur le **sujet prioritaire** ci-dessous.

## Classification des retours

| # | Retour (résumé) | Type | Besoin sous-jacent | Zone IHM/Tech |
|---|---|---|---|---|
| 1 | … | nouveau besoin | … | IHM - général |

## Arbitrage

- **Objectif de l'itération** — …
- **Arbitre (départage)** — quand deux besoins s'opposent : … gagne, parce que …

## Séquence de livraison

| Rang | Besoin | Type | Sujet make-gherkin | Dépend de |
|---|---|---|---|---|
| 1 | … | nouveau besoin | `<slug>` | — |
| 2 | … | évolution | `<slug>` | rang 1 |

## Prochain sujet → make-gherkin

- **Sujet** : `<slug-kebab>` — <intitulé>
- **Périmètre** : <ce que le prochain passage /2 doit couvrir, en langage besoin>
- **Hors périmètre (reporté)** : <ce qui attend un rang ultérieur>

## Risques & questions encore ouvertes

- …
````

## Mode agent (orchestré)

Quand ce skill est exécuté par un **subagent** (pas le thread principal), l'agent **ne
pose pas** les questions — il **ne peut pas** appeler `AskUserQuestion`. Il **renvoie**
les questions au thread principal (round-trip), puis, une fois le cadrage tranché,
**écrit lui-même** le fichier de sortie.

**Phase challenge** — à chaque appel, renvoie **uniquement** :

```json
{
  "classification": [
    { "retour": "<résumé>", "type": "bug|évolution|nouveau besoin|question ouverte", "besoin": "<besoin observable>", "zone": "<section du retours>" }
  ],
  "tensions": ["angle mort nommé", "..."],
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
` (Recommandé)`. `classification` + `tensions` remplis au **1er tour**, `[]` ensuite.
**Combinaisons acceptées, départage exigé** (cf. `brainstorm`) : un « tous à égalité »
sans règle ne passe pas à `done` — repose une question qui extrait l'arbitre. Quand
tranché : `done: true`, `questions: []`, et `synthese` rempli :

```json
{
  "sujet_incrément": "<sujet de l'incrément retourné>",
  "objectif": "<objectif de l'itération>",
  "arbitre": "<règle de départage>",
  "besoins": [
    { "rang": 1, "besoin": "<résumé>", "type": "nouveau besoin", "sujet_make_gherkin": "<slug>", "depend_de": null }
  ],
  "prochain_sujet": { "slug": "<slug-kebab>", "intitule": "<titre>", "perimetre": "<...>", "hors_perimetre": "<...>" },
  "risques": ["..."]
}
```

**Phase écriture** — quand le thread principal renvoie l'ordre d'écrire (avec le chemin
cible `NN-besoins.md`), l'agent écrit le fichier au format ci-dessus et renvoie
**uniquement** :

```json
{ "path": "docs/sprints/<dossier>/NN-besoins.md", "besoins": <n>, "prochain_sujet": "<slug>", "notes": "<bref>" }
```

Aucun texte hors du JSON dans chaque phase.

## Signaux d'alarme

- **Retour pris pour un besoin** (« j'aime pas X ») → extrais le besoin observable
  derrière la gêne.
- **Bug confondu avec évolution** → un comportement censé marcher (scénario vert) qui
  casse est un `bug`, pas une nouvelle feature ; il se répare via `/3` ciblé.
- **« Tout est prioritaire »** → égalité plate, pas d'arbitre → extrais la règle de
  départage (combinaison priorisée OK, égalité plate refusée).
- **Ampleur sous-estimée** — un retour anodin (« config des parents ») qui implique un
  modèle d'acteurs entier → nomme la dépendance, ne le glisse pas en v1.
- **Tech ignoré faute de section** — si `hasTech=false`, le bypass AskUser **doit** avoir
  eu lieu ; ne conclus pas sans avoir su s'il y a des contraintes techniques.

## Erreurs fréquentes

- **Conclure sans classifier** — la table de classification est le socle ; sans elle,
  bugs et besoins se mélangent.
- **Plusieurs questions d'un coup** — une seule question tranchante par tour.
- **Accepter une égalité plate** — toute la valeur est de casser l'égalité.
- **Désigner zéro ou plusieurs « prochains sujets »** — la sortie pointe **un** sujet
  pour `/2-make-gherkin`, le reste séquencé derrière.
- **Faire le cheerleader** — pas d'éloge ; challenge ou tais-toi.
