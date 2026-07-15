# Sprint 44 — Déléguer la récupération d'UN jour (imprévu & échange de dernière minute — 1ʳᵉ ÉCRITURE du noyau produit)

> **Goal G2 (tranché PO — délégation, goal 1 du SM · 1ʳᵉ ÉCRITURE du NOYAU PRODUIT)** : après les
> deux incréments **LECTURE** du noyau produit (carte « Aujourd'hui » s42, panneau « À venir » s43),
> on livre la **première ACTION D'ÉCRITURE** de la promesse « qui récupère » : un parent qui **ne peut
> pas récupérer un jour donné** en **délègue la récupération à un autre acteur**, pour **CE jour-là
> uniquement** (imprévu ponctuel, non récurrent).
>
> **SÉMANTIQUE CADRÉE (anti-tension s31 — À LIRE AVANT DE CODER).** « Déléguer la récupération d'un
> jour » est l'**ACTION UTILISATEUR task-orientée** « je ne récupère pas ce jour-là, X le fera » qui
> **EXPOSE l'écriture « surcharge ponctuelle » DÉJÀ EXISTANTE** (une période de garde d'UN jour,
> s06) — **PAS un mécanisme neuf**. Le **transfert bicolore** qui en résulte reste **AUTO-DÉRIVÉ par
> s31** (bascule fond→surcharge→fond, R24, priorité SAISI > DÉRIVÉ) : **on ne ré-invente NI un modèle
> de transfert, NI une commande de transfert**. La distinction avec s31 est la **surface d'action**
> — une **entrée « déléguer ce jour » du MENU CLIC-CASE de la grille agenda** (Palier 7 : clic sur
> une case `jour-case` → `menu-actions-case` → dialog), pas la mécanique de fond.
>
> **Tranche verticale back d'abord** puis IHM :
> - **@back — use case `DeleguerRecuperation(jour, enfant, versActeur)` qui COMPOSE le chemin
>   d'écriture « affecter une surcharge ponctuelle »** (période d'UN jour, s06) — miroir de la façon
>   dont `CarteDuJourQuery` s42 / `AVenirQuery` s43 **composent** `GrilleAgendaQuery`. **AUCUN nouveau
>   modèle de résolution** (surcharge > fond > neutre inchangée), **AUCUN store neuf**, **AUCUNE
>   nouvelle dérivation de transfert** (le bicolore sort de s31, non réécrit). **Deux adaptateurs**
>   (InMemory + Mongo durable), écriture prouvée store réel.
> - **@back — cas LIMITE** : un jour **déjà couvert par une surcharge** → **last-write-wins R11**
>   (réaffecte le responsable, **aucun doublon** de période) ; **délégation à soi-même** (versActeur =
>   responsable déjà résolu) → **refus explicite** (no-op inutile, pas d'écriture) ; **jour hors
>   fenêtre chargée** → l'écriture reste valide (une date), l'affichage suit la limitation s42/s43,
>   **sans crash**.
> - **@back — cas ERREUR** : **délégataire inconnu / orphelin** (id stable absent du store) →
>   **refus AVANT écriture**, store **intact**, **aucune écriture partielle**.
> - **@ihm — action « déléguer ce jour » = ENTRÉE DU MENU CLIC-CASE** de la grille agenda (clic sur
>   une case `jour-case` → `menu-actions-case`, à côté d'« Affecter une période » / « Définir un
>   transfert ») → **mini-dialog** de choix de l'acteur recevant ; **refus** (domaine) → dialog
>   **reste ouverte** + **motif** + **saisie conservée** ; **Échap = Annuler** (port
>   `IEcouteurEchapModal` s33) ; **Parent-gated** (l'**Invité ne voit ni le menu ni l'entrée** —
>   aucune commande émissible). **La carte « Aujourd'hui » s42 et le panneau « À venir » s43 ne
>   portent PLUS aucun bouton de délégation : ils redeviennent STRICTEMENT lecture seule** (invariant
>   CLAUDE.md « les cartes de lecture n'hébergent pas d'écriture »).
> - **@ihm — temps réel** : après une délégation **via le menu clic-case**, la **GRILLE (case du
>   jour)** reprojette le **nouveau responsable** ET le **transfert dérivé** (rendu **bicolore s31**) ;
>   convergence sur un **2ᵉ écran** via **SignalR** (**0 GET**, reprojection client — garde
>   anti-amplification flake s42/s43).
>
> **Ordre d'attaque (backend d'abord, CLAUDE.md)** : @back (use case composant l'écriture ponctuelle,
> cas limite/erreur, deux adaptateurs) → puis @ihm (bouton + mini-dialog, gating, refus, convergence
> SignalR).
>
> **HORS scope explicite (périmètre resserré, décision SM)** :
> - **Délégation récurrente / série** : ce sprint délègue **UN jour ponctuel** — **pas** de « déléguer
>   tous les mardis », pas de plage. Le récurrent reste la dette D2 (backlog).
> - **Nouveau modèle / nouvelle commande de transfert** : le transfert reste **DÉRIVÉ s31** (R24) —
>   **interdit** d'écrire une entité « transfert temporaire » neuve (cf. point de vigilance).
> - **Annulation / undo dédié de la délégation** : **re-déléguer au responsable d'origine** (ou à un
>   autre) via une **nouvelle délégation** (last-write-wins R11 sur la surcharge du jour) **suffit** —
>   **aucun** bouton « annuler la délégation » spécifique dans ce sprint. *(Défaire complètement =
>   supprimer la surcharge du jour via la dialog de suppression EXISTANTE s16, hors scope de cette
>   surface.)*
> - **Notifications / alertes** : aucune cloche « X a délégué » — c'est le Palier 11 (backlog).

