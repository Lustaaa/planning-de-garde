---
name: retro-sprint
description: À utiliser à chaque fin de sprint (rétrospective SCRUM) pour challenger et optimiser la MÉTHODE — les agents, skills et commands du pipeline lui-même — à la lumière du sprint qui vient de se terminer. Dresse un bilan (ce qui a marché / à améliorer), propose des éditions concrètes et ciblées des fichiers du pipeline, et applique celles validées par le PO. À ne pas confondre avec /4-retours (rétro produit, sur l'app).
---

# Retro-sprint — Rétrospective de la méthode (pipeline)

## Vue d'ensemble

La **rétrospective SCRUM** de fin de sprint, version pipeline. Elle ne porte **pas sur
le produit** (ça, c'est `/4-retours` : les retours d'usage sur l'app) mais sur la
**méthode** : les **agents, skills et commands** qui orchestrent le travail. Objectif —
amélioration continue : à chaque sprint, on regarde ce qui a frotté dans le pipeline et
on **corrige l'outillage** pour que le sprint suivant aille mieux.

**Principe central :** une rétro sans action est du bavardage. Chaque point « à
améliorer » doit aboutir à une **édition concrète et ciblée** d'un fichier du pipeline
(`.claude/agents/*.md`, `.claude/skills/*/SKILL.md`, `.claude/skills/*/scripts/*.ps1`,
`.claude/commands/*.md`) — pas à un vœu pieux. Le PO valide lesquelles appliquer ; les
retenues sont **appliquées** dans la foulée.

**Position :** dernier maillon de la boucle, déclenchée **à chaque clôture** par
`/6-cloture-sprint` (étape 1, avant le push — les améliorations validées partent dans la
PR du sprint).

## Entrées

- **Journal méthode `99-sprint<NN>-retours.md` (SOURCE PRIMAIRE)** — le journal des
  retours méthode rempli **pendant le sprint** par le thread principal : chaque ligne
  `| Date | Cible (agent/skill/command) | Retour | Décision prise |` est un retour à la
  volée du PO sur un fichier du pipeline. C'est le **canal privilégié** des retours du
  PO sur la méthode. Lis-le **en premier** : transforme **chaque entrée consignée** en
  une **proposition d'édition concrète** de l'agent/skill/command ciblé (cible précise +
  changement en clair + raison reprise du retour). À ne pas confondre avec le backlog
  produit `99-sprint<NN>-besoins-fin-itération.md` ni avec le retours produit `NN-retours.md`.
- **Sprint clos** — le dossier `docs/sprints/<sprint>/` (`00-sprint<NN>-suivi.md`,
  `*-retours.md`, `99-sprint<NN>-besoins-fin-itération.md` — `<NN>` = numéro du sprint =
  préfixe 2 chiffres du dossier, ex. `00-sprint02-suivi.md` ; scénarios sous `archive/`) :
  la trace de ce qui s'est passé.
- **Le pipeline** — les fichiers `.claude/agents/`, `.claude/skills/`, `.claude/commands/`
  effectivement exercés pendant le sprint.
- **Frictions observées** — transmises par le thread principal (le PO les a vécues : un
  agent qui a dû passer en fallback, une propagation oubliée, un format dévié, un early
  green non anticipé, une question reposée en boucle…). L'agent ne voit pas la
  conversation : il s'appuie sur ces frictions + ce que les artefacts révèlent.

## Processus

1. **Explore d'abord — le journal méthode en premier.** Lis le journal
   `99-sprint<NN>-retours.md` (source primaire) puis les autres artefacts du sprint clos et
   les fichiers du pipeline concernés. Croise avec les frictions transmises. Ne pars pas
   d'une page blanche. **Chaque entrée du journal méthode devient au moins une action**
   (cf. étape 3).

2. **Bilan SCRUM — honnête, sans complaisance.**
   - **Ce qui a bien marché** (à conserver / renforcer) — les mécaniques du pipeline qui
     ont tenu.
   - **Ce qui a coincé** (à améliorer) — chaque friction nommée **avec sa preuve** (le
     fichier/étape concerné, ce qui s'est passé).

3. **Transforme chaque friction en action concrète.** Une action = `{ cible (fichier
   précis), édition (ce qu'on change, en clair), raison }`. Pas d'action vague (« mieux
   documenter ») : pointe le fichier et le changement. **Chaque entrée du journal méthode
   `99-sprint<NN>-retours.md` donne lieu à une action** ciblant la `Cible` consignée (en
   tenant compte de la `Décision prise` déjà notée). Si une friction n'a pas de remède
   outillé clair, formule-la en question au PO plutôt qu'en fausse action.

4. **Round-trip de priorisation.** Présente les actions ; le PO valide **lesquelles
   appliquer** (toutes, un sous-ensemble, ou en redéfinir une). Tu ne tranches pas seul ce
   qui touche le pipeline.

5. **Applique les actions validées.** Édite les fichiers cibles retenus, puis écris le
   compte rendu `98-retrospective.md`. N'applique **que** les actions validées.

## Format du compte rendu

`<dossier-du-sprint>/98-retrospective.md` (préfixe `98` = juste avant le backlog `99`).

````markdown
# Rétrospective — <sprint>

> Rétro de la **méthode** (pipeline d'agents/skills/commands) · produite par `/7-retrospective`.
> Distincte de `99-sprint<NN>-besoins-fin-itération.md` (rétro produit).

## Ce qui a bien marché

- …

## Ce qui a coincé

- **<friction>** — <preuve : fichier/étape, ce qui s'est passé>

## Actions sur le pipeline

| # | Cible (fichier) | Édition | Statut |
|---|---|---|---|
| 1 | `.claude/agents/…` | … | ✅ appliquée / ⏸️ reportée / ❌ écartée |

## Questions ouvertes (méthode)

- …
````

## Mode agent (orchestré)

Exécuté par un **subagent**, l'agent **ne pose pas** les questions (pas d'`AskUserQuestion`) :
il les **renvoie** au thread principal (round-trip), puis applique et écrit.

**Phase rétro** — à chaque appel, renvoie **uniquement** :

```json
{
  "bilan": { "bien": ["…"], "ameliorer": [ { "friction": "…", "preuve": "fichier/étape + ce qui s'est passé" } ] },
  "actions": [ { "id": 1, "cible": ".claude/…", "edition": "ce qu'on change, en clair", "raison": "…" } ],
  "questions": [
    { "question": "…?", "header": "≤12 car", "multiSelect": true, "options": [ { "label": "Action 1 — …", "description": "…" }, { "label": "Action 2 — …", "description": "…" } ] }
  ],
  "synthese": null,
  "done": false
}
```

Règles : `bilan` + `actions` remplis au **1er tour**. La question de priorisation peut
être **multiSelect** (le PO coche les actions à appliquer). Quand le PO a tranché :
`done: true`, `questions: []`, `synthese` = `{ "appliquees": [ids], "reportees": [ids], "ecartees": [ids] }`.

**Phase application** — quand le thread principal renvoie l'ordre d'appliquer (avec la
liste des actions validées et le chemin `98-retrospective.md`), l'agent **édite les
fichiers cibles retenus**, écrit le compte rendu, et renvoie **uniquement** :

```json
{ "path": "docs/sprints/<sprint>/98-retrospective.md", "appliquees": [1,3], "fichiers_modifies": ["…"], "notes": "<bref>" }
```

Aucun texte hors du JSON dans chaque phase.

## Signaux d'alarme

- **Rétro produit déguisée** — des actions qui parlent de l'app (features, écrans) au lieu
  du pipeline → c'est `/4-retours`, pas ici. Recentre sur agents/skills/commands.
- **Action vague** — « améliorer X » sans fichier ni changement précis → transforme en
  édition ciblée ou en question.
- **Auto-application non validée** — éditer un fichier du pipeline sans l'aval du PO.
- **Complaisance** — « tout s'est bien passé » sans regarder les frictions ; il y en a
  toujours (un fallback, un format, une question reposée).

## Erreurs fréquentes

- **Confondre rétro méthode et rétro produit.**
- **Conclure sans action concrète** par friction réelle.
- **Appliquer plus que ce que le PO a validé.**
