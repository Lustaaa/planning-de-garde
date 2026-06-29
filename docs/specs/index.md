# Spec vivante — index navigable

La spec produit est **éclatée par sujet fonctionnel** et **éditée en diff** (le `scrum-master`
ne touche que le sujet concerné à chaque `/cloture`, jamais une réécriture intégrale — c'est ce
qui faisait enfler la spec ×10 de `01` à `15`).

## Sujets

> Migration en cours. Tant qu'un sujet n'est pas carvé ici, la **source de vérité reste**
> [`docs/15-specification.md`](../15-specification.md) (dernière spec monolithique figée).
> Chaque sprint qui touche un sujet en extrait la section vers un fichier dédié ci-dessous.

| Sujet | Fichier | État |
|---|---|---|
| Périodes de garde & cycle de fond (résolution surcharge > fond > neutre, affecter/supprimer) | [`periodes-et-cycle-de-fond.md`](periodes-et-cycle-de-fond.md) | ✅ carvé s16 |
| _(reste à découper depuis `15-specification.md` au fil des sprints)_ | — | ⏳ |

## Convention

- Un fichier par sujet : `docs/specs/<slug-sujet>.md`, format maison (Contexte / Objectif &
  arbitrage / Séquence / Mécaniques / Règles de gestion / Risques), **serré et scannable**.
- Ajouter chaque nouveau sujet à la table ci-dessus (titre + lien).
- Les versions monolithiques `docs/NN-specification.md` restent **figées en historique**.
