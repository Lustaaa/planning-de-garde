---
name: redaction-spec
description: Use when producing or updating a functional product specification — after the idea has been challenged and decisions are made, when you need to capture business rules in a consistent, scannable format (Contexte / Objectif & arbitrage / Séquence / Mécaniques / Règles de gestion / Risques).
---

# Rédaction spec

## Overview

Produce a functional spec that is short on prose and rich on **business rules**.
The output has a fixed shape — fill the slots, don't improvise structure.

**Core principle:** functional only. No technical choices. Each rule is a
business behavior, named and one line long.

## When to Use

- After a `challenge-po` pass, to write down the decisions
- Updating an existing spec when scope or priorities change
- Any time a spec needs the house format below

## The Output Contract

Write the spec with these sections **in this order**. Omit a section only if it
genuinely has no content.

1. `# <Titre> — <sous-titre court>`
2. `## Contexte` — 2-3 lines max. What the product is, for whom. No history.
3. `## Objectif & arbitrage` — the goals, and **the arbiter that decides when they conflict** (as a blockquote). Skip if there's a single uncontested goal.
4. `## Séquence de livraison` — numbered phases, each with a one-line justification. Use when needs must be sequenced rather than shipped in a block.
5. `## Mécaniques de base` — bulleted structural invariants (durations, limits, core entities). The fixed facts everything else builds on.
6. `## Règles de gestion` — the heart. See format below.
7. `## Risques & questions ouvertes` — bulleted; name what's unresolved.

### Règles de gestion — format

- Group rules under `###` thematic categories.
- **Number rules continuously across categories** (1, 2, 3… not restarting per category).
- Each rule: `N. **Nom court** — description fonctionnelle en une phrase`.
- Functional only — a rule describes a behavior, never an implementation.
- Embed a short clarification in parentheses when it removes ambiguity; keep it on the rule line.

Example:

```markdown
### Foyer & enfants

1. **Multi-enfants** — Un foyer peut compter plusieurs enfants, chacun avec sa propre organisation

### Rôles & accès

2. **Deux rôles** — Un Parent gère tout ; un Invité est en consultation seule
```

## After Writing

- Propagate consistency: if a README or other docs reference the spec, update their summary/roadmap to match the new sequence and link to the canonical file.
- Keep one **canonical** spec location; mark superseded drafts as inspiration with a pointer, don't leave two sources of truth.

## Common Mistakes

- **Technical leakage** — "stocké en base", "via une API" → not a business rule. Cut it.
- **Restarting numbering per category** — breaks cross-referencing. Keep it continuous.
- **Long contexte** — if Contexte runs past 3 lines, it's eating the spec. Trim.
- **Vague rules** — a rule without a clear subject and behavior isn't a rule. Name it precisely.
- **Examples as separate prose** — keep clarification inline on the rule, not in a detached paragraph.
