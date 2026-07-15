# Écriture en contexte — dialogs depuis le planning

> Sujet **migré** depuis `docs/15-specification.md` (palier 7 + règles 14/16/17/24/25/28) à la
> migration complète des specs. Source de vérité pour le **menu clic-case**, les **trois dialogs**
> (slot / période / transfert), leurs **issues** et le **gating**. Épic **refermé**. Édité en diff,
> jamais réécrit en bloc.

## Contexte

L'**écriture en contexte par dialogs** est **livrée et complète** : l'utilisateur **agit là où il
lit**. Un **clic sur une case** ouvre un **menu d'actions** à **trois entrées** (Poser un slot /
Affecter une période / Définir un transfert) ; chaque entrée ouvre une **dialog** pré-remplie sur la
**date de la case**, alimentée par les acteurs et lieux du foyer. **Tous les écrans de saisie dédiés**
(et leurs routes) — slot, période **et transfert** — ont été **retirés** : il n'existe plus qu'**un
seul chemin d'écriture**, en contexte. L'épic « écriture en contexte » est **refermé**.

> **Menu clic-case = point d'entrée mutualisé.** Au-delà des trois dialogs d'écriture, le menu porte
> désormais les usages de **cycle de vie** ajoutés par les paliers suivants, tous gatés sur le même
> déclencheur et alignés sur le même registre d'issues (succès / échec → dialog reste ouverte /
> accusé-à-part non bloquant / temps réel) : **suppression** (s16) et **édition** (s17) de période
> (texte canonique dans [`periodes-et-cycle-de-fond.md`](periodes-et-cycle-de-fond.md), R15/R15bis),
> et **suppression de slot** (s18, ci-dessous).

## Séquence

**Palier 7 — LIVRÉ COMPLET, épic refermé.** Trois dialogs livrées (Poser un slot, Affecter une
période, Définir un transfert), tous écrans/routes dédiés retirés (y compris `definir-transfert`).
Découpé en deux temps (slot + période, **puis** transfert). Réutilise les commandes/handlers
(`PoserSlot`, `AffecterPeriode`, `DefinirTransfert`) et le canal déjà livrés (**pas de handler neuf**) ;
aucune persistance tirée en avant. Texte complet :
[`sequence-de-livraison.md` § palier 7](sequence-de-livraison.md).

## Mécaniques

- **Clic sur une case → menu d'actions → dialog** pré-remplie sur la **date de la case**, alimentée par
  acteurs et lieux du foyer.
- **Source unique des acteurs des dialogs *(précisé s19)*** : `PlanningPartage` est la **seule source**
  qui passe la **liste des acteurs déclarés** (lue depuis le **store vivant**, id stable) **en paramètre**
  aux trois dialogs. Les sélecteurs de responsable ne montrent donc **que des acteurs réels** — jamais un
  libellé fictif « Parent A/B ». Sur **store vide**, les sélecteurs sont vides et invitent par **« Aucun
  acteur, ajoutez-en. »**. Cf. [`acteurs-et-config-foyer.md`](acteurs-et-config-foyer.md) (R5).
- **Issues de la commande** :
  - **succès** → la dialog se ferme, la grille est relue ; pour le transfert, un **accusé « Transfert
    défini »** s'affiche **à part**, non bloquant.
  - **échec** (refus domaine **ou** API injoignable) → la dialog **reste ouverte**, message **dans la
    dialog**, saisie **conservée**, grille **inchangée**.
  - **chevauchement** (pose de slot) → l'écriture **aboutit**, la dialog se ferme, un **avertissement
    non bloquant** s'affiche **à part**.
- **Date pré-remplie = case cliquée** : cet **ancrage de contexte prime** sur le défaut « aujourd'hui »
  de l'horloge (repli horloge non exercé tant que toute saisie passe par une case — R17).
- **Grille en lecture seule** : toute écriture passe par les dialogs ; **annuler** n'émet aucune
  commande. La **sélection de plage** (palier 9) ouvrira l'affectation sur l'intervalle.
- **Gating** : le menu n'apparaît qu'aux Parents (consultation seule des Invités préservée), gating
  **mutualisé** sur le déclencheur quelle que soit l'entrée.
