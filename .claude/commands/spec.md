---
description: Cadre un produit ou une feature de bout en bout — challenge le PO (via agent) puis rédige/maj la spec (via agent) au format maison.
argument-hint: "[sujet ou feature à cadrer] (optionnel)"
---

# /spec — Cadrage produit orchestré

**Tout le travail vit dans les agents** (`brainstorm`, `redaction-spec`). Toi
(thread principal) tu es un **relais pur** : tu ne challenges pas, tu ne rédiges
pas, tu ne calcules ni tensions ni synthèse. Tu te bornes à : dispatcher les
agents, rendre leurs questions via `AskUserQuestion`, leur renvoyer les réponses
brutes via `SendMessage`. Objectif : **garder le contexte du main propre** — tout
le raisonnement reste chez les agents.

> ⚠️ Seul le thread principal peut appeler `AskUserQuestion` ; un subagent ne le
> peut pas. C'est la **seule** raison du round-trip. La communication =
> `SendMessage` (main → agent) et la valeur de retour de l'agent (agent → main).

Sujet (optionnel) : $ARGUMENTS

## Déroulé

1. **Contexte.** Repère les **chemins** de spec/docs/commits pertinents à passer aux
   agents. **Ne les lis pas toi-même** — les agents s'en chargent.

2. **Challenge (agent + round-trip) :**
   - Dispatche l'agent `brainstorm` avec le sujet + les chemins de contexte. Garde
     son `agentId`.
     - **Fallback** : si le type `brainstorm` n'est pas dans le registre de la
       session, dispatche `general-purpose` avec « applique le skill `brainstorm`,
       mode agent orchestré » + le sujet/chemins. Ne bascule **pas** en inline.
   - Il renvoie un JSON `{ tensions, questions, synthese, done }`.
   - Au plus **une ligne** de contexte (les `tensions` en bref), puis rends **chaque**
     `questions[]` via `AskUserQuestion` en passant l'objet **tel quel** (pas de
     reformulation).
   - Renvoie les réponses **brutes** à l'agent via `SendMessage` (même `agentId`).
   - Répète tant que `done` est faux. Si le PO répond « tous », l'agent reposera une
     question de séquencement — c'est voulu. **N'analyse pas**, **ne devine pas** la
     question suivante.

3. **Validation PO.** Quand `done: true`, présente la `synthese` de l'agent (verbatim)
   et demande l'accord avant de rédiger.

4. **Rédaction (agent) :**
   - Dispatche l'agent `redaction-spec` avec le chemin cible + la `synthese`.
     - **Fallback** : type absent → `general-purpose` + « applique le skill
       `redaction-spec` » + le chemin cible et la synthèse.
   - Il écrit le fichier et renvoie `{ path, sections, regles, notes }`.

5. **Propagation.** Mets à jour les docs qui référencent la spec (README, roadmap) ; garde une seule source de vérité, pointe les brouillons obsolètes vers elle.

6. **Commit.** Propose un commit (sans pousser sauf demande explicite).

## Notes

- **Relais pur** : si tu te surprends à challenger, lister ou rédiger toi-même, tu as
  quitté ton rôle — redélègue à l'agent.
- `AskUserQuestion` est appelé **par toi** (thread principal), jamais par les agents.
- Une question à la fois pendant le challenge — pas de rafale.
- Fonctionnel uniquement dans la spec : aucun choix technique.
- Le challenge n'est pas une formalité : pas de complaisance, on tranche.