## Avancement — 6/6

> **Réouverture après gate visuel G3 (décision PO).** La **SURFACE** d'écriture de la délégation
> change : elle devient une **entrée du MENU CLIC-CASE de la grille agenda** (`menu-actions-case`
> ouvert au clic sur une `jour-case`, convention Palier 7), **et non plus** un bouton posé sur la
> carte « Aujourd'hui » (s42) ni le panneau « À venir » (s43) — **ces cartes redeviennent
> STRICTEMENT lecture seule**. Le back Sc.1-3 (use case, cas limite/erreur, 2 adaptateurs) **reste
> vert et inchangé** ; le mini-dialog, le gating Parent, Échap=Annuler, le refus domaine et la
> convergence SignalR 0 GET **restent exigés** — seule la surface qui ouvre le dialog change.
> **Sc.4-6 repassent 🔴 (à refaire).**

| # | Scénario | Type | Statut |
|--:|----------|------|:------:|
| 1 | **Déléguer un jour COMPOSE l'écriture surcharge ponctuelle (nominal)** : un jour résolu par le **fond** est délégué à un autre acteur → une **surcharge d'UN jour** est écrite via le **chemin s06 existant** (aucune commande de transfert neuve, aucun store neuf) ; la résolution **surcharge > fond** fait **primer** le délégataire pour ce jour ; le **transfert cédant→recevant** apparaît **AUTO-DÉRIVÉ s31** ; écriture **identique et durable** sur les **deux adaptateurs** (InMemory + Mongo) | back | ✅ |
| 2 | **Cas LIMITE — last-write-wins + délégation à soi-même** : un jour **déjà couvert par une surcharge** → la délégation **réaffecte** le responsable (**last-write-wins R11**, **aucun doublon** de période) ; une **délégation à soi-même** (délégataire = responsable déjà résolu) → **refus explicite**, **aucune écriture** ; un **jour hors fenêtre chargée** reste **écrivable** sans crash | back | ✅ |
| 3 | **Cas ERREUR — délégataire inconnu / orphelin** : déléguer vers un acteur dont l'**id stable est absent du store** (inconnu / supprimé) → **refus AVANT écriture**, store **intact**, **aucune écriture partielle** ; identique sur les deux adaptateurs | back | ✅ |
| 4 | **SWAP DE SURFACE (lot atomique) — « déléguer ce jour » = entrée du MENU CLIC-CASE + retrait des boutons cartes** : l'action « déléguer ce jour » devient une **entrée de `menu-actions-case`** (clic sur une `jour-case`, à côté d'« Affecter une période » / « Définir un transfert ») ouvrant le **mini-dialog** ; valider émet la commande via le **canal d'écriture** ; **Échap = Annuler** ; **Parent-gated** (l'Invité ne voit ni le menu ni l'entrée) ; **ET** la carte « Aujourd'hui » s42 et le panneau « À venir » s43 **ne portent PLUS aucun bouton de délégation** (lecture seule) | 🖥️ IHM | ✅ |
| 5 | **Refus domaine → dialog reste ouverte + motif + saisie conservée** : une délégation (ouverte depuis le **menu clic-case**) refusée (soi-même, délégataire inconnu) laisse le **mini-dialog OUVERT**, affiche le **motif**, **conserve la saisie** (acteur choisi) ; **store intact** ; fermeture uniquement sur Annuler/Échap ou succès | 🖥️ IHM | ✅ |
| 6 | **Temps réel — la GRILLE converge, transfert dérivé visible (0 GET)** : après une délégation **via le menu clic-case** sur le 1ᵉʳ écran, la **case du jour de la grille** d'un **2ᵉ écran CONVERGE** (nouveau responsable + **transfert bicolore dérivé s31**) **sans rechargement** ; convergence par **reprojection client** via **SignalR lecture seule** (**0 GET** sur push, garde anti-flake s42/s43) | 🖥️ IHM | ✅ |

