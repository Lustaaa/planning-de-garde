# Sprint 53 — Multi-enfants exercé de BOUT EN BOUT (R1)

> **Goal (G2 tranché PO)** : de-risquer l'invariant fondateur **R1 « N enfants ≥1 »**, jamais
> prouvé de bout en bout. Toute la série récente (délégation s44/s45, échange s47/s52, imprévu
> s48, digest s50) est en réalité **MONO-ENFANT**. Ce sprint peuple le store de **≥2 enfants**
> (Léa + un 2e) et prouve l'**ISOLATION STRICTE** : une écriture ciblée enfant A ne touche
> **jamais** la résolution ni les cases de l'enfant B. Débloque l'échange/délégation
> multi-enfants borné hors s52.

## Avancement — 16/16

> **Gate G3 (1er passage) ÉCHOUÉ** : l'isolation n'était qu'en LECTURE ; le chemin d'ÉCRITURE
> « Affecter une période » (s06) n'était pas scopé enfant → période au bucket partagé `''`, visible
> pour TOUS les enfants. **Correctif** (scope enfant à l'écriture Option A + filtrage STRICT
> sans repli `''`) + **Sc.10/Sc.11**.
>
> **Gate G3 (2e passage) ÉCHOUÉ** (navigateur 5292) : « le transfert de Mia apparaît toujours sur le
> planning de Charlie ». Les **transferts SAISIS** (s29) n'étaient pas scopés (modèle sans `EnfantId`)
> — les DÉRIVÉS l'étaient déjà (issus des périodes filtrées). **Correctif** : `EnfantId` de bout en
> bout sur le transfert saisi (modèle/command/DTO/endpoint/dialog hérité de l'enfant courant) +
> filtrage STRICT dans la projection + **Sc.12 @back / Sc.13 @ihm**.
>
> **Gate G3 (3e passage) ÉCHOUÉ** (navigateur 5292) : « Éditer le cycle de fond ne prend pas en compte
> l'enfant ». L'**ÉCRITURE du cycle** (`DefinirCycle`) n'était pas scopée (la LECTURE l'était). **AUDIT
> EXHAUSTIF** de tous les chemins d'écriture (voir « Audit » ci-dessous) → 4 fuites restantes corrigées :
> **cycle de fond** (write), **slots « où »** (read filter), **reprise/annuler-délégation** (scope +
> segments), **imprévu** (lecture scopée). Config : l'onglet Cycle porte un **sélecteur d'enfant**
> (Option A, cycle par enfant — familles recomposées). **Sc.14/15 @back + Sc.16 @ihm**.

### Audit exhaustif des chemins d'écriture (gate G3 3e passage)

