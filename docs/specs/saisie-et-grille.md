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
- **Rendu bicolore d'un transfert sur la pastille de date** *(livré s29 — présentation seule ; étendu au
  transfert dérivé s31)* : un jour portant un **transfert** — **saisi (s29)** OU **AUTO-dérivé (s31, D3 :
  succession de périodes ou bascule du cycle de fond, cf. R24)** — rend sa **pastille de date coupée par
  une diagonale** séparant la **couleur de départ** (acteur **cédant**) de la **couleur d'arrivée** (acteur
  **recevant**), toutes deux **résolues sur l'identifiant d'acteur stable** (R19) depuis le référentiel
  acteurs ; un acteur **orphelin** (supprimé du foyer) retombe sur la **teinte neutre** sans nom ni couleur
  fantôme. La **légende** signale le **motif bicolore = transfert** dès qu'un transfert (saisi ou dérivé)
  couvre la fenêtre. Un jour **sans bascule** garde une pastille **unicolore inchangée** (non-régression).
  C'est un enrichissement de **présentation** : le **modèle du transfert saisi est inchangé** (R25), la
  dérivation **n'écrit rien**, et la **résolution de responsabilité de la case** (surcharge > fond > neutre)
  n'est **pas** affectée.

- **Grille agenda = SEULE surface de LECTURE du planning** *(décision PO gate G3 s44 — amendée s47 pour la
  cloche)* : le noyau produit « qui récupère » se **lit directement sur la grille agenda** (socle `GrilleAgendaQuery`)
  — responsable résolu (surcharge > fond > neutre, R19), slot(s) de localisation du jour (s29) et transfert éventuel
  (saisi OU dérivé s31, rendu bicolore ci-dessus). Il n'existe **aucune surface de lecture du planning intermédiaire**.
  *La carte « Aujourd'hui » (s42) et le panneau « À venir » (s43), incréments de lecture précédemment posés en tête du
  planning, ont été **RETIRÉS ENTIÈREMENT en s44** — composants IHM **et** read models `CarteDuJourQuery` / `AVenirQuery`
  — sur décision PO : ils faisaient doublon avec la grille. Ne pas les réintroduire sans arbitrage PO.*
  - **AMENDEMENT s47 — la CLOCHE (barre du haut) est une surface hors-grille ASSUMÉE.** La décision « seule surface »
    vise les surfaces de **re-lecture du planning** redondantes avec la grille. Elle **ne couvre PAS** la **cloche de
    notifications** rouverte en s47 : la cloche **n'est pas une re-lecture du planning**, c'est une **surface de
    NOTIFICATION de CHANGEMENT** (journal append-only, lu/non-lu, propositions d'échange actionnables), **assumée
    hors-grille**, posée **dans la barre d'application du haut** (`MainLayout`), gatée connecté && Parent. Le **noyau de
    lecture** du planning reste la grille ; la cloche s'y ajoute comme **surface transverse**. Texte complet :
    [`notifications-et-echange.md`](notifications-et-echange.md).

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