> **⚠️ Point de vigilance — RÉUTILISER l'écriture s06 + la dérivation s31, ne RIEN inventer (Sc.1-3,
> décision SM).** `DeleguerRecuperation` est un **use case de composition** : il **appelle le chemin
> d'écriture « affecter une période » (surcharge d'UN jour, s06)** avec le délégataire comme
> responsable. **INTERDIT** de créer une entité/commande « transfert temporaire » neuve, ou de recopier
> la priorité surcharge > fond, ou de réécrire la dérivation de transfert : le **bicolore sort de s31**
> (R24) **par construction** dès que la surcharge fait basculer la responsabilité du jour. Une commande
> de transfert neuve serait **deux vérités divergentes** et **hors scope**.

> **⚠️ Point de vigilance — SURFACE = MENU CLIC-CASE (Palier 7), cartes en LECTURE SEULE stricte
> (Sc.4-6, décision PO au gate G3).** L'écriture s'ouvre **exclusivement** depuis l'**entrée
> « déléguer ce jour » du `menu-actions-case`** (clic sur une `jour-case`), aux côtés des actions
> Palier 7 existantes — **jamais** depuis la carte « Aujourd'hui » ni le panneau « À venir », qui
> **ne portent AUCUN bouton d'écriture** (invariant CLAUDE.md « les cartes de lecture n'hébergent pas
> d'écriture »). L'entrée déclenche une **commande** par le **canal d'écriture** (jamais par la
> diffusion) ; la **convergence** du 2ᵉ écran passe **exclusivement** par la **diffusion SignalR de
> lecture** (s20) **par reprojection client** (**0 GET** sur push).

> **⚠️ Point de vigilance — SWAP DE SURFACE = LOT ATOMIQUE (Sc.4).** Le retrait des boutons de
> délégation des cartes s42/s43 et le branchement de l'entrée dans `menu-actions-case` sont les
> **deux faces d'un même refactor** (mêmes commandes, mêmes testids d'écriture) : les traiter en **un
> seul lot atomique** (Sc.4), chaque assertion restant vérifiée dedans — **pas** de coexistence
> durable ancien bouton + nouvelle entrée (rempart suite-complète-verte). Les fichiers d'acceptation
> qui pilotaient le bouton des cartes **migrent** vers l'entrée du menu.

