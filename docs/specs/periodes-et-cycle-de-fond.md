# Périodes de garde & cycle de fond

> Sujet **découpé** depuis `docs/15-specification.md` (règles 11/12/14/15) à la clôture s16.
> Source de vérité pour la **résolution du responsable d'une case** (fond ↔ surcharge) et le
> **cycle de vie d'une période** (affecter / supprimer). Édité en diff, jamais réécrit en bloc.

## Contexte

Le planning se lit case par case. Chaque case résout **un seul responsable** par **priorité
descendante** : une **surcharge** (période explicitement saisie) prime sur le **fond** (cycle
récurrent), qui prime sur le **neutre** (rien à afficher → teinte neutre, sans nom). La grille
est en **lecture seule** ; toute écriture (affecter, supprimer) passe par une **dialog ouverte
depuis une case**. Le store des périodes et du cycle est **durable (Mongo, s15)**.

## Objectif & arbitrage

Donner un cycle récurrent qui couvre le quotidien **sans saisie**, surchargeable au cas par cas,
et permettre de **défaire** une surcharge depuis l'IHM. Arbitrages actés :

- **Ancrage ISO sans ancre** : l'index de semaine = parité ISO (`semaine ISO % N`). Choisir un
  début/ancre explicite **rouvre cette décision** → palier « cycle de fond riche » (tranché à
  son make-gherkin, pas avant).
- **Suppression idempotente** : supprimer une période absente / déjà supprimée = **no-op qui
  réussit** (jamais un refus). Clé = **identifiant stable**, jamais un libellé.
