# Sprint 45 — Déléguer sur une PLAGE de jours (imprévu de plusieurs jours — extension de s44)

> **Goal G2 (tranché PO — « prends le premier choix » = goal recommandé du SM)** : on **étend la
> délégation s44 du jour UNIQUE à une PLAGE de jours [J1..J2]**. Usage réel : l'imprévu qui dure
> (voyage, hospitalisation, « je pars du 20 au 25, X récupère les enfants »). L'écriture reste
> portée **directement sur la GRILLE AGENDA** via le **menu clic-case** — **seule surface de
> lecture/action** (décision PO s44, tenue).
>
> **PORTE DE CONCEPTION — SURFACE (arbitrée AU CADRAGE, PAS au gate G3).** **AUCUNE surface neuve.**
> On **enrichit le mini-dialog « déléguer ce jour » EXISTANT (s44)** d'un **champ « jusqu'au »**
> (date de fin). L'entrée reste celle du `menu-actions-case` (clic sur une `jour-case`). Emplacement
> retenu = **dans le mini-dialog déjà ouvert**. **Alternatives explicitement ÉCARTÉES** (pour éviter
> tout rework G3) : (a) **sélection de plage par drag sur la grille** → dépend du **palier 9
> calendrier-navigable NON livré**, écartée ; (b) **écran/onglet « déléguer une période » séparé** →
> contredit la décision PO s44 « grille = seule surface », écarté ; (c) **récurrence hebdo « tous les
> mardis » (D2)** → plus lourde (extension du modèle de récurrence), gardée pour un sprint dédié.
> **Défaut du champ « jusqu'au » = jour cliqué (J2 = J1)** → le **comportement s44 (délégation d'UN
> jour) reste STRICTEMENT inchangé**.
>
> **SÉMANTIQUE CADRÉE (anti-tension s31/s44 — À LIRE AVANT DE CODER).** « Déléguer sur une plage »
> est l'**ACTION UTILISATEUR task-orientée** « je ne récupère pas ces jours-là, X le fera » qui
> **EXPOSE l'écriture « surcharge » DÉJÀ EXISTANTE (s06) sur une période MULTI-JOURS** — `s06`
> supporte déjà une période `[début..fin]`, **rien de neuf côté modèle**. Les **transferts bicolores**
> aux frontières restent **AUTO-DÉRIVÉS par s31** (R24, bascule fond→surcharge→fond, priorité SAISI >
> DÉRIVÉ) : **on ne ré-invente NI modèle NI commande de transfert**. La distinction avec « Affecter
> une période » reste la **validation task-orientée de la délégation** (existence du délégataire,
> refus soi-même) et la **surface** (mini-dialog de délégation), **pas la mécanique de fond**.
>
> **Tranche verticale back d'abord** puis IHM :
> - **@back — `DeleguerRecuperation(jour DÉBUT, jour FIN, enfant, versActeur)` COMPOSE l'écriture
>   « affecter une surcharge » sur la période `[début..fin]` (s06)**. **AUCUN nouveau modèle de
>   résolution** (surcharge > fond > neutre inchangée), **AUCUN store neuf**, **AUCUNE nouvelle
>   dérivation de transfert** (les bicolores sortent de s31 aux **deux frontières** — entrée à
>   `début`, sortie après `fin`). **Deux adaptateurs** (InMemory + Mongo durable), écriture prouvée
>   store réel. **Défaut `fin = début`** = parité stricte s44 (une période d'UN jour).
> - **@back — cas LIMITE** : plage **chevauchant une surcharge existante** → **last-write-wins R11**
>   (réaffecte, **aucun doublon** de période) ; **plage vide / `fin < début`** → **refus AVANT
>   écriture**, store intact ; **délégation à soi-même** (versActeur = responsable de fond de tous
>   les jours de la plage) → **refus explicite sans écriture** ; **`fin` hors fenêtre chargée** →
>   l'écriture reste valide, l'affichage suit la fenêtre, **sans crash**.
> - **@back — cas ERREUR** : **délégataire inconnu / orphelin** (id absent du store) → **refus AVANT
>   écriture**, store **intact**, **aucune écriture partielle** (aucun jour de la plage écrit).
> - **@ihm — mini-dialog ENRICHI d'un champ « jusqu'au »** (depuis l'entrée « déléguer ce jour » du
>   `menu-actions-case`, s44) → valider émet la commande par le **canal d'écriture** ; **refus**
>   (domaine) → dialog **reste ouverte** + **motif** + **saisie conservée (plage incluse)** ;
>   **Échap = Annuler** (port `IEcouteurEchapModal` s33) ; **Parent-gated** (l'Invité ne voit ni le
>   menu ni l'entrée).
> - **@ihm — temps réel** : après une délégation de plage, **TOUTES les cases de la plage** de la
>   grille reprojettent le **nouveau responsable** + les **transferts bicolores dérivés s31** aux
>   frontières ; convergence sur un **2ᵉ écran** via **SignalR** (**0 GET**, reprojection client —
>   garde anti-amplification flake).
>
> **Ordre d'attaque (backend d'abord, CLAUDE.md)** : @back (composition surcharge multi-jours, cas
> limite/erreur, deux adaptateurs) → puis @ihm (champ « jusqu'au », gating, refus, convergence
> SignalR de toute la plage).
>
> **HORS scope explicite (périmètre resserré, décision SM)** :
> - **Récurrence / série** (« tous les mardis ») : ce sprint délègue une **plage CONTIGUË `[J1..J2]`**,
>   **pas** un motif hebdomadaire. Le récurrent reste la dette **D2** (backlog).
> - **Sélection de plage par drag sur la grille** : palier 9 (calendrier navigable) **non livré** —
>   la plage se saisit par le **champ « jusqu'au »** du mini-dialog, pas par un geste sur la grille.
> - **Nouveau modèle / nouvelle commande de transfert** : les bicolores restent **DÉRIVÉS s31** (R24).
> - **Annulation / undo dédié** : re-déléguer (last-write-wins R11) ou supprimer via la dialog de
>   suppression EXISTANTE s16 — **aucun** bouton « annuler » neuf (candidat backlog séparé).
> - **Notifications** : aucune cloche « X a délégué » — Palier 11 (backlog).

## Avancement — 1/6

| # | Scénario | Type | Statut |
|--:|----------|------|:------:|
| 1 | **Déléguer une PLAGE COMPOSE l'écriture surcharge multi-jours (nominal)** : une plage `[J1..J2]` de jours résolus par le **fond** est déléguée à un acteur B → une **SURCHARGE de la période `[J1..J2]`** est écrite via le **chemin s06 existant** (aucune commande/modèle/store neuf) ; B **prime** (surcharge > fond) sur **chaque** jour de la plage ; les **transferts bicolores** apparaissent **AUTO-DÉRIVÉS s31** aux **deux frontières** (entrée à J1, sortie après J2) ; **défaut `fin=début`** = parité s44 ; écriture **durable et identique** sur **deux adaptateurs** (InMemory + Mongo) | back | ✅ |
| 2 | **Cas LIMITE — chevauchement / plage vide / soi-même / frontière de fenêtre** : plage chevauchant une surcharge existante → **last-write-wins R11**, **aucun doublon** ; **`fin < début` (plage vide)** → **refus AVANT écriture**, store intact ; **délégation à soi-même** (B = responsable de fond de toute la plage) → **refus explicite** sans écriture ; **`fin` hors fenêtre chargée** → écriture **valide sans crash**, affichage suivant la fenêtre | back | 🔴 |
| 3 | **Cas ERREUR — délégataire inconnu / orphelin** : déléguer une plage `[J1..J2]` vers un acteur dont l'**id est absent du store** → **refus AVANT écriture**, store **intact**, **aucune écriture partielle** (aucun jour de la plage écrit) ; identique sur les deux adaptateurs | back | 🔴 |
| 4 | **Champ « jusqu'au » dans le mini-dialog EXISTANT (menu clic-case)** : depuis l'entrée « déléguer ce jour » du `menu-actions-case`, le mini-dialog porte un **champ « jusqu'au »** (défaut = jour cliqué) ; valider émet la commande `[début..fin]` via le **canal d'écriture** ; **Échap = Annuler** (aucune commande) ; **Parent-gated** (l'Invité ne voit ni menu ni entrée) | 🖥️ IHM | 🔴 |
| 5 | **Refus domaine → dialog reste ouverte + motif + saisie (plage) conservée** : une délégation de plage refusée (`fin < début`, soi-même, délégataire inconnu) laisse le **mini-dialog OUVERT**, affiche le **motif**, **conserve la saisie** (acteur **ET** plage début/fin) ; **store intact** ; fermeture uniquement sur Annuler/Échap ou succès | 🖥️ IHM | 🔴 |
| 6 | **Temps réel — TOUTES les cases de la plage convergent (0 GET)** : après une délégation de plage sur le 1ᵉʳ écran, **chaque case `[J1..J2]`** de la grille d'un **2ᵉ écran CONVERGE** sans rechargement (nouveau responsable + **transferts bicolores dérivés s31** aux frontières) ; convergence par **reprojection client** via **SignalR lecture seule** (**0 GET** sur push) | 🖥️ IHM | 🔴 |

> **⚠️ Porte de conception SURFACE — AUCUNE surface neuve (décision SM au cadrage, anti-rework G3).**
> La plage se saisit **dans le mini-dialog EXISTANT s44** (champ « jusqu'au »), ouvert depuis
> l'entrée « déléguer ce jour » du `menu-actions-case`. **Ne PAS** introduire d'écran/onglet séparé,
> **ni** de geste de drag sur la grille (palier 9 non livré). La **grille agenda reste la seule
> surface de lecture** (décision PO s44). Le **défaut `fin = début`** garantit que la délégation d'UN
> jour (s44) reste **strictement inchangée**.

> **⚠️ Point de vigilance — RÉUTILISER l'écriture s06 (période multi-jours) + la dérivation s31, ne
> RIEN inventer (Sc.1-3, décision SM).** `DeleguerRecuperation` composé sur `[début..fin]` **appelle
> le chemin « affecter une période » (surcharge s06)** avec le délégataire pour responsable. `s06`
> gère **déjà** une période multi-jours — **INTERDIT** de créer une entité/commande « délégation
> multi-jours » neuve, de recopier la priorité surcharge > fond, ou de réécrire la dérivation de
> transfert : les **bicolores sortent de s31** (R24) **par construction** aux **deux frontières** de
> la plage. Une commande de transfert neuve serait **deux vérités divergentes** et **hors scope**.

> **⚠️ Point de vigilance — REFUS = ATOMIQUE sur toute la plage (Sc.2-3).** Un refus (`fin < début`,
> soi-même, délégataire inconnu) doit rejeter **AVANT toute écriture** : **aucun jour** de `[J1..J2]`
> ne doit être écrit partiellement. Le store doit rester **strictement intact** après un refus,
> identique sur les deux adaptateurs.

> **⚠️ Point de vigilance — cohérence DATES ↔ résolution de fond au make-gherkin (Sc.1).** Le nominal
> exige une plage `[J1..J2]` de jours **résolus par le CYCLE DE FOND** (pour prouver la bascule
> fond→surcharge→fond et les transferts dérivés aux frontières). **Choisir des dates réellement
> résolues par le fond** (index de cycle mappé à un responsable ≠ délégataire) — ancrer l'attendu sur
> la **règle de résolution** (`semaine ISO % N`), pas sur un index codé en dur. Vérifier
> `index = ISOWeek(date) % N` **avant d'écrire** toute date + index dans un scénario.

> **⚠️ Anti-vert-qui-ment — preuve runtime sur profil RÉALISTE (Sc.1-6).** La délégation de plage doit
> être prouvée de bout en bout : une **plage de fond déléguée** → **surcharge `[J1..J2]` écrite (Mongo
> durable)** → **délégataire résolu responsable sur CHAQUE jour** ET **transferts bicolores dérivés
> VISIBLES aux frontières** dans les cases de la grille, **convergés sur un 2ᵉ écran** sans reload ni
> GET. Une preuve qui n'écrirait pas réellement (doublure de port), n'écrirait qu'un seul jour, ou ne
> montrerait pas les transferts dérivés **surestimerait** la couverture. Preuve finale = **round-trip
> runtime réel (Mongo durable)** + **gate navigateur PO**.

---

## Scénarios

### Sc.1 — Déléguer une PLAGE COMPOSE l'écriture surcharge multi-jours (nominal) @back @vert
```gherkin
Étant donné un foyer configuré (acteurs s30, cycle de fond, enfant sélectionné)
Et une plage de jours [J1..J2] (au moins 3 jours) TOUS résolus par le CYCLE DE FOND (aucune surcharge existante), responsable A
Et un autre acteur B éligible et présent dans le store, distinct de A
Quand je délègue la récupération de la plage [J1..J2] de l'enfant à l'acteur B (DeleguerRecuperation(J1, J2, enfant, B))
Alors une SEULE SURCHARGE couvrant la période [J1..J2] est écrite via le CHEMIN « affecter une période » EXISTANT (s06), avec B pour responsable
Et AUCUNE commande de délégation neuve, AUCUN store neuf, AUCUN nouveau modèle de résolution n'est introduit
Et la résolution de CHAQUE jour de la plage fait désormais PRIMER B (surcharge > fond), A restant le fond hors plage
Et un TRANSFERT A → B est AUTO-DÉRIVÉ à l'ENTRÉE de la plage (jour J1) et un TRANSFERT B → A à la SORTIE (jour J2+1) — s31, R24, LUS jamais réécrits
Et l'écriture est DURABLE et IDENTIQUE sur les deux adaptateurs (InMemory ET Mongo réel), prouvée par relecture store

Étant donné le même foyer
Quand je délègue une plage réduite à UN jour (fin = début, J1)
Alors le comportement est STRICTEMENT identique à la délégation d'un jour s44 (parité, une période d'UN jour)
```

### Sc.2 — Cas LIMITE : chevauchement / plage vide / soi-même / frontière de fenêtre @back @pending
```gherkin
Étant donné une plage [J1..J2] dont certains jours sont DÉJÀ couverts par une surcharge existante (responsable C)
Quand je délègue la récupération de la plage [J1..J2] à l'acteur B (B ≠ C)
Alors la surcharge de la plage est RÉAFFECTÉE à B (last-write-wins R11) SANS créer de période en doublon
Et chaque jour de [J1..J2] résout désormais B

Étant donné une plage INVALIDE où fin < début (plage vide)
Quand je tente de déléguer cette plage
Alors la délégation est REFUSÉE AVANT toute écriture, AUCUN jour n'est écrit, le store reste intact

Étant donné une plage [J1..J2] dont TOUS les jours ont déjà pour responsable de fond l'acteur A
Quand je délègue cette plage à ce MÊME acteur A (délégation à soi-même)
Alors la délégation est REFUSÉE explicitement, AUCUNE écriture n'est effectuée, le store reste intact

Étant donné une plage [J1..J2] dont la fin J2 est située HORS de la fenêtre de grille chargée
Quand je délègue cette plage à un acteur valide
Alors l'écriture RÉUSSIT (les dates restent écrivables) sans crash ; l'AFFICHAGE suit la fenêtre chargée (seuls les jours couverts par la grille sont rendus)
```

### Sc.3 — Cas ERREUR : délégataire inconnu / orphelin @back @pending
```gherkin
Étant donné une plage [J1..J2] et un enfant sélectionné
Et un identifiant d'acteur délégataire ABSENT du store (inconnu, ou acteur supprimé du foyer)
Quand je tente de déléguer la récupération de la plage [J1..J2] à cet acteur
Alors la délégation est REFUSÉE AVANT toute écriture (validation d'existence du délégataire)
Et le store des périodes reste INTACT — AUCUN jour de la plage n'est écrit, aucune écriture partielle
Et le comportement est IDENTIQUE sur les deux adaptateurs (InMemory ET Mongo réel)
```

### Sc.4 — Champ « jusqu'au » dans le mini-dialog EXISTANT (menu clic-case) @ihm @pending
```gherkin
Étant donné le planning ouvert (grille agenda), un enfant sélectionné, un utilisateur PARENT
Quand je clique sur une case « jour-case » et choisis l'entrée « déléguer ce jour » du menu « menu-actions-case »
Alors le mini-dialog EXISTANT (s44) s'ouvre et porte, en plus du choix de l'acteur recevant, un CHAMP « jusqu'au » (date de fin)
Et le champ « jusqu'au » a pour DÉFAUT le jour cliqué (fin = début) — la délégation d'UN jour s44 reste inchangée
Quand je choisis un acteur recevant et une date de fin postérieure, puis je valide
Alors la commande de délégation [début..fin] est émise via le CANAL D'ÉCRITURE (requête/réponse), puis la grille se met à jour
Et Échap FERME le dialog sans émettre aucune commande (port IEcouteurEchapModal s33)

Étant donné un utilisateur INVITÉ (lecture seule)
Quand il clique sur une case « jour-case »
Alors soit le menu ne s'ouvre pas, soit il ne contient PAS l'entrée « déléguer ce jour » (Parent-gated) et AUCUNE commande n'est émissible
```

### Sc.5 — Refus domaine → dialog reste ouverte + motif + saisie (plage) conservée @ihm @pending
```gherkin
Étant donné le mini-dialog « déléguer ce jour » ouvert (depuis le menu clic-case, Sc.4) avec un acteur choisi et une plage saisie
Quand je valide une délégation que le domaine REFUSE (fin < début, délégation à soi-même, ou délégataire inconnu/orphelin — Sc.2/Sc.3)
Alors le mini-dialog RESTE OUVERT
Et un MOTIF de refus clair est affiché dans le dialog
Et la SAISIE est CONSERVÉE — l'acteur choisi ET la plage (début « jusqu'au » fin) restent renseignés
Et rien n'est appliqué, le store reste intact
Et le dialog ne se ferme que sur Annuler / Échap, ou sur un succès
```

### Sc.6 — Temps réel : TOUTES les cases de la plage convergent (0 GET) @ihm @pending
```gherkin
Étant donné deux écrans planning ouverts sur le même enfant et la même fenêtre de grille chargée
Quand un PARENT délègue la récupération d'une plage [J1..J2] depuis le menu clic-case sur le 1ᵉʳ écran (Sc.4)
Alors CHAQUE case [J1..J2] de la grille agenda du 2ᵉ écran CONVERGE sans rechargement
Et chacune affiche le NOUVEAU responsable (le délégataire) pour son jour
Et les cases de FRONTIÈRE matérialisent les TRANSFERTS cédant → recevant (entrée J1) et recevant → cédant (sortie J2+1) par le rendu BICOLORE dérivé s31 (présentation réutilisée, aucune teinte réinventée)
Et la convergence passe par une REPROJECTION CLIENT depuis la grille rafraîchie — AUCUN GET dédié sur push (anti-amplification flake)
Et la convergence passe EXCLUSIVEMENT par le canal SignalR de LECTURE SEULE (l'écriture, elle, a transité par le canal requête/réponse)
```

---

# Retours produit (PO)