> **⚠️ Point de vigilance — cohérence JOUR ↔ résolution de fond au make-gherkin (Sc.1).** Le scénario
> nominal exige un jour **résolu par le CYCLE DE FOND** (pour prouver la bascule fond→surcharge et le
> transfert dérivé). **Choisir une date qui est RÉELLEMENT résolue par le fond** (index de cycle mappé
> à un responsable ≠ délégataire) — ne pas poser une date « au hasard » supposée de fond. Ancrer
> l'attendu sur la **règle de résolution** (`semaine ISO % N`), pas sur un index codé en dur.

> **⚠️ Anti-vert-qui-ment — preuve runtime sur profil RÉALISTE (Sc.1-6).** La délégation doit être
> prouvée de bout en bout : un **jour de fond délégué** → **surcharge écrite (Mongo durable)** →
> **délégataire résolu responsable** ET **transfert dérivé bicolore VISIBLE** dans la carte/le panneau,
> **convergé sur un 2ᵉ écran** sans reload ni GET. Une preuve qui n'écrirait pas réellement (doublure de
> port) ou ne montrerait pas le transfert dérivé **surestimerait** la couverture. Preuve finale =
> **round-trip runtime réel (Mongo durable)** + **gate navigateur PO**.

> **⚠️ Repli neutre — JAMAIS de délégataire orphelin résolu (Sc.3).** Un délégataire dont l'id stable
> est absent du store est **refusé à l'écriture** (Sc.3) ; et si, par ailleurs, une surcharge existante
> pointe un acteur supprimé, la case retombe sur le **repli neutre** sans nom/couleur fantôme
> (`Resolvable()` s13, miroir R5/R6). Une délégation affichant un nom fantôme serait un **vert-qui-ment**.

---

## Scénarios

### Sc.1 — Déléguer un jour COMPOSE l'écriture surcharge ponctuelle (nominal) @back @vert
```gherkin
Étant donné un foyer configuré (acteurs s30, cycle de fond, enfant sélectionné)
Et un jour J dont le responsable est RÉSOLU PAR LE CYCLE DE FOND (aucune surcharge existante ce jour-là), soit l'acteur A
Et un autre acteur B éligible et présent dans le store, distinct de A
Quand je délègue la récupération du jour J de l'enfant à l'acteur B (use case DeleguerRecuperation(J, enfant, B))
Alors une SURCHARGE d'UN SEUL jour (J → J) est écrite via le CHEMIN D'ÉCRITURE « affecter une période » EXISTANT (s06), avec B pour responsable
Et AUCUNE commande de transfert neuve, AUCUN store neuf, AUCUN nouveau modèle de résolution n'est introduit
Et la résolution de la case du jour J fait désormais PRIMER B (surcharge > fond), A restant le fond des autres jours
Et un TRANSFERT cédant A → recevant B est AUTO-DÉRIVÉ pour J (s31, R24, bascule fond→surcharge→fond) — LU, jamais réécrit
Et l'écriture est DURABLE et IDENTIQUE sur les deux adaptateurs (InMemory ET Mongo réel), prouvée par relecture store
```

### Sc.2 — Cas LIMITE : last-write-wins + délégation à soi-même @back @vert
```gherkin
Étant donné un jour J déjà couvert par une SURCHARGE existante (responsable C)
Quand je délègue la récupération du jour J à l'acteur B (B ≠ C)
Alors la surcharge du jour J est RÉAFFECTÉE à B (last-write-wins R11), SANS créer de période en doublon
Et la case du jour J résout désormais B

Étant donné un jour J dont le responsable déjà résolu est l'acteur A
Quand je délègue la récupération du jour J à ce MÊME acteur A (délégation à soi-même)
Alors la délégation est REFUSÉE explicitement (aucun changement utile), AUCUNE écriture n'est effectuée, le store reste intact

Étant donné un jour J situé HORS de la fenêtre de grille chargée
Quand je délègue la récupération du jour J à un acteur valide
Alors l'écriture RÉUSSIT (une date reste écrivable) sans crash ; son AFFICHAGE suit la limitation assumée s42/s43 (visible seulement si la fenêtre couvre J)
```

