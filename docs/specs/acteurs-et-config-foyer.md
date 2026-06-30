# Acteurs & configuration du foyer

> Sujet **migré** depuis `docs/15-specification.md` (paliers 4/5/8 + règles 1-9) à la migration
> complète des specs. Source de vérité pour le **référentiel des acteurs** (édition, ajout, suppression
> — CRUD complet), sa **persistance Mongo**, le **gating** et l'**impersonation bornée lecture**. Édité
> en diff, jamais réécrit en bloc.

## Contexte

L'**appropriation des acteurs** est **livrée** : noms et couleurs **éditables** depuis un écran de
configuration, la grille (case et légende) suit immédiatement. La **config foyer persistante** est
**acquise** : on peut **ajouter** des acteurs (parent ou « autre » comme la nounou) au-delà du
renommage du seed — l'ajout génère un **identifiant stable neuf** (jamais le libellé) — et la config
foyer **survit au redémarrage** (persistée derrière un adaptateur de droite durable, Mongo, ports
inchangés). **Volatilité de l'édition éteinte ICI, pour la config foyer uniquement** ; le reste du
domaine (slots, périodes, transferts, cycle de fond) reste en mémoire.

Le **CRUD acteurs est complet** (Create + Read + Update + **Delete**) : supprimer un acteur le **retire
du store durable** et **neutralise par repli** ses cases orphelines. Le dernier maillon, une
**impersonation bornée lecture seule**, ferme la **boucle du cycle de vie des acteurs**.

## Objectif & arbitrage

La **persistance de la config foyer** a été tirée **devant l'usage** (exception **bornée**, observable
direct : survie au redémarrage ; premier client du store durable). La **suppression d'acteur** a opéré
sur ce **même store Mongo** (a exercé une persistance acquise, sans cliquet). L'**impersonation bornée**
n'a tiré **aucune persistance neuve** (état session / mémoire). Détail :
[`objectif-et-arbitrage.md`](objectif-et-arbitrage.md).

## Séquence

- **Palier 4 — Édition des acteurs (en mémoire) *(livré)*** : noms + couleurs éditables, grille relue
  immédiatement (durabilité portée par le palier 5).
- **Palier 5 — Config foyer persistante *(livré)*** : **(a)** ajouter un acteur (id stable neuf) ;
  **(b)** persister la config foyer derrière les ports (`IReferentielResponsables` / `IPaletteCouleurs`
  / `IEditeurConfigurationFoyer` inchangés). Survie au redémarrage tenue. Ajout **sans** suppression
  d'abord, **sans** édition du cycle de fond ici.
- **Palier 8 — CRUD complet & impersonation bornée lecture *(livré)*** : **Delete** (épic É2) +
  **impersonation bornée lecture** (épic É10). Suppression → neutralisation par repli des cases
  orphelines + repli de l'incarnation ; **type d'acteur** surfacé lecture seule depuis le seed.

Texte complet : [`sequence-de-livraison.md` § paliers 4/5/8](sequence-de-livraison.md).

## Mécaniques

- **Acteurs (noms + couleurs) éditables, ajoutables, supprimables** depuis l'écran de config ; ajout =
  identifiant stable neuf ; suppression = retrait du store + neutralisation des cases orphelines par
  repli. Config foyer **persistée** (durabilité **bornée** au référentiel des acteurs).
- **Toutes les écritures de l'écran de config** (édition, ajout, édition du cycle de fond, suppression)
  sont **gatées sur l'identité effective** (durcissement complet, plus seulement le bouton supprimer).
- **Impersonation bornée lecture** : session = identité **réelle** (configurateur fixe) vs identité
  **effective** (acteur incarné, ou **repli sur la réelle**) ; bandeau « Vous incarnez X » ; droit
  d'écriture dérivé du type de l'identité effective ; retour à l'identité réelle ; repli auto sur
  suppression concurrente ; **aucune écriture « au nom de »**, état en session / mémoire.

*Texte complet des mécaniques transverses :* [`mecaniques-de-base.md`](mecaniques-de-base.md).

## Règles de gestion (catalogue : `regles-de-gestion.md`)

- **R1 Multi-enfants · R2 Familles recomposées · R3 Toujours deux parents** (composition du foyer).
- **R4 — Acteurs « autres » ajoutables, éditables et supprimables.**
- **R5 — Édition des acteurs (noms + couleurs)** : grille relue immédiatement, store vivant partout,
  type surfacé lecture seule. Distincte de la durabilité (R30).
- **R6 — Ajout & suppression d'acteur, neutralisation par repli (cases ET incarnation), persistance
  bornée** : id stable neuf, persistance Mongo bornée, suppression autorisée & idempotente, repli des
  cases orphelines + de l'incarnation, accusé « Acteur supprimé », pas de réaffectation auto.
- **R7 — Responsabilité de fond en config, exception au calendrier** (cycle déclaré en config ;
  sélecteurs sur identifiant stable).
- **R8 — Trois types d'acteurs & impersonation bornée lecture** (identité réelle vs effective, bornes
  dures, hors-cap = écriture « au nom de »).
- **R9 — Modification réservée aux parents/admin, gating sur l'identité effective** (durcissement
  config livré).

## Risques

- **Impersonation bornée lecture livrée — frontière avec l'auth réelle (palier 16) ; écriture « au nom
  de » = hors-cap** (décision PO explicite, candidat G1).
- **Borne anti-cliquet** : la persistance reste **bornée à la config foyer** ; le reste du domaine en
  queue (paliers « config foyer durable — reste » puis « persistance réelle »).
- **Variantes refus/réaffectation de suppression** = porte G1 au make-gherkin si un vrai trou émerge.
- **Évolutions de surface config non priorisées** : palette/picker, onglets par acteur.

Cf. [`risques-et-questions-ouvertes.md`](risques-et-questions-ouvertes.md).
