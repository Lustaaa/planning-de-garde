---
description: Cadre un produit ou une feature de bout en bout — challenge le PO (via agent) puis rédige/maj la spec (via agent) au format maison.
argument-hint: "[sujet ou feature à cadrer] (optionnel)"
---

# /1-spec — Cadrage produit orchestré

**Tout le travail vit dans les agents** (`brainstorm`, `redaction-spec`). Toi
(thread principal) tu es un **orchestrateur** : tu ne challenges pas, tu ne rédiges
pas, tu ne calcules ni tensions ni synthèse. Tu te bornes à : dispatcher les
agents, **router leurs questions vers le chef de projet** (et n'escalader au PO via
`AskUserQuestion` que les portes G1/G2), leur renvoyer les réponses brutes via
`SendMessage`. Objectif : **garder le contexte du main propre** — tout le raisonnement
reste chez les agents, et **le PO n'est sollicité que sur les portes essentielles**.

> ⚠️ Seul le thread principal peut appeler `AskUserQuestion` ; un subagent ne le
> peut pas. C'est la **seule** raison du round-trip. La communication =
> `SendMessage` (main → agent) et la valeur de retour de l'agent (agent → main).

> **Protocole d'escalade — chef de projet (CP).** Tu ne poses plus directement au PO les
> questions des agents. Quand un agent dev renvoie une `question`, **dispatche d'abord l'agent
> `chef-de-projet`** avec : la `question`, la **spec courante** (`docs/NN-specification.md`, la
> plus récente), `docs/BACKLOG.md`, le palier d'autonomie (défaut `0 — conservateur`).
> - `{type:"decision",…}` → **affiche le `resume` du CP en une ligne** (`🧭 CP — <resume>`) pour
>   le suivi du PO (sans `AskUserQuestion`, sans l'interrompre), puis **relaie la décision** à
>   l'agent dev via `SendMessage`.
> - `{type:"escalate", gate:"G1"|"G2", …}` → **seulement là** appelle `AskUserQuestion`, en
>   affichant le payload riche du CP (`contexte` + `recommandation_cp` + `consequences`)
>   au-dessus des `options`. Renvoie la réponse brute à l'agent dev.
> - **Fallback** : type `chef-de-projet` absent du registre → `general-purpose` + « applique le
>   skill `chef-de-projet` » + les mêmes entrées.

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
   - Au plus **une ligne** de contexte (les `tensions` en bref), puis, pour **chaque**
     `questions[]`, applique le **Protocole d'escalade CP** ci-dessus : dispatche d'abord
     `chef-de-projet` ; n'appelle `AskUserQuestion` que sur une `escalate` (G1/G2). *(`/1-spec`
     est le chemin de cadrage produit — backlog épuisé/besoin nouveau ; le CP y escaladera
     souvent en G1/G2, c'est attendu.)*
   - Renvoie les réponses **brutes** (décision du CP ou réponse du PO) à l'agent via
     `SendMessage` (même `agentId`).
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