- **Supprimer un slot** *(6ᵉ usage du menu clic-case, livré s18)* → dialog listant les slots
  **couvrant** la date (`SlotsDuJourQuery` : enfant, lieu, bornes horaires, **identifiant stable** ;
  lecture seule, ne déclenche **jamais** la diffusion) → bouton supprimer par ligne → commande `POST
  /api/canal/supprimer-slot` (**idempotente** : id absent / déjà supprimé = **succès no-op** ; clé =
  identifiant stable, jamais un libellé). **Succès** → **accusé « Slot supprimé » à part** (non
  bloquant) + **diffusion temps réel** : le slot disparaît de la case relue **et de chacun des deux
  jours** s'il franchit minuit, la **pile horaire** des autres slots restant **empilée dans l'ordre**.
  **Échec API** → dialog **reste ouverte**, rien appliqué ; **annulation** → aucune commande émise ;
  **gating Invité** (R9). *À la différence de la suppression de période, supprimer un slot **n'ouvre
  AUCUNE règle de résolution** : un slot est une **localisation**, pas une responsabilité — pas de
  repli surcharge > fond > neutre, aucun effet sur teinte / responsable / légende de la case. Réutilise
  le store **durable (Mongo, s15)** ; aucune persistance neuve.*
- **Slot RÉCURRENT hebdomadaire simple** *(livré s29, dans la dialog « Poser un slot » unifiée)* → la
  même dialog porte une case **« Répéter chaque semaine »** : cochée, la pose devient récurrente, le
  **jour de semaine** étant **déduit de la case cliquée** (pas de champ jour séparé), avec la **plage
  horaire début→fin** et le **lieu** (validation lieu/plage **miroir de `PoserSlot`** : lieu connu du
  référentiel, durée strictement positive). Le slot récurrent reste une **LOCALISATION orthogonale à la
  responsabilité** — **enfant + lieu + plage hebdo**, **aucun responsable embarqué** (la responsabilité
  de la case continue de se résoudre par période / cycle de fond). L'**enfant est implicite** (transmis
  via la session, non choisi — *dette P1 : référentiel d'enfants + sélecteur explicite*). Persisté
  **durable (Mongo)** en parité slot ponctuel s15 (aucun seed en mode Mongo), **projeté en occurrences**
  sur **chaque** case du bon jour de semaine dans la fenêtre par `GrilleAgendaQuery`, empilé dans l'ordre
  horaire avec les slots ponctuels ; **suppression idempotente par identifiant stable** (id absent =
  succès no-op). **Hors scope s29** (backlog) : récurrences riches (bi-hebdo, mensuelle, fins de série,
  exceptions), **multi-jours**, configuration en Config du foyer, **édition** d'un récurrent *(le
  **conditionnement à la garde** est désormais livré s31, ci-dessous)*.
- **Slot RÉCURRENT conditionné à la garde** *(livré s31 — D1, révision d'invariant assumée)* → la dialog
  « Poser un slot » porte un **toggle « seulement les jours où l'enfant est chez moi »** (`ConditionneGarde`
  + `PoseurId`). Cochée, l'occurrence du slot n'est **projetée que les jours de récurrence OÙ la
  résolution (surcharge > fond) désigne le parent POSEUR responsable** ; un jour de récurrence où la
  résolution désigne un **autre** responsable → **occurrence masquée**. Le conditionnement **lit la
  résolution sans la modifier** (aucun effet sur teinte / responsable / légende de la case). **Révision
  d'invariant** : le slot, jusque-là **localisation orthogonale à la responsabilité** (s29), **lit
  désormais la responsabilité** — mais **seulement quand le toggle est actif**. Un slot **non conditionné**
  (toggle inactif, **défaut**) garde le **comportement s29 strictement inchangé** : projeté sur **tous**
  ses jours de récurrence, la résolution **n'intervient pas** dans sa projection. **Hors scope s31** :
  multi-jours + config foyer (D2), édition d'un récurrent, **suppression d'un récurrent depuis l'IHM**
  (affordance IHM manquante, re-signalée gate s31 — candidat goal prochain).
