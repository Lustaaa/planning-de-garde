# Sprint 44 — Déléguer la récupération d'UN jour (imprévu & échange de dernière minute — 1ʳᵉ ÉCRITURE du noyau produit)

> **Goal G2 (tranché PO — délégation, goal 1 du SM · 1ʳᵉ ÉCRITURE du NOYAU PRODUIT)** : on livre la
> **première ACTION D'ÉCRITURE** de la promesse « qui récupère » : un parent qui **ne peut pas récupérer
> un jour donné** en **délègue la récupération à un autre acteur**, pour **CE jour-là uniquement**
> (imprévu ponctuel, non récurrent). L'écriture est portée **directement sur la GRILLE AGENDA** (menu
> clic-case), **seule surface de lecture/action** de la page planning.
>
> **RÉCONCILIATION NARRATIVE (décision PO au gate G3 — À LIRE).** Le noyau produit **ne s'appuie plus**
> sur deux incréments de LECTURE dédiés (carte « Aujourd'hui » s42, panneau « À venir » s43) : ces deux
> briques sont **RETIRÉES ENTIÈREMENT sur décision PO** (composants IHM **et** read models backend
> `CarteDuJourQuery` / `AVenirQuery` + leurs tests — **pas de code mort**). Le PO ne veut que la **grille
> agenda** et l'**action sur la case**. La valeur de s44 devient donc la **1ʳᵉ ÉCRITURE (délégation)
> portée directement sur la grille**, sans surface de lecture intermédiaire.
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
>   d'écriture « affecter une surcharge ponctuelle »** (période d'UN jour, s06). **AUCUN nouveau
>   modèle de résolution** (surcharge > fond > neutre inchangée), **AUCUN store neuf**, **AUCUNE
>   nouvelle dérivation de transfert** (le bicolore sort de s31, non réécrit). **Deux adaptateurs**
>   (InMemory + Mongo durable), écriture prouvée store réel.
> - **@back — cas LIMITE** : un jour **déjà couvert par une surcharge** → **last-write-wins R11**
>   (réaffecte le responsable, **aucun doublon** de période) ; **délégation à soi-même** (versActeur =
>   responsable déjà résolu) → **refus explicite** (no-op inutile, pas d'écriture) ; **jour hors
>   fenêtre chargée** → l'écriture reste valide (une date), l'affichage suit la fenêtre de grille
>   chargée, **sans crash**.
> - **@back — cas ERREUR** : **délégataire inconnu / orphelin** (id stable absent du store) →
>   **refus AVANT écriture**, store **intact**, **aucune écriture partielle**.
> - **@ihm — action « déléguer ce jour » = ENTRÉE DU MENU CLIC-CASE** de la grille agenda (clic sur
>   une case `jour-case` → `menu-actions-case`, à côté d'« Affecter une période » / « Définir un
>   transfert ») → **mini-dialog** de choix de l'acteur recevant ; **refus** (domaine) → dialog
>   **reste ouverte** + **motif** + **saisie conservée** ; **Échap = Annuler** (port
>   `IEcouteurEchapModal` s33) ; **Parent-gated** (l'**Invité ne voit ni le menu ni l'entrée** —
>   aucune commande émissible).
> - **@ihm — temps réel** : après une délégation **via le menu clic-case**, la **GRILLE (case du
>   jour)** reprojette le **nouveau responsable** ET le **transfert dérivé** (rendu **bicolore s31**) ;
>   convergence sur un **2ᵉ écran** via **SignalR** (**0 GET**, reprojection client — garde
>   anti-amplification flake).
> - **@ihm — RETRAIT s42+s43 (décision PO gate G3)** : la page planning ne rend **NI carte
>   « Aujourd'hui » NI panneau « À venir »** ; les read models `CarteDuJourQuery` (s42) et `AVenirQuery`
>   (s43) **et leurs tests** sont **supprimés** (pas de code mort) ; la **grille agenda reste la seule
>   surface de lecture**.
>
> **Ordre d'attaque (backend d'abord, CLAUDE.md)** : @back (use case composant l'écriture ponctuelle,
> cas limite/erreur, deux adaptateurs) → puis @ihm (entrée menu + mini-dialog, gating, refus, convergence
> SignalR) → puis @ihm retrait des deux surfaces de lecture s42/s43 + read models.
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

## Avancement — 7/7

