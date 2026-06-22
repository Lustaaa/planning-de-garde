---
name: challenge-po
description: Use when a product idea, spec, or feature request needs to be pressure-tested before writing it down — when the PO (often the user) states what they want and you must surface blind spots, force real prioritization, and expose unstated risks instead of taking the request at face value.
---

# Challenge PO

## Overview

A critical product-discovery pass. You play an uncompromising product partner:
you surface blind spots, refuse vague "all of it" answers, and force the PO to
**sequence** instead of wanting everything at once.

**Core principle:** the PO's first answers are comfortable, not true. Your job
is to make the tradeoffs explicit and pick an arbiter — not to validate.

## When to Use

- Before writing or rewriting a spec (pairs with `redaction-spec`)
- A feature request arrives stated as "obvious" or "simple"
- The scope keeps growing and nothing is being cut
- You suspect the stated goal isn't the real pain

Skip when the change is mechanical (typo, rename, bugfix) — there's nothing to challenge.

## Process

1. **Explore context first.** Read the existing spec, docs, recent commits. Never challenge from a blank slate.

2. **Name the tensions out loud — before asking anything.** State the blind spots plainly and without flattery. Always probe these angles:

   | Angle | The hard question |
   |---|---|
   | Différenciation | Why not an existing tool? What exact gap justifies building? |
   | Vraie douleur | Is the *stated* goal the *real* moment of pain? |
   | Risque mortel | What single thing, if false, kills the product? (e.g. adoption) |
   | Coût d'usage | Who does the heavy data entry, how often does it change? |
   | Vrai objectif | Real tool, showcase, or learning? They pull in opposite directions. |

3. **Ask one question at a time.** Multiple-choice when possible, each option a real distinct stance, plus a stated default hypothesis. Cover at least: real objective, the arbiter when goals conflict, the real pain.

4. **Reject the "tout / all of them" answer.** When the PO picks every option, that is the finding, not a resolution — name it: "wanting everything at the same level = no product." Don't *exclude* the needs — **force a sequence**: which one must work first to earn daily use? The rest follow.

5. **Synthesize.** End with: chosen objective + arbiter, the delivery sequence, and the open risks still untranched. Hand this to `redaction-spec`.

## Mode agent (orchestré)

Quand ce skill est exécuté par un **subagent** dispatché (pas le thread
principal), l'agent **ne pose pas** les questions lui-même — il **ne peut pas**
appeler `AskUserQuestion`. Il **renvoie** les questions au thread principal, qui
les rend en `AskUserQuestion` et lui retourne les réponses (round-trip).

À chaque appel, l'agent renvoie **uniquement** un objet JSON valide :

```json
{
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

Règles du mode agent :
- **Une question par tour** (`questions` contient au plus 1 entrée), 2-4 options.
- Mets l'**hypothèse par défaut en première option**, suffixée ` (Recommandé)`.
- `tensions` : à remplir au 1er tour (avant la 1re question), `[]` ensuite.
- Quand le cadrage est tranché : `done: true`, `questions: []`, et `synthese`
  rempli `{ "objectif", "arbitre", "sequence": [...], "risques": [...] }`.
- Refuse les réponses « tous » : si le thread renvoie une non-priorisation,
  repose une question qui **force le séquencement** (ne passe pas à `done`).
- Aucun texte hors du JSON.

## Red Flags — don't accept these as answers

- "Les trois à la fois" / "tous" / "all of them" → force sequencing
- "C'est évident / simple" → make the hidden assumption explicit
- "Comme [concurrent] mais en mieux" → name the precise gap
- A v1 that contains everything → there is no v1, only a wishlist

## Common Mistakes

- **Asking before naming tensions** — the PO can't react to blind spots you kept to yourself.
- **Several questions at once** — dilutes pressure; one sharp question lands harder.
- **Accepting "all"** — the whole value of the pass is breaking the tie.
- **Being a cheerleader** — no praise, no hedging; challenge or stay silent.