- **Déléguer la récupération d'UN jour** *(livré s44 — 1ᵉʳ écriture du noyau produit « qui récupère »)* → une
  **entrée « déléguer ce jour » du menu clic-case** (à côté d'« Affecter une période » / « Définir un transfert »)
  ouvre un **mini-dialog** de choix de l'acteur **recevant** parmi les acteurs éligibles du foyer. C'est l'**action
  utilisateur task-orientée** « je ne récupère pas ce jour-là, X le fera » (imprévu / échange de dernière minute,
  **UN jour ponctuel**, non récurrent). Côté back, `DeleguerRecuperation(jour, enfant, versActeur)` est un **use case
  de COMPOSITION** : il **expose l'écriture « surcharge ponctuelle » EXISTANTE** (une période d'UN jour, s06) avec le
  délégataire comme responsable — **aucun modèle/commande/store de transfert neuf**. La résolution **surcharge > fond**
  fait primer le délégataire pour ce jour, et le **transfert cédant → recevant** en résultant est **AUTO-DÉRIVÉ par
  s31** (R24, bascule fond→surcharge→fond, rendu bicolore réutilisé), **jamais réécrit**. **Deux adaptateurs** (InMemory
  + Mongo durable). **Cas limite** : jour déjà couvert par une surcharge → **last-write-wins R11** (réaffecte, aucun
  doublon) ; **délégation à soi-même** (délégataire = responsable déjà résolu) → **refus explicite sans écriture** ; jour
  **hors fenêtre chargée** → écriture valide (une date), affichage suivant la fenêtre, sans crash. **Cas erreur** :
  **délégataire inconnu / orphelin** (id stable absent du store) → **refus AVANT écriture**, store intact, aucune
  écriture partielle. **IHM** : mini-dialog **Parent-gated** (l'Invité ne voit ni le menu ni l'entrée) ; **refus domaine**
  → dialog **reste ouverte** + **motif** + **saisie conservée**, store intact ; **Échap = Annuler** (port
  `IEcouteurEchapModal` s33) ; valider émet la commande par le **canal d'écriture** (jamais la diffusion). **Temps réel** :
  la **case du jour de la grille** d'un 2ᵉ écran **converge** (nouveau responsable + transfert bicolore dérivé) par
  **reprojection client** via **SignalR lecture seule**, **0 GET** sur push. **Défaire** *(undo dédié livré s46, ci-dessous)* = **reprendre ce jour** (`AnnulerDelegation`) ;
  à défaut, re-déléguer (last-write-wins) ou supprimer la surcharge du jour via la dialog s16. **Hors scope s44**
  (backlog) : délégation **récurrente/série** (D2), **notifications** « X a délégué » (Palier 11).
- **Déléguer la récupération sur une PLAGE `[J1..J2]`** *(livré s45 — EXTENSION de s44 du jour unique à une plage contiguë)* →
  l'imprévu qui **dure** (voyage, hospitalisation). **AUCUNE surface neuve** : le **mini-dialog « déléguer ce jour » de s44 est
  enrichi d'un champ « jusqu'au »** (date de fin) ; son **défaut = le jour cliqué (`fin = début`)** → la délégation d'UN jour
  (s44) reste **strictement inchangée**. Côté back, `DeleguerRecuperation(début, fin, enfant, versActeur)` **COMPOSE l'écriture
  « surcharge » sur la période `[début..fin]` via le chemin EXISTANT s06** (qui gère déjà une période multi-jours) — **aucun
  modèle / commande / store neuf, aucune nouvelle dérivation de transfert**. Le délégataire **prime (surcharge > fond) sur
  CHAQUE jour** de la plage, et les **transferts bicolores** sont **AUTO-DÉRIVÉS par s31** (R24) aux **DEUX frontières** (entrée
  à `J1`, sortie après `J2`), **jamais réécrits**. **Deux adaptateurs** (InMemory + Mongo durable), écriture prouvée store réel.
  **Cas limite** : plage chevauchant une surcharge existante → **last-write-wins R11** (réaffecte, **aucun doublon**) ; **`fin <
  début` (plage vide)** → **refus AVANT écriture**, store intact ; **délégation à soi-même** (délégataire = responsable de fond
  de toute la plage) → **refus explicite sans écriture** ; **`fin` hors fenêtre chargée** → écriture valide **sans crash**,
  affichage suivant la fenêtre. **Cas erreur** : **délégataire inconnu / orphelin** → **refus AVANT écriture**, store intact,
  **aucune écriture partielle** (aucun jour de la plage écrit) — le refus est **ATOMIQUE** sur toute la plage. **IHM** : champ
  « jusqu'au » **Parent-gated**, **Échap = Annuler** (port `IEcouteurEchapModal` s33) ; **refus domaine → dialog reste ouverte**
  + **motif** + **saisie conservée (acteur ET plage début/fin)**, store intact ; valider émet la commande `[début..fin]` par le
  **canal d'écriture**. **Temps réel** : **TOUTES les cases de la plage** de la grille d'un 2ᵉ écran **convergent** (nouveau
  responsable + transferts bicolores dérivés aux frontières) par **reprojection client** via **SignalR lecture seule**, **0 GET**
  sur push. **Hors scope s45** (backlog) : délégation **récurrente/série** « tous les mardis » (D2, distincte d'une plage
  contiguë), **sélection de plage par DRAG sur la grille** (dépend du palier 9 calendrier-navigable non livré — la plage se
  saisit par le champ « jusqu'au »), **notifications** (Palier 11) *(l'**undo dédié** est livré s46, ci-dessous)*.
