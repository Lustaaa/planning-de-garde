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

- **Carte du jour « Qui récupère ce soir » en tête du planning** *(livré s42 — LECTURE seule ; 1ᵉʳ incrément
  du noyau produit)* : le planning affiche **en tête** une carte **« Aujourd'hui »** résumant, pour le **jour
  courant** et l'**enfant sélectionné**, **QUI** récupère l'enfant ce soir (responsable **résolu** surcharge >
  fond > neutre, R19), **OÙ** (le(s) **slot(s) de localisation** du jour, s29) et le **transfert éventuel**
  cédant → recevant (**saisi OU dérivé**, s31, priorité SAISI > DÉRIVÉ). Côté back, c'est une **query PURE
  `CarteDuJourQuery` qui COMPOSE** `GrilleAgendaQuery` — elle **ne réimplémente pas** la résolution, la
  dérivation de transfert ni la projection des slots (source unique = le domaine existant, sous peine de deux
  vérités divergentes) ; **aucun store neuf, aucune mutation, aucune persistance neuve** ; **identique sur les
  deux adaptateurs** (InMemory + Mongo durable). Le rendu **réutilise couleurs/repli de la grille** (aucune
  teinte réinventée) : transfert = **rendu bicolore réutilisé**, jour sans transfert = **unicolore**. **Repli
  fidèle** : aucun responsable résolu = **« personne assignée »** (neutre, sans nom fantôme) ; responsable
  **orphelin** (id stable absent du store) = **repli neutre sans nom ni couleur fantôme** (filtre `Resolvable()`
  s13, miroir R5/R6) ; jour **sans slot** = **sans lieu**, sans erreur. La carte est **STRICTEMENT en lecture**
  (aucun contrôle d'édition, aucune commande émise — l'écriture reste dans les dialogs de pose / la Config) ;
  l'**Invité VOIT** la carte (lecture non gatée). Sa convergence temps réel passe **exclusivement** par la
  **diffusion SignalR de lecture** (s20), **sans rechargement**, par **reprojection client** depuis la fenêtre
  de grille déjà chargée (aucun GET dédié sur push — choix anti-amplification de flake).
  - **Limitation assumée (s42, routée au backlog)** : la carte se **reprojette depuis la fenêtre de grille
    chargée**. Si l'utilisateur **navigue vers une semaine ne contenant pas le jour courant**, la carte
    **disparaît** (elle n'a plus de source dans la fenêtre). Rendre la carte **persistante hors de la vue du jour
    courant** supposerait un GET dédié (arbitrage persistance vs coût/flake) — non tranché.

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
