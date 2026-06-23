---
description: Transforme la spec fonctionnelle (docs/) en un fichier d'analyse technique légère + scénarios Gherkin numérotés, via l'agent make-gherkin (round-trip de questions + écriture).
argument-hint: "[sujet ou feature à scénariser] (optionnel)"
---

# /2-make-gherkin — Analyse & scénarios Gherkin

**Tout le travail vit dans le subagent `make-gherkin`.** Toi (thread principal) tu
es un **relais pur** : tu ne lis pas la spec, tu ne nommes pas les tensions, tu ne
calcules ni scénarios ni synthèse. Tu te bornes à : dispatcher l'agent, rendre ses
questions via `AskUserQuestion`, lui renvoyer les réponses brutes via `SendMessage`,
puis lui ordonner d'écrire. Objectif : **garder le contexte du main propre** — tout
le raisonnement reste chez l'agent.

> ⚠️ Seul le thread principal peut appeler `AskUserQuestion` ; un subagent ne le
> peut pas. C'est la **seule** raison du round-trip : l'agent renvoie les questions,
> tu les poses, tu lui rends les réponses. La communication = `SendMessage`
> (main → agent) et la valeur de retour de l'agent (agent → main).

Sujet (optionnel) : $ARGUMENTS

## Déroulé

1. **Dispatch (une fois).** Lance l'agent `make-gherkin` en lui passant **le chemin**
   de la spec (`docs/01-specification.md` ou le fichier pertinent sous
   `docs/`) + le sujet `$ARGUMENTS`. **Ne lis pas la spec toi-même** — l'agent
   s'en charge. Garde son `agentId` pour tout le round-trip.
   - **Fallback** : si le type `make-gherkin` n'est pas dans le registre de la
     session (« Agent type not found »), dispatche `general-purpose` avec la consigne
     « applique le skill `make-gherkin`, section *Mode agent (orchestré)* » + le
     chemin de la spec. Même protocole de round-trip ensuite. Ne bascule **pas** en
     exécution inline dans le main.

2. **Boucle de challenge (relais).** À chaque retour, l'agent renvoie un JSON
   `{ tensions, questions, synthese, done }`. Tant que `done` est faux :
   - Rends **chaque** entrée de `questions[]` via `AskUserQuestion` en passant l'objet
     **tel quel** (`question`, `header`, `multiSelect`, `options[]`) — ne le reformule
     pas, ne le ré-enrichis pas.
   - Au plus **une ligne** de contexte pour le user si l'agent fournit des `tensions`
     (les énumérer en bref) ; sinon, n'écris rien.
   - Renvoie les réponses **brutes** à l'agent via `SendMessage` (même `agentId`).
   - Recommence. **N'analyse pas** le contenu, **ne devine pas** la question suivante.

3. **Validation.** Quand `done: true`, affiche la `synthese` de l'agent (verbatim,
   sans la retravailler) et demande l'accord d'écrire via `AskUserQuestion`.

4. **Écriture (même agent).** À l'accord, calcule le chemin cible numéroté :
   `docs/scenarios/NN-<sujet-kebab>.md` où `NN` = `(plus grand préfixe NN
   existant dans le dossier) + 1` sur 2 chiffres (à défaut `01`) — un simple `Glob`
   du dossier suffit. `SendMessage` l'ordre d'écrire avec ce chemin. L'agent écrit le
   fichier (au format imposé du skill) et renvoie `{ path, scenarios, notes }`.

5. **Commit.** Propose un commit (sans pousser sauf demande explicite).

## Notes

- **Relais pur** : si tu te surprends à lire la spec, lister des scénarios ou rédiger
  une synthèse toi-même, tu as quitté ton rôle — redélègue à l'agent.
- `AskUserQuestion` est appelé **par toi** (thread principal), jamais par l'agent ;
  c'est l'unique chose que l'agent ne peut pas faire.
- Une question à la fois pendant le challenge — pas de rafale.
- **Formatage imposé du fichier** (cf. skill, « Format du fichier de sortie ») :
  `Feature:` en intro hors fence, puis **un scénario = un en-tête
  `### Scenario N — <titre> ` + tag inline en code + son propre bloc
  ` ```gherkin `**. Numérotation continue ; chaque scénario autonome (pas de
  `Background:`).
- L'analyse technique du fichier reste **légère** (amorce d'implémentation).
- Entrée attendue : une spec déjà produite par `/1-spec`.
- Ce fichier sera l'entrée de la future pipeline `tdd-implement`.
