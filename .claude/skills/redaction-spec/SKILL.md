---
name: redaction-spec
description: À utiliser pour produire ou mettre à jour une spécification produit fonctionnelle — après que l'idée a été challengée et les décisions tranchées, quand il faut capturer les règles de gestion dans un format cohérent et scannable (Contexte / Objectif & arbitrage / Séquence / Mécaniques / Règles de gestion / Risques).
---

# Rédaction spec

## Vue d'ensemble

Produire une spec fonctionnelle pauvre en prose et riche en **règles de
gestion**. La sortie a une forme fixe — remplis les cases, n'improvise pas la
structure.

**Principe central :** fonctionnel uniquement. Aucun choix technique. Chaque
règle est un comportement métier, nommé et tenant en une ligne.

## Quand l'utiliser

- Après une passe `challenge-po`, pour écrire les décisions
- Mise à jour d'une spec existante quand le périmètre ou les priorités changent
- Chaque fois qu'une spec a besoin du format maison ci-dessous

## Le contrat de sortie

Écris la spec avec ces sections **dans cet ordre**. N'omets une section que si
elle n'a réellement aucun contenu.

1. `# <Titre> — <sous-titre court>`
2. `## Contexte` — 2-3 lignes max. Ce qu'est le produit, pour qui. Pas d'historique.
3. `## Objectif & arbitrage` — les objectifs, et **l'arbitre qui tranche quand ils s'opposent** (en blockquote). À sauter s'il y a un seul objectif incontesté.
4. `## Séquence de livraison` — phases numérotées, chacune avec une justification d'une ligne. À utiliser quand les besoins doivent être séquencés plutôt que livrés en bloc.
5. `## Mécaniques de base` — invariants structurels en puces (durées, limites, entités cœur). Les faits fixes sur lesquels tout le reste s'appuie.
6. `## Règles de gestion` — le cœur. Voir le format ci-dessous.
7. `## Risques & questions ouvertes` — en puces ; nomme ce qui n'est pas résolu.

### Règles de gestion — format

- Regroupe les règles sous des catégories thématiques `###`.
- **Numérote les règles en continu à travers les catégories** (1, 2, 3… sans recommencer à chaque catégorie).
- Chaque règle : `N. **Nom court** — description fonctionnelle en une phrase`.
- Fonctionnel uniquement — une règle décrit un comportement, jamais une implémentation.
- Glisse une courte précision entre parenthèses quand elle lève une ambiguïté ; garde-la sur la ligne de la règle.

Exemple :

```markdown
### Foyer & enfants

1. **Multi-enfants** — Un foyer peut compter plusieurs enfants, chacun avec sa propre organisation

### Rôles & accès

2. **Deux rôles** — Un Parent gère tout ; un Invité est en consultation seule
```

## Après l'écriture

- Propage la cohérence : si un README ou d'autres docs référencent la spec, mets à jour leur résumé/roadmap pour coller à la nouvelle séquence et lie le fichier canonique.
- Garde une seule localisation **canonique** de la spec ; marque les brouillons remplacés comme inspiration avec un pointeur, ne laisse pas deux sources de vérité.

## Mode agent (orchestré)

Quand ce skill est exécuté par un **subagent**, il reçoit dans son prompt : le
chemin de la spec à écrire/mettre à jour et les décisions tranchées (objectif,
arbitre, séquence, mécaniques, règles, risques). Il **écrit le fichier**
directement, puis renvoie au thread principal **uniquement** :

```json
{ "path": "docs/init/01-specification.md", "sections": 6, "regles": 12, "notes": "…" }
```

Restreins le périmètre d'écriture au chemin fourni — n'écris nulle part ailleurs.

## Erreurs fréquentes

- **Fuite technique** — « stocké en base », « via une API » → ce n'est pas une règle de gestion. Coupe.
- **Recommencer la numérotation par catégorie** — casse les références croisées. Garde-la continue.
- **Contexte trop long** — si le Contexte dépasse 3 lignes, il mange la spec. Élague.
- **Règles vagues** — une règle sans sujet ni comportement clair n'est pas une règle. Nomme-la précisément.
- **Exemples en prose détachée** — garde la précision en ligne sur la règle, pas dans un paragraphe séparé.