> **Réouverture après gate visuel G3 (2 décisions PO successives).**
> **Décision PO n°1** : la **SURFACE** d'écriture de la délégation devient une **entrée du MENU
> CLIC-CASE de la grille agenda** (`menu-actions-case` ouvert au clic sur une `jour-case`, convention
> Palier 7), **et non plus** un bouton posé sur une carte de lecture. Sc.4-6 refaits (commit 8fa4e65),
> **verts**.
> **Décision PO n°2 (celle-ci)** : les **deux briques de LECTURE** s42 (carte « Aujourd'hui ») et s43
> (panneau « À venir ») **ne sont plus voulues du tout** — **retrait ENTIER** des composants IHM **et**
> des read models backend `CarteDuJourQuery` / `AVenirQuery` + leurs tests (**pas de code mort**). État
> cible : **grille agenda seule** + délégation par le **menu clic-case**. Ce retrait est un **Sc.7 🔴 à
> implémenter** ; le back de délégation (Sc.1-3) et la surface menu clic-case (Sc.4-6) **restent
> valables et verts**.

| # | Scénario | Type | Statut |
|--:|----------|------|:------:|
| 1 | **Déléguer un jour COMPOSE l'écriture surcharge ponctuelle (nominal)** : un jour résolu par le **fond** est délégué à un autre acteur → une **surcharge d'UN jour** est écrite via le **chemin s06 existant** (aucune commande de transfert neuve, aucun store neuf) ; la résolution **surcharge > fond** fait **primer** le délégataire pour ce jour ; le **transfert cédant→recevant** apparaît **AUTO-DÉRIVÉ s31** ; écriture **identique et durable** sur les **deux adaptateurs** (InMemory + Mongo) | back | ✅ |
| 2 | **Cas LIMITE — last-write-wins + délégation à soi-même** : un jour **déjà couvert par une surcharge** → la délégation **réaffecte** le responsable (**last-write-wins R11**, **aucun doublon** de période) ; une **délégation à soi-même** (délégataire = responsable déjà résolu) → **refus explicite**, **aucune écriture** ; un **jour hors fenêtre chargée** reste **écrivable** sans crash | back | ✅ |
| 3 | **Cas ERREUR — délégataire inconnu / orphelin** : déléguer vers un acteur dont l'**id stable est absent du store** (inconnu / supprimé) → **refus AVANT écriture**, store **intact**, **aucune écriture partielle** ; identique sur les deux adaptateurs | back | ✅ |
| 4 | **« Déléguer ce jour » = entrée du MENU CLIC-CASE de la grille** : l'action « déléguer ce jour » est une **entrée de `menu-actions-case`** (clic sur une `jour-case`, à côté d'« Affecter une période » / « Définir un transfert ») ouvrant le **mini-dialog** ; valider émet la commande via le **canal d'écriture** ; **Échap = Annuler** ; **Parent-gated** (l'Invité ne voit ni le menu ni l'entrée) | 🖥️ IHM | ✅ |
| 5 | **Refus domaine → dialog reste ouverte + motif + saisie conservée** : une délégation (ouverte depuis le **menu clic-case**) refusée (soi-même, délégataire inconnu) laisse le **mini-dialog OUVERT**, affiche le **motif**, **conserve la saisie** (acteur choisi) ; **store intact** ; fermeture uniquement sur Annuler/Échap ou succès | 🖥️ IHM | ✅ |
| 6 | **Temps réel — la GRILLE converge, transfert dérivé visible (0 GET)** : après une délégation **via le menu clic-case** sur le 1ᵉʳ écran, la **case du jour de la grille** d'un **2ᵉ écran CONVERGE** (nouveau responsable + **transfert bicolore dérivé s31**) **sans rechargement** ; convergence par **reprojection client** via **SignalR lecture seule** (**0 GET** sur push) | 🖥️ IHM | ✅ |
| 7 | **RETRAIT des surfaces de lecture s42/s43 (décision PO)** : la page planning ne rend **NI carte « Aujourd'hui » NI panneau « À venir »** ; les read models `CarteDuJourQuery` (s42) et `AVenirQuery` (s43) **et leurs tests** sont **supprimés** (pas de code mort) ; la **grille agenda reste la SEULE surface de lecture** ; la délégation par le menu clic-case (Sc.4-6) reste intacte | 🖥️ IHM | ✅ |

> **⚠️ Point de vigilance — RÉUTILISER l'écriture s06 + la dérivation s31, ne RIEN inventer (Sc.1-3,
> décision SM).** `DeleguerRecuperation` est un **use case de composition** : il **appelle le chemin
> d'écriture « affecter une période » (surcharge d'UN jour, s06)** avec le délégataire comme
> responsable. **INTERDIT** de créer une entité/commande « transfert temporaire » neuve, ou de recopier
> la priorité surcharge > fond, ou de réécrire la dérivation de transfert : le **bicolore sort de s31**
> (R24) **par construction** dès que la surcharge fait basculer la responsabilité du jour. Une commande
> de transfert neuve serait **deux vérités divergentes** et **hors scope**.

> **⚠️ Point de vigilance — SURFACE = MENU CLIC-CASE (Palier 7), GRILLE seule surface (Sc.4-6, décisions
> PO au gate G3).** L'écriture s'ouvre **exclusivement** depuis l'**entrée « déléguer ce jour » du
> `menu-actions-case`** (clic sur une `jour-case`), aux côtés des actions Palier 7 existantes.
> L'entrée déclenche une **commande** par le **canal d'écriture** (jamais par la diffusion) ; la
> **convergence** du 2ᵉ écran passe **exclusivement** par la **diffusion SignalR de lecture** (s20)
> **par reprojection client** (**0 GET** sur push). Depuis la décision PO n°2, il n'existe **aucune
> autre surface de lecture** (ni carte, ni panneau) : la **grille agenda est la seule**.