### Sc.3 — Cas ERREUR : délégataire inconnu / orphelin @back @vert
```gherkin
Étant donné un jour J et un enfant sélectionné
Et un identifiant d'acteur délégataire ABSENT du store (inconnu, ou acteur supprimé du foyer)
Quand je tente de déléguer la récupération du jour J à cet acteur
Alors la délégation est REFUSÉE AVANT toute écriture (validation d'existence du délégataire)
Et le store des périodes reste INTACT (aucune surcharge écrite, aucune écriture partielle)
Et le comportement est IDENTIQUE sur les deux adaptateurs (InMemory ET Mongo réel)
```

### Sc.4 — Swap de surface (lot atomique) : « déléguer ce jour » = entrée du MENU CLIC-CASE + retrait des boutons cartes @ihm @vert
```gherkin
Étant donné le planning ouvert (grille agenda, carte « Aujourd'hui » s42, panneau « À venir » s43), un enfant sélectionné, un utilisateur PARENT
Quand je clique sur une case « jour-case » de la grille agenda
Alors le menu « menu-actions-case » s'ouvre et propose une ENTRÉE « déléguer ce jour », à côté d'« Affecter une période » et « Définir un transfert » (convention Palier 7)
Quand je choisis l'entrée « déléguer ce jour »
Alors un MINI-DIALOG s'ouvre proposant de choisir l'acteur RECEVANT parmi les acteurs éligibles du foyer
Et valider émet la commande de délégation via le CANAL D'ÉCRITURE (requête/réponse), puis la grille se met à jour
Et Échap FERME le dialog sans émettre aucune commande (port IEcouteurEchapModal s33)

Étant donné le même planning affiché
Alors la carte « Aujourd'hui » s42 et le panneau « À venir » s43 ne portent PLUS AUCUN bouton de délégation (lecture seule stricte, invariant CLAUDE.md)

Étant donné un utilisateur INVITÉ (lecture seule)
Quand il clique sur une case « jour-case »
Alors soit le menu « menu-actions-case » ne s'ouvre pas, soit il ne contient PAS l'entrée « déléguer ce jour » (Parent-gated) et AUCUNE commande de délégation n'est émissible
```

### Sc.5 — Refus domaine → dialog reste ouverte + motif + saisie conservée @ihm @vert
```gherkin
Étant donné le mini-dialog « déléguer ce jour » ouvert (depuis l'entrée du menu clic-case, Sc.4) avec un acteur choisi
Quand je valide une délégation que le domaine REFUSE (délégation à soi-même, ou délégataire inconnu/orphelin — Sc.2/Sc.3)
Alors le mini-dialog RESTE OUVERT
Et un MOTIF de refus clair est affiché dans le dialog
Et la SAISIE (acteur choisi) est CONSERVÉE, rien n'est appliqué, le store reste intact
Et le dialog ne se ferme que sur Annuler / Échap, ou sur un succès
```

### Sc.6 — Temps réel : la GRILLE converge, transfert dérivé visible (0 GET) @ihm @vert
```gherkin
Étant donné deux écrans planning ouverts sur le même enfant et la même fenêtre de grille chargée
Quand un PARENT délègue la récupération d'un jour depuis le MENU CLIC-CASE sur le 1ᵉʳ écran (Sc.4)
Alors la CASE DU JOUR de la grille agenda du 2ᵉ écran CONVERGE sans rechargement
Et elle affiche le NOUVEAU responsable (le délégataire) pour ce jour
Et elle matérialise le TRANSFERT cédant → recevant par le rendu BICOLORE dérivé s31 (présentation réutilisée, aucune teinte réinventée)
Et la convergence passe par une REPROJECTION CLIENT depuis la grille rafraîchie — AUCUN GET dédié sur push (garde anti-amplification flake s42/s43)
Et la convergence passe EXCLUSIVEMENT par le canal SignalR de LECTURE SEULE (l'écriture, elle, a transité par le canal requête/réponse)
```

---

# Retours produit (PO)