- **Annuler / reprendre une délégation d'un jour** *(livré s46 — ferme la boucle undo laissée ouverte en s44/s45)* → une
  **entrée « reprendre ce jour » du menu clic-case EXISTANT** (à côté de « déléguer ce jour » s44), affichée
  **CONDITIONNELLEMENT** : **présente uniquement quand la case cliquée porte une délégation active** (surcharge de
  délégation résolue via `JourCase.PorteSurcharge`, surface en **lecture**), **absente** sinon. C'est l'action « finalement
  je peux récupérer ». **AUCUNE surface neuve** (ni bouton undo, ni toast overlay : la grille reste la seule surface, garde
  s44). Côté back, `AnnulerDelegation(jour[, enfant])` est un **use case de COMPOSITION** : il **compose la SUPPRESSION de
  surcharge EXISTANTE (s16)** → la case du jour **retombe sur le FOND (cycle)** et le **transfert bicolore dérivé s31
  DISPARAÎT** (re-dérivé de la résolution après suppression, jamais réécrit) — **aucun modèle / commande / store neuf**.
  **Granularité = UNE OCCURRENCE** : « reprendre ce jour » annule **le seul jour cliqué**, **même s'il appartient à une plage
  `[J1..J2]` déléguée (s45)** — les autres jours de la plage **restent délégués**, les **segments restants sont réécrits par
  le chemin période s06** et les **transferts dérivés s31 aux frontières RECALCULÉS** (le trou créé produit ses propres
  bascules) ; reprendre toute une plage = répéter l'action jour par jour (pas d'action « plage » dédiée). **Deux adaptateurs**
  (InMemory + Mongo durable), écriture prouvée store réel. **Cas limite** : jour **sans délégation active** → **no-op
  idempotent** (succès, store intact, aucune suppression collatérale) ; **ré-annulation idempotente** ; écriture concurrente
  → **last-write-wins R11** sans doublon ni jour tiers touché. **IHM** : entrée **Parent-gated** (l'Invité ne voit ni le menu
  ni l'entrée), **mini-dialog de confirmation** ; **Échap = Annuler** (port `IEcouteurEchapModal` s33, aucune commande émise,
  store intact) ; valider émet `annuler-delegation` par le **canal d'écriture** (jamais la diffusion). **Temps réel** : la
  **case du jour** d'un 2ᵉ écran **converge** (responsable de fond restauré, transfert dérivé disparu) par **reprojection
  client** via **SignalR lecture seule**, **0 GET** sur push. **Hors scope s46** (backlog) : action « reprendre toute la
  plage » d'un coup, **notifications** (Palier 11).

*Texte complet des mécaniques transverses :* [`mecaniques-de-base.md`](mecaniques-de-base.md).
*Résolution de la case (surcharge > fond > neutre) & suppression/édition de période :*
[`periodes-et-cycle-de-fond.md`](periodes-et-cycle-de-fond.md).

## Règles de gestion (catalogue : `regles-de-gestion.md` ; cycle/période : `periodes-et-cycle-de-fond.md`)

- **R14 — Grille en lecture seule, écriture en dialog contextuelle** *(texte canonique dans
  `periodes-et-cycle-de-fond.md`)*.
- **R16 — Pose répétée d'un même slot acceptée avec avertissement** (succès + avertissement à part,
  issue surfacée par le contrat de réponse, jamais recalculée).
- **R17 — Date par défaut = aujourd'hui, ancrage de contexte prioritaire** (repli horloge = code mort
  tant que toute saisie passe par une case ; ne pas supprimer le port d'horloge).
- **R24 — Transfert dérivé par défaut** · **R25 — Transfert modifiable et ponctuel, saisi en contexte**
  (3ᵉ dialog, accusé « Transfert défini », reste InMemory).
- **R28 — Écriture par le canal, échec clair si l'API est injoignable** (issue d'échec des trois
  dialogs + suppression d'acteur).

## Risques

- **Diffusion déclenchée par l'écriture** : acquis pour les dialogs (l'ouverture d'une dialog
  n'interfère pas avec le rafraîchissement de fond).
- **Édition concurrente du même jour sous dialog** (différée, dépend du rétrofit SignalR).
- **Révision de règle en attente** : interdiction/dédoublonnage de slot (révision R16, hors boucle).

Cf. [`risques-et-questions-ouvertes.md`](risques-et-questions-ouvertes.md).
