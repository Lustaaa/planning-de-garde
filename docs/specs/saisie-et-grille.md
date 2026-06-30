# Saisie visible, grille lisible & thème

> Sujet **migré** depuis `docs/15-specification.md` (paliers 2/3 + règles 18/19/20/21/22) à la
> migration complète des specs. Source de vérité pour la **saisie visible**, la **lisibilité de la
> grille** (couleur + nom + légende) et le **thème**. Édité en diff, jamais réécrit en bloc.

## Contexte

La saisie est **visible** : une saisie posée réapparaît immédiatement dans la grille, **à la bonne
date** et **en couleur du parent responsable** (la couleur se résout sur l'identifiant stable de
l'acteur). La grille est **lisible d'un coup d'œil** : la couleur seule ne porte plus l'information — le
**nom du responsable** est affiché dans la case, **doublé d'une légende**, et l'app porte un **thème en
accord avec son domaine** (garde d'enfants). Ces paliers de saisie visible et de lisibilité & thème
sont **livrés**.

## Séquence

- **Palier 2 — Saisie visible *(livré)*** : une saisie posée **réapparaît immédiatement** à la **bonne
  date** (dates pré-remplies = « aujourd'hui », pas une date figée) **et en couleur du parent
  responsable** (résolue sur l'identifiant stable, pas le libellé).
- **Palier 3 — Lisibilité & thème *(livré)*** : responsabilité **explicite** (nom dans la case +
  légende couleur) ; nom trop long **tronqué** mais lisible au **survol** ; acteur hors set connu
  **affiché et distingué** (gris assumé) sans perdre son nom ; **thème** en accord avec le domaine.

Texte complet : [`sequence-de-livraison.md` § paliers 2-3](sequence-de-livraison.md).

## Mécaniques

- La responsabilité de chaque garde se lit par un **code couleur par personne**, **doublé du nom dans
  la case et d'une légende**. La **légende** agrège les responsables présents dans la fenêtre, **y
  compris les responsables de fond** issus du cycle, dédoublonnés par identifiant. Nom trop long
  **tronqué** (intitulé complet au survol) ; acteur hors set connu **affiché et distingué** (teinte
  neutre assumée).
- La couleur d'un responsable se résout sur un **identifiant d'acteur stable**, jamais sur le libellé :
  sélecteurs de saisie **et** mapping du cycle de fond fournissent ce même identifiant que la palette,
  sinon la case retombe sur la couleur neutre.
- L'app porte un **thème** en accord avec son domaine (garde d'enfants) : ergonomie de surface
  subordonnée à l'usage.

*Texte complet des mécaniques transverses :* [`mecaniques-de-base.md`](mecaniques-de-base.md).

## Règles de gestion (catalogue : `regles-de-gestion.md`)

- **R18 — Responsabilité lisible : couleur par personne + nom + légende** (légende dédoublonnée, fond
  inclus ; troncature + survol ; acteur hors set distingué).
- **R19 — Couleur résolue sur un identifiant d'acteur stable** (sélecteurs + mapping de fond +
  identité effective sur le même id ; index non mappé → neutre sans nom fantôme).
- **R20 — Set de couleurs par défaut, recoloriable** (différencie d'emblée ; recoloriable en config).
- **R21 — Personnalisation des couleurs par utilisateur** (dépend de l'auth, ouverture de l'accès).
- **R22 — Thème en accord avec le domaine** (ergonomie de surface ; thème sombre = évolution additive
  consignée non priorisée ; harmonisation de teinte = même registre).

## Risques

- **Légende ≠ bug (non-bug, harmonisation de teinte)** : pastille saturée vs fond pâle = écart de
  présentation, pas de défaut de résolution.
- **Thème sombre + bascule** et **sélecteur de couleur (palette / picker)** = évolutions de surface non
  priorisées seules.

Cf. [`risques-et-questions-ouvertes.md`](risques-et-questions-ouvertes.md).