| Chemin d'écriture | Statut avant | Action |
|---|---|---|
| Affecter une période (surcharge s06) | scopé (Sc.10/11) | — |
| Déléguer une récupération (compose surcharge) | scopé (Sc.2, `commande.EnfantId`) | — |
| Échange accepté / Proposer / Suite-imprévu (compose délégation) | scopé (Sc.3, hérite l'enfant) | — |
| Définir un transfert SAISI (s29) | scopé (Sc.12/13) | — |
| **Éditer le cycle de fond (`DefinirCycle`)** | **FUITE** (écrivait bucket partagé) | corrigé — `EnfantId` bout-en-bout + sélecteur config (Sc.14/16) |
| **Slots « où » (`PoserSlot`/récurrents) — RENDU grille** | **FUITE** (grille non filtrée) | corrigé — filtrage STRICT slots par enfant (Sc.15) |
| **Reprise / annuler-délégation (`AnnulerDelegation`)** | **FUITE** (supprimait inter-enfants + segments dé-scopés) | corrigé — filtre + réécriture scopée (Sc.15) |
| Signaler un imprévu (`SignalerImprevu`) | correct (journal transverse) mais lecture non scopée | corrigé — résolution lue par enfant (pas de fuite grille) |
| Éditer/Modifier/Supprimer période (par Id) | scopé (préserve l'`EnfantId` via `with`) | — |

| # | Scénario | Type | Statut |
|---|----------|------|--------|
| 1 | Résolution isolée par enfant (2 enfants, chacun son cycle/surcharges) | @back | ✅ |
| 2 | Délégation ciblée enfant A n'écrit rien sur enfant B (2 adaptateurs) | @back | ✅ |
| 3 | Échange accepté ciblé enfant A compose la délégation pour A seul | @back | ✅ |
| 4 | Même jour, 2 enfants = 2 surcharges indépendantes (PAS de LWW entre enfants) | @back | ✅ |
| 5 | Digest « qui récupère ce soir » résolu PAR enfant | @back | ✅ |
| 6 | Suppression / orphelin d'un enfant laisse l'autre intact (Mongo durable) | @back | ✅ |
| 7 | Bascule du sélecteur recharge la grille du bon enfant (Parent-gated) | @ihm | ✅ |
| 8 | Le digest de la cloche suit l'enfant sélectionné (cloche transverse, digest filtré) | @ihm | ✅ |
| 9 | Temps réel 0-GET : délégation enfant A converge sur A, laisse B inchangé | @ihm | ✅ |
| 10 | Affecter une période EN VUE enfant A (write path réel) → visible A seul, jamais B | @back | ✅ |
| 11 | Affecter avec Mia sélectionnée → visible grille Mia, absente grille Tom ; dialog affiche l'enfant courant (lecture seule) | @ihm | ✅ |
| 12 | Transfert SAISI (s29) issu d'une action sur A n'apparaît QUE chez A, jamais chez B (write path réel) | @back | ✅ |
| 13 | Définir un transfert avec enfant sélectionné → bicolore chez A, absent chez B après bascule ; dialog enfant lecture seule | @ihm | ✅ |
| 14 | Éditer le cycle de fond EN VUE enfant A (write path réel) ne change QUE le cycle de A | @back | ✅ |
| 15 | Slot « où » posé pour A visible de A seul + reprise/annuler-délégation de A laisse B intact | @back | ✅ |
| 16 | Config : sélecteur d'enfant sur l'onglet Cycle → éditer en vue A change la grille de A seul ; dialog enfant lecture seule | @ihm | ✅ |

---

## Portes de conception — TRANCHÉES AU CADRAGE (PAS au gate G3)

> Anti-rework s44 : les surfaces se tranchent au cadrage, pas au gate visuel.

**P1 — Surface de lecture : vue MONO-enfant (sélecteur) vs vue MULTI-enfants simultanée.**
**TRANCHÉ : vue MONO-enfant existante.** s53 réemploie le **sélecteur d'enfant s30** (un enfant à
la fois) ; **aucune surface de lecture neuve**. La vue **multi-enfants simultanée** (lanes/colonnes)
est **routée backlog** (surface neuve = décision PO au coût gate). *Justif : anti-rework s44 ; le
goal est de PROUVER l'isolation, pas d'ajouter une surface ; la vue simultanée est un incrément
séparable.*

**P2 — Enfant par défaut du sélecteur (reliquat s30 « seed Léa »).**
**TRANCHÉ : sélection VOLATILE (session/état de navigation), non persistée par contexte.** Aligné
sur l'état non-persisté du calendrier (s15/s49, borne anti-cliquet). La **persistance du choix
d'enfant par contexte utilisateur** est **routée backlog** (Épic 1, reliquat s30).

**P3 — Cloche & digest : filtrés par enfant ou transverses ?**
**TRANCHÉ : la CLOCHE reste GÉNÉRALE/transverse** (notification de changement, tous enfants — le
journal s47 porte déjà `{type, jour, enfant, …}`), **le DIGEST « immédiat » (s50) est FILTRÉ par
l'enfant sélectionné** (lecture du planning, cohérent avec la vue mono-enfant). *Justif : la cloche
signale QU'un changement a eu lieu (transverse) ; le digest LIT le planning d'un enfant.*

## Invariants & gardes à tenir

- **ISOLATION STRICTE (cœur du sprint)** : toute écriture (surcharge s06 / délégation s44 / échange
  s47 accepté / imprévu s48) ciblée enfant A **ne touche jamais** la résolution, les surcharges, les
  transferts dérivés ni les cases de l'enfant B. Prouvé **store réel** sur **deux adaptateurs
  InMemory + Mongo durable** — jamais par doublure (garde anti-✅-qui-ment).
- **Pas de last-write-wins ENTRE enfants** : deux enfants, même jour = deux surcharges qui
  **coexistent** (le LWW R11 ne s'applique qu'à un même (enfant, jour)).
- **GARDE date↔index/parité de cycle** : `index = ISOWeek(date) % N` (`CycleDeFond`). Les scénarios
  **ancrent l'attendu sur la RÈGLE de résolution** (« le responsable de fond résolu pour ce jour »),
  **jamais** sur un index codé en dur. Un cycle par enfant peut différer → l'attendu reste « chacun
  résolu sur SON cycle ».
- **Temps réel 0-GET** : convergence par **reprojection client** depuis la diffusion porteuse de
  payload `INotificateurChangement` s47 ; **0 GET sur push** ; garde [[flake-signalr-blast-radius]]
  respectée (sérialisation `SignalRTempsReelCollection`).
- **GARDE gate visuel (leçon s49)** : **rebuild du build WASM SERVI** (conteneur `web` docker) avant
  sollicitation PO. Le sélecteur = **clic** (pas de geste souris natif) → **bUnit suffit**, pas de
  smoke Playwright attendu ; si un geste natif émerge, smoke `tests/PlanningDeGarde.Web.E2E` (hors slnx).
- **Parent-gated** : Invité ne pilote aucune écriture (sélecteur en lecture only côté Invité, comme
  l'existant).

---

## Scénarios

### @back — Isolation prouvée à la frontière Application, deux adaptateurs

```gherkin
@back @vert
Scénario 1 — Résolution isolée : deux enfants, chacun son cycle et ses surcharges
  Étant donné un foyer peuplé de DEUX enfants "Léa" et "Tom"
  Et un cycle de fond et des surcharges propres à chacun
  Quand je résous la grille pour "Léa" puis pour "Tom" sur la même fenêtre
  Alors chaque case est résolue sur le cycle/les surcharges de SON enfant
  Et le responsable résolu de "Léa" un jour donné n'est pas imposé à "Tom"
  Et aucune case de "Tom" ne reflète une surcharge posée pour "Léa"
  # attendu ancré sur la règle : responsable = surcharge(enfant,jour) > fond(enfant,jour) > neutre
```

```gherkin
@back @vert
Scénario 2 — Délégation ciblée "Léa" n'écrit RIEN sur "Tom"
  Étant donné deux enfants "Léa" et "Tom" et un jour J
  Quand je délègue la récupération de "Léa" le jour J à un autre acteur
  Alors une surcharge est écrite pour (Léa, J) et son transfert bicolore dérivé s31 apparaît
  Et le store des surcharges de "Tom" est STRICTEMENT intact (aucune surcharge, aucun transfert)
  Et la case (Tom, J) reste résolue exactement comme avant l'écriture
  Et l'isolation est prouvée à l'identique sur InMemory ET sur Mongo durable
```

```gherkin
@back @vert
Scénario 3 — Échange accepté ciblé "Léa" compose la délégation pour "Léa" seul
  Étant donné une proposition d'échange pending sur (Léa, J) — store des surcharges intact
  Quand la proposition est acceptée
  Alors AccepterProposition compose la délégation s44 pour (Léa, J) uniquement
  Et la surcharge + transfert dérivé apparaissent pour "Léa"
  Et aucune surcharge ni transfert n'est écrit pour "Tom" (résolution de Tom inchangée)
  Et le tout est prouvé durable sur Mongo
```

```gherkin
@back @vert
Scénario 4 — Même jour, deux enfants : deux surcharges INDÉPENDANTES (pas de LWW entre enfants)
  Étant donné deux enfants "Léa" et "Tom" et un même jour J
  Quand je délègue (Léa, J) vers l'acteur A, puis (Tom, J) vers l'acteur B
  Alors DEUX surcharges coexistent : (Léa, J)→A et (Tom, J)→B
  Et la seconde écriture n'écrase PAS la première (le last-write-wins R11 ne joue que par (enfant, jour))
  Et chaque case résout son propre responsable et son propre transfert dérivé
  Et la coexistence est prouvée sur InMemory ET Mongo durable
```

```gherkin
@back @vert
Scénario 5 — Digest « qui récupère ce soir » résolu PAR enfant
  Étant donné deux enfants "Léa" et "Tom" avec des responsables du jour distincts
  Quand je compose le digest immédiat s50 pour "Léa" puis pour "Tom"
  Alors chaque digest restitue le responsable résolu (surcharge>fond>neutre) de SON enfant
  Et les « transferts à venir » de chaque digest ne portent que sur SON enfant
  Et la query reste PURE (aucun store neuf, aucune mutation), identique InMemory + Mongo durable
```

```gherkin
@back @vert
Scénario 6 — Suppression / orphelin d'un enfant laisse l'AUTRE intact
  Étant donné deux enfants "Léa" et "Tom" avec leurs surcharges
  Quand un enfant devient orphelin (ou est retiré du référentiel)
  Alors la résolution et les surcharges de l'autre enfant restent STRICTEMENT intactes
  Et aucune case de l'autre enfant ne bascule ni ne se replie à tort
  Et la lecture ne crashe pas (repli neutre côté enfant absent, Resolvable s13), Mongo durable
```

### @ihm — Sélecteur d'enfant, menés RED→GREEN runtime (build WASM servi rebâti avant PO)

```gherkin
@ihm @vert
Scénario 7 — Bascule du sélecteur recharge la grille du BON enfant
  Étant donné la grille affichant l'enfant "Léa" (sélection volatile, défaut de session)
  Quand je sélectionne "Tom" dans le sélecteur d'enfant
  Alors la grille recharge et résout les cases de "Tom" (responsables, surcharges, transferts propres)
  Et aucune case ne conserve la résolution de "Léa"
  Et le sélecteur pilote la vue côté Parent ; Invité le voit en lecture, ne modifie rien
```

```gherkin
@ihm @vert
Scénario 8 — Le digest de la cloche SUIT l'enfant sélectionné (cloche transverse, digest filtré)
  Étant donné le panneau cloche ouvert avec l'enfant "Léa" sélectionné
  Quand je bascule le sélecteur sur "Tom"
  Alors la section digest « qui récupère ce soir » se recompose pour "Tom" (responsable + à-venir de Tom)
  Et le flux de notifications lu/non-lu de la cloche reste GÉNÉRAL (transverse, tous enfants)
  Et le digest reste en lecture stricte (aucun bouton), Parent-gated
```

```gherkin
@ihm @vert
Scénario 9 — Temps réel 0-GET : délégation enfant A converge sur A, laisse B inchangé
  Étant donné deux écrans connectés Parent, le 1er affichant "Tom", le 2e délègue (Léa, J)
  Quand la diffusion porteuse de payload arrive au 1er écran
  Alors la reprojection client met à jour l'état de "Léa" SANS aucun GET (0 GET sur push)
  Et la vue affichée (Tom) reste INCHANGÉE tant que "Tom" est sélectionné (isolation temps réel par enfant)
  Et en basculant sur "Léa" la case déléguée apparaît déjà à jour
  # garde flake-signalr-blast-radius respectée (SignalRTempsReelCollection sérialisée)
```

### Correctif gate G3 — scope enfant à l'ÉCRITURE (le trou : Sc.1-6 seedaient des périodes déjà estampillées)

```gherkin
@back @vert
Scénario 10 — Affecter une période EN VUE enfant A (chemin d'écriture RÉEL) est visible de A seul
  Étant donné deux enfants "Léa" et "Tom" et l'enfant "Léa" courant (vue)
  Quand j'affecte une période via AffecterPeriodeHandler (use case s06, pas un seed) EN VUE de "Léa"
  Alors la période persistée porte EnfantId="Léa" (jamais le bucket partagé "")
  Et elle n'apparaît QUE dans la grille de "Léa", jamais chez "Tom" (qui garde son fond)
  Et l'isolation est prouvée sur InMemory ET Mongo durable
```

```gherkin
@ihm @vert
Scénario 11 — Affecter avec un enfant sélectionné : scopé à SA grille, dialog enfant en lecture seule
  Étant donné la grille affichant l'enfant courant (sélection du sélecteur de vue)
  Et la dialog « Affecter une période » ouverte depuis le menu clic-case
  Alors elle affiche l'enfant courant en LECTURE SEULE (« Pour : … (sélection courante) »), sans sélecteur d'enfant
  Quand je choisis un responsable et je valide
  Alors la période s'affiche dans la grille de l'enfant courant
  Et en basculant sur un autre enfant elle est ABSENTE (la case retombe sur le fond)
  # runtime réel : écriture par le canal, résolution isolée par enfant réelle
```

### Correctif gate G3 (2e passage) — scope enfant aux TRANSFERTS SAISIS (« Mia » ne doit plus fuir chez « Charlie »)

```gherkin
@back @vert
Scénario 12 — Un transfert SAISI (s29) issu d'une action sur l'enfant A n'apparaît QUE chez A
  Étant donné deux enfants "Léa" et "Tom" et un transfert SAISI défini EN VUE de "Léa" le jour J
  Quand je projette la grille pour "Léa" puis pour "Tom"
  Alors la case J de "Léa" porte le transfert bicolore (+ motif de légende), celle de "Tom" NON
  Et le transfert persisté porte EnfantId="Léa" (jamais le bucket partagé "")
  Et les transferts DÉRIVÉS restent scopés par construction (issus des périodes déjà filtrées par enfant)
  Et l'isolation est prouvée sur InMemory ET Mongo durable
```

```gherkin
@ihm @vert
Scénario 13 — Définir un transfert avec un enfant sélectionné : bicolore chez A, absent chez B
  Étant donné la grille affichant l'enfant courant et la dialog « Définir un transfert » ouverte
  Alors la dialog affiche l'enfant courant en LECTURE SEULE (« Pour : … (sélection courante) »)
  Quand je saisis et valide le transfert
  Alors la case du jour de l'enfant courant porte la pastille bicolore (convergence par diffusion SignalR)
  Et en basculant sur un autre enfant la pastille est ABSENTE (plus de fuite du transfert)
  # runtime réel navigateur-fidèle : écriture par le canal, projection isolée par enfant réelle
```

### Correctif gate G3 (3e passage) — cycle de fond + slots + reprise scopés par enfant (audit exhaustif)

```gherkin
@back @vert
Scénario 14 — Éditer le cycle de fond EN VUE enfant A ne change QUE le cycle de A
  Étant donné deux enfants "Léa" et "Tom" chacun avec son cycle de fond propre
  Quand j'édite le cycle de "Léa" via DefinirCycleHandler (use case d'écriture réel) EN VUE de "Léa"
  Alors seule la résolution de "Léa" change ; celle de "Tom" reste STRICTEMENT inchangée
  Et l'isolation est prouvée sur InMemory ET Mongo durable
```

```gherkin
@back @vert
Scénario 15 — Slot « où » posé pour A + reprise (annuler délégation) sur A : isolés de B
  Étant donné un slot de localisation posé pour "Léa" et des surcharges de "Léa" et "Tom" le même jour
  Quand je projette la grille et quand j'annule la délégation du jour pour "Léa" seule
  Alors le slot de "Léa" n'apparaît QUE chez "Léa" (jamais chez "Tom")
  Et la reprise retire la surcharge de "Léa" mais laisse celle de "Tom" STRICTEMENT intacte
```

```gherkin
@ihm @vert
Scénario 16 — Config : sélecteur d'enfant sur l'onglet Cycle, édition scopée
  Étant donné l'écran de configuration, onglet Cycle, avec un sélecteur d'enfant
  Quand j'édite le cycle EN VUE de "Léa" (Alice) puis EN VUE de "Tom" (Bruno)
  Alors la modal affiche l'enfant courant en LECTURE SEULE (« Pour : … (sélection courante) »)
  Et la grille câblée à la même API résout le cycle PROPRE de chaque enfant (Léa→Alice, Tom→Bruno)
  # runtime réel : chaque enfant a son cycle de fond propre (familles recomposées)
```

---

# Retours produit (PO)

<!-- Rempli au gate G3. Vide tant que le gate n'a pas eu lieu. -->