- **Édition de période** (re-borner / réaffecter) — **livrée s17** (R15bis). Clé =
  **identifiant stable** ; invariant **fin > début** ; **rejet sur état périmé** (concurrence,
  modèle de l'agrégat période — *pas* le last-write-wins du mapping de fond, R11).

## Séquence (résolution d'une case)

> **Résolution SCOPÉE PAR ENFANT — STRICTE *(dé-risqué de bout en bout s53, R1)*.** Toute résolution
> se fait **pour un enfant donné** : `GrilleAgendaQuery.Projeter(ancre, vue, enfantId)` ne restitue
> **QUE** les périodes, surcharges, transferts (saisis ET dérivés), slots et le cycle de fond de
> **CET** enfant — **aucun repli global / bucket partagé `''`**. Le **cycle de fond est PAR enfant** :
> `CycleCourant(enfant)` d'un enfant **non-null** lit **UNIQUEMENT son cycle** ; **sans cycle propre →
> NEUTRE** (repli du point 3), **jamais** le cycle d'un autre enfant ni un cycle legacy partagé `''`.
> Les données legacy de cycle `EnfantId=''`/`undefined` (pré-scoping) sont désormais **INERTES** —
> jamais lues pour un enfant précis (l'app passe toujours un enfant). Corollaire : **pas de
> last-write-wins ENTRE enfants** — deux enfants, même jour = **deux surcharges qui coexistent** (le
> LWW R11 ne joue que par `(enfant, jour)`). Le chemin legacy `enfantId = null` (lit `''`) reste pour
> les tests mono-enfant explicites.

1. Une **surcharge** couvre la date **pour cet enfant** → la case affiche son responsable et sa couleur.
2. Sinon, le **cycle de fond de cet enfant** résout l'index (`semaine ISO % N`) → responsable mappé sur
   cet index, s'il existe.
3. Sinon (index non mappé, acteur du fond supprimé, **ou enfant sans cycle propre**) → **neutre** :
   teinte neutre, **aucun nom** (pas de nom fantôme).

Supprimer une surcharge fait **re-jouer cette séquence** : la case retombe sur le fond si le
cycle le résout, sinon sur le neutre. **Re-borner** une surcharge re-joue la séquence sur **les
deux portions** : la portion **libérée** retombe sur le fond (ou le neutre, sans nom fantôme),
la portion **encore couverte** affiche le responsable (ré)affecté.

## Mécaniques

- **Affecter une période** — dialog « Affecter une période » (palier 7) ou **sélection de plage
  de cases** (palier 9) ; écrit par le canal requête/réponse, réapparaît dans la grille.
- **Lister les périodes d'une date** — lecture (`PeriodesDuJourQuery`) renvoyant les périodes
  **couvrant** la date avec **identifiant stable, bornes, responsable** ; alimente la dialog de
  suppression ; ne déclenche **jamais** la diffusion.
- **Supprimer une période** — 4ᵉ usage du **menu clic-case** → dialog listant les périodes de la
  date → bouton supprimer par ligne → commande `POST /api/canal/supprimer-periode` (idempotente) ;
  sur succès, **accusé « Période supprimée » à part** (non bloquant) et **diffusion temps réel**
  (case + légende re-résolues sans rechargement). Échec API → la dialog **reste ouverte**, message
  clair, **rien appliqué** ; annulation → **aucune commande émise**. **Gating Invité** : entrée
  absente, aucune commande émissible.
- **Éditer une période** — 5ᵉ usage du **menu clic-case** : un bouton « Éditer » par ligne de la
  dialog liste ouvre un **formulaire pré-rempli** (bornes + responsable) → re-borner et/ou
  réaffecter → commande `POST /api/canal/editer-periode` (clé = identifiant stable). Sur succès,
  **accusé « Période modifiée » à part** (non bloquant) et **diffusion temps réel** (case + légende
  re-résolues sans rechargement). **Rejet** si bornes invalides (fin ≤ début), responsable manquant,
  **ou état périmé** (concurrence) → message clair dans la dialog, **rien appliqué**. Échec API → la
  dialog **reste ouverte**, rien appliqué ; annulation → **aucune commande émise**. **Gating
  Invité** : aucun bouton « Éditer », aucune commande émissible.
- **Transfert AUTO-dérivé d'une bascule de responsabilité** *(livré s31 — D3, R24)* → sur un jour où
  le responsable **change d'un jour à l'autre**, un transfert (cédant = responsable de la veille,
  recevant = responsable du jour) est **dérivé automatiquement**, **sans saisie**, et rendu en
  **pastille bicolore** comme un transfert saisi (présentation s29). La dérivation lit **deux chemins
  SÉPARÉS**, tous deux ancrés sur la résolution surcharge > fond > neutre : **(1) chemin
  « période-existence »** — une **succession de périodes saisies** (fin période A le jour J + début
  période B, **même enfant**, le jour J+1) dérive le transfert le jour de relève ; **(2) chemin
  « cycle-résolu »** — une **bascule du cycle de fond** (`ResoudreResponsable(J-1) ≠
  ResoudreResponsable(J)`) dérive le transfert le jour de bascule **même si aucune période ne trace
  la succession** (c'est le cas nominal du planning réel, ajouté au rework s31). **Priorité SAISI >
  DÉRIVÉ** : un transfert **saisi** le même jour prime et est **seul retenu** (aucun doublon dérivé).
  **Cas limites** : fin de garde **sans successeur** → **aucune** dérivation (retombée **neutre**) ;
  **bord de fenêtre** (J+1 hors de la fenêtre chargée) → **pas** de dérivation fantôme sur données non
  chargées ; **acteur orphelin (R6)** (cédant ou recevant supprimé) → le côté orphelin retombe sur le
  **neutre** (sans nom ni couleur fantôme). La dérivation est de la **présentation dérivée** : elle
  **n'écrit rien** (pas de transfert persisté), la résolution de responsabilité de la case reste
  **inchangée**.

## Règles de gestion

> Numérotation conservée depuis le monolithe v15 pour la traçabilité.

- **R11 — Cycle de fond récurrent, éditable.** Cycle de **N semaines** (N ≥ 1) ; `index =
  semaine ISO du jour % N`, chaque index mappé sur un responsable de fond résolu sur
  l'**identifiant stable** (jamais le libellé ; index non mappé → neutre). Définissable/éditable
  depuis la config foyer (nombre de semaines + responsable par index, alimenté par les acteurs
  persistés), non figé dans le code. **Zéro semaine refusé** (« le cycle doit compter au moins
  une semaine »), cycle précédent inchangé. Ré-édition → grille à jour **sans rechargement** ;
  édition concurrente → **dernière écriture gagne**. Une dialog d'écriture ouverte **n'interfère
  pas** avec le rafraîchissement de fond. *Suppression d'un acteur mappé → index non mappé →
  neutre, sans nom fantôme (R6). Ancre/début explicite, frontière de jour, plages, sur-cycles,
  WE-only = palier « cycle de fond riche » (rouvre l'ancrage ISO).* **Cycle PAR ENFANT *(s53)*** :
  `DefinirCycle` écrit le cycle de **l'enfant courant** (Option A, hérité du sélecteur) ; un enfant
  **non-null** ne lit **QUE** son cycle (`CycleCourant(enfant)`), **sans cycle propre → NEUTRE**
  (jamais le cycle d'un autre ni le legacy partagé `''`, désormais inerte). L'onglet Cycle de la config
  porte un **sélecteur d'enfant** (familles recomposées : chaque enfant a son cycle).

- **Chemins d'écriture SCOPÉS PAR ENFANT *(dé-risqué de bout en bout s53, R1, Option A)*.** **TOUS** les
  chemins d'écriture portent et propagent l'`EnfantId` **hérité de l'enfant courant du sélecteur**
  (s30), **affiché en LECTURE SEULE** dans les dialogs (« Pour : X (sélection courante) »), **jamais un
  champ de choix** : affecter une période (`PeriodeSnapshot.EnfantId`), **transfert SAISI**
  (`Transfert.EnfantId`, s29 — était dé-scopé, corrigé s53), **cycle de fond** (`DefinirCycle`), **slots
  « où »**, **reprise / annulation de délégation** (`AnnulerDelegation` — filtre + segments réécrits
  scopés). Une écriture ciblée enfant A **ne touche jamais** la résolution ni les cases de l'enfant B.
  La **cloche et le journal de changements restent TRANSVERSES par design** (P3 : ils signalent QU'un
  changement a eu lieu, tous enfants) ; le **digest s50 est FILTRÉ** par l'enfant sélectionné (il LIT le
  planning d'un enfant). Isolation prouvée **store réel** sur **deux adaptateurs InMemory + Mongo durable**.
- **R12 — Exception ponctuelle prime sur le fond.** Une **période saisie prime** sur le fond
  (surcharge > fond > neutre) ; le cycle **reprend ensuite** autour de la surcharge. *Une
  surcharge **orpheline** (acteur supprimé, R6) cesse de primer → case retombe sur fond ou
  neutre (R15).*
- **R14 — Grille en lecture seule, écriture en dialog contextuelle.** La grille consomme slots,
  périodes et **fond résolu** déjà enregistrés et les rend **sans jamais écrire**. Toute écriture
  passe par une **dialog ouverte depuis une case** (seul chemin) ; **annuler** n'émet **aucune
  commande**. La **sélection de plage** pour affecter une période est une capacité du palier 9
  (livrée s15).
- **R15 — Suppression de période *(livrée s16)*.** Un **Parent / Admin** supprime une période
  depuis une **dialog contextuelle** (4ᵉ usage du menu clic-case → liste des périodes couvrant la
  date → supprimer). Sous la période supprimée, le **fond reprend** (case → responsable de fond,
  ou neutre si l'index n'est pas mappé), **sans nom fantôme**. La suppression est **idempotente**
  (absente / déjà supprimée = succès no-op), opère sur le **store Mongo durable** (disparaît du
  store relu **et après redémarrage**), porte un **accusé « Période supprimée » à part** (R16,
  registre avertissement-à-part), **propage en temps réel** (case + légende, sans rechargement),
  est **gatée Invité** (R9) et **robuste à l'échec** (API injoignable → dialog ouverte, rien
  appliqué, aucune mise en file ni rejeu). *Le même repli s'applique à une période **orpheline**
  (acteur supprimé, R6).* **Édition** (re-borner / réaffecter) = R15bis.
- **R15bis — Édition de période *(livrée s17)*.** Un **Parent / Admin** édite une période depuis le
  **formulaire pré-rempli** ouvert par le 5ᵉ usage du menu clic-case (bouton « Éditer » par ligne) :
  **re-borner** (début et/ou fin) et/ou **réaffecter** le responsable d'une période **existante**,
  clé = **identifiant stable** (jamais un libellé). À l'enregistrement, le **store Mongo durable**
  est mis à jour (reflété dans le store relu **et après redémarrage**) et les cases concernées
  **re-résolvent** (R12) : la portion **libérée** retombe sur le **fond** (ou le **neutre**, sans nom
  fantôme, R6/R15), la portion **couverte** affiche le **nouveau** responsable. Invariant **fin >
  début** (fin ≤ début refusée, période inchangée). **Concurrence : rejet sur état périmé** —
  l'édition se fonde sur l'**état (version) attendu** de l'agrégat période (modèle Sc.10 s01) ; une
  édition sur état périmé est **rejetée, rien appliqué** (*modèle de l'agrégat période, distinct du
  last-write-wins de R11 qui ne vise que le mapping du cycle de fond*). Porte un **accusé « Période
  modifiée » à part** (R16), **propage en temps réel** (case + légende, sans rechargement), est
  **gatée Invité** (R9) et **robuste à l'échec** (API injoignable → dialog ouverte, rien appliqué) ;
  annulation → **aucune commande émise**.

## Risques

- **Édition concurrente du MÊME jour** sous dialog ouverte (dernière-écriture-gagne à démontrer
  sous dialog) — séquencée derrière la stabilisation temps-réel SignalR (P2/P3) ; aucune règle
  neuve.
- **Cohérence date ↔ index ISO** dans les exemples Gherkin : tout scénario nommant une date ET
  un index/parité doit vérifier `index = ISOWeek(date) % N` (friction s16, Sc.3 — cf. journal
  méthode).
- **Cycle de fond riche** (ancre, frontière, plages, sur-cycles) réclamé par l'usage (gate s10) —
  sujet plein qui rouvre l'ancrage ISO ; séquencé.
