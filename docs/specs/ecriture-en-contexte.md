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
  exceptions), **multi-jours**, configuration en Config du foyer, **édition** d'un récurrent, et le
  **conditionnement à la garde** (couplerait l'occurrence à la responsabilité = **révision d'invariant**).

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