> **⚠️ Point de vigilance — RETRAIT s42/s43 = suppression FRANCHE, pas de code mort (Sc.7, décision PO).**
> Supprimer **les composants IHM** (carte « Aujourd'hui », panneau « À venir ») **ET** les read models
> backend `CarteDuJourQuery` (s42) / `AVenirQuery` (s43) **ET leurs tests**. Ne **PAS** laisser de query,
> de DTO, d'endpoint, de composant ou de test orphelin « au cas où » : le PO tranche que ces surfaces ne
> reviennent pas. La **suite complète doit rester verte** après retrait (les tests qui exerçaient s42/s43
> partent **avec** le code qu'ils couvraient ; aucun test résiduel ne doit référencer une query supprimée).
> La **grille agenda** (`GrilleAgendaQuery`, socle des deux read models retirés) **reste intacte** — elle
> n'était pas dérivée d'elles, ce sont elles qui la composaient.

> **⚠️ Point de vigilance — cohérence JOUR ↔ résolution de fond au make-gherkin (Sc.1).** Le scénario
> nominal exige un jour **résolu par le CYCLE DE FOND** (pour prouver la bascule fond→surcharge et le
> transfert dérivé). **Choisir une date qui est RÉELLEMENT résolue par le fond** (index de cycle mappé
> à un responsable ≠ délégataire) — ne pas poser une date « au hasard » supposée de fond. Ancrer
> l'attendu sur la **règle de résolution** (`semaine ISO % N`), pas sur un index codé en dur.

> **⚠️ Anti-vert-qui-ment — preuve runtime sur profil RÉALISTE (Sc.1-7).** La délégation doit être
> prouvée de bout en bout : un **jour de fond délégué** → **surcharge écrite (Mongo durable)** →
> **délégataire résolu responsable** ET **transfert dérivé bicolore VISIBLE** dans la **case de la
> grille**, **convergé sur un 2ᵉ écran** sans reload ni GET. Une preuve qui n'écrirait pas réellement
> (doublure de port) ou ne montrerait pas le transfert dérivé **surestimerait** la couverture. Preuve
> finale = **round-trip runtime réel (Mongo durable)** + **gate navigateur PO** (dont la page planning
> ne montrant PLUS ni carte ni panneau — Sc.7).

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
Alors l'écriture RÉUSSIT (une date reste écrivable) sans crash ; son AFFICHAGE suit la fenêtre de grille chargée (visible seulement si la grille couvre J)
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

### Sc.4 — « Déléguer ce jour » = entrée du MENU CLIC-CASE de la grille @ihm @vert
```gherkin
Étant donné le planning ouvert (grille agenda), un enfant sélectionné, un utilisateur PARENT
Quand je clique sur une case « jour-case » de la grille agenda
Alors le menu « menu-actions-case » s'ouvre et propose une ENTRÉE « déléguer ce jour », à côté d'« Affecter une période » et « Définir un transfert » (convention Palier 7)
Quand je choisis l'entrée « déléguer ce jour »
Alors un MINI-DIALOG s'ouvre proposant de choisir l'acteur RECEVANT parmi les acteurs éligibles du foyer
Et valider émet la commande de délégation via le CANAL D'ÉCRITURE (requête/réponse), puis la grille se met à jour
Et Échap FERME le dialog sans émettre aucune commande (port IEcouteurEchapModal s33)

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
Et la convergence passe par une REPROJECTION CLIENT depuis la grille rafraîchie — AUCUN GET dédié sur push (anti-amplification flake)
Et la convergence passe EXCLUSIVEMENT par le canal SignalR de LECTURE SEULE (l'écriture, elle, a transité par le canal requête/réponse)
```

### Sc.7 — RETRAIT des surfaces de lecture s42/s43, la grille reste seule surface @ihm @vert
```gherkin
Étant donné la page planning ouverte (grille agenda + délégation par le menu clic-case Sc.4-6)
Quand la page planning est rendue
Alors elle ne rend NI carte « Aujourd'hui » (s42) NI panneau « À venir » (s43) — ces surfaces sont supprimées
Et la GRILLE AGENDA est la SEULE surface de lecture de la page

Étant donné le code du sprint après retrait
Alors le read model backend CarteDuJourQuery (s42) et le composant IHM de la carte « Aujourd'hui » sont SUPPRIMÉS, avec leurs tests (aucun code mort, aucun test orphelin)
Et le read model backend AVenirQuery (s43) et le composant IHM du panneau « À venir » sont SUPPRIMÉS, avec leurs tests (aucun code mort, aucun test orphelin)
Et aucune query, DTO, endpoint ou composant orphelin ne subsiste pour ces deux surfaces

Étant donné le retrait effectué
Quand j'exécute la suite COMPLÈTE (test.ps1, Docker actif)
Alors elle reste VERTE, et la délégation par le menu clic-case (Sc.4-6) demeure inchangée
Et la GrilleAgendaQuery (socle que composaient les read models retirés) reste intacte
```

---

# Retours produit (PO)
